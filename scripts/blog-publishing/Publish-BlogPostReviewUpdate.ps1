[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$Slug,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$SqlConnectionString,

    [string]$PostsRoot,

    [string]$PrepublishReportPath,

    [string]$OutputRoot,

    [string]$ReportPath,

    [string]$RenderedUrl
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

if ([string]::IsNullOrWhiteSpace($PrepublishReportPath)) {
    $safeSlugForReport = [regex]::Replace($Slug.ToLowerInvariant(), '[^a-z0-9._-]+', '-').Trim('-')
    $PrepublishReportPath = Join-Path $script:ScriptRoot "..\..\docs\blogs\prepublish-compare-$safeSlugForReport-20260516.md"
}

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $script:ScriptRoot '..\..\docs\blogs\source-of-truth\publish-review-updates\20260516'
}

if ([string]::IsNullOrWhiteSpace($ReportPath)) {
    $safeSlugForPublishReport = [regex]::Replace($Slug.ToLowerInvariant(), '[^a-z0-9._-]+', '-').Trim('-')
    $ReportPath = Join-Path $script:ScriptRoot "..\..\docs\blogs\publish-review-update-$safeSlugForPublishReport-20260516.md"
}

function Resolve-FullPath {
    param([string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $Path))
}

function ConvertTo-IsoString {
    param([object]$Value)

    if ($null -eq $Value -or $Value -eq [DBNull]::Value) {
        return $null
    }

    return ([datetime]$Value).ToString('yyyy-MM-ddTHH:mm:ss.fff', [System.Globalization.CultureInfo]::InvariantCulture)
}

function ConvertTo-StringArray {
    param([object]$Value)

    if ($null -eq $Value -or $Value -eq [DBNull]::Value) {
        return @()
    }

    $text = [string]$Value
    if ([string]::IsNullOrWhiteSpace($text)) {
        return @()
    }

    return ,@($text -split '\|' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
}

function ConvertTo-MarkdownValue {
    param([object]$Value)

    if ($null -eq $Value) {
        return 'None'
    }

    $text = [string]$Value
    if ([string]::IsNullOrWhiteSpace($text)) {
        return 'None'
    }

    return $text.Replace('|', '\|').Replace("`r", '').Replace("`n", '<br>')
}

function ConvertTo-InlineList {
    param([object[]]$Values)

    if ($null -eq $Values -or @($Values).Count -eq 0) {
        return 'None'
    }

    return (@($Values) | ForEach-Object { ConvertTo-MarkdownValue $_ }) -join ', '
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
    if ($Size -ne 0) {
        $parameter.Size = $Size
    }

    $parameter.Value = if ($null -eq $Value) { [DBNull]::Value } else { $Value }
    return $parameter
}

function Get-Sha256Hex {
    param([string]$Value)

    if ($null -eq $Value) {
        $Value = ''
    }

    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($Value)
        $hashBytes = $sha256.ComputeHash($bytes)
        return [System.BitConverter]::ToString($hashBytes).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $sha256.Dispose()
    }
}

function Get-RegexValues {
    param(
        [string]$Content,
        [string]$Pattern
    )

    if ([string]::IsNullOrEmpty($Content)) {
        return @()
    }

    return @(
        [regex]::Matches($Content, $Pattern) |
            ForEach-Object { $_.Value } |
            Sort-Object -Unique
    )
}

function Get-ExpectedPrepublishHash {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Prepublish report was not found at '$Path'."
    }

    $reportText = Get-Content -LiteralPath $Path -Raw -Encoding UTF8
    $match = [regex]::Match($reportText, '\|\s*Current live DB content\s*\|\s*([a-f0-9]{64})\s*\|', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if (-not $match.Success) {
        throw "Could not find the current live DB content hash in '$Path'."
    }

    return $match.Groups[1].Value.ToLowerInvariant()
}

function Get-CurrentPost {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [string]$Slug
    )

    $command = $Connection.CreateCommand()
    $command.CommandType = [System.Data.CommandType]::Text
    $command.CommandTimeout = 120
    $command.CommandText = @'
