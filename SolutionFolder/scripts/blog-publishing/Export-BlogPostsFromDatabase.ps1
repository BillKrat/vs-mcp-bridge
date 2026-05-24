[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$SqlConnectionString,

    [string]$OutputRoot,

    [switch]$ActiveOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($PSScriptRoot)) {
    $script:ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition
}
else {
    $script:ScriptRoot = $PSScriptRoot
}

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $script:ScriptRoot '..\..\docs\blogs\source-of-truth\db-export-20260516'
}

function ConvertTo-SafePathSegment {
    param(
        [string]$Value,
        [string]$Fallback
    )

    $candidate = if ([string]::IsNullOrWhiteSpace($Value)) { $Fallback } else { $Value.Trim().ToLowerInvariant() }
    $candidate = [regex]::Replace($candidate, '[^a-z0-9._-]+', '-')
    $candidate = $candidate.Trim('-')

    if ([string]::IsNullOrWhiteSpace($candidate)) {
        return $Fallback
    }

    return $candidate
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

function Get-PostLinks {
    param([string]$Content)

    if ([string]::IsNullOrEmpty($Content)) {
        return @()
    }

    $links = New-Object 'System.Collections.Generic.List[object]'
    $seen = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    $pattern = '(?i)\b(?:href|src)\s*=\s*["''](?<url>[^"'']+)["'']'
    $sha256 = [System.Security.Cryptography.SHA256]::Create()

    foreach ($match in [regex]::Matches($Content, $pattern)) {
        $url = $match.Groups['url'].Value
        if ([string]::IsNullOrWhiteSpace($url)) {
            continue
        }

        if (-not $seen.Add($url)) {
            continue
        }

        if ($url.StartsWith('data:', [StringComparison]::OrdinalIgnoreCase)) {
            $bytes = [System.Text.Encoding]::UTF8.GetBytes($url)
            $hashBytes = $sha256.ComputeHash($bytes)
            $hash = [System.BitConverter]::ToString($hashBytes).Replace('-', '').ToLowerInvariant()
            $links.Add([pscustomobject]@{
                url = 'data:[embedded-data-omitted-from-manifest]'
                kind = 'embedded-data'
                length = $url.Length
                sha256 = $hash
            })
            continue
        }

        $kind = 'external'
        if ($url.StartsWith('#') -or $url.StartsWith('/') -or $url.StartsWith('http://adventuresontheedge.net', [StringComparison]::OrdinalIgnoreCase) -or $url.StartsWith('https://adventuresontheedge.net', [StringComparison]::OrdinalIgnoreCase)) {
            $kind = 'internal'
        }

        $links.Add([pscustomobject]@{
            url = $url
            kind = $kind
        })
    }

    return ,@($links.ToArray())
}

function New-SqlParameter {
    param(
        [string]$Name,
        [System.Data.SqlDbType]$Type,
        [object]$Value
    )

    $parameter = New-Object System.Data.SqlClient.SqlParameter
    $parameter.ParameterName = $Name
    $parameter.SqlDbType = $Type
    $parameter.Value = if ($null -eq $Value) { [DBNull]::Value } else { $Value }
    return $parameter
}

if ([System.IO.Path]::IsPathRooted($OutputRoot)) {
    $resolvedOutputRoot = [System.IO.Path]::GetFullPath($OutputRoot)
}
else {
    $resolvedOutputRoot = [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $OutputRoot))
}
New-Item -ItemType Directory -Path $resolvedOutputRoot -Force | Out-Null

$connection = New-Object System.Data.SqlClient.SqlConnection $SqlConnectionString
$utf8NoBom = [System.Text.UTF8Encoding]::new($false)
$manifestPosts = New-Object 'System.Collections.Generic.List[object]'
$slugCounts = @{}
$ambiguousRecords = New-Object 'System.Collections.Generic.List[object]'

