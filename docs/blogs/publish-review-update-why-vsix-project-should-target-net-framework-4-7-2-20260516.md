# Publish Review Update - why-vsix-project-should-target-net-framework-4-7-2 - 2026-05-16

## Scope

This report records a single-post BlogEngine body update from canonical repo source.
The update was intentionally narrow: one existing `dbo.be_Posts` row was updated, no category/tag tables were touched, and the update script itself did not call the reload endpoint.

## Result Summary

| Check | Result |
| --- | --- |
| Slug | why-vsix-project-should-target-net-framework-4-7-2 |
| DB PostRowID | 141 |
| DB BlogID | 27604f05-86ad-47ef-9e05-950bb762570c |
| DB PostID | ae00d3f4-7c9a-4084-b690-d974e945d69e |
| Rows updated | 1 |
| Fields intentionally changed | Description, PostContent, DateModified |
| Identity fields preserved | True |
| Publication state preserved | True |
| Taxonomy preserved | True |
| Updated body matches canonical | True |
| Updated description matches canonical | True |
| BlogEngine tokens preserved | True |
| Stale direct-link check passed | True |
| Before export | Y:\vs-mcp-bridge\docs\blogs\source-of-truth\publish-review-updates\20260516\why-vsix-project-should-target-net-framework-4-7-2-20260516-183615\before |
| After export | Y:\vs-mcp-bridge\docs\blogs\source-of-truth\publish-review-updates\20260516\why-vsix-project-should-target-net-framework-4-7-2-20260516-183615\after |
| Rendered route check | Rendered route responded, but canonical-only markers were not all visible; content may still be cached or rendered differently. |
| BlogAI reload result | HTTP 200, body `true` |
| Rendered route after reload | Updated canonical markers visible |

## Before And After

| Field | Before | After |
| --- | --- | --- |
| Title | Why a VSIX Project Should Target .NET Framework 4.7.2 | Why a VSIX Project Should Target .NET Framework 4.7.2 |
| Description | When working with Visual Studio extensions, one detail that is easy to miss is that the extension pr | Why VS MCP Bridge keeps the VSIX project on .NET Framework 4.7.2 while separating Visual Studio host code from shared, testable bridge infrastructure and out-of-process MCP runtime components. |
| Status | published | published |
| IsPublished | True | True |
| DateCreated | 2026-04-01T17:02:00.000 | 2026-04-01T17:02:00.000 |
| DateModified | 2026-04-01T10:03:20.493 | 2026-05-16T16:36:16.203 |
| Content SHA-256 | bcb944aa8b3eb63ef09bffde2354c349361181102619801774d418354f29824a | 7a6f72e937ab406e22726a901f5ed9d0c69d3e4165c72e24b2143b3da1c57d70 |
| Categories | AI Systems Author | AI Systems Author |
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
| URL | https://www.global-webnet.com/post/2026/04/01/why-vsix-project-should-target-net-framework-4-7-2 |
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
https://www.global-webnet.com/post/2026/04/01/why-vsix-project-should-target-net-framework-4-7-2
```

| Marker | Result |
| --- | --- |
| `Source of Truth:` | True |
| `.NET Framework 4.7.2` | True |
| `Visual Studio in-process extension host` | True |
| `netstandard2.0` | True |
| `VSIX host boundary` | True |

The suggested markers `Visual Studio extension host` and `host isolation` were not present as exact phrases in the canonical body. The canonical text uses `Visual Studio in-process extension host` and `VSIX host boundary` for those concepts, and both markers were visible after reload.

## Recommended Next Slice

Proceed to `inference-driven-software-design-with-copilot-pros-and-cons`, the next safe row in the ready-post compare where current DB still matched the preserved export baseline.
