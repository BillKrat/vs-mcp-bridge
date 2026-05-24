# Pre-Publish Compare - vs-mcp-bridge-blog-series-part-6 - 2026-05-16

## Scope

This report compares the current live BlogEngine database row, the preserved `db-export-20260516` baseline, and the canonical repo post before any publish operation.
The compare script is read-only: it performs a parameterized `SELECT`, reads repository files, and writes this report.
It does not update the database, call reload endpoints, or change public site behavior.

## Result Summary

| Check | Result |
| --- | --- |
| Slug | vs-mcp-bridge-blog-series-part-6 |
| DB PostID | 12db1be9-4143-476d-a12a-04c7ca045a71 |
| DB BlogID | 27604f05-86ad-47ef-9e05-950bb762570c |
| DB PostRowID | 148 |
| Current DB DateModified | 2026-04-23T06:18:44.137 |
| Preserved export timestamp | 2026-05-16T20:56:27.197Z |
| Preserved export DateModified | 2026-04-23T06:18:44.137 |
| Canonical repo post path | Y:\vs-mcp-bridge\docs\blogs\posts\vs-mcp-bridge-blog-series-part-6 |
| Current DB matches preserved export | False |
| Current DB content matches preserved export content | True |
| Current DB metadata matches preserved export metadata | False |
| Canonical content differs from current DB content | True |
| Canonical stale direct links found | 0 |
| Recommended safety decision | Do not publish yet: current DB no longer matches the preserved export baseline. Re-export or review live edits before overwrite. |

## Content Hashes

| Source | SHA-256 |
| --- | --- |
| Current live DB content | 1522c4fbd83598fb6639232b226ccdfffc1618d8e5d9512512cb649690c98c86 |
| Preserved export content | 1522c4fbd83598fb6639232b226ccdfffc1618d8e5d9512512cb649690c98c86 |
| Canonical repo content | 820289bbea45c2e9508404259b172b5a8d5b07ab57ad15a707278cecfa79a2c3 |

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
| Title | VS MCP Bridge Blog Series: Part 6 |
| Description | Evidence Model for the Current Bridge. This post explains what signals the current VS MCP Bridge needs so validation is practical, failures are triageable, and the runtime does not behave like a black box. |
| Author | AI Systems Author |
| Status | published |
| IsPublished | True |
| IsDeleted | False |
| AllowComments | True |
| Categories | AI Systems Author, MCP Bridge |
| Tags | AI Tooling, blog/vs-mcp-bridge-part-6, Failure Recovery, MCP, Observability, Runtime validation, Visual Studio, VS MCP Bridge, VSIX |

## Canonical Repo Metadata

| Field | Value |
| --- | --- |
| Title | VS MCP Bridge Blog Series: Part 6 |
| Description | How VS MCP Bridge keeps tool execution security explicit through policy checks, approval-aware execution, capability metadata, secret-reference seams, redaction, audit envelopes, and correlation without claiming production authentication or sandboxing. |
| Author | AI Systems Author |
| Slug | vs-mcp-bridge-blog-series-part-6 |
| BlogID | 27604F05-86AD-47EF-9E05-950BB762570C |
| PostID | 12DB1BE9-4143-476D-A12A-04C7CA045A71 |
| IsPublished | True |
| AllowComments | True |
| Categories | AI Systems Author, MCP Bridge |
| Tags | VS MCP Bridge, MCP, Security, Policy, Approval, Audit, Redaction, Secrets, Architecture |

## Intentional BlogEngine Tokens

| Source | Tokens |
| --- | --- |
| Current DB content | [Page:Evidence] |
| Canonical repo content | [Page:Evidence] |

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