try {
    $connection.Open()

    $command = $connection.CreateCommand()
    $command.CommandType = [System.Data.CommandType]::Text
    $command.CommandTimeout = 120
    $command.CommandText = @'
SELECT
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
WHERE (@ActiveOnly = 0 OR p.IsDeleted = 0)
ORDER BY p.DateCreated, p.PostRowID;
'@
    [void]$command.Parameters.Add((New-SqlParameter -Name '@ActiveOnly' -Type Bit -Value ([bool]$ActiveOnly)))

    $reader = $command.ExecuteReader()

    while ($reader.Read()) {
        $postRowId = [int]$reader['PostRowID']
        $blogId = [string]$reader['BlogID']
        $postId = [string]$reader['PostID']
        $slug = if ($reader['Slug'] -eq [DBNull]::Value) { '' } else { [string]$reader['Slug'] }
        $title = if ($reader['Title'] -eq [DBNull]::Value) { '' } else { [string]$reader['Title'] }
        $content = if ($reader['PostContent'] -eq [DBNull]::Value) { '' } else { [string]$reader['PostContent'] }
        $safeSlug = ConvertTo-SafePathSegment -Value $slug -Fallback ("postrowid-$postRowId")

        if (-not $slugCounts.ContainsKey($safeSlug)) {
            $slugCounts[$safeSlug] = 0
        }

        $slugCounts[$safeSlug] = [int]$slugCounts[$safeSlug] + 1
        $folderName = if ($slugCounts[$safeSlug] -eq 1) { $safeSlug } else { "$safeSlug--postrowid-$postRowId" }
        $postDirectory = Join-Path $resolvedOutputRoot $folderName
        New-Item -ItemType Directory -Path $postDirectory -Force | Out-Null

        $categories = ConvertTo-StringArray $reader['Categories']
        $tags = ConvertTo-StringArray $reader['Tags']
        $links = Get-PostLinks -Content $content
        $isPublished = [bool]$reader['IsPublished']
        $isDeleted = [bool]$reader['IsDeleted']
        $status = if ($isDeleted) { 'deleted' } elseif ($isPublished) { 'published' } else { 'draft' }

        if ([string]::IsNullOrWhiteSpace($slug) -or [string]::IsNullOrWhiteSpace($title)) {
            $ambiguousRecords.Add([pscustomobject]@{
                postRowId = $postRowId
                postId = $postId
                slug = $slug
                title = $title
                issue = 'Missing slug or title'
            })
        }

        $metadata = [ordered]@{
            source = [ordered]@{
                databaseTable = 'dbo.be_Posts'
                postRowId = $postRowId
                blogId = $blogId
                postId = $postId
            }
            title = $title
            description = if ($reader['Description'] -eq [DBNull]::Value) { '' } else { [string]$reader['Description'] }
            author = if ($reader['Author'] -eq [DBNull]::Value) { '' } else { [string]$reader['Author'] }
            slug = $slug
            status = $status
            isPublished = $isPublished
            isDeleted = $isDeleted
            allowComments = [bool]$reader['IsCommentEnabled']
            raters = [int]$reader['Raters']
            rating = [double]$reader['Rating']
            dateCreated = ConvertTo-IsoString $reader['DateCreated']
            dateModified = ConvertTo-IsoString $reader['DateModified']
            categories = @($categories)
            tags = @($tags)
            links = @($links)
            export = [ordered]@{
                folder = $folderName
                metadataFile = 'post.database.json'
                contentFile = 'content.html'
            }
        }

        [System.IO.File]::WriteAllText((Join-Path $postDirectory 'post.database.json'), ($metadata | ConvertTo-Json -Depth 12), $utf8NoBom)
        [System.IO.File]::WriteAllText((Join-Path $postDirectory 'content.html'), $content, $utf8NoBom)

        $manifestPosts.Add([pscustomobject]@{
            postRowId = $postRowId
            blogId = $blogId
            postId = $postId
            slug = $slug
            title = $title
            status = $status
            isPublished = $isPublished
            isDeleted = $isDeleted
            dateCreated = $metadata.dateCreated
            dateModified = $metadata.dateModified
            folder = $folderName
            linkCount = @($links).Count
            links = @($links)
        })
    }

    $reader.Close()
}
finally {
    if ($null -ne $connection) {
        $connection.Dispose()
    }
}

