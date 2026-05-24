# Publish Review Update - understanding-local-mcp-server-over-stdio-and-local-only-communication-over-a-named-pipe - 2026-05-16

## Scope

This report records a single-post BlogEngine body update from canonical repo source.
The update was intentionally narrow: one existing `dbo.be_Posts` row was updated, no category/tag tables were touched, and the update script itself did not call the reload endpoint.

## Result Summary

| Check | Result |
| --- | --- |
| Slug | understanding-local-mcp-server-over-stdio-and-local-only-communication-over-a-named-pipe |
| DB PostRowID | 142 |
| DB BlogID | 27604f05-86ad-47ef-9e05-950bb762570c |
| DB PostID | 43bf6aae-15c9-4c90-b3b2-66ac51c4a7c8 |
| Rows updated | 1 |
| Fields intentionally changed | Description, PostContent, DateModified |
| Identity fields preserved | True |
| Publication state preserved | True |
| Taxonomy preserved | True |
| Updated body matches canonical | True |
| Updated description matches canonical | True |
| BlogEngine tokens preserved | True |
| Stale direct-link check passed | True |
| Before export | Y:\vs-mcp-bridge\docs\blogs\source-of-truth\publish-review-updates\20260516\understanding-local-mcp-server-over-stdio-and-local-only-communication-over-a-named-pipe-20260516-182837\before |
| After export | Y:\vs-mcp-bridge\docs\blogs\source-of-truth\publish-review-updates\20260516\understanding-local-mcp-server-over-stdio-and-local-only-communication-over-a-named-pipe-20260516-182837\after |
| Rendered route check | Rendered route responded, but canonical-only markers were not all visible; content may still be cached or rendered differently. |
| BlogAI reload result | HTTP 200, body `true` |
| Rendered route after reload | Updated canonical markers visible |

## Before And After

| Field | Before | After |
| --- | --- | --- |
| Title | Understanding a Local MCP Server Over Stdio and Local-Only Communication Over a Named Pipe | Understanding a Local MCP Server Over Stdio and Local-Only Communication Over a Named Pipe |
| Description | When integrating systems inside Visual Studio, two communication patterns often appear early in the  | A concise explanation of how VS MCP Bridge combines MCP over stdio with local named-pipe communication to keep AI protocol handling, Visual Studio integration, diagnostics, and tool execution boundaries separate. |
| Status | published | published |
| IsPublished | True | True |
| DateCreated | 2026-04-01T17:04:00.000 | 2026-04-01T17:04:00.000 |
| DateModified | 2026-04-12T10:31:23.680 | 2026-05-16T16:28:38.073 |
| Content SHA-256 | 87d475d7636d2396380b4baa08f29ea4300414a3e7dd5f040e6dedd63a23742b | 076638fe92631fb5f79f8df271cc8148e0ebb3473faec4dfcc75b9a8c0d43c8c |
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
| URL | https://www.global-webnet.com/post/2026/04/01/understanding-local-mcp-server-over-stdio-and-local-only-communication-over-a-named-pipe |
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
https://www.global-webnet.com/post/2026/04/01/understanding-local-mcp-server-over-stdio-and-local-only-communication-over-a-named-pipe
```

| Marker | Result |
| --- | --- |
| `Source of Truth:` | True |
| `two transports` | True |
| `stdout must stay clean` | True |
| `named pipe` | True |
| `BridgeToolExecutor` | True |

The suggested marker `clean stdout` was not present as an exact phrase in the canonical body. The canonical text uses `stdout must stay clean` for that concept, and that marker was visible after reload.

## Recommended Next Slice

Proceed to `wpf-vsix-threading-understanding-ui-switching-async-behavior-and-pipe-safety`, the next safe row in the ready-post compare where current DB still matched the preserved export baseline.
