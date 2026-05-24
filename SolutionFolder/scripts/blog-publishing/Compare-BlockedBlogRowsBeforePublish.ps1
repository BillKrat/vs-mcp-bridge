[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$SqlConnectionString,

    [string[]]$Slug = @(
        'vs-mcp-bridge-blog-series-part-3',
        'vs-mcp-bridge-blog-series-part-4',
        'vs-mcp-bridge-blog-series-part-5',
        'vs-mcp-bridge-blog-series-part-6',
        'how-stdio-works-in-vs-mcp-bridge',
        'understanding-ai-chat-sessions-models-and-agents'
    ),

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

if ([string]::IsNullOrWhiteSpace($ReportPath)) {
    $ReportPath = Join-Path $script:ScriptRoot '..\..\docs\blogs\prepublish-blocked-row-diff-20260516.md'
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

function Compare-StringArray {
    param(
        [object[]]$Left,
        [object[]]$Right
    )

    $leftText = (@($Left) | ForEach-Object { [string]$_ }) -join '|'
    $rightText = (@($Right) | ForEach-Object { [string]$_ }) -join '|'
    return [string]::Equals($leftText, $rightText, [System.StringComparison]::Ordinal)
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

$resolvedPostsRoot = Resolve-FullPath $PostsRoot
$resolvedExportRoot = Resolve-FullPath $ExportRoot
$resolvedReportPath = Resolve-FullPath $ReportPath
$manifestPath = Join-Path $resolvedExportRoot 'manifest.json'

if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw "Export manifest was not found at '$manifestPath'."
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
$manifestBySlug = @{}
foreach ($post in $manifest.posts) {
    $manifestBySlug[$post.slug] = $post
}

$connection = New-Object System.Data.SqlClient.SqlConnection $SqlConnectionString
$results = New-Object 'System.Collections.Generic.List[object]'

try {
    $connection.Open()

    foreach ($slugValue in $Slug) {
        if (-not $manifestBySlug.ContainsKey($slugValue)) {
            throw "Export manifest has no entry for slug '$slugValue'."
        }

        $manifestEntry = $manifestBySlug[$slugValue]
        $exportDirectory = Join-Path $resolvedExportRoot ([string]$manifestEntry.folder)
        $exportPostPath = Join-Path $exportDirectory 'post.database.json'
        $exportContentPath = Join-Path $exportDirectory 'content.html'
        $canonicalDirectory = Join-Path $resolvedPostsRoot $slugValue
        $canonicalPostPath = Join-Path $canonicalDirectory 'post.json'
        $canonicalContentPath = Join-Path $canonicalDirectory 'content.html'

        foreach ($requiredPath in @($exportPostPath, $exportContentPath, $canonicalPostPath, $canonicalContentPath)) {
            if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
                throw "Required compare input was not found: '$requiredPath'."
            }
        }

        $current = Get-CurrentPost -Connection $connection -Slug $slugValue
        $exportPost = Get-Content -LiteralPath $exportPostPath -Raw -Encoding UTF8 | ConvertFrom-Json
        $exportContent = Get-Content -LiteralPath $exportContentPath -Raw -Encoding UTF8
        $canonicalPost = Get-Content -LiteralPath $canonicalPostPath -Raw -Encoding UTF8 | ConvertFrom-Json
        $canonicalContent = Get-Content -LiteralPath $canonicalContentPath -Raw -Encoding UTF8

        $bodyChanged = -not [string]::Equals($current.content, $exportContent, [System.StringComparison]::Ordinal)
        $titleChanged = -not [string]::Equals($current.title, [string]$exportPost.title, [System.StringComparison]::Ordinal)
        $slugChanged = -not [string]::Equals($current.slug, [string]$exportPost.slug, [System.StringComparison]::Ordinal)
        $statusChanged = -not [string]::Equals($current.status, [string]$exportPost.status, [System.StringComparison]::Ordinal)
        $descriptionChanged = -not [string]::Equals($current.description, [string]$exportPost.description, [System.StringComparison]::Ordinal)
        $authorChanged = -not [string]::Equals($current.author, [string]$exportPost.author, [System.StringComparison]::Ordinal)
        $allowCommentsChanged = $current.allowComments -ne [bool]$exportPost.allowComments
        $dateModifiedChanged = -not [string]::Equals($current.dateModified, [string]$exportPost.dateModified, [System.StringComparison]::Ordinal)
        $categoriesChanged = -not (Compare-StringArray -Left $current.categories -Right $exportPost.categories)
        $tagsChanged = -not (Compare-StringArray -Left $current.tags -Right $exportPost.tags)
        $canonicalDiffersFromCurrent = -not [string]::Equals($canonicalContent, $current.content, [System.StringComparison]::Ordinal)

        $changedFields = New-Object 'System.Collections.Generic.List[string]'
        if ($bodyChanged) { $changedFields.Add('body') }
        if ($titleChanged) { $changedFields.Add('title') }
        if ($slugChanged) { $changedFields.Add('slug') }
        if ($statusChanged) { $changedFields.Add('status') }
        if ($descriptionChanged) { $changedFields.Add('description') }
        if ($authorChanged) { $changedFields.Add('author') }
        if ($allowCommentsChanged) { $changedFields.Add('allowComments') }
        if ($categoriesChanged) { $changedFields.Add('categories') }
        if ($tagsChanged) { $changedFields.Add('tags') }
        if ($dateModifiedChanged) { $changedFields.Add('dateModified') }

        $classification = if ($bodyChanged) {
            'unknown-live-body-change'
        }
        elseif ($titleChanged -or $slugChanged -or $statusChanged) {
            'metadata-identity-change'
        }
        elseif ($descriptionChanged -or $authorChanged -or $allowCommentsChanged -or $categoriesChanged -or $tagsChanged) {
            'metadata-taxonomy-or-description-change'
        }
        elseif ($dateModifiedChanged) {
            'timestamp-only-change'
        }
        else {
            'no-difference-detected'
        }

        $appears = if ($bodyChanged) {
            'unknown'
        }
        elseif (($categoriesChanged -or $tagsChanged) -and -not ($descriptionChanged -or $titleChanged -or $slugChanged -or $statusChanged -or $authorChanged -or $allowCommentsChanged -or $dateModifiedChanged)) {
            'mechanical-taxonomy-drift'
        }
        elseif ($dateModifiedChanged -and ($descriptionChanged -or $categoriesChanged -or $tagsChanged) -and -not ($titleChanged -or $slugChanged -or $statusChanged)) {
            'mechanical-or-admin-metadata'
        }
        elseif ($dateModifiedChanged -and @($changedFields.ToArray()).Count -eq 1) {
            'timestamp-only'
        }
        else {
            'unknown'
        }

        $recommendedAction = if ($bodyChanged) {
            'Manual review required before publishing; inspect current live body and refresh the preserved baseline if the live change is intentional.'
        }
        elseif ($titleChanged -or $slugChanged -or $statusChanged) {
            'Manual review required before publishing because identity/status metadata changed.'
        }
        elseif ($descriptionChanged -or $categoriesChanged -or $tagsChanged -or $authorChanged -or $allowCommentsChanged) {
            'Likely safe after taxonomy metadata review; body content is unchanged, but publishing canonical content may intentionally replace live taxonomy metadata.'
        }
        elseif ($dateModifiedChanged) {
            'Likely safe after acknowledging timestamp-only drift.'
        }
        else {
            'Safe after inspection; no remaining live-vs-export difference was detected.'
        }

        $results.Add([pscustomobject]@{
            slug = $slugValue
            postId = $current.postId
            bodyChanged = $bodyChanged
            titleSlugStatusChanged = ($titleChanged -or $slugChanged -or $statusChanged)
            categoriesTagsChanged = ($categoriesChanged -or $tagsChanged)
            descriptionChanged = $descriptionChanged
            authorOrCommentsChanged = ($authorChanged -or $allowCommentsChanged)
            dateModifiedChanged = $dateModifiedChanged
            canonicalDiffersFromCurrent = $canonicalDiffersFromCurrent
            classification = $classification
            appears = $appears
            recommendedAction = $recommendedAction
            changedFields = @($changedFields.ToArray())
            exportDateModified = [string]$exportPost.dateModified
            currentDateModified = $current.dateModified
            exportContentHash = Get-Sha256Hex $exportContent
            currentContentHash = Get-Sha256Hex $current.content
            canonicalContentHash = Get-Sha256Hex $canonicalContent
            exportCategories = @($exportPost.categories)
            currentCategories = @($current.categories)
            canonicalCategories = @($canonicalPost.categories)
            exportTags = @($exportPost.tags)
            currentTags = @($current.tags)
            canonicalTags = @($canonicalPost.tags)
            exportDescription = [string]$exportPost.description
            currentDescription = $current.description
            canonicalDescription = [string]$canonicalPost.description
        })
    }
}
finally {
    $connection.Dispose()
}

$resultArray = @($results.ToArray())
$manualReviewRows = @($resultArray | Where-Object { $_.bodyChanged -or $_.titleSlugStatusChanged })
$safeAfterInspectionRows = @($resultArray | Where-Object { -not $_.bodyChanged -and -not $_.titleSlugStatusChanged })

$reportLines = New-Object 'System.Collections.Generic.List[string]'
$reportLines.Add('# Blocked Row Pre-Publish Diff - 2026-05-16')
$reportLines.Add('')
$reportLines.Add('## Scope')
$reportLines.Add('')
$reportLines.Add('This report inspects the six rows blocked by the ready-post batch compare because the current live DB row no longer matched the preserved `db-export-20260516` baseline.')
$reportLines.Add('It compares preserved DB export metadata/content, current live DB metadata/content, and canonical repo metadata/content.')
$reportLines.Add('No database writes, reload calls, public site changes, or canonical post rewrites were performed.')
$reportLines.Add('')
$reportLines.Add('## Summary')
$reportLines.Add('')
$reportLines.Add('| Metric | Count |')
$reportLines.Add('| --- | ---: |')
$reportLines.Add("| Blocked rows inspected | $($resultArray.Count) |")
$reportLines.Add("| Rows with live body content changes | $(@($resultArray | Where-Object { $_.bodyChanged }).Count) |")
$reportLines.Add("| Rows with title/slug/status changes | $(@($resultArray | Where-Object { $_.titleSlugStatusChanged }).Count) |")
$reportLines.Add("| Rows with category/tag changes | $(@($resultArray | Where-Object { $_.categoriesTagsChanged }).Count) |")
$reportLines.Add("| Rows with dateModified changes | $(@($resultArray | Where-Object { $_.dateModifiedChanged }).Count) |")
$reportLines.Add("| Rows safe after inspection | $($safeAfterInspectionRows.Count) |")
$reportLines.Add("| Rows needing manual review | $($manualReviewRows.Count) |")
$reportLines.Add('')
$reportLines.Add('## Per-Row Diff Classification')
$reportLines.Add('')
$reportLines.Add('| Slug | DB PostID | Body changed | Title/slug/status changed | Categories/tags changed | DateModified changed | Canonical differs from current DB | Likely cause | Recommended action |')
$reportLines.Add('| --- | --- | --- | --- | --- | --- | --- | --- | --- |')

foreach ($result in $resultArray) {
    $reportLines.Add("| $(ConvertTo-MarkdownValue $result.slug) | $(ConvertTo-MarkdownValue $result.postId) | $($result.bodyChanged) | $($result.titleSlugStatusChanged) | $($result.categoriesTagsChanged) | $($result.dateModifiedChanged) | $($result.canonicalDiffersFromCurrent) | $(ConvertTo-MarkdownValue $result.appears) | $(ConvertTo-MarkdownValue $result.recommendedAction) |")
}

$reportLines.Add('')
$reportLines.Add('## Detailed Findings')

foreach ($result in $resultArray) {
    $reportLines.Add('')
    $reportLines.Add("### $($result.slug)")
    $reportLines.Add('')
    $reportLines.Add('| Field | Export baseline | Current live DB | Canonical repo |')
    $reportLines.Add('| --- | --- | --- | --- |')
    $reportLines.Add("| DateModified | $(ConvertTo-MarkdownValue $result.exportDateModified) | $(ConvertTo-MarkdownValue $result.currentDateModified) | N/A |")
    $reportLines.Add("| Content SHA-256 | $(ConvertTo-MarkdownValue $result.exportContentHash) | $(ConvertTo-MarkdownValue $result.currentContentHash) | $(ConvertTo-MarkdownValue $result.canonicalContentHash) |")
    $reportLines.Add("| Categories | $(ConvertTo-InlineList $result.exportCategories) | $(ConvertTo-InlineList $result.currentCategories) | $(ConvertTo-InlineList $result.canonicalCategories) |")
    $reportLines.Add("| Tags | $(ConvertTo-InlineList $result.exportTags) | $(ConvertTo-InlineList $result.currentTags) | $(ConvertTo-InlineList $result.canonicalTags) |")
    $reportLines.Add("| Description | $(ConvertTo-MarkdownValue $result.exportDescription) | $(ConvertTo-MarkdownValue $result.currentDescription) | $(ConvertTo-MarkdownValue $result.canonicalDescription) |")
    $reportLines.Add('')
    $reportLines.Add("Changed fields: $(ConvertTo-InlineList $result.changedFields)")
    $reportLines.Add("Classification: $(ConvertTo-MarkdownValue $result.classification)")
}

$reportLines.Add('')
$reportLines.Add('## Rows Safe After Inspection')
$reportLines.Add('')
if ($safeAfterInspectionRows.Count -eq 0) {
    $reportLines.Add('None.')
}
else {
    foreach ($row in $safeAfterInspectionRows) {
        $reportLines.Add("- $(ConvertTo-MarkdownValue $row.slug)")
    }
}

$reportLines.Add('')
$reportLines.Add('## Rows Needing Manual Review')
$reportLines.Add('')
if ($manualReviewRows.Count -eq 0) {
    $reportLines.Add('None.')
}
else {
    foreach ($row in $manualReviewRows) {
        $reportLines.Add("- $(ConvertTo-MarkdownValue $row.slug)")
    }
}

$reportLines.Add('')
$reportLines.Add('## Recommended Next Slice')
$reportLines.Add('')
if ($manualReviewRows.Count -gt 0) {
    $reportLines.Add('For rows needing manual review, preserve a fresh DB export or produce focused body diffs before any overwrite. For rows safe after inspection, decide whether to refresh metadata baselines or proceed with draft publishing one post at a time.')
}
else {
    $reportLines.Add('Proceed with draft publishing one safe post and verify BlogAI/global-webnet rendering before continuing.')
}

$reportDirectory = Split-Path -Parent $resolvedReportPath
New-Item -ItemType Directory -Path $reportDirectory -Force | Out-Null
$utf8NoBom = [System.Text.UTF8Encoding]::new($false)
[System.IO.File]::WriteAllText($resolvedReportPath, ($reportLines -join "`r`n"), $utf8NoBom)

Write-Host "Blocked row diff completed."
Write-Host "Report: $resolvedReportPath"
Write-Host "Blocked rows inspected: $($resultArray.Count)"
Write-Host "Rows safe after inspection: $($safeAfterInspectionRows.Count)"
Write-Host "Rows needing manual review: $($manualReviewRows.Count)"
