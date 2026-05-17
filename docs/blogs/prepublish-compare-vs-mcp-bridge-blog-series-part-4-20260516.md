# Pre-Publish Compare - vs-mcp-bridge-blog-series-part-4 - 2026-05-16

## Scope

This report compares the current live BlogEngine database row, the preserved `db-export-20260516` baseline, and the canonical repo post before any publish operation.
The compare script is read-only: it performs a parameterized `SELECT`, reads repository files, and writes this report.
It does not update the database, call reload endpoints, or change public site behavior.

## Result Summary

| Check | Result |
| --- | --- |
| Slug | vs-mcp-bridge-blog-series-part-4 |
| DB PostID | f62f7756-269a-4d49-a87d-c0394c7627d9 |
| DB BlogID | 27604f05-86ad-47ef-9e05-950bb762570c |
| DB PostRowID | 146 |
| Current DB DateModified | 2026-04-12T10:30:50.303 |
| Preserved export timestamp | 2026-05-16T20:56:27.197Z |
| Preserved export DateModified | 2026-04-12T10:30:50.303 |
| Canonical repo post path | Y:\vs-mcp-bridge\docs\blogs\posts\vs-mcp-bridge-blog-series-part-4 |
| Current DB matches preserved export | False |
| Current DB content matches preserved export content | True |
| Current DB metadata matches preserved export metadata | False |
| Canonical content differs from current DB content | True |
| Canonical stale direct links found | 0 |
| Recommended safety decision | Do not publish yet: current DB no longer matches the preserved export baseline. Re-export or review live edits before overwrite. |

## Content Hashes

| Source | SHA-256 |
| --- | --- |
| Current live DB content | 7057316908f8d1b111d82eae0e82a2e96616e97113f8527ed1e0764e3eb55762 |
| Preserved export content | 7057316908f8d1b111d82eae0e82a2e96616e97113f8527ed1e0764e3eb55762 |
| Canonical repo content | ca34e445f2dbc801f3584549f0c85b04f228b25410d3f9a22af32733db895ba4 |

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
| Title | VS MCP Bridge Blog Series: Part 4 |
| Description | Runtime Validation, Failure Loops, and Clean Recovery. This post explains how to validate the current VS MCP Bridge end to end, what evidence to look for during testing, and why clean failure and recovery behavior matter as much as success.<br><br>The next likely topic is the concrete validation playbook: exact order of checks, what to open, what to run, and what signals confirm each step. |
| Author | AI Systems Author |
| Status | published |
| IsPublished | True |
| IsDeleted | False |
| AllowComments | True |
| Categories | AI Systems Author, MCP Bridge |
| Tags | AI Tooling, Failure Recovery, MCP, Named Pipes, Runtime validation, Visual Studio, VS MCP Bridge, VSIX |

## Canonical Repo Metadata

| Field | Value |
| --- | --- |
| Title | VS MCP Bridge Blog Series: Part 4 |
| Description | How VS MCP Bridge turns compiled bridge tools into observable contracts through IBridgeTool, descriptors, requests, results, catalog discovery, BridgeToolExecutor, approval-aware execution, audit metadata, and correlation-preserving tests. |
| Author | AI Systems Author |
| Slug | vs-mcp-bridge-blog-series-part-4 |
| BlogID | 27604F05-86AD-47EF-9E05-950BB762570C |
| PostID | F62F7756-269A-4D49-A87D-C0394C7627D9 |
| IsPublished | True |
| AllowComments | True |
| Categories | AI Systems Author, MCP Bridge |
| Tags | VS MCP Bridge, MCP, Bridge Tools, Compiled Tools, Approval, Audit, Diagnostics, Regex Search, Architecture |

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
| Current DB content | https://github.com/BillKrat/vs-mcp-bridge/blob/feature/approval-apply-ui-slice/docs/ARCHITECTURE.md |
| Preserved export content | https://github.com/BillKrat/vs-mcp-bridge/blob/feature/approval-apply-ui-slice/docs/ARCHITECTURE.md |
| Canonical repo content | None |

## Publish Safety Recommendation

Do not publish yet: current DB no longer matches the preserved export baseline. Re-export or review live edits before overwrite.

If publishing proceeds, use the draft-only workflow first and verify runtime rendering before touching the next post.