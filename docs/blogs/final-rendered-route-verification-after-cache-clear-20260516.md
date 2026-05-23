# Final Rendered Route Verification After Cache Clear Attempt - 2026-05-16

Evidence Classification: Rendered Failure Evidence
Intended Use: Preserve observed rendered-site behavior after a controlled cache-clear attempt.
Search Interpretation: Stale marker hits here prove rendered/runtime failure evidence, not canonical source content.
Currentness: Point-in-time route verification from 2026-05-16.

This report records the controlled BlogEngine cache-clear attempt and the rendered-route verification pass for the 14 BlogAI posts that received guarded publish-review updates.

Scope constraints for this slice:

- No database writes were performed.
- No BlogAI reload endpoint was called.
- No posts were published.
- No app recycle or restart was performed.
- One BlogEngine cache-clear request was attempted against `https://www.global-webnet.com/api/settings?action=clearCache`.
- Route verification used HTTP GET against `https://www.global-webnet.com` only.

## Cache Clear Attempt

| Check | Result |
| --- | --- |
| Attempted | Yes |
| Method | `PUT` |
| Endpoint | `https://www.global-webnet.com/api/settings?action=clearCache` |
| Existing local auth/config used | `BlogEngineReloadKey` was present and sent as `X-Blog-Reload-Key`; the secret value was not logged. |
| HTTP status | `500 Internal Server Error` |
| Response body | Empty |
| App recycle/restart performed | No |
| Second cache-clear attempt performed | No |
| Database writes performed | No |

The cache-clear endpoint did not complete successfully. Per the slice constraint, no app recycle or restart was attempted.

## Summary

| Metric | Value |
| --- | ---: |
| Checked at | 2026-05-16 20:14:02 -05:00 |
| Total routes checked | 14 |
| HTTP 200 routes | 14 |
| Routes with expected content markers visible | 14 |
| Routes passing stale-marker checks | 0 |
| Routes containing `feature/approval-apply-ui-slice` | 14 |
| Passed | 0 |
| Failed | 14 |
| Reload called | No |
| App recycle/restart needed | Not performed; still likely needed or an authenticated cache clear must succeed. |

## Stale Marker Policy

The verifier failed any route containing the removed feature-branch URL markers:

- `feature/approval-apply-ui-slice`
- `github.com/BillKrat/vs-mcp-bridge/blob/feature/`
- `raw.githubusercontent.com/BillKrat/vs-mcp-bridge/feature/`

The verifier also counted `post.aspx?id=` occurrences. Those links are expected only on routes whose intentional BlogEngine tokens expand through the current `GwnWikiExtension` settings. Unexpected `post.aspx?id=` occurrences fail the row.

## Route Results

| Order | Slug | HTTP | Result | Markers verified | Stale markers | `post.aspx?id=` count | Notes |
| ---: | --- | ---: | --- | --- | --- | ---: | --- |
| 1 | `vs-mcp-bridge-blog-series-part-1` | 200 | Fail | Source of Truth:, BridgeToolExecutor, anti-black-box rule | feature/approval-apply-ui-slice | 2 | Expected token-expanded links; [NamedPipeListener], [Page:VS MCP Bridge\|VsMcpBridge], [Stdio] |
| 2 | `vs-mcp-bridge-blog-series-part-2` | 200 | Fail | Source of Truth:, observability became architecture, BridgeToolExecutor, tool-approval-trace-20260516.mmd | feature/approval-apply-ui-slice | 0 | None |
| 3 | `vs-mcp-bridge-blog-series-part-3` | 200 | Fail | Source of Truth:, host correctness, IProposalManager, proposal lifecycle, BridgeToolExecutor | feature/approval-apply-ui-slice | 0 | None |
| 4 | `vs-mcp-bridge-blog-series-part-4` | 200 | Fail | Source of Truth:, BridgeToolExecutor, CompiledBridgeToolCatalog, RegexTextSearchTool, Bm25TextSearchTool | feature/approval-apply-ui-slice | 0 | None |
| 5 | `vs-mcp-bridge-blog-series-part-5` | 200 | Fail | Source of Truth:, tool discovery, MEF discovery, BridgeToolExecutor, CompiledBridgeToolCatalog | feature/approval-apply-ui-slice | 0 | [Page:Playbook] |
| 6 | `vs-mcp-bridge-blog-series-part-6` | 200 | Fail | Source of Truth:, security seam, BridgeToolExecutor, redaction, audit | feature/approval-apply-ui-slice | 0 | [Page:Evidence] |
| 7 | `vs-mcp-bridge-blog-series-part-7` | 200 | Fail | Source of Truth:, durable traces, durable evidence, .metadata.json, reconstructable evidence | feature/approval-apply-ui-slice | 0 | None |
| 8 | `how-stdio-works-in-vs-mcp-bridge` | 200 | Fail | Source of Truth:, stdio, clean stdout, named pipe, BridgeToolExecutor | feature/approval-apply-ui-slice | 0 | [Page:Stdio] |
| 9 | `understanding-a-named-pipe-listener` | 200 | Fail | Source of Truth:, local-only bridge, PipeClient, PipeServer, BridgeToolExecutor | feature/approval-apply-ui-slice | 0 | [Page:NamedPipeListener] |
| 10 | `understanding-local-mcp-server-over-stdio-and-local-only-communication-over-a-named-pipe` | 200 | Fail | Source of Truth:, two transports, stdout must stay clean, named pipe, BridgeToolExecutor | feature/approval-apply-ui-slice | 0 | None |
| 11 | `wpf-vsix-threading-understanding-ui-switching-async-behavior-and-pipe-safety` | 200 | Fail | Source of Truth:, UI thread, async work, pipe server dispatch, host correctness | feature/approval-apply-ui-slice | 0 | None |
| 12 | `why-vsix-project-should-target-net-framework-4-7-2` | 200 | Fail | Source of Truth:, .NET Framework 4.7.2, Visual Studio in-process extension host, netstandard2.0, VSIX host boundary | feature/approval-apply-ui-slice | 0 | None |
| 13 | `inference-driven-software-design-with-copilot-pros-and-cons` | 200 | Fail | Source of Truth:, inference-driven development, prompt-to-evidence, durable evidence, VS MCP Bridge | feature/approval-apply-ui-slice | 1 | Expected token-expanded links; [Display:ChatSessionsModelsAndAgents], [Page:InferenceDriven] |
| 14 | `understanding-ai-chat-sessions-models-and-agents` | 200 | Fail | Source of Truth:, Sessions, Models, Agents, Tool-Backed Work, durable evidence | feature/approval-apply-ui-slice | 1 | Expected token-expanded links; [Display:inference-driven\|InferenceDriven], [Page:ChatSessionsModelsAndAgents] |

