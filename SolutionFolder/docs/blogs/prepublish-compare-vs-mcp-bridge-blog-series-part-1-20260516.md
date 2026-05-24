# Pre-Publish Compare - vs-mcp-bridge-blog-series-part-1 - 2026-05-16

## Scope

This report compares the current live BlogEngine database row, the preserved `db-export-20260516` baseline, and the canonical repo post before any publish operation.
The compare script is read-only: it performs a parameterized `SELECT`, reads repository files, and writes this report.
It does not update the database, call reload endpoints, or change public site behavior.

## Result Summary

| Check | Result |
| --- | --- |
| Slug | vs-mcp-bridge-blog-series-part-1 |
| DB PostID | f0c7a958-f41a-4143-b601-82ce84fd4af0 |
| DB BlogID | 27604f05-86ad-47ef-9e05-950bb762570c |
| DB PostRowID | 143 |
| Current DB DateModified | 2026-05-12T17:56:50.340 |
| Preserved export timestamp | 2026-05-16T20:56:27.197Z |
| Preserved export DateModified | 2026-05-12T17:56:50.340 |
| Canonical repo post path | Y:\vs-mcp-bridge\docs\blogs\posts\vs-mcp-bridge-blog-series-part-1 |
| Current DB matches preserved export | True |
| Current DB content matches preserved export content | True |
| Current DB metadata matches preserved export metadata | True |
| Canonical content differs from current DB content | True |
| Canonical stale direct links found | 0 |
| Recommended safety decision | Safe for human draft-publish review: current DB still matches preserved export, and canonical content intentionally differs from runtime baseline. |

## Content Hashes

| Source | SHA-256 |
| --- | --- |
| Current live DB content | bc6d3850e5ba74062a9c3744877f5a6a8fbe8687e873a15be54068a7a33b2e8a |
| Preserved export content | bc6d3850e5ba74062a9c3744877f5a6a8fbe8687e873a15be54068a7a33b2e8a |
| Canonical repo content | 03c196c998e3819b27bf1dc6c2b43dd4f40a04e9fe1bb2057fbc4bbd918d1ab8 |

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
| Title | VS MCP Bridge Blog Series: Part 1 |
| Description | This post is the first in a short developer ramp-up series for the VS MCP Bridge project. Its purpose is to make the bootstrap flow easier to understand, especially in a codebase that is intentionally decoupled. |
| Author | AI Systems Author |
| Status | published |
| IsPublished | True |
| IsDeleted | False |
| AllowComments | True |
| Categories | None |
| Tags | None |

## Canonical Repo Metadata

| Field | Value |
| --- | --- |
| Title | VS MCP Bridge Blog Series: Part 1 |
| Description | A developer ramp-up post explaining the current VS MCP Bridge startup path, stdio and named-pipe boundaries, tool-window role, approval behavior, diagnostics, and shared tool execution boundary. |
| Author | AI Systems Author |
| Slug | vs-mcp-bridge-blog-series-part-1 |
| BlogID | 27604F05-86AD-47EF-9E05-950BB762570C |
| PostID | F0C7A958-F41A-4143-B601-82CE84FD4AF0 |
| IsPublished | True |
| AllowComments | True |
| Categories | AI Systems Author, MCP Bridge |
| Tags | VS MCP Bridge, MCP, Visual Studio, VSIX, Named Pipes, Stdio, AI Tooling, Architecture |

## Intentional BlogEngine Tokens

| Source | Tokens |
| --- | --- |
| Current DB content | [NamedPipeListener], [Page:VS MCP Bridge\|VsMcpBridge], [Stdio] |
| Canonical repo content | [NamedPipeListener], [Page:VS MCP Bridge\|VsMcpBridge], [Stdio] |

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