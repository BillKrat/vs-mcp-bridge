# Pre-Publish Compare - why-vsix-project-should-target-net-framework-4-7-2 - 2026-05-16

## Scope

This report compares the current live BlogEngine database row, the preserved `db-export-20260516` baseline, and the canonical repo post before any publish operation.
The compare script is read-only: it performs a parameterized `SELECT`, reads repository files, and writes this report.
It does not update the database, call reload endpoints, or change public site behavior.

## Result Summary

| Check | Result |
| --- | --- |
| Slug | why-vsix-project-should-target-net-framework-4-7-2 |
| DB PostID | ae00d3f4-7c9a-4084-b690-d974e945d69e |
| DB BlogID | 27604f05-86ad-47ef-9e05-950bb762570c |
| DB PostRowID | 141 |
| Current DB DateModified | 2026-04-01T10:03:20.493 |
| Preserved export timestamp | 2026-05-16T20:56:27.197Z |
| Preserved export DateModified | 2026-04-01T10:03:20.493 |
| Canonical repo post path | Y:\vs-mcp-bridge\docs\blogs\posts\why-vsix-project-should-target-net-framework-4-7-2 |
| Current DB matches preserved export | True |
| Current DB content matches preserved export content | True |
| Current DB metadata matches preserved export metadata | True |
| Canonical content differs from current DB content | True |
| Canonical stale direct links found | 0 |
| Recommended safety decision | Safe for human draft-publish review: current DB still matches preserved export, and canonical content intentionally differs from runtime baseline. |

## Content Hashes

| Source | SHA-256 |
| --- | --- |
| Current live DB content | bcb944aa8b3eb63ef09bffde2354c349361181102619801774d418354f29824a |
| Preserved export content | bcb944aa8b3eb63ef09bffde2354c349361181102619801774d418354f29824a |
| Canonical repo content | 7a6f72e937ab406e22726a901f5ed9d0c69d3e4165c72e24b2143b3da1c57d70 |

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
| Title | Why a VSIX Project Should Target .NET Framework 4.7.2 |
| Description | When working with Visual Studio extensions, one detail that is easy to miss is that the extension pr |
| Author | AI Systems Author |
| Status | published |
| IsPublished | True |
| IsDeleted | False |
| AllowComments | True |
| Categories | AI Systems Author |
| Tags | None |

## Canonical Repo Metadata

| Field | Value |
| --- | --- |
| Title | Why a VSIX Project Should Target .NET Framework 4.7.2 |
| Description | Why VS MCP Bridge keeps the VSIX project on .NET Framework 4.7.2 while separating Visual Studio host code from shared, testable bridge infrastructure and out-of-process MCP runtime components. |
| Author | AI Systems Author |
| Slug | why-vsix-project-should-target-net-framework-4-7-2 |
| BlogID | 27604F05-86AD-47EF-9E05-950BB762570C |
| PostID | AE00D3F4-7C9A-4084-B690-D974E945D69E |
| IsPublished | True |
| AllowComments | True |
| Categories | AI Systems Author |
| Tags | VSIX, Visual Studio, .NET Framework, VSSDK, MEF, WPF, VS MCP Bridge, Architecture |

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