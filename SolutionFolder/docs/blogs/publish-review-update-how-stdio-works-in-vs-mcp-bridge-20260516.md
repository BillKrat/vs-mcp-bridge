# Publish Review Update - how-stdio-works-in-vs-mcp-bridge - 2026-05-16

## Scope

This report records a single-post BlogEngine body update from canonical repo source.
The update was intentionally narrow: one existing `dbo.be_Posts` row was updated, no category/tag tables were touched, and the update script itself did not call the reload endpoint.

## Result Summary

| Check | Result |
| --- | --- |
| Slug | how-stdio-works-in-vs-mcp-bridge |
| DB PostRowID | 155 |
| DB BlogID | 27604f05-86ad-47ef-9e05-950bb762570c |
| DB PostID | d0541943-0de1-4c25-a7af-9950c55f1591 |
| Rows updated | 1 |
| Fields intentionally changed | Description, PostContent, DateModified |
| Identity fields preserved | True |
| Publication state preserved | True |
| Taxonomy preserved | True |
| Updated body matches canonical | True |
| Updated description matches canonical | True |
| BlogEngine tokens preserved | True |
| Stale direct-link check passed | True |
| Before export | Y:\vs-mcp-bridge\docs\blogs\source-of-truth\publish-review-updates\20260516\how-stdio-works-in-vs-mcp-bridge-20260516-195132\before |
| After export | Y:\vs-mcp-bridge\docs\blogs\source-of-truth\publish-review-updates\20260516\how-stdio-works-in-vs-mcp-bridge-20260516-195132\after |
| Rendered route check | Rendered route responded, but canonical-only markers were not all visible; content may still be cached or rendered differently. |
| BlogAI reload result | HTTP 200, body `true` |
| Rendered route after reload | Updated canonical markers visible |
| Required `[Page:Stdio]` token preserved in DB body | True |

## Before And After

| Field | Before | After |
| --- | --- | --- |
| Title | How stdio Works in VS MCP Bridge | How stdio Works in VS MCP Bridge |
| Description | A practical walkthrough of how the VS MCP Bridge MCP server uses stdio, where that transport begins and ends, and how work then crosses into Visual Studio through the named-pipe boundary. | A practical walkthrough of how VS MCP Bridge uses stdio as the MCP transport boundary, keeps stdout clean, and routes VS-backed work through the named-pipe activation boundary. |
| Status | published | published |
| IsPublished | True | True |
| DateCreated | 2026-04-19T23:24:00.000 | 2026-04-19T23:24:00.000 |
| DateModified | 2026-04-27T10:21:07.833 | 2026-05-16T17:51:32.807 |
| Content SHA-256 | 27874b495d02eb1a60865210b54837ac0f6359c33fe15430f4f99a130296ae81 | 29d6e0fb2554f601703e83becda8d577be5546dd1f97b142951853b696f84ea7 |
| Categories | AI Systems Author, MCP Bridge | AI Systems Author, MCP Bridge |
| Tags | AI Tooling, MCP, Named Pipes, stdio, Visual Studio, VS MCP Bridge, VSIX | AI Tooling, MCP, Named Pipes, stdio, Visual Studio, VS MCP Bridge, VSIX |

## Taxonomy Decision

Taxonomy was preserved from the live DB row for this body publish. Canonical `post.json` categories and tags were not applied in this slice because the goal was the smallest safe mutation for a single-post review update.

## Tokens And Links

| Check | Value |
| --- | --- |
| Canonical BlogEngine tokens | [Page:Stdio] |
| After-update BlogEngine tokens | [Page:Stdio] |
| After-update stale direct links | None |

## Rendered Route

| Field | Value |
| --- | --- |
| URL | https://www.global-webnet.com/post/2026/04/19/how-stdio-works-in-vs-mcp-bridge |
| Attempted | True |
| HTTP status | 200 |
| Canonical markers visible | False |
| Result | Rendered route responded, but canonical-only markers were not all visible; content may still be cached or rendered differently. |

The rendered route check above was performed by the update script before the BlogAI post-cache reload.
The route was then verified again after the reload endpoint returned success.

## BlogAI Reload And Final Render Verification

After the database write, the BlogAI development post-cache reload endpoint was called once:

```text
POST https://www.global-webnet.com/api/posts/reload/27604f05-86ad-47ef-9e05-950bb762570c
```

Result:

```text
HTTP 200
Body: true
```

The rendered route was then verified:

```text
https://www.global-webnet.com/post/2026/04/19/how-stdio-works-in-vs-mcp-bridge
```

| Marker | Visible after reload |
| --- | --- |
| `Source of Truth:` | True |
| `stdio` | True |
| `clean stdout` | True |
| `named pipe` | True |
| `BridgeToolExecutor` | True |

Read-only DB verification after reload confirmed:

| Check | Result |
| --- | --- |
| DB body matches canonical content | True |
| DB body contains `[Page:Stdio]` | True |
| DB body SHA-256 | `29d6e0fb2554f601703e83becda8d577be5546dd1f97b142951853b696f84ea7` |
| Canonical body SHA-256 | `29d6e0fb2554f601703e83becda8d577be5546dd1f97b142951853b696f84ea7` |
| PostRowID | `155` |
| PostID | `d0541943-0de1-4c25-a7af-9950c55f1591` |
| Slug | `how-stdio-works-in-vs-mcp-bridge` |

## Recommended Next Slice

Run a publishing status reconciliation across the 14 ready posts, then decide whether any remaining unreviewed/blocked row needs inspection or whether the next slice should move to post-publish verification/reporting.
