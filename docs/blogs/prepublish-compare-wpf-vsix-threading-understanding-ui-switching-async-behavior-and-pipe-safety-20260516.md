# Pre-Publish Compare - wpf-vsix-threading-understanding-ui-switching-async-behavior-and-pipe-safety - 2026-05-16

## Scope

This report compares the current live BlogEngine database row, the preserved `db-export-20260516` baseline, and the canonical repo post before any publish operation.
The compare script is read-only: it performs a parameterized `SELECT`, reads repository files, and writes this report.
It does not update the database, call reload endpoints, or change public site behavior.

## Result Summary

| Check | Result |
| --- | --- |
| Slug | wpf-vsix-threading-understanding-ui-switching-async-behavior-and-pipe-safety |
| DB PostID | 46828793-1f3d-4031-906e-87c1c31dce7e |
| DB BlogID | 27604f05-86ad-47ef-9e05-950bb762570c |
| DB PostRowID | 135 |
| Current DB DateModified | 2026-03-28T04:46:13.027 |
| Preserved export timestamp | 2026-05-16T20:56:27.197Z |
| Preserved export DateModified | 2026-03-28T04:46:13.027 |
| Canonical repo post path | Y:\vs-mcp-bridge\docs\blogs\posts\wpf-vsix-threading-understanding-ui-switching-async-behavior-and-pipe-safety |
| Current DB matches preserved export | True |
| Current DB content matches preserved export content | True |
| Current DB metadata matches preserved export metadata | True |
| Canonical content differs from current DB content | True |
| Canonical stale direct links found | 0 |
| Recommended safety decision | Safe for human draft-publish review: current DB still matches preserved export, and canonical content intentionally differs from runtime baseline. |

## Content Hashes

| Source | SHA-256 |
| --- | --- |
| Current live DB content | db251035d359048dcc6b1f28fe5bf6a9ae07ca616a8d2adcf431da2490ea9fe1 |
| Preserved export content | db251035d359048dcc6b1f28fe5bf6a9ae07ca616a8d2adcf431da2490ea9fe1 |
| Canonical repo content | f0399ffb2aae84c8f57323ba497ab183381439912b8459f1a9315c008370937e |

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
| categories | True |
| tags | True |

## Current DB Metadata

| Field | Value |
| --- | --- |
| Title | WPF VSIX Threading: Understanding UI Switching, Async Behavior, and Pipe Safety |
| Description | When building a WPF-based Visual Studio extension (VSIX), threading issues are one of the most commo |
| Author | BillKrat |
| Status | published |
| IsPublished | True |
| IsDeleted | False |
| AllowComments | True |
| Categories | None |
| Tags | None |

## Canonical Repo Metadata

| Field | Value |
| --- | --- |
| Title | WPF VSIX Threading: Understanding UI Switching, Async Behavior, and Pipe Safety |
| Description | How VS MCP Bridge uses intentional UI-thread switching, async boundaries, named-pipe safety, clean stdio, proposal lifecycle ownership, and observable diagnostics to keep AI-assisted Visual Studio tooling reliable. |
| Author | BillKrat |
| Slug | wpf-vsix-threading-understanding-ui-switching-async-behavior-and-pipe-safety |
| BlogID | 27604F05-86AD-47EF-9E05-950BB762570C |
| PostID | 46828793-1F3D-4031-906E-87C1C31DCE7E |
| IsPublished | True |
| AllowComments | True |
| Categories | None |
| Tags | VSIX, WPF, Threading, Visual Studio, Async, Named Pipes, VS MCP Bridge, Diagnostics, Architecture |

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
| Current DB content | None |
| Preserved export content | None |
| Canonical repo content | None |

## Publish Safety Recommendation

Safe for human draft-publish review: current DB still matches preserved export, and canonical content intentionally differs from runtime baseline.

If publishing proceeds, use the draft-only workflow first and verify runtime rendering before touching the next post.