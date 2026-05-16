[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$Slug,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$SqlConnectionString,

    [string]$PostsRoot,

    [string]$ExportRoot,

    [string]$ReportPath
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

if ([string]::IsNullOrWhiteSpace($ExportRoot)) {
    $ExportRoot = Join-Path $script:ScriptRoot '..\..\docs\blogs\source-of-truth\db-export-20260516'
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

function Compare-StringArray {
    param(
        [object[]]$Left,
        [object[]]$Right
    )

    $leftText = (@($Left) | ForEach-Object { [string]$_ }) -join '|'
    $rightText = (@($Right) | ForEach-Object { [string]$_ }) -join '|'
    return [string]::Equals($leftText, $rightText, [System.StringComparison]::Ordinal)
}

$resolvedPostsRoot = Resolve-FullPath $PostsRoot
$resolvedExportRoot = Resolve-FullPath $ExportRoot
$postDirectory = Join-Path $resolvedPostsRoot $Slug
$postJsonPath = Join-Path $postDirectory 'post.json'
$canonicalContentPath = Join-Path $postDirectory 'content.html'
$manifestPath = Join-Path $resolvedExportRoot 'manifest.json'

if (-not (Test-Path -LiteralPath $postJsonPath -PathType Leaf)) {
    throw "Canonical post.json was not found at '$postJsonPath'."
}

if (-not (Test-Path -LiteralPath $canonicalContentPath -PathType Leaf)) {
    throw "Canonical content.html was not found at '$canonicalContentPath'."
}

if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw "Export manifest was not found at '$manifestPath'."
}

$canonicalPost = Get-Content -LiteralPath $postJsonPath -Raw -Encoding UTF8 | ConvertFrom-Json
$canonicalContent = Get-Content -LiteralPath $canonicalContentPath -Raw -Encoding UTF8
$manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
$manifestEntry = @($manifest.posts | Where-Object { $_.slug -eq $Slug }) | Select-Object -First 1

if ($null -eq $manifestEntry) {
    throw "Export manifest has no entry for slug '$Slug'."
}

$exportFolder = [string]$manifestEntry.folder
$exportPostJsonPath = Join-Path (Join-Path $resolvedExportRoot $exportFolder) 'post.database.json'
$exportContentPath = Join-Path (Join-Path $resolvedExportRoot $exportFolder) 'content.html'

if (-not (Test-Path -LiteralPath $exportPostJsonPath -PathType Leaf)) {
    throw "Export post.database.json was not found at '$exportPostJsonPath'."
}

if (-not (Test-Path -LiteralPath $exportContentPath -PathType Leaf)) {
    throw "Export content.html was not found at '$exportContentPath'."
}

$exportPost = Get-Content -LiteralPath $exportPostJsonPath -Raw -Encoding UTF8 | ConvertFrom-Json
$exportContent = Get-Content -LiteralPath $exportContentPath -Raw -Encoding UTF8

$connection = New-Object System.Data.SqlClient.SqlConnection $SqlConnectionString
$currentPost = $null

try {
    $connection.Open()

    $command = $connection.CreateCommand()
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
    if ($reader.Read()) {
        $isPublished = [bool]$reader['IsPublished']
        $isDeleted = [bool]$reader['IsDeleted']
        $currentPost = [pscustomobject]@{
            postRowId = [int]$reader['PostRowID']
            blogId = [string]$reader['BlogID']
            postId = [string]$reader['PostID']
            title = if ($reader['Title'] -eq [DBNull]::Value) { '' } else { [string]$reader['Title'] }
            description = if ($reader['Description'] -eq [DBNull]::Value) { '' } else { [string]$reader['Description'] }
            author = if ($reader['Author'] -eq [DBNull]::Value) { '' } else { [string]$reader['Author'] }
            slug = if ($reader['Slug'] -eq [DBNull]::Value) { '' } else { [string]$reader['Slug'] }
            status = if ($isDeleted) { 'deleted' } elseif ($isPublished) { 'published' } else { 'draft' }
            isPublished = $isPublished
            isDeleted = $isDeleted
            allowComments = [bool]$reader['IsCommentEnabled']
            raters = [int]$reader['Raters']
            rating = [double]$reader['Rating']
            dateCreated = ConvertTo-IsoString $reader['DateCreated']
            dateModified = ConvertTo-IsoString $reader['DateModified']
            categories = @(ConvertTo-StringArray $reader['Categories'])
            tags = @(ConvertTo-StringArray $reader['Tags'])
            content = if ($reader['PostContent'] -eq [DBNull]::Value) { '' } else { [string]$reader['PostContent'] }
        }
    }

    $reader.Close()
}
finally {
    if ($null -ne $connection) {
        $connection.Dispose()
    }
}

if ($null -eq $currentPost) {
    throw "No active DB post found for slug '$Slug'."
}

$currentContentHash = Get-Sha256Hex $currentPost.content
$exportContentHash = Get-Sha256Hex $exportContent
$canonicalContentHash = Get-Sha256Hex $canonicalContent

$contentMatchesExport = [string]::Equals($currentPost.content, $exportContent, [System.StringComparison]::Ordinal)
$canonicalMatchesCurrentContent = [string]::Equals($canonicalContent, $currentPost.content, [System.StringComparison]::Ordinal)

$metadataChecks = [ordered]@{
    postRowId = ($currentPost.postRowId -eq [int]$exportPost.source.postRowId)
    blogId = [string]::Equals($currentPost.blogId, [string]$exportPost.source.blogId, [System.StringComparison]::OrdinalIgnoreCase)
    postId = [string]::Equals($currentPost.postId, [string]$exportPost.source.postId, [System.StringComparison]::OrdinalIgnoreCase)
    title = [string]::Equals($currentPost.title, [string]$exportPost.title, [System.StringComparison]::Ordinal)
    description = [string]::Equals($currentPost.description, [string]$exportPost.description, [System.StringComparison]::Ordinal)
    author = [string]::Equals($currentPost.author, [string]$exportPost.author, [System.StringComparison]::Ordinal)
    slug = [string]::Equals($currentPost.slug, [string]$exportPost.slug, [System.StringComparison]::Ordinal)
    status = [string]::Equals($currentPost.status, [string]$exportPost.status, [System.StringComparison]::Ordinal)
    isPublished = ($currentPost.isPublished -eq [bool]$exportPost.isPublished)
    isDeleted = ($currentPost.isDeleted -eq [bool]$exportPost.isDeleted)
    allowComments = ($currentPost.allowComments -eq [bool]$exportPost.allowComments)
    dateCreated = [string]::Equals($currentPost.dateCreated, [string]$exportPost.dateCreated, [System.StringComparison]::Ordinal)
    dateModified = [string]::Equals($currentPost.dateModified, [string]$exportPost.dateModified, [System.StringComparison]::Ordinal)
    categories = Compare-StringArray -Left $currentPost.categories -Right $exportPost.categories
    tags = Compare-StringArray -Left $currentPost.tags -Right $exportPost.tags
}

$metadataMatchesExport = -not ($metadataChecks.Values -contains $false)
$currentMatchesExport = $contentMatchesExport -and $metadataMatchesExport
$canonicalDiffersFromCurrent = -not $canonicalMatchesCurrentContent

$tokenPattern = '\[(?:Page|Display):[^\]]+\]|\[[A-Za-z][A-Za-z0-9 ]+(?:\|[A-Za-z0-9 ]+)?\]'
$stalePattern = 'https://github\.com/BillKrat/vs-mcp-bridge/blob/feature/approval-apply-ui-slice/[^"''<>\s]+|https?://(?:www\.)?adventuresontheedge\.net[^"''<>\s]*|https?://AdventuresOnTheEdge\.net[^"''<>\s]*|post\.aspx\?id=[A-Za-z0-9-]+'

$canonicalTokens = @(Get-RegexValues -Content $canonicalContent -Pattern $tokenPattern)
$currentTokens = @(Get-RegexValues -Content $currentPost.content -Pattern $tokenPattern)
$canonicalStaleLinks = @(Get-RegexValues -Content $canonicalContent -Pattern $stalePattern)
$currentStaleLinks = @(Get-RegexValues -Content $currentPost.content -Pattern $stalePattern)
$exportStaleLinks = @(Get-RegexValues -Content $exportContent -Pattern $stalePattern)

$canonicalStaleLinkCount = @($canonicalStaleLinks).Count

$safetyDecision = if ($currentMatchesExport -and $canonicalDiffersFromCurrent -and $canonicalStaleLinkCount -eq 0) {
    'Safe for human draft-publish review: current DB still matches preserved export, and canonical content intentionally differs from runtime baseline.'
}
elseif (-not $currentMatchesExport) {
    'Do not publish yet: current DB no longer matches the preserved export baseline. Re-export or review live edits before overwrite.'
}
elseif (-not $canonicalDiffersFromCurrent) {
    'No publish needed for body content: canonical content matches current DB content.'
}
else {
    'Review before publishing: one or more stale-link or metadata checks need attention.'
}

if ([string]::IsNullOrWhiteSpace($ReportPath)) {
    $safeSlug = [regex]::Replace($Slug.ToLowerInvariant(), '[^a-z0-9._-]+', '-').Trim('-')
    $ReportPath = Join-Path $script:ScriptRoot "..\..\docs\blogs\prepublish-compare-$safeSlug-20260516.md"
}

$resolvedReportPath = Resolve-FullPath $ReportPath
$reportDirectory = Split-Path -Parent $resolvedReportPath
New-Item -ItemType Directory -Path $reportDirectory -Force | Out-Null

$canonicalPostPath = (Resolve-Path -LiteralPath $postDirectory).Path
$reportLines = New-Object 'System.Collections.Generic.List[string]'

$reportLines.Add("# Pre-Publish Compare - $Slug - 2026-05-16")
$reportLines.Add('')
$reportLines.Add('## Scope')
$reportLines.Add('')
$reportLines.Add('This report compares the current live BlogEngine database row, the preserved `db-export-20260516` baseline, and the canonical repo post before any publish operation.')
$reportLines.Add('The compare script is read-only: it performs a parameterized `SELECT`, reads repository files, and writes this report.')
$reportLines.Add('It does not update the database, call reload endpoints, or change public site behavior.')
$reportLines.Add('')
$reportLines.Add('## Result Summary')
$reportLines.Add('')
$reportLines.Add('| Check | Result |')
$reportLines.Add('| --- | --- |')
$reportLines.Add("| Slug | $(ConvertTo-MarkdownValue $Slug) |")
$reportLines.Add("| DB PostID | $(ConvertTo-MarkdownValue $($currentPost.postId)) |")
$reportLines.Add("| DB BlogID | $(ConvertTo-MarkdownValue $($currentPost.blogId)) |")
$reportLines.Add("| DB PostRowID | $($currentPost.postRowId) |")
$reportLines.Add("| Current DB DateModified | $(ConvertTo-MarkdownValue $($currentPost.dateModified)) |")
$reportLines.Add("| Preserved export timestamp | $(ConvertTo-MarkdownValue $($manifest.exportedAt)) |")
$reportLines.Add("| Preserved export DateModified | $(ConvertTo-MarkdownValue $($exportPost.dateModified)) |")
$reportLines.Add("| Canonical repo post path | $(ConvertTo-MarkdownValue $canonicalPostPath) |")
$reportLines.Add("| Current DB matches preserved export | $currentMatchesExport |")
$reportLines.Add("| Current DB content matches preserved export content | $contentMatchesExport |")
$reportLines.Add("| Current DB metadata matches preserved export metadata | $metadataMatchesExport |")
$reportLines.Add("| Canonical content differs from current DB content | $canonicalDiffersFromCurrent |")
$reportLines.Add("| Canonical stale direct links found | $canonicalStaleLinkCount |")
$reportLines.Add("| Recommended safety decision | $(ConvertTo-MarkdownValue $safetyDecision) |")
$reportLines.Add('')
$reportLines.Add('## Content Hashes')
$reportLines.Add('')
$reportLines.Add('| Source | SHA-256 |')
$reportLines.Add('| --- | --- |')
$reportLines.Add("| Current live DB content | $currentContentHash |")
$reportLines.Add("| Preserved export content | $exportContentHash |")
$reportLines.Add("| Canonical repo content | $canonicalContentHash |")
$reportLines.Add('')
$reportLines.Add('## Metadata Baseline Checks')
$reportLines.Add('')
$reportLines.Add('| Metadata field | Current DB matches preserved export |')
$reportLines.Add('| --- | --- |')
foreach ($key in $metadataChecks.Keys) {
    $reportLines.Add("| $key | $($metadataChecks[$key]) |")
}
$reportLines.Add('')
$reportLines.Add('## Current DB Metadata')
$reportLines.Add('')
$reportLines.Add('| Field | Value |')
$reportLines.Add('| --- | --- |')
$reportLines.Add("| Title | $(ConvertTo-MarkdownValue $($currentPost.title)) |")
$reportLines.Add("| Description | $(ConvertTo-MarkdownValue $($currentPost.description)) |")
$reportLines.Add("| Author | $(ConvertTo-MarkdownValue $($currentPost.author)) |")
$reportLines.Add("| Status | $(ConvertTo-MarkdownValue $($currentPost.status)) |")
$reportLines.Add("| IsPublished | $($currentPost.isPublished) |")
$reportLines.Add("| IsDeleted | $($currentPost.isDeleted) |")
$reportLines.Add("| AllowComments | $($currentPost.allowComments) |")
$reportLines.Add("| Categories | $(ConvertTo-InlineList $($currentPost.categories)) |")
$reportLines.Add("| Tags | $(ConvertTo-InlineList $($currentPost.tags)) |")
$reportLines.Add('')
$reportLines.Add('## Canonical Repo Metadata')
$reportLines.Add('')
$reportLines.Add('| Field | Value |')
$reportLines.Add('| --- | --- |')
$reportLines.Add("| Title | $(ConvertTo-MarkdownValue $($canonicalPost.title)) |")
$reportLines.Add("| Description | $(ConvertTo-MarkdownValue $($canonicalPost.description)) |")
$reportLines.Add("| Author | $(ConvertTo-MarkdownValue $($canonicalPost.author)) |")
$reportLines.Add("| Slug | $(ConvertTo-MarkdownValue $($canonicalPost.slug)) |")
$reportLines.Add("| BlogID | $(ConvertTo-MarkdownValue $($canonicalPost.blogId)) |")
$reportLines.Add("| PostID | $(ConvertTo-MarkdownValue $($canonicalPost.postId)) |")
$reportLines.Add("| IsPublished | $($canonicalPost.isPublished) |")
$reportLines.Add("| AllowComments | $($canonicalPost.allowComments) |")
$reportLines.Add("| Categories | $(ConvertTo-InlineList $($canonicalPost.categories)) |")
$reportLines.Add("| Tags | $(ConvertTo-InlineList $($canonicalPost.tags)) |")
$reportLines.Add('')
$reportLines.Add('## Intentional BlogEngine Tokens')
$reportLines.Add('')
$reportLines.Add('| Source | Tokens |')
$reportLines.Add('| --- | --- |')
$reportLines.Add("| Current DB content | $(ConvertTo-InlineList $currentTokens) |")
$reportLines.Add("| Canonical repo content | $(ConvertTo-InlineList $canonicalTokens) |")
$reportLines.Add('')
$reportLines.Add('Preserve these tokens unless a separate `GwnWikiExtension` mapping decision is made.')
$reportLines.Add('')
$reportLines.Add('## Stale Link Checks')
$reportLines.Add('')
$reportLines.Add('Checked for:')
$reportLines.Add('')
$reportLines.Add('- `feature/approval-apply-ui-slice`')
$reportLines.Add('- direct `adventuresontheedge.net` URLs')
$reportLines.Add('- direct `post.aspx?id=...` URLs')
$reportLines.Add('')
$reportLines.Add('| Source | Matches |')
$reportLines.Add('| --- | --- |')
$reportLines.Add("| Current DB content | $(ConvertTo-InlineList $currentStaleLinks) |")
$reportLines.Add("| Preserved export content | $(ConvertTo-InlineList $exportStaleLinks) |")
$reportLines.Add("| Canonical repo content | $(ConvertTo-InlineList $canonicalStaleLinks) |")
$reportLines.Add('')
$reportLines.Add('## Publish Safety Recommendation')
$reportLines.Add('')
$reportLines.Add($safetyDecision)
$reportLines.Add('')
$reportLines.Add('If publishing proceeds, use the draft-only workflow first and verify runtime rendering before touching the next post.')

$report = $reportLines -join "`r`n"

$utf8NoBom = [System.Text.UTF8Encoding]::new($false)
[System.IO.File]::WriteAllText($resolvedReportPath, $report, $utf8NoBom)

Write-Host "Pre-publish compare completed for '$Slug'."
Write-Host "Report: $resolvedReportPath"
Write-Host "Current DB matches preserved export: $currentMatchesExport"
Write-Host "Canonical content differs from current DB: $canonicalDiffersFromCurrent"
Write-Host "Safety decision: $safetyDecision"
