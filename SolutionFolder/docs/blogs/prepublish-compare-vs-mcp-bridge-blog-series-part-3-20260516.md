# Pre-Publish Compare - vs-mcp-bridge-blog-series-part-3 - 2026-05-16

## Scope

This report compares the current live BlogEngine database row, the preserved `db-export-20260516` baseline, and the canonical repo post before any publish operation.
The compare script is read-only: it performs a parameterized `SELECT`, reads repository files, and writes this report.
It does not update the database, call reload endpoints, or change public site behavior.

## Result Summary

| Check | Result |
| --- | --- |
| Slug | vs-mcp-bridge-blog-series-part-3 |
| DB PostID | 78dbd347-397e-4185-b6d5-d67558cc06be |
| DB BlogID | 27604f05-86ad-47ef-9e05-950bb762570c |
| DB PostRowID | 145 |
| Current DB DateModified | 2026-05-12T18:00:24.280 |
| Preserved export timestamp | 2026-05-16T20:56:27.197Z |
| Preserved export DateModified | 2026-05-12T18:00:24.280 |
| Canonical repo post path | Y:\vs-mcp-bridge\docs\blogs\posts\vs-mcp-bridge-blog-series-part-3 |
| Current DB matches preserved export | False |
| Current DB content matches preserved export content | True |
| Current DB metadata matches preserved export metadata | False |
| Canonical content differs from current DB content | True |
| Canonical stale direct links found | 0 |
| Recommended safety decision | Do not publish yet: current DB no longer matches the preserved export baseline. Re-export or review live edits before overwrite. |

## Content Hashes

| Source | SHA-256 |
| --- | --- |
| Current live DB content | 821ca8dbd244f37df6485abde8de6347a34bd6a08ea460c26e67a36952d7ee06 |
| Preserved export content | 821ca8dbd244f37df6485abde8de6347a34bd6a08ea460c26e67a36952d7ee06 |
| Canonical repo content | 6ee4125b9c1ed9630a910786442c3b2fe2092e05028b129425d0a9bde492458e |

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
| tags | True |

## Current DB Metadata

| Field | Value |
| --- | --- |
| Title | VS MCP Bridge Blog Series: Part 3 |
| Description | Async Work, Approval Flow, and UI Thread Safety. This post continues the developer ramp-up series for VS MCP Bridge and explains where async behavior matters in the current implementation, what must stay on the UI thread, and how approval flow stays predictable.<br><br>One small correction: the current slug field you showed, vs-mcp-bridge-blog-series-part, is too generic. It will make all posts look the same and will not identify Part 3 cleanly. I recommend using the numbered slug above. |
| Author | AI Systems Author |
| Status | published |
| IsPublished | True |
| IsDeleted | False |
| AllowComments | True |
| Categories | AI Systems Author, MCP Bridge |
| Tags | None |

## Canonical Repo Metadata

| Field | Value |
| --- | --- |
| Title | VS MCP Bridge Blog Series: Part 3 |
| Description | How VS MCP Bridge keeps AI-assisted workflows host-correct through Visual Studio threading discipline, UI state ownership, IProposalManager, approval callbacks, completed previews, and reset/new-chat cleanup. |
| Author | AI Systems Author |
| Slug | vs-mcp-bridge-blog-series-part-3 |
| BlogID | 27604F05-86AD-47EF-9E05-950BB762570C |
| PostID | 78DBD347-397E-4185-B6D5-D67558CC06BE |
| IsPublished | True |
| AllowComments | True |
| Categories | AI Systems Author, MCP Bridge |
| Tags | VS MCP Bridge, Visual Studio, VSIX, MCP, Approval, Proposal Lifecycle, UI Threading, Diagnostics, Architecture |

## Intentional BlogEngine Tokens

| Source | Tokens |
| --- | --- |
| Current DB content | None |
| Canonical repo content | None |

Preserve these tokens unless a separate `GwnWikiExtension` mapping decision is made.

## Stale Link Checks

Checked for:

- `feature/approval-apply-ui-slice`
- direct `adventuresontheedge.net` URLs
- direct `post.aspx?id=...` URLs

| Source | Matches |
| --- | --- |
| Current DB content | https://github.com/BillKrat/vs-mcp-bridge/blob/feature/approval-apply-ui-slice/SolutionFolder/docs/ARCHITECTURE.md |
| Preserved export content | https://github.com/BillKrat/vs-mcp-bridge/blob/feature/approval-apply-ui-slice/SolutionFolder/docs/ARCHITECTURE.md |
| Canonical repo content | None |

## Publish Safety Recommendation

Do not publish yet: current DB no longer matches the preserved export baseline. Re-export or review live edits before overwrite.

If publishing proceeds, use the draft-only workflow first and verify runtime rendering before touching the next post.