SELECT TOP (1)
    p.PostRowID,
    p.BlogID,
    p.PostID,
    p.Title,
    p.Description,
    p.PostContent,
    p.DateCreated,
    p.DateModified,
    p.Author,
    p.IsPublished,
    p.IsCommentEnabled,
    p.Raters,
    p.Rating,
    p.Slug,
    p.IsDeleted,
    Categories = STUFF((
        SELECT N'|' + c.CategoryName
        FROM dbo.be_PostCategory pc
        INNER JOIN dbo.be_Categories c
            ON c.BlogID = pc.BlogID
           AND c.CategoryID = pc.CategoryID
        WHERE pc.BlogID = p.BlogID
          AND pc.PostID = p.PostID
        ORDER BY c.CategoryName
        FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N''),
    Tags = STUFF((
        SELECT N'|' + pt.Tag
        FROM dbo.be_PostTag pt
        WHERE pt.BlogID = p.BlogID
          AND pt.PostID = p.PostID
        ORDER BY pt.Tag
        FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N'')
FROM dbo.be_Posts p
WHERE p.Slug = @Slug
  AND p.IsDeleted = 0
ORDER BY p.DateModified DESC, p.PostRowID DESC;
'@
    [void]$command.Parameters.Add((New-SqlParameter -Name '@Slug' -Type NVarChar -Value $Slug -Size 255))

    $reader = $command.ExecuteReader()
    try {
        if (-not $reader.Read()) {
            throw "No active DB post found for slug '$Slug'."
        }

        $isPublished = [bool]$reader['IsPublished']
        $isDeleted = [bool]$reader['IsDeleted']

        return [pscustomobject]@{
            postRowId = [int]$reader['PostRowID']
            blogId = [string]$reader['BlogID']
            postId = [string]$reader['PostID']
            title = if ($reader['Title'] -eq [DBNull]::Value) { '' } else { [string]$reader['Title'] }
            description = if ($reader['Description'] -eq [DBNull]::Value) { '' } else { [string]$reader['Description'] }
            content = if ($reader['PostContent'] -eq [DBNull]::Value) { '' } else { [string]$reader['PostContent'] }
            dateCreated = ConvertTo-IsoString $reader['DateCreated']
            dateModified = ConvertTo-IsoString $reader['DateModified']
            author = if ($reader['Author'] -eq [DBNull]::Value) { '' } else { [string]$reader['Author'] }
            isPublished = $isPublished
            allowComments = [bool]$reader['IsCommentEnabled']
            raters = [int]$reader['Raters']
            rating = [double]$reader['Rating']
            slug = if ($reader['Slug'] -eq [DBNull]::Value) { '' } else { [string]$reader['Slug'] }
            isDeleted = $isDeleted
            status = if ($isDeleted) { 'deleted' } elseif ($isPublished) { 'published' } else { 'draft' }
            categories = @(ConvertTo-StringArray $reader['Categories'])
            tags = @(ConvertTo-StringArray $reader['Tags'])
        }
    }
    finally {
        $reader.Close()
    }
}

function Export-PostRow {
    param(
        [object]$Post,
        [string]$Directory,
        [string]$Phase,
        [string]$ContentHash
    )

    New-Item -ItemType Directory -Path $Directory -Force | Out-Null

    $metadata = [ordered]@{
        source = [ordered]@{
            databaseTable = 'dbo.be_Posts'
            postRowId = $Post.postRowId
            blogId = $Post.blogId
            postId = $Post.postId
        }
        title = $Post.title
        description = $Post.description
        author = $Post.author
        slug = $Post.slug
        status = $Post.status
        isPublished = $Post.isPublished
        isDeleted = $Post.isDeleted
        allowComments = $Post.allowComments
        raters = $Post.raters
        rating = $Post.rating
        dateCreated = $Post.dateCreated
        dateModified = $Post.dateModified
        categories = @($Post.categories)
        tags = @($Post.tags)
        contentHashSha256 = $ContentHash
        export = [ordered]@{
            phase = $Phase
            folder = (Split-Path -Leaf $Directory)
            metadataFile = 'post.database.json'
            contentFile = 'content.html'
            exportedAt = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ', [System.Globalization.CultureInfo]::InvariantCulture)
        }
    }

    $utf8NoBom = [System.Text.UTF8Encoding]::new($false)
    [System.IO.File]::WriteAllText((Join-Path $Directory 'post.database.json'), ($metadata | ConvertTo-Json -Depth 8), $utf8NoBom)
    [System.IO.File]::WriteAllText((Join-Path $Directory 'content.html'), $Post.content, $utf8NoBom)
}

function Invoke-RenderedRouteCheck {
    param(
        [string]$Url,
        [string]$CanonicalContent
    )

    if ([string]::IsNullOrWhiteSpace($Url)) {
        return [pscustomobject]@{
            attempted = $false
            url = $null
            statusCode = $null
            canonicalMarkersVisible = $false
            result = 'Not checked; no rendered URL was provided.'
        }
    }

    try {
        $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 30
        $content = [string]$response.Content
        $markers = @(
            'Source of Truth:',
            'BridgeToolExecutor',
            'anti-black-box rule'
        )
        $visibleCount = @($markers | Where-Object { $content.Contains($_) }).Count
        $canonicalMarkersVisible = ($visibleCount -eq $markers.Count)
        $result = if ($canonicalMarkersVisible) {
            'Rendered route appears to show the canonical update.'
        }
        else {
            'Rendered route responded, but canonical-only markers were not all visible; content may still be cached or rendered differently.'
        }

        return [pscustomobject]@{
            attempted = $true
            url = $Url
            statusCode = [int]$response.StatusCode
            canonicalMarkersVisible = $canonicalMarkersVisible
            result = $result
        }
    }
    catch {
        return [pscustomobject]@{
            attempted = $true
            url = $Url
            statusCode = $null
            canonicalMarkersVisible = $false
            result = "Rendered route check failed: $($_.Exception.Message)"
        }
    }
}

$resolvedPostsRoot = Resolve-FullPath $PostsRoot
$resolvedPrepublishReportPath = Resolve-FullPath $PrepublishReportPath
$resolvedOutputRoot = Resolve-FullPath $OutputRoot
$resolvedReportPath = Resolve-FullPath $ReportPath

$postDirectory = Join-Path $resolvedPostsRoot $Slug
$postJsonPath = Join-Path $postDirectory 'post.json'
$canonicalContentPath = Join-Path $postDirectory 'content.html'

if (-not (Test-Path -LiteralPath $postJsonPath -PathType Leaf)) {
    throw "Canonical post.json was not found at '$postJsonPath'."
}

if (-not (Test-Path -LiteralPath $canonicalContentPath -PathType Leaf)) {
    throw "Canonical content.html was not found at '$canonicalContentPath'."
}

$canonicalPost = Get-Content -LiteralPath $postJsonPath -Raw -Encoding UTF8 | ConvertFrom-Json
$canonicalContent = Get-Content -LiteralPath $canonicalContentPath -Raw -Encoding UTF8

if ([string]::IsNullOrWhiteSpace($canonicalContent)) {
    throw "Canonical content.html cannot be empty."
}

if (-not [string]::Equals([string]$canonicalPost.slug, $Slug, [System.StringComparison]::Ordinal)) {
    throw "Canonical post slug '$($canonicalPost.slug)' does not match requested slug '$Slug'."
}

$expectedPrepublishHash = Get-ExpectedPrepublishHash -Path $resolvedPrepublishReportPath
$canonicalContentHash = Get-Sha256Hex $canonicalContent
$tokenPattern = '\[(?:Page|Display):[^\]]+\]|\[[A-Za-z][A-Za-z0-9 ]+(?:\|[A-Za-z0-9 ]+)?\]'
$stalePattern = 'https://github\.com/BillKrat/vs-mcp-bridge/blob/feature/approval-apply-ui-slice/[^"''<>\s]+|https?://(?:www\.)?adventuresontheedge\.net[^"''<>\s]*|https?://AdventuresOnTheEdge\.net[^"''<>\s]*|post\.aspx\?id=[A-Za-z0-9-]+'
$canonicalTokens = @(Get-RegexValues -Content $canonicalContent -Pattern $tokenPattern)
$canonicalStaleLinks = @(Get-RegexValues -Content $canonicalContent -Pattern $stalePattern)

if (@($canonicalStaleLinks).Count -gt 0) {
    throw "Canonical content contains stale direct links; aborting before DB write."
}

$timestamp = (Get-Date).ToString('yyyyMMdd-HHmmss', [System.Globalization.CultureInfo]::InvariantCulture)
$runRoot = Join-Path $resolvedOutputRoot "$Slug-$timestamp"
$beforeExportDirectory = Join-Path $runRoot 'before'
$afterExportDirectory = Join-Path $runRoot 'after'

$connection = New-Object System.Data.SqlClient.SqlConnection $SqlConnectionString
$beforePost = $null
$afterPost = $null
$rowsAffected = 0

try {
    $connection.Open()
    $beforePost = Get-CurrentPost -Connection $connection -Slug $Slug
    $beforeContentHash = Get-Sha256Hex $beforePost.content

    if (-not [string]::Equals($beforeContentHash, $expectedPrepublishHash, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Current DB content hash '$beforeContentHash' no longer matches prepublish report hash '$expectedPrepublishHash'. Aborting before DB write."
    }

    if (-not [string]::Equals($beforePost.blogId, [string]$canonicalPost.blogId, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Current DB BlogID '$($beforePost.blogId)' does not match canonical BlogID '$($canonicalPost.blogId)'."
    }

    if (-not [string]::Equals($beforePost.postId, [string]$canonicalPost.postId, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Current DB PostID '$($beforePost.postId)' does not match canonical PostID '$($canonicalPost.postId)'."
    }

    if (-not [string]::Equals($beforePost.slug, [string]$canonicalPost.slug, [System.StringComparison]::Ordinal)) {
        throw "Current DB slug '$($beforePost.slug)' does not match canonical slug '$($canonicalPost.slug)'."
    }

    if (-not [string]::Equals($beforePost.title, [string]$canonicalPost.title, [System.StringComparison]::Ordinal)) {
        throw "Current DB title '$($beforePost.title)' does not match canonical title '$($canonicalPost.title)'. This script preserves title."
    }

    Export-PostRow -Post $beforePost -Directory $beforeExportDirectory -Phase 'before-review-update' -ContentHash $beforeContentHash

    $transaction = $connection.BeginTransaction()
    try {
        $updateCommand = $connection.CreateCommand()
        $updateCommand.Transaction = $transaction
        $updateCommand.CommandType = [System.Data.CommandType]::Text
        $updateCommand.CommandTimeout = 120
        $updateCommand.CommandText = @'
UPDATE dbo.be_Posts
SET
    Description = @Description,
    PostContent = @PostContent,
    DateModified = GETDATE()
WHERE PostRowID = @PostRowID
  AND BlogID = @BlogID
  AND PostID = @PostID
  AND Slug = @Slug
  AND IsDeleted = 0;
'@
        [void]$updateCommand.Parameters.Add((New-SqlParameter -Name '@Description' -Type NVarChar -Value ([string]$canonicalPost.description) -Size -1))
        [void]$updateCommand.Parameters.Add((New-SqlParameter -Name '@PostContent' -Type NVarChar -Value $canonicalContent -Size -1))
        [void]$updateCommand.Parameters.Add((New-SqlParameter -Name '@PostRowID' -Type Int -Value $beforePost.postRowId))
        [void]$updateCommand.Parameters.Add((New-SqlParameter -Name '@BlogID' -Type UniqueIdentifier -Value ([Guid]$beforePost.blogId)))
        [void]$updateCommand.Parameters.Add((New-SqlParameter -Name '@PostID' -Type UniqueIdentifier -Value ([Guid]$beforePost.postId)))
        [void]$updateCommand.Parameters.Add((New-SqlParameter -Name '@Slug' -Type NVarChar -Value $beforePost.slug -Size 255))

        $rowsAffected = $updateCommand.ExecuteNonQuery()
        if ($rowsAffected -ne 1) {
            throw "Expected to update exactly one row, but updated $rowsAffected."
        }

        $transaction.Commit()
    }
    catch {
        $transaction.Rollback()
        throw
    }

    $afterPost = Get-CurrentPost -Connection $connection -Slug $Slug
}
finally {
    if ($null -ne $connection) {
        $connection.Dispose()
    }
}

$afterContentHash = Get-Sha256Hex $afterPost.content
Export-PostRow -Post $afterPost -Directory $afterExportDirectory -Phase 'after-review-update' -ContentHash $afterContentHash

$identityPreserved = (
    $beforePost.postRowId -eq $afterPost.postRowId -and
    [string]::Equals($beforePost.blogId, $afterPost.blogId, [System.StringComparison]::OrdinalIgnoreCase) -and
    [string]::Equals($beforePost.postId, $afterPost.postId, [System.StringComparison]::OrdinalIgnoreCase) -and
    [string]::Equals($beforePost.slug, $afterPost.slug, [System.StringComparison]::Ordinal) -and
    [string]::Equals($beforePost.title, $afterPost.title, [System.StringComparison]::Ordinal) -and
    [string]::Equals($beforePost.dateCreated, $afterPost.dateCreated, [System.StringComparison]::Ordinal)
)
$publicationStatePreserved = ($beforePost.isPublished -eq $afterPost.isPublished -and $beforePost.status -eq $afterPost.status)
$taxonomyPreserved = ((@($beforePost.categories) -join '|') -eq (@($afterPost.categories) -join '|') -and (@($beforePost.tags) -join '|') -eq (@($afterPost.tags) -join '|'))
$bodyMatchesCanonical = [string]::Equals($afterPost.content, $canonicalContent, [System.StringComparison]::Ordinal)
$descriptionMatchesCanonical = [string]::Equals($afterPost.description, [string]$canonicalPost.description, [System.StringComparison]::Ordinal)
$afterTokens = @(Get-RegexValues -Content $afterPost.content -Pattern $tokenPattern)
$afterStaleLinks = @(Get-RegexValues -Content $afterPost.content -Pattern $stalePattern)
$tokensPreserved = ((@($canonicalTokens) -join '|') -eq (@($afterTokens) -join '|'))
$staleLinkCheckPassed = (@($afterStaleLinks).Count -eq 0)
$routeCheck = Invoke-RenderedRouteCheck -Url $RenderedUrl -CanonicalContent $canonicalContent

if (-not $identityPreserved) {
    throw 'Post identity validation failed after update.'
}

if (-not $publicationStatePreserved) {
    throw 'Publication state validation failed after update.'
}

if (-not $taxonomyPreserved) {
    throw 'Taxonomy preservation validation failed after update.'
}

if (-not $bodyMatchesCanonical) {
    throw 'Updated DB body does not match canonical content.'
}

if (-not $descriptionMatchesCanonical) {
    throw 'Updated DB description does not match canonical description.'
}

if (-not $tokensPreserved) {
    throw 'BlogEngine token preservation validation failed after update.'
}

if (-not $staleLinkCheckPassed) {
    throw 'Updated DB body contains stale direct links.'
}

$reportDirectory = Split-Path -Parent $resolvedReportPath
New-Item -ItemType Directory -Path $reportDirectory -Force | Out-Null

$reportLines = New-Object 'System.Collections.Generic.List[string]'
$reportLines.Add("# Publish Review Update - $Slug - 2026-05-16")
$reportLines.Add('')
$reportLines.Add('## Scope')
$reportLines.Add('')
$reportLines.Add('This report records the first single-post BlogEngine body update from canonical repo source.')
$reportLines.Add('The update was intentionally narrow: one existing `dbo.be_Posts` row was updated, no category/tag tables were touched, and no reload endpoint was called.')
$reportLines.Add('')
$reportLines.Add('## Result Summary')
$reportLines.Add('')
$reportLines.Add('| Check | Result |')
$reportLines.Add('| --- | --- |')
$reportLines.Add("| Slug | $(ConvertTo-MarkdownValue $Slug) |")
$reportLines.Add("| DB PostRowID | $($afterPost.postRowId) |")
$reportLines.Add("| DB BlogID | $(ConvertTo-MarkdownValue $($afterPost.blogId)) |")
$reportLines.Add("| DB PostID | $(ConvertTo-MarkdownValue $($afterPost.postId)) |")
$reportLines.Add("| Rows updated | $rowsAffected |")
$reportLines.Add("| Fields intentionally changed | Description, PostContent, DateModified |")
$reportLines.Add("| Identity fields preserved | $identityPreserved |")
$reportLines.Add("| Publication state preserved | $publicationStatePreserved |")
$reportLines.Add("| Taxonomy preserved | $taxonomyPreserved |")
$reportLines.Add("| Updated body matches canonical | $bodyMatchesCanonical |")
$reportLines.Add("| Updated description matches canonical | $descriptionMatchesCanonical |")
$reportLines.Add("| BlogEngine tokens preserved | $tokensPreserved |")
$reportLines.Add("| Stale direct-link check passed | $staleLinkCheckPassed |")
$reportLines.Add("| Before export | $(ConvertTo-MarkdownValue $beforeExportDirectory) |")
$reportLines.Add("| After export | $(ConvertTo-MarkdownValue $afterExportDirectory) |")
$reportLines.Add("| Rendered route check | $(ConvertTo-MarkdownValue $($routeCheck.result)) |")
$reportLines.Add('')
$reportLines.Add('## Before And After')
$reportLines.Add('')
$reportLines.Add('| Field | Before | After |')
$reportLines.Add('| --- | --- | --- |')
$reportLines.Add("| Title | $(ConvertTo-MarkdownValue $($beforePost.title)) | $(ConvertTo-MarkdownValue $($afterPost.title)) |")
$reportLines.Add("| Description | $(ConvertTo-MarkdownValue $($beforePost.description)) | $(ConvertTo-MarkdownValue $($afterPost.description)) |")
$reportLines.Add("| Status | $(ConvertTo-MarkdownValue $($beforePost.status)) | $(ConvertTo-MarkdownValue $($afterPost.status)) |")
$reportLines.Add("| IsPublished | $($beforePost.isPublished) | $($afterPost.isPublished) |")
$reportLines.Add("| DateCreated | $(ConvertTo-MarkdownValue $($beforePost.dateCreated)) | $(ConvertTo-MarkdownValue $($afterPost.dateCreated)) |")
$reportLines.Add("| DateModified | $(ConvertTo-MarkdownValue $($beforePost.dateModified)) | $(ConvertTo-MarkdownValue $($afterPost.dateModified)) |")
$reportLines.Add("| Content SHA-256 | $(ConvertTo-MarkdownValue $beforeContentHash) | $(ConvertTo-MarkdownValue $afterContentHash) |")
$reportLines.Add("| Categories | $(ConvertTo-InlineList $($beforePost.categories)) | $(ConvertTo-InlineList $($afterPost.categories)) |")
$reportLines.Add("| Tags | $(ConvertTo-InlineList $($beforePost.tags)) | $(ConvertTo-InlineList $($afterPost.tags)) |")
$reportLines.Add('')
$reportLines.Add('## Taxonomy Decision')
$reportLines.Add('')
$reportLines.Add('Taxonomy was preserved from the live DB row for this first body publish. Canonical `post.json` categories and tags were not applied in this slice because the goal was the smallest safe mutation to prove the single-post body update path.')
$reportLines.Add('')
$reportLines.Add('## Tokens And Links')
$reportLines.Add('')
$reportLines.Add('| Check | Value |')
$reportLines.Add('| --- | --- |')
$reportLines.Add("| Canonical BlogEngine tokens | $(ConvertTo-InlineList $canonicalTokens) |")
$reportLines.Add("| After-update BlogEngine tokens | $(ConvertTo-InlineList $afterTokens) |")
$reportLines.Add("| After-update stale direct links | $(ConvertTo-InlineList $afterStaleLinks) |")
$reportLines.Add('')
$reportLines.Add('## Rendered Route')
$reportLines.Add('')
$reportLines.Add('| Field | Value |')
$reportLines.Add('| --- | --- |')
$reportLines.Add("| URL | $(ConvertTo-MarkdownValue $($routeCheck.url)) |")
$reportLines.Add("| Attempted | $($routeCheck.attempted) |")
$reportLines.Add("| HTTP status | $(ConvertTo-MarkdownValue $($routeCheck.statusCode)) |")
$reportLines.Add("| Canonical markers visible | $($routeCheck.canonicalMarkersVisible) |")
$reportLines.Add("| Result | $(ConvertTo-MarkdownValue $($routeCheck.result)) |")
$reportLines.Add('')
$reportLines.Add('## Recommended Next Slice')
$reportLines.Add('')
$reportLines.Add('Review the rendered Part 1 page and cache behavior, then run the same single-post review-update workflow for Part 2 if the runtime result is acceptable.')

$utf8NoBom = [System.Text.UTF8Encoding]::new($false)
[System.IO.File]::WriteAllText($resolvedReportPath, ($reportLines -join "`r`n"), $utf8NoBom)

Write-Host "Publish review update completed for '$Slug'."
Write-Host "Rows updated: $rowsAffected"
Write-Host "Before export: $beforeExportDirectory"
Write-Host "After export: $afterExportDirectory"
Write-Host "Report: $resolvedReportPath"
Write-Host "Rendered route: $($routeCheck.result)"
