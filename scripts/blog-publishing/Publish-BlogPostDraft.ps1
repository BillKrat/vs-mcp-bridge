[CmdletBinding(DefaultParameterSetName = 'BySlug')]
param(
    [Parameter(Mandatory = $true, ParameterSetName = 'BySlug')]
    [ValidateNotNullOrEmpty()]
    [string]$Slug,

    [Parameter(Mandatory = $true, ParameterSetName = 'ByPath')]
    [ValidateNotNullOrEmpty()]
    [string]$PostPath,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$SqlConnectionString,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$ReloadBaseUrl,

    [string]$PostsRoot,

    [string]$ReloadKey
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($PSScriptRoot)) {
    $script:ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition
}
else {
    $script:ScriptRoot = $PSScriptRoot
}

if ([string]::IsNullOrWhiteSpace($PostsRoot)) {
    $PostsRoot = Join-Path $script:ScriptRoot '..\..\docs\blogs\posts'
}

function Resolve-PostDirectory {
    param(
        [string]$Slug,
        [string]$PostPath,
        [string]$PostsRoot
    )

    if (-not [string]::IsNullOrWhiteSpace($PostPath)) {
        return (Resolve-Path -LiteralPath $PostPath).Path
    }

    $candidate = Join-Path $PostsRoot $Slug
    return (Resolve-Path -LiteralPath $candidate).Path
}

function Assert-RequiredProperty {
    param(
        [object]$Object,
        [string]$PropertyName
    )

    $property = $Object.PSObject.Properties[$PropertyName]
    if ($null -eq $property) {
        throw "Missing required metadata field '$PropertyName' in post.json."
    }

    $value = $property.Value
    if ($null -eq $value) {
        throw "Required metadata field '$PropertyName' cannot be null."
    }

    if ($value -is [string] -and [string]::IsNullOrWhiteSpace($value)) {
        throw "Required metadata field '$PropertyName' cannot be empty."
    }

    return $value
}

function Assert-ArrayProperty {
    param(
        [object]$Object,
        [string]$PropertyName
    )

    $property = $Object.PSObject.Properties[$PropertyName]
    if ($null -eq $property) {
        throw "Required array field '$PropertyName' is missing from post.json."
    }

    if ($null -eq $property.Value) {
        throw "Required array field '$PropertyName' cannot be null."
    }

    if ($property.Value -isnot [System.Collections.IEnumerable] -or $property.Value -is [string]) {
        throw "Required array field '$PropertyName' must be an array."
    }
}

function New-StringListDataTable {
    param(
        [System.Collections.IEnumerable]$Values
    )

    $table = New-Object System.Data.DataTable
    [void]$table.Columns.Add('Value', [string])

    foreach ($value in $Values) {
        $row = $table.NewRow()
        $row['Value'] = if ($null -eq $value) { [DBNull]::Value } else { [string]$value }
        [void]$table.Rows.Add($row)
    }

    return $table
}

function New-SqlParameter {
    param(
        [string]$Name,
        [System.Data.SqlDbType]$Type,
        [object]$Value,
        [int]$Size = 0
    )

    $parameter = New-Object System.Data.SqlClient.SqlParameter
    $parameter.ParameterName = $Name
    $parameter.SqlDbType = $Type
    if ($Size -gt 0) {
        $parameter.Size = $Size
    }
    $parameter.Value = if ($null -eq $Value) { [DBNull]::Value } else { $Value }
    return $parameter
}

function Resolve-BlogEnginePostId {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [Guid]$BlogId,
        [string]$Slug
    )

    $lookupCommand = $Connection.CreateCommand()
    $lookupCommand.CommandType = [System.Data.CommandType]::Text
    $lookupCommand.CommandText = @'
SELECT TOP (1) PostID
FROM dbo.be_Posts
WHERE BlogID = @BlogID
  AND Slug = @Slug
  AND IsDeleted = 0;
'@
    [void]$lookupCommand.Parameters.Add((New-SqlParameter -Name '@BlogID' -Type UniqueIdentifier -Value $BlogId))
    [void]$lookupCommand.Parameters.Add((New-SqlParameter -Name '@Slug' -Type NVarChar -Value $Slug -Size 255))

    $existingPostId = $lookupCommand.ExecuteScalar()
    if ($null -eq $existingPostId -or $existingPostId -eq [DBNull]::Value) {
        return [Guid]::NewGuid()
    }

    return [Guid]$existingPostId
}

$resolvedPostsRoot = (Resolve-Path -LiteralPath $PostsRoot).Path
$postDirectory = Resolve-PostDirectory -Slug $Slug -PostPath $PostPath -PostsRoot $resolvedPostsRoot
$postJsonPath = Join-Path $postDirectory 'post.json'
$contentPath = Join-Path $postDirectory 'content.html'

if (-not (Test-Path -LiteralPath $postJsonPath -PathType Leaf)) {
    throw "post.json was not found at '$postJsonPath'."
}

if (-not (Test-Path -LiteralPath $contentPath -PathType Leaf)) {
    throw "content.html was not found at '$contentPath'."
}

$post = Get-Content -LiteralPath $postJsonPath -Raw -Encoding UTF8 | ConvertFrom-Json
$content = Get-Content -LiteralPath $contentPath -Raw -Encoding UTF8

if ([string]::IsNullOrWhiteSpace($content)) {
    throw "content.html cannot be empty."
}

$blogId = [Guid](Assert-RequiredProperty -Object $post -PropertyName 'blogId')
$repoPostId = Assert-RequiredProperty -Object $post -PropertyName 'postId'
$title = [string](Assert-RequiredProperty -Object $post -PropertyName 'title')
$description = [string](Assert-RequiredProperty -Object $post -PropertyName 'description')
$author = [string](Assert-RequiredProperty -Object $post -PropertyName 'author')
$slugValue = [string](Assert-RequiredProperty -Object $post -PropertyName 'slug')
$allowComments = [bool](Assert-RequiredProperty -Object $post -PropertyName 'allowComments')

Assert-ArrayProperty -Object $post -PropertyName 'categories'
Assert-ArrayProperty -Object $post -PropertyName 'tags'

$categories = @($post.categories)
$tags = @($post.tags)

$categoryTable = New-StringListDataTable -Values $categories
$tagTable = New-StringListDataTable -Values $tags

$connection = New-Object System.Data.SqlClient.SqlConnection $SqlConnectionString

try {
    $connection.Open()
    $postId = Resolve-BlogEnginePostId -Connection $connection -BlogId $blogId -Slug $slugValue

    $command = $connection.CreateCommand()
    $command.CommandType = [System.Data.CommandType]::StoredProcedure
    $command.CommandText = 'dbo.UpsertBlogPostFromRepo'
    $command.CommandTimeout = 120

    [void]$command.Parameters.Add((New-SqlParameter -Name '@BlogID' -Type UniqueIdentifier -Value $blogId))
    [void]$command.Parameters.Add((New-SqlParameter -Name '@PostID' -Type UniqueIdentifier -Value $postId))
    [void]$command.Parameters.Add((New-SqlParameter -Name '@Title' -Type NVarChar -Value $title -Size 255))
    [void]$command.Parameters.Add((New-SqlParameter -Name '@Description' -Type NVarChar -Value $description))
    [void]$command.Parameters.Add((New-SqlParameter -Name '@PostContent' -Type NVarChar -Value $content))
    [void]$command.Parameters.Add((New-SqlParameter -Name '@Author' -Type NVarChar -Value $author -Size 50))
    [void]$command.Parameters.Add((New-SqlParameter -Name '@Slug' -Type NVarChar -Value $slugValue -Size 255))
    [void]$command.Parameters.Add((New-SqlParameter -Name '@IsPublished' -Type Bit -Value $false))
    [void]$command.Parameters.Add((New-SqlParameter -Name '@IsCommentEnabled' -Type Bit -Value $allowComments))

    $categoriesParameter = New-Object System.Data.SqlClient.SqlParameter
    $categoriesParameter.ParameterName = '@Categories'
    $categoriesParameter.SqlDbType = [System.Data.SqlDbType]::Structured
    $categoriesParameter.TypeName = 'dbo.StringList'
    $categoriesParameter.Value = $categoryTable
    [void]$command.Parameters.Add($categoriesParameter)

    $tagsParameter = New-Object System.Data.SqlClient.SqlParameter
    $tagsParameter.ParameterName = '@Tags'
    $tagsParameter.SqlDbType = [System.Data.SqlDbType]::Structured
    $tagsParameter.TypeName = 'dbo.StringList'
    $tagsParameter.Value = $tagTable
    [void]$command.Parameters.Add($tagsParameter)

    [void]$command.ExecuteNonQuery()

    $reloadUri = ([System.Uri]::new(($ReloadBaseUrl.TrimEnd('/') + "/api/posts/reload/$blogId"))).AbsoluteUri
    $reloadHeaders = @{}
    if (-not [string]::IsNullOrWhiteSpace($ReloadKey)) {
        $reloadHeaders['X-Blog-Reload-Key'] = $ReloadKey
    }

    if ($reloadHeaders.Count -gt 0) {
        [void](Invoke-RestMethod -Method Post -Uri $reloadUri -Headers $reloadHeaders)
    }
    else {
        [void](Invoke-RestMethod -Method Post -Uri $reloadUri)
    }

    Write-Host "Draft publish succeeded for '$slugValue'." -ForegroundColor Green
    Write-Host "Post folder: $postDirectory"
    Write-Host "BlogID: $blogId"
    Write-Host "Repo postId: $repoPostId"
    Write-Host "BlogEngine PostID: $postId"
    Write-Host "Reload endpoint: $reloadUri"
    if ($reloadHeaders.Count -gt 0) {
        Write-Host "Reload auth: X-Blog-Reload-Key header sent"
    }
    Write-Host "Publish mode: draft-only (IsPublished forced to false for this script)"
}
finally {
    if ($null -ne $connection) {
        $connection.Dispose()
    }
}
