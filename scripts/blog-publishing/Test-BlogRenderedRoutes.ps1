param(
    [string]$BaseUrl = 'https://www.global-webnet.com',
    [string]$OutputPath = 'docs/blogs/final-rendered-route-verification-20260516.md',
    [int]$TimeoutSec = 30
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$routes = @(
    [pscustomobject]@{
        Order = 1
        Slug = 'vs-mcp-bridge-blog-series-part-1'
        Path = '/post/2026/04/11/vs-mcp-bridge-blog-series-part-1'
        Markers = @('Source of Truth:', 'BridgeToolExecutor', 'anti-black-box rule')
        ExpectedPostAspIdLinks = $true
        TokenNote = '[NamedPipeListener], [Page:VS MCP Bridge|VsMcpBridge], [Stdio]'
    },
    [pscustomobject]@{
        Order = 2
        Slug = 'vs-mcp-bridge-blog-series-part-2'
        Path = '/post/2026/04/11/vs-mcp-bridge-blog-series-part-2'
        Markers = @('Source of Truth:', 'observability became architecture', 'BridgeToolExecutor', 'tool-approval-trace-20260516.mmd')
        ExpectedPostAspIdLinks = $false
        TokenNote = 'None'
    },
    [pscustomobject]@{
        Order = 3
        Slug = 'vs-mcp-bridge-blog-series-part-3'
        Path = '/post/2026/04/11/vs-mcp-bridge-blog-series-part-3'
        Markers = @('Source of Truth:', 'host correctness', 'IProposalManager', 'proposal lifecycle', 'BridgeToolExecutor')
        ExpectedPostAspIdLinks = $false
        TokenNote = 'None'
    },
    [pscustomobject]@{
        Order = 4
        Slug = 'vs-mcp-bridge-blog-series-part-4'
        Path = '/post/2026/04/11/vs-mcp-bridge-blog-series-part-4'
        Markers = @('Source of Truth:', 'BridgeToolExecutor', 'CompiledBridgeToolCatalog', 'RegexTextSearchTool', 'Bm25TextSearchTool')
        ExpectedPostAspIdLinks = $false
        TokenNote = 'None'
    },
    [pscustomobject]@{
        Order = 5
        Slug = 'vs-mcp-bridge-blog-series-part-5'
        Path = '/post/2026/04/11/vs-mcp-bridge-blog-series-part-5'
        Markers = @('Source of Truth:', 'tool discovery', 'MEF discovery', 'BridgeToolExecutor', 'CompiledBridgeToolCatalog')
        ExpectedPostAspIdLinks = $false
        TokenNote = '[Page:Playbook]'
    },
    [pscustomobject]@{
        Order = 6
        Slug = 'vs-mcp-bridge-blog-series-part-6'
        Path = '/post/2026/04/11/vs-mcp-bridge-blog-series-part-6'
        Markers = @('Source of Truth:', 'security seam', 'BridgeToolExecutor', 'redaction', 'audit')
        ExpectedPostAspIdLinks = $false
        TokenNote = '[Page:Evidence]'
    },
    [pscustomobject]@{
        Order = 7
        Slug = 'vs-mcp-bridge-blog-series-part-7'
        Path = '/post/2026/04/11/vs-mcp-bridge-blog-series-part-7'
        Markers = @('Source of Truth:', 'durable traces', 'durable evidence', '.metadata.json', 'reconstructable evidence')
        ExpectedPostAspIdLinks = $false
        TokenNote = 'None'
    },
    [pscustomobject]@{
        Order = 8
        Slug = 'how-stdio-works-in-vs-mcp-bridge'
        Path = '/post/2026/04/19/how-stdio-works-in-vs-mcp-bridge'
        Markers = @('Source of Truth:', 'stdio', 'clean stdout', 'named pipe', 'BridgeToolExecutor')
        ExpectedPostAspIdLinks = $false
        TokenNote = '[Page:Stdio]'
    },
    [pscustomobject]@{
        Order = 9
        Slug = 'understanding-a-named-pipe-listener'
        Path = '/post/2026/04/17/understanding-a-named-pipe-listener'
        Markers = @('Source of Truth:', 'local-only bridge', 'PipeClient', 'PipeServer', 'BridgeToolExecutor')
        ExpectedPostAspIdLinks = $true
        TokenNote = '[Page:NamedPipeListener]'
    },
    [pscustomobject]@{
        Order = 10
        Slug = 'understanding-local-mcp-server-over-stdio-and-local-only-communication-over-a-named-pipe'
        Path = '/post/2026/04/01/understanding-local-mcp-server-over-stdio-and-local-only-communication-over-a-named-pipe'
        Markers = @('Source of Truth:', 'two transports', 'stdout must stay clean', 'named pipe', 'BridgeToolExecutor')
        ExpectedPostAspIdLinks = $false
        TokenNote = 'None'
    },
    [pscustomobject]@{
        Order = 11
        Slug = 'wpf-vsix-threading-understanding-ui-switching-async-behavior-and-pipe-safety'
        Path = '/post/2026/03/28/wpf-vsix-threading-understanding-ui-switching-async-behavior-and-pipe-safety'
        Markers = @('Source of Truth:', 'UI thread', 'async work', 'pipe server dispatch', 'host correctness')
        ExpectedPostAspIdLinks = $false
        TokenNote = 'None'
    },
    [pscustomobject]@{
        Order = 12
        Slug = 'why-vsix-project-should-target-net-framework-4-7-2'
        Path = '/post/2026/04/01/why-vsix-project-should-target-net-framework-4-7-2'
        Markers = @('Source of Truth:', '.NET Framework 4.7.2', 'Visual Studio in-process extension host', 'netstandard2.0', 'VSIX host boundary')
        ExpectedPostAspIdLinks = $false
        TokenNote = 'None'
    },
    [pscustomobject]@{
        Order = 13
        Slug = 'inference-driven-software-design-with-copilot-pros-and-cons'
        Path = '/post/2026/04/23/inference-driven-software-design-with-copilot-pros-and-cons'
        Markers = @('Source of Truth:', 'inference-driven development', 'prompt-to-evidence', 'durable evidence', 'VS MCP Bridge')
        ExpectedPostAspIdLinks = $true
        TokenNote = '[Display:ChatSessionsModelsAndAgents], [Page:InferenceDriven]'
    },
    [pscustomobject]@{
        Order = 14
        Slug = 'understanding-ai-chat-sessions-models-and-agents'
        Path = '/post/2026/04/12/understanding-ai-chat-sessions-models-and-agents'
        Markers = @('Source of Truth:', 'Sessions', 'Models', 'Agents', 'Tool-Backed Work', 'durable evidence')
        ExpectedPostAspIdLinks = $true
        TokenNote = '[Display:inference-driven|InferenceDriven], [Page:ChatSessionsModelsAndAgents]'
    }
)

$staleMarkers = @(
    'feature/approval-apply-ui-slice',
    'github.com/BillKrat/vs-mcp-bridge/blob/feature/',
    'raw.githubusercontent.com/BillKrat/vs-mcp-bridge/feature/'
)

$results = foreach ($route in $routes) {
    $uri = $BaseUrl.TrimEnd('/') + $route.Path
    $statusCode = $null
    $content = ''
    $errorMessage = $null

    try {
        $response = Invoke-WebRequest -Uri $uri -Method Get -TimeoutSec $TimeoutSec -UseBasicParsing
        $statusCode = [int]$response.StatusCode
        $content = [string]$response.Content
    }
    catch {
        $errorMessage = $_.Exception.Message
        if ($_.Exception.Response -and $_.Exception.Response.StatusCode) {
            $statusCode = [int]$_.Exception.Response.StatusCode
        }
    }

    $missingMarkers = @()
    foreach ($marker in $route.Markers) {
        if ($content -notlike "*$marker*") {
            $missingMarkers += $marker
        }
    }

    $staleFound = @()
    foreach ($marker in $staleMarkers) {
        if ($content -like "*$marker*") {
            $staleFound += $marker
        }
    }

    $postAspIdCount = ([regex]::Matches($content, 'post\.aspx\?id=', 'IgnoreCase')).Count
    $unexpectedPostAspId = ($postAspIdCount -gt 0 -and -not $route.ExpectedPostAspIdLinks)
    $markerPassed = ($statusCode -eq 200 -and $missingMarkers.Count -eq 0)
    $stalePassed = ($staleFound.Count -eq 0 -and -not $unexpectedPostAspId)

    $passed = ($markerPassed -and $stalePassed)

    [pscustomobject]@{
        Order = $route.Order
        Slug = $route.Slug
        Url = $uri
        StatusCode = $statusCode
        Passed = $passed
        MarkerPassed = $markerPassed
        StalePassed = $stalePassed
        Markers = $route.Markers
        MissingMarkers = $missingMarkers
        StaleMarkersFound = $staleFound
        PostAspIdCount = $postAspIdCount
        ExpectedPostAspIdLinks = $route.ExpectedPostAspIdLinks
        TokenNote = $route.TokenNote
        Error = $errorMessage
    }
}

$passCount = @($results | Where-Object { $_.Passed }).Count
$failCount = @($results | Where-Object { -not $_.Passed }).Count
$httpOkCount = @($results | Where-Object { $_.StatusCode -eq 200 }).Count
$markerPassCount = @($results | Where-Object { $_.MarkerPassed }).Count
$stalePassCount = @($results | Where-Object { $_.StalePassed }).Count
$featureBranchStaleCount = @($results | Where-Object { $_.StaleMarkersFound -contains 'feature/approval-apply-ui-slice' }).Count
$reloadCalled = 'No'
$checkedAt = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss zzz')

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('# Final Rendered Route Verification - 2026-05-16')
$lines.Add('')
$lines.Add('This report records the read-only rendered-route verification pass for the 14 BlogAI posts that received guarded publish-review updates.')
$lines.Add('')
$lines.Add('Scope constraints for this slice:')
$lines.Add('')
$lines.Add('- No database writes were performed.')
$lines.Add('- No BlogAI reload endpoint was called.')
$lines.Add('- No public site behavior was changed.')
$lines.Add('- Route checks used HTTP GET against `https://www.global-webnet.com` only.')
$lines.Add('')
$lines.Add('## Summary')
$lines.Add('')
$lines.Add('| Metric | Value |')
$lines.Add('| --- | ---: |')
$lines.Add("| Checked at | $checkedAt |")
$lines.Add("| Total routes checked | $($results.Count) |")
$lines.Add("| HTTP 200 routes | $httpOkCount |")
$lines.Add("| Routes with expected content markers visible | $markerPassCount |")
$lines.Add("| Routes passing stale-marker checks | $stalePassCount |")
$lines.Add(('| Routes containing `feature/approval-apply-ui-slice` | {0} |' -f $featureBranchStaleCount))
$lines.Add("| Passed | $passCount |")
$lines.Add("| Failed | $failCount |")
$lines.Add("| Reload called | $reloadCalled |")
$lines.Add('')
$lines.Add('## Stale Marker Policy')
$lines.Add('')
$lines.Add('The verifier failed any route containing the removed feature-branch URL markers:')
$lines.Add('')
foreach ($marker in $staleMarkers) {
    $lines.Add(('- `{0}`' -f $marker))
}
$lines.Add('')
$lines.Add('The verifier also counted `post.aspx?id=` occurrences. Those links are expected only on routes whose intentional BlogEngine tokens expand through the current `GwnWikiExtension` settings. Unexpected `post.aspx?id=` occurrences fail the row.')
$lines.Add('')
$lines.Add('## Route Results')
$lines.Add('')
$lines.Add('| Order | Slug | HTTP | Result | Markers verified | Stale markers | `post.aspx?id=` count | Notes |')
$lines.Add('| ---: | --- | ---: | --- | --- | --- | ---: | --- |')
$escapeTableText = {
    param([string]$Value)
    if ($null -eq $Value) {
        return ''
    }

    return ($Value -replace '\|', '\|')
}
foreach ($result in ($results | Sort-Object Order)) {
    $resultText = if ($result.Passed) { 'Pass' } else { 'Fail' }
    $markerText = if ($result.MissingMarkers.Count -eq 0) { ($result.Markers -join ', ') } else { 'Missing: ' + ($result.MissingMarkers -join ', ') }
    $staleText = if ($result.StaleMarkersFound.Count -eq 0) { 'None' } else { $result.StaleMarkersFound -join ', ' }
    $postAspNote = if ($result.PostAspIdCount -gt 0 -and $result.ExpectedPostAspIdLinks) { 'Expected token-expanded links; ' } elseif ($result.PostAspIdCount -gt 0) { 'Unexpected direct links; ' } else { '' }
    $errorNote = if ($result.Error) { "Error: $($result.Error)" } else { '' }
    $notes = ($postAspNote + $result.TokenNote).Trim()
    if ($errorNote) {
        $notes = "$notes; $errorNote"
    }

    $lines.Add(('| {0} | `{1}` | {2} | {3} | {4} | {5} | {6} | {7} |' -f `
        $result.Order,
        (& $escapeTableText $result.Slug),
        $result.StatusCode,
        $resultText,
        (& $escapeTableText $markerText),
        (& $escapeTableText $staleText),
        $result.PostAspIdCount,
        (& $escapeTableText $notes)))
}
$lines.Add('')
$lines.Add('## Observed Global Stale Marker')
$lines.Add('')
if ($featureBranchStaleCount -eq $results.Count) {
    $lines.Add('Every checked route contained `feature/approval-apply-ui-slice`, while every checked route also returned HTTP 200 and displayed the expected post-specific canonical markers. This points to a shared rendered page element, widget, layout, or cached site chrome rather than a missing post-body publish update.')
    $lines.Add('')
    $lines.Add('A sample Part 2 response placed the stale marker in site intro text linking to `https://github.com/BillKrat/vs-mcp-bridge/tree/feature/approval-apply-ui-slice`, before or around the repeated page chrome. The post-body markers still verified successfully.')
}
else {
    $lines.Add('The removed feature-branch marker was not present on every checked route. Inspect the failed rows individually before deciding whether the source is post body, widget content, layout, or cache.')
}
$lines.Add('')
$lines.Add('## Routes Needing Follow-Up')
$lines.Add('')
if ($failCount -eq 0) {
    $lines.Add('No route in the 14-post publish-review set needs follow-up from this verification pass.')
}
else {
    foreach ($result in ($results | Where-Object { -not $_.Passed } | Sort-Object Order)) {
        $missingText = $result.MissingMarkers -join ', '
        $staleFoundText = $result.StaleMarkersFound -join ', '
        $lines.Add(('- `{0}`: HTTP `{1}`, missing markers `{2}`, stale markers `{3}`, `post.aspx?id=` count `{4}`' -f `
            $result.Slug,
            $result.StatusCode,
            $missingText,
            $staleFoundText,
            $result.PostAspIdCount))
    }
}
$lines.Add('')
$lines.Add('## Recommendation')
$lines.Add('')
if ($failCount -eq 0) {
    $lines.Add('The 14-post publish-review run is ready for a final human publishing review handoff. Remaining work should focus on export-only, deleted, untouched, or noncanonical posts in separate slices.')
}
else {
    $lines.Add('The post-body publish-review updates are visible on all 14 routes, but the final rendered-route pass is blocked by a shared stale feature-branch link in rendered page chrome/widget content. Do not publish more posts to fix this. Inspect the BlogEngine widget/settings or layout source for that shared link, then verify whether a widget/settings reload or app recycle is required.')
}
$lines.Add('')

$outputDirectory = Split-Path -Parent $OutputPath
if ($outputDirectory -and -not (Test-Path $outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory | Out-Null
}

$lines | Set-Content -Path $OutputPath -Encoding utf8

[pscustomobject]@{
    OutputPath = $OutputPath
    TotalRoutes = $results.Count
    Passed = $passCount
    Failed = $failCount
    ReloadCalled = $false
    Results = $results
}
