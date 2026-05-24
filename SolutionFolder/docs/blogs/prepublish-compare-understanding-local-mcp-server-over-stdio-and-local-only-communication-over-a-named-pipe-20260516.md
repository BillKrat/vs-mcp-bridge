# Pre-Publish Compare - understanding-local-mcp-server-over-stdio-and-local-only-communication-over-a-named-pipe - 2026-05-16

## Scope

This report compares the current live BlogEngine database row, the preserved `db-export-20260516` baseline, and the canonical repo post before any publish operation.
The compare script is read-only: it performs a parameterized `SELECT`, reads repository files, and writes this report.
It does not update the database, call reload endpoints, or change public site behavior.

## Result Summary

| Check | Result |
| --- | --- |
| Slug | understanding-local-mcp-server-over-stdio-and-local-only-communication-over-a-named-pipe |
| DB PostID | 43bf6aae-15c9-4c90-b3b2-66ac51c4a7c8 |
| DB BlogID | 27604f05-86ad-47ef-9e05-950bb762570c |
| DB PostRowID | 142 |
| Current DB DateModified | 2026-04-12T10:31:23.680 |
| Preserved export timestamp | 2026-05-16T20:56:27.197Z |
| Preserved export DateModified | 2026-04-12T10:31:23.680 |
| Canonical repo post path | Y:\vs-mcp-bridge\docs\blogs\posts\understanding-local-mcp-server-over-stdio-and-local-only-communication-over-a-named-pipe |
| Current DB matches preserved export | True |
| Current DB content matches preserved export content | True |
| Current DB metadata matches preserved export metadata | True |
| Canonical content differs from current DB content | True |
| Canonical stale direct links found | 0 |
| Recommended safety decision | Safe for human draft-publish review: current DB still matches preserved export, and canonical content intentionally differs from runtime baseline. |

## Content Hashes

| Source | SHA-256 |
| --- | --- |
| Current live DB content | 87d475d7636d2396380b4baa08f29ea4300414a3e7dd5f040e6dedd63a23742b |
| Preserved export content | 87d475d7636d2396380b4baa08f29ea4300414a3e7dd5f040e6dedd63a23742b |
| Canonical repo content | 076638fe92631fb5f79f8df271cc8148e0ebb3473faec4dfcc75b9a8c0d43c8c |

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
| Title | Understanding a Local MCP Server Over Stdio and Local-Only Communication Over a Named Pipe |
| Description | When integrating systems inside Visual Studio, two communication patterns often appear early in the  |
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
| Title | Understanding a Local MCP Server Over Stdio and Local-Only Communication Over a Named Pipe |
| Description | A concise explanation of how VS MCP Bridge combines MCP over stdio with local named-pipe communication to keep AI protocol handling, Visual Studio integration, diagnostics, and tool execution boundaries separate. |
| Author | AI Systems Author |
| Slug | understanding-local-mcp-server-over-stdio-and-local-only-communication-over-a-named-pipe |
| BlogID | 27604F05-86AD-47EF-9E05-950BB762570C |
| PostID | 43BF6AAE-15C9-4C90-B3B2-66AC51C4A7C8 |
| IsPublished | True |
| AllowComments | True |
| Categories | AI Systems Author, MCP Bridge |
| Tags | VS MCP Bridge, MCP, stdio, Named Pipes, Visual Studio, VSIX, AI Tooling, Diagnostics, Architecture |

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

Safe for human draft-publish review: current DB still matches preserved export, and canonical content intentionally differs from runtime baseline.

If publishing proceeds, use the draft-only workflow first and verify runtime rendering before touching the next post.