$duplicateSlugs = @(
    $slugCounts.GetEnumerator() |
        Where-Object { $_.Value -gt 1 } |
        Sort-Object Name |
        ForEach-Object { [pscustomobject]@{ slug = $_.Name; count = $_.Value } }
)

$manifestPostArray = @($manifestPosts.ToArray())
$publishedCount = 0
$draftCount = 0
$deletedCount = 0

foreach ($manifestPost in $manifestPostArray) {
    if ($manifestPost.isDeleted -eq $true) {
        $deletedCount++
    }
    elseif ($manifestPost.isPublished -eq $true) {
        $publishedCount++
    }
    else {
        $draftCount++
    }
}

$exportName = [System.IO.Path]::GetFileName($resolvedOutputRoot)
$exportedAt = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ', [System.Globalization.CultureInfo]::InvariantCulture)
$includesDeleted = -not [bool]$ActiveOnly
$duplicateSlugCount = @($duplicateSlugs).Count
$ambiguousRecordCount = $ambiguousRecords.Count
$ambiguousRecordArray = @($ambiguousRecords.ToArray())

$manifest = [ordered]@{
    exportName = $exportName
    exportedAt = $exportedAt
    source = [ordered]@{
        databaseTable = 'dbo.be_Posts'
        includesDeleted = $includesDeleted
    }
    counts = [ordered]@{
        posts = $manifestPostArray.Count
        published = $publishedCount
        drafts = $draftCount
        deleted = $deletedCount
        duplicateSlugs = $duplicateSlugCount
        ambiguousRecords = $ambiguousRecordCount
    }
    posts = $manifestPostArray
    duplicateSlugs = $duplicateSlugs
    ambiguousRecords = $ambiguousRecordArray
}

[System.IO.File]::WriteAllText((Join-Path $resolvedOutputRoot 'manifest.json'), ($manifest | ConvertTo-Json -Depth 14), $utf8NoBom)

$readme = @'
# Blog Database Export

This folder is a read-only preservation export from BlogEngine database table `dbo.be_Posts`.

Export contents:

- `manifest.json`: index of every exported row, source identifiers, status, timestamps, and links found in body content.
- `<slug-or-row>/post.database.json`: metadata for one database row.
- `<slug-or-row>/content.html`: exact exported `PostContent` body for one database row.

Re-run from the repo root:

```powershell
.\scripts\blog-publishing\Export-BlogPostsFromDatabase.ps1 `
  -SqlConnectionString $env:AdventuresOnTheEdgeCS
```

Use `-OutputRoot` to write to a different dated folder. Use `-ActiveOnly` only when you intentionally want to exclude rows where `IsDeleted = 1`.

Do not edit database records from this export path. It is intended to preserve the current database baseline before blog rewrites.
'@

[System.IO.File]::WriteAllText((Join-Path $resolvedOutputRoot 'README.md'), $readme, $utf8NoBom)

Write-Host "Exported $($manifestPosts.Count) blog rows to $resolvedOutputRoot"
Write-Host "Published: $($manifest.counts.published); Drafts: $($manifest.counts.drafts); Deleted: $($manifest.counts.deleted)"
if ($manifest.counts.duplicateSlugs -gt 0) {
    Write-Host "Duplicate slug folders were disambiguated: $($manifest.counts.duplicateSlugs)" -ForegroundColor Yellow
}
if ($manifest.counts.ambiguousRecords -gt 0) {
    Write-Host "Ambiguous records found: $($manifest.counts.ambiguousRecords)" -ForegroundColor Yellow
}
