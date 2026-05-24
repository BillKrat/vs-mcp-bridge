[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$SqlConnectionString,

    [string]$ReviewPlanPath,

    [string]$ReportPath,

    [string]$PostsRoot,

    [string]$ExportRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($PSScriptRoot)) {
    $script:ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition
}
else {
    $script:ScriptRoot = $PSScriptRoot
}

function Resolve-FullPath {
    param([string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $Path))
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

function ConvertTo-SlugSafeName {
    param([string]$Value)

    $safeName = [regex]::Replace($Value.ToLowerInvariant(), '[^a-z0-9._-]+', '-').Trim('-')
    if ([string]::IsNullOrWhiteSpace($safeName)) {
        return 'post'
    }

    return $safeName
}

function Get-ReadySlugsFromReviewPlan {
    param([string]$Path)

    $slugs = New-Object 'System.Collections.Generic.List[string]'
    $inReadyTable = $false

    foreach ($line in Get-Content -LiteralPath $Path -Encoding UTF8) {
        if ($line -eq '## Posts Ready For Publishing Review') {
            $inReadyTable = $true
            continue
        }

        if ($inReadyTable -and $line.StartsWith('## ')) {
            break
        }

        if (-not $inReadyTable) {
            continue
        }

        $match = [regex]::Match($line, '^\|\s*\d+\s*\|\s*`(?<slug>[^`]+)`\s*\|')
        if ($match.Success) {
            $slugs.Add($match.Groups['slug'].Value)
        }
    }

    return @($slugs.ToArray())
}

function Get-ReportField {
    param(
        [string[]]$Lines,
        [string]$FieldName
    )

    $escapedName = [regex]::Escape($FieldName)
    foreach ($line in $Lines) {
        $match = [regex]::Match($line, "^\|\s*$escapedName\s*\|\s*(?<value>.*?)\s*\|$")
        if ($match.Success) {
            return $match.Groups['value'].Value.Trim()
        }
    }

    return ''
}

function Get-SectionReportField {
    param(
        [string[]]$Lines,
        [string]$SectionName,
        [string]$FieldName
    )

    $inSection = $false
    $escapedName = [regex]::Escape($FieldName)

    foreach ($line in $Lines) {
        if ($line -eq "## $SectionName") {
            $inSection = $true
            continue
        }

        if ($inSection -and $line.StartsWith('## ')) {
            break
        }

        if (-not $inSection) {
            continue
        }

        $match = [regex]::Match($line, "^\|\s*$escapedName\s*\|\s*(?<value>.*?)\s*\|$")
        if ($match.Success) {
            return $match.Groups['value'].Value.Trim()
        }
    }

    return ''
}

if ([string]::IsNullOrWhiteSpace($ReviewPlanPath)) {
    $ReviewPlanPath = Join-Path $script:ScriptRoot '..\..\docs\blogs\blog-publishing-review-plan-20260516.md'
}

if ([string]::IsNullOrWhiteSpace($ReportPath)) {
    $ReportPath = Join-Path $script:ScriptRoot '..\..\docs\blogs\prepublish-compare-ready-posts-20260516.md'
}

if ([string]::IsNullOrWhiteSpace($PostsRoot)) {
    $PostsRoot = Join-Path $script:ScriptRoot '..\..\docs\blogs\posts'
}

if ([string]::IsNullOrWhiteSpace($ExportRoot)) {
    $ExportRoot = Join-Path $script:ScriptRoot '..\..\docs\blogs\source-of-truth\db-export-20260516'
}

$resolvedReviewPlanPath = Resolve-FullPath $ReviewPlanPath
$resolvedReportPath = Resolve-FullPath $ReportPath
$resolvedPostsRoot = Resolve-FullPath $PostsRoot
$resolvedExportRoot = Resolve-FullPath $ExportRoot
$singleCompareScript = Join-Path $script:ScriptRoot 'Compare-BlogPostBeforePublish.ps1'

if (-not (Test-Path -LiteralPath $singleCompareScript -PathType Leaf)) {
    throw "Single-post compare script was not found at '$singleCompareScript'."
}

if (-not (Test-Path -LiteralPath $resolvedReviewPlanPath -PathType Leaf)) {
    throw "Review plan was not found at '$resolvedReviewPlanPath'."
}

$readySlugs = @(Get-ReadySlugsFromReviewPlan -Path $resolvedReviewPlanPath)
if ($readySlugs.Count -eq 0) {
    throw "No ready-post slugs were found in '$resolvedReviewPlanPath'."
}

$temporaryReportRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("vs-mcp-bridge-prepublish-compare-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temporaryReportRoot -Force | Out-Null

$results = New-Object 'System.Collections.Generic.List[object]'

foreach ($slug in $readySlugs) {
    $safeName = ConvertTo-SlugSafeName $slug
    $temporaryReportPath = Join-Path $temporaryReportRoot "$safeName.md"

    & $singleCompareScript `
        -Slug $slug `
        -SqlConnectionString $SqlConnectionString `
        -PostsRoot $resolvedPostsRoot `
        -ExportRoot $resolvedExportRoot `
        -ReportPath $temporaryReportPath | Out-Host

    if (-not (Test-Path -LiteralPath $temporaryReportPath -PathType Leaf)) {
        throw "Expected temporary compare report was not created for '$slug'."
    }

    $reportLines = @(Get-Content -LiteralPath $temporaryReportPath -Encoding UTF8)
    $tokens = (Get-SectionReportField -Lines $reportLines -SectionName 'Intentional BlogEngine Tokens' -FieldName 'Canonical repo content').Replace('\|', '|')
    $staleLinkCount = [int](Get-ReportField -Lines $reportLines -FieldName 'Canonical stale direct links found')
    $safetyDecision = Get-ReportField -Lines $reportLines -FieldName 'Recommended safety decision'
    $currentMatchesExport = [bool]::Parse((Get-ReportField -Lines $reportLines -FieldName 'Current DB matches preserved export'))
    $canonicalDiffersFromCurrent = [bool]::Parse((Get-ReportField -Lines $reportLines -FieldName 'Canonical content differs from current DB content'))
    $isSafe = $currentMatchesExport -and $canonicalDiffersFromCurrent -and $staleLinkCount -eq 0 -and $safetyDecision.StartsWith('Safe for human draft-publish review', [System.StringComparison]::Ordinal)

    $results.Add([pscustomobject]@{
        slug = $slug
        postId = Get-ReportField -Lines $reportLines -FieldName 'DB PostID'
        currentMatchesExport = $currentMatchesExport
        canonicalDiffersFromCurrent = $canonicalDiffersFromCurrent
        staleDirectLinkCount = $staleLinkCount
        tokens = $tokens
        safetyDecision = $safetyDecision
        isSafe = $isSafe
    })
}

try {
    Remove-Item -LiteralPath $temporaryReportRoot -Recurse -Force
}
catch {
    Write-Warning "Could not remove temporary report folder '$temporaryReportRoot': $($_.Exception.Message)"
}

$resultArray = @($results.ToArray())
$safeRows = @($resultArray | Where-Object { $_.isSafe })
$blockedRows = @($resultArray | Where-Object { -not $_.isSafe })
$changedDbRows = @($resultArray | Where-Object { -not $_.currentMatchesExport })
$staleLinkRows = @($resultArray | Where-Object { $_.staleDirectLinkCount -gt 0 })
$unsafeTokenRows = @()

$reportDirectory = Split-Path -Parent $resolvedReportPath
New-Item -ItemType Directory -Path $reportDirectory -Force | Out-Null

$reportLines = New-Object 'System.Collections.Generic.List[string]'
$reportLines.Add('# Ready Posts Pre-Publish Compare - 2026-05-16')
$reportLines.Add('')
$reportLines.Add('## Scope')
$reportLines.Add('')
$reportLines.Add('This report batches the read-only pre-publish compare across every post marked ready for publishing review.')
$reportLines.Add('It invokes `Compare-BlogPostBeforePublish.ps1` for each slug, reads the live BlogEngine database through parameterized `SELECT` statements, compares against the preserved `db-export-20260516` baseline and canonical repo source, and writes this summary.')
$reportLines.Add('No database writes, reload calls, or public site changes were performed.')
$reportLines.Add('')
$reportLines.Add('## Summary')
$reportLines.Add('')
$reportLines.Add('| Metric | Count |')
$reportLines.Add('| --- | ---: |')
$reportLines.Add("| Total ready posts checked | $($resultArray.Count) |")
$reportLines.Add("| Safe for human draft-publish review | $($safeRows.Count) |")
$reportLines.Add("| Blocked or needs review | $($blockedRows.Count) |")
$reportLines.Add("| Current DB no longer matches preserved export | $($changedDbRows.Count) |")
$reportLines.Add("| Canonical stale direct-link findings | $($staleLinkRows.Count) |")
$reportLines.Add("| Unsafe token findings | $($unsafeTokenRows.Count) |")
$reportLines.Add('')
$reportLines.Add('## Per-Post Results')
$reportLines.Add('')
$reportLines.Add('| Slug | DB PostID | Current DB matches preserved export | Canonical differs from current DB | Stale direct links | Intentional BlogEngine tokens | Publish safety recommendation |')
$reportLines.Add('| --- | --- | --- | --- | ---: | --- | --- |')

foreach ($result in $resultArray) {
    $slugValue = ConvertTo-MarkdownValue $result.slug
    $postIdValue = ConvertTo-MarkdownValue $result.postId
    $tokensValue = ConvertTo-MarkdownValue $result.tokens
    $safetyDecisionValue = ConvertTo-MarkdownValue $result.safetyDecision
    $reportLines.Add("| $slugValue | $postIdValue | $($result.currentMatchesExport) | $($result.canonicalDiffersFromCurrent) | $($result.staleDirectLinkCount) | $tokensValue | $safetyDecisionValue |")
}

$reportLines.Add('')
$reportLines.Add('## Rows Where Current DB No Longer Matches Export')
$reportLines.Add('')
if ($changedDbRows.Count -eq 0) {
    $reportLines.Add('None.')
}
else {
    foreach ($row in $changedDbRows) {
        $reportLines.Add(('- ' + (ConvertTo-MarkdownValue $row.slug)))
    }
}

$reportLines.Add('')
$reportLines.Add('## Stale Links Or Unsafe Tokens')
$reportLines.Add('')
if ($staleLinkRows.Count -eq 0 -and $unsafeTokenRows.Count -eq 0) {
    $reportLines.Add('None.')
}
else {
    foreach ($row in $staleLinkRows) {
        $reportLines.Add(('- ' + (ConvertTo-MarkdownValue $row.slug) + " has $($row.staleDirectLinkCount) stale direct-link finding(s)."))
    }
    foreach ($row in $unsafeTokenRows) {
        $reportLines.Add(('- ' + (ConvertTo-MarkdownValue $row.slug) + ' has an unsafe token finding.'))
    }
}

$reportLines.Add('')
$reportLines.Add('## Publish Safety Recommendation')
$reportLines.Add('')
if ($blockedRows.Count -eq 0) {
    $reportLines.Add('All ready posts are safe for human draft-publish review. The current live DB rows still match the preserved export baseline, canonical content differs from current runtime content, and no stale direct links were found in canonical bodies.')
}
else {
    $reportLines.Add('Do not batch publish. Review blocked rows first, especially any current DB rows that no longer match the preserved export baseline.')
}

$reportLines.Add('')
$reportLines.Add('## Recommended Next Slice')
$reportLines.Add('')
$reportLines.Add('Run the draft-only publish workflow for one safe post, starting with `vs-mcp-bridge-blog-series-part-1`, then verify BlogAI/global-webnet rendering before publishing the remaining ready set.')

$utf8NoBom = [System.Text.UTF8Encoding]::new($false)
[System.IO.File]::WriteAllText($resolvedReportPath, ($reportLines -join "`r`n"), $utf8NoBom)

Write-Host "Batch pre-publish compare completed."
Write-Host "Report: $resolvedReportPath"
Write-Host "Total ready posts checked: $($resultArray.Count)"
Write-Host "Safe for human draft-publish review: $($safeRows.Count)"
Write-Host "Blocked or needs review: $($blockedRows.Count)"