## Observed Global Stale Marker

Every checked route contained `feature/approval-apply-ui-slice`, while every checked route also returned HTTP 200 and displayed the expected post-specific canonical markers. This points to a shared rendered page element, widget, layout, or cached site chrome rather than a missing post-body publish update.

A sample Part 2 response placed the stale marker in site intro text linking to `https://github.com/BillKrat/vs-mcp-bridge/tree/feature/approval-apply-ui-slice`, before or around the repeated page chrome. The post-body markers still verified successfully.

## Routes Needing Follow-Up

- `vs-mcp-bridge-blog-series-part-1`: HTTP `200`, missing markers ``, stale markers `feature/approval-apply-ui-slice`, `post.aspx?id=` count `2`
- `vs-mcp-bridge-blog-series-part-2`: HTTP `200`, missing markers ``, stale markers `feature/approval-apply-ui-slice`, `post.aspx?id=` count `0`
- `vs-mcp-bridge-blog-series-part-3`: HTTP `200`, missing markers ``, stale markers `feature/approval-apply-ui-slice`, `post.aspx?id=` count `0`
- `vs-mcp-bridge-blog-series-part-4`: HTTP `200`, missing markers ``, stale markers `feature/approval-apply-ui-slice`, `post.aspx?id=` count `0`
- `vs-mcp-bridge-blog-series-part-5`: HTTP `200`, missing markers ``, stale markers `feature/approval-apply-ui-slice`, `post.aspx?id=` count `0`
- `vs-mcp-bridge-blog-series-part-6`: HTTP `200`, missing markers ``, stale markers `feature/approval-apply-ui-slice`, `post.aspx?id=` count `0`
- `vs-mcp-bridge-blog-series-part-7`: HTTP `200`, missing markers ``, stale markers `feature/approval-apply-ui-slice`, `post.aspx?id=` count `0`
- `how-stdio-works-in-vs-mcp-bridge`: HTTP `200`, missing markers ``, stale markers `feature/approval-apply-ui-slice`, `post.aspx?id=` count `0`
- `understanding-a-named-pipe-listener`: HTTP `200`, missing markers ``, stale markers `feature/approval-apply-ui-slice`, `post.aspx?id=` count `0`
- `understanding-local-mcp-server-over-stdio-and-local-only-communication-over-a-named-pipe`: HTTP `200`, missing markers ``, stale markers `feature/approval-apply-ui-slice`, `post.aspx?id=` count `0`
- `wpf-vsix-threading-understanding-ui-switching-async-behavior-and-pipe-safety`: HTTP `200`, missing markers ``, stale markers `feature/approval-apply-ui-slice`, `post.aspx?id=` count `0`
- `why-vsix-project-should-target-net-framework-4-7-2`: HTTP `200`, missing markers ``, stale markers `feature/approval-apply-ui-slice`, `post.aspx?id=` count `0`
- `inference-driven-software-design-with-copilot-pros-and-cons`: HTTP `200`, missing markers ``, stale markers `feature/approval-apply-ui-slice`, `post.aspx?id=` count `1`
- `understanding-ai-chat-sessions-models-and-agents`: HTTP `200`, missing markers ``, stale markers `feature/approval-apply-ui-slice`, `post.aspx?id=` count `1`

## Recommendation

The post-body publish-review updates remain visible on all 14 routes, but the rendered-route pass is still blocked by the shared stale feature-branch link in cached widget/page chrome content.

Do not publish more posts to fix this. The cache clear request returned HTTP 500, so the remaining remediation is an authenticated BlogEngine cache clear through a known-good admin path, a source fix for the deployed cache-clear endpoint, or an explicitly approved app recycle/restart.

Final publishing status for the 14 post bodies: ready for human review from a post-content perspective, blocked from final rendered-site signoff by stale shared widget/cache content.

