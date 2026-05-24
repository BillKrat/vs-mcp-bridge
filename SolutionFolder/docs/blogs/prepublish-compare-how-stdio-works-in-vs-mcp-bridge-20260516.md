# Pre-Publish Compare - how-stdio-works-in-vs-mcp-bridge - 2026-05-16

## Scope

This report compares the current live BlogEngine database row, the preserved `db-export-20260516` baseline, and the canonical repo post before any publish operation.
The compare script is read-only: it performs a parameterized `SELECT`, reads repository files, and writes this report.
It does not update the database, call reload endpoints, or change public site behavior.

## Result Summary

| Check | Result |
| --- | --- |
| Slug | how-stdio-works-in-vs-mcp-bridge |
| DB PostID | d0541943-0de1-4c25-a7af-9950c55f1591 |
| DB BlogID | 27604f05-86ad-47ef-9e05-950bb762570c |
| DB PostRowID | 155 |
| Current DB DateModified | 2026-04-27T10:21:07.833 |
| Preserved export timestamp | 2026-05-16T20:56:27.197Z |
| Preserved export DateModified | 2026-04-27T10:21:07.833 |
| Canonical repo post path | Y:\vs-mcp-bridge\docs\blogs\posts\how-stdio-works-in-vs-mcp-bridge |
| Current DB matches preserved export | False |
| Current DB content matches preserved export content | True |
| Current DB metadata matches preserved export metadata | False |
| Canonical content differs from current DB content | True |
| Canonical stale direct links found | 0 |
| Recommended safety decision | Do not publish yet: current DB no longer matches the preserved export baseline. Re-export or review live edits before overwrite. |

## Content Hashes

| Source | SHA-256 |
| --- | --- |
| Current live DB content | 27874b495d02eb1a60865210b54837ac0f6359c33fe15430f4f99a130296ae81 |
| Preserved export content | 27874b495d02eb1a60865210b54837ac0f6359c33fe15430f4f99a130296ae81 |
| Canonical repo content | 29d6e0fb2554f601703e83becda8d577be5546dd1f97b142951853b696f84ea7 |

## Metadata Baseline Checks

| Metadata field | Current DB matches preserved export |
| --- | --- |
| postRowId | True |
| blogId | True |
| postId | True |
| title | True |
| description | True |
| author | True |
| slug | True |
| status | True |
| isPublished | True |
| isDeleted | True |
| allowComments | True |
| dateCreated | True |
| dateModified | True |
| categories | False |
| tags | False |

## Current DB Metadata

| Field | Value |
| --- | --- |
| Title | How stdio Works in VS MCP Bridge |
| Description | A practical walkthrough of how the VS MCP Bridge MCP server uses stdio, where that transport begins and ends, and how work then crosses into Visual Studio through the named-pipe boundary. |
| Author | AI Systems Author |
| Status | published |
| IsPublished | True |
| IsDeleted | False |
| AllowComments | True |
| Categories | AI Systems Author, MCP Bridge |
| Tags | AI Tooling, MCP, Named Pipes, stdio, Visual Studio, VS MCP Bridge, VSIX |

## Canonical Repo Metadata

| Field | Value |
| --- | --- |
| Title | How stdio Works in VS MCP Bridge |
| Description | A practical walkthrough of how VS MCP Bridge uses stdio as the MCP transport boundary, keeps stdout clean, and routes VS-backed work through the named-pipe activation boundary. |
| Author | AI Systems Author |
| Slug | how-stdio-works-in-vs-mcp-bridge |
| BlogID | 27604F05-86AD-47EF-9E05-950BB762570C |
| PostID | D0541943-0DE1-4C25-A7AF-9950C55F1591 |
| IsPublished | True |
| AllowComments | True |
| Categories | AI Systems Author, MCP Bridge |
| Tags | VS MCP Bridge, MCP, stdio, Named Pipes, Visual Studio, VSIX, AI Tooling, Diagnostics |

## Intentional BlogEngine Tokens

| Source | Tokens |
| --- | --- |
| Current DB content | [Page:Stdio] |
| Canonical repo content | [Page:Stdio] |

Preserve these tokens unless a separate `GwnWikiExtension` mapping decision is made.

## Stale Link Checks

Checked for:

- `feature/approval-apply-ui-slice`
- direct `adventuresontheedge.net` URLs
- direct `post.aspx?id=...` URLs

| Source | Matches |
| --- | --- |
| Current DB content | None |
| Preserved export content | None |
| Canonical repo content | None |

## Publish Safety Recommendation

Do not publish yet: current DB no longer matches the preserved export baseline. Re-export or review live edits before overwrite.

If publishing proceeds, use the draft-only workflow first and verify runtime rendering before touching the next post.