# Publish Review Update - wpf-vsix-threading-understanding-ui-switching-async-behavior-and-pipe-safety - 2026-05-16

## Scope

This report records a single-post BlogEngine body update from canonical repo source.
The update was intentionally narrow: one existing `dbo.be_Posts` row was updated, no category/tag tables were touched, and the update script itself did not call the reload endpoint.

## Result Summary

| Check | Result |
| --- | --- |
| Slug | wpf-vsix-threading-understanding-ui-switching-async-behavior-and-pipe-safety |
| DB PostRowID | 135 |
| DB BlogID | 27604f05-86ad-47ef-9e05-950bb762570c |
| DB PostID | 46828793-1f3d-4031-906e-87c1c31dce7e |
| Rows updated | 1 |
| Fields intentionally changed | Description, PostContent, DateModified |
| Identity fields preserved | True |
| Publication state preserved | True |
| Taxonomy preserved | True |
| Updated body matches canonical | True |
| Updated description matches canonical | True |
| BlogEngine tokens preserved | True |
| Stale direct-link check passed | True |
| Before export | Y:\vs-mcp-bridge\docs\blogs\source-of-truth\publish-review-updates\20260516\wpf-vsix-threading-understanding-ui-switching-async-behavior-and-pipe-safety-20260516-183230\before |
| After export | Y:\vs-mcp-bridge\docs\blogs\source-of-truth\publish-review-updates\20260516\wpf-vsix-threading-understanding-ui-switching-async-behavior-and-pipe-safety-20260516-183230\after |
| Rendered route check | Rendered route responded, but canonical-only markers were not all visible; content may still be cached or rendered differently. |
| BlogAI reload result | HTTP 200, body `true` |
| Rendered route after reload | Updated canonical markers visible |

## Before And After

| Field | Before | After |
| --- | --- | --- |
| Title | WPF VSIX Threading: Understanding UI Switching, Async Behavior, and Pipe Safety | WPF VSIX Threading: Understanding UI Switching, Async Behavior, and Pipe Safety |
| Description | When building a WPF-based Visual Studio extension (VSIX), threading issues are one of the most commo | How VS MCP Bridge uses intentional UI-thread switching, async boundaries, named-pipe safety, clean stdio, proposal lifecycle ownership, and observable diagnostics to keep AI-assisted Visual Studio tooling reliable. |
| Status | published | published |
| IsPublished | True | True |
| DateCreated | 2026-03-28T11:41:00.000 | 2026-03-28T11:41:00.000 |
| DateModified | 2026-03-28T04:46:13.027 | 2026-05-16T16:32:31.637 |
| Content SHA-256 | db251035d359048dcc6b1f28fe5bf6a9ae07ca616a8d2adcf431da2490ea9fe1 | f0399ffb2aae84c8f57323ba497ab183381439912b8459f1a9315c008370937e |
| Categories | None | None |
| Tags | None | None |

## Taxonomy Decision

Taxonomy was preserved from the live DB row for this body publish. Canonical `post.json` categories and tags were not applied in this slice because the goal was the smallest safe mutation for a single-post review update.

## Tokens And Links

| Check | Value |
| --- | --- |
| Canonical BlogEngine tokens | None |
| After-update BlogEngine tokens | None |
| After-update stale direct links | None |

## Rendered Route

| Field | Value |
| --- | --- |
| URL | https://www.global-webnet.com/post/2026/03/28/wpf-vsix-threading-understanding-ui-switching-async-behavior-and-pipe-safety |
| Attempted | True |
| HTTP status | 200 |
| Canonical markers visible | False |
| Result | Rendered route responded, but canonical-only markers were not all visible; content may still be cached or rendered differently. |

## BlogAI Reload And Final Render Verification

After the guarded DB update and before moving to any other post, the BlogAI development reload endpoint was called once:

```text
POST https://www.global-webnet.com/api/posts/reload/27604f05-86ad-47ef-9e05-950bb762570c
X-Blog-Reload-Key: provided from local BlogEngineReloadKey environment variable
```

Reload result:

```text
HTTP 200
Body: true
```

Rendered route after reload:

```text
https://www.global-webnet.com/post/2026/03/28/wpf-vsix-threading-understanding-ui-switching-async-behavior-and-pipe-safety
```

| Marker | Result |
| --- | --- |
| `Source of Truth:` | True |
| `UI thread` | True |
| `async work` | True |
| `pipe server dispatch` | True |
| `host correctness` | True |

The suggested markers `async boundaries` and `PipeServer` were not present as exact phrases in the canonical body. The canonical text uses `async work` and `pipe server dispatch` for those concepts, and both markers were visible after reload.

## Recommended Next Slice

Proceed to `why-vsix-project-should-target-net-framework-4-7-2`, the next safe row in the ready-post compare where current DB still matched the preserved export baseline.
