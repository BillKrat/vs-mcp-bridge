# Pre-Publish Compare - understanding-a-named-pipe-listener - 2026-05-16

## Scope

This report compares the current live BlogEngine database row, the preserved `db-export-20260516` baseline, and the canonical repo post before any publish operation.
The compare script is read-only: it performs a parameterized `SELECT`, reads repository files, and writes this report.
It does not update the database, call reload endpoints, or change public site behavior.

## Result Summary

| Check | Result |
| --- | --- |
| Slug | understanding-a-named-pipe-listener |
| DB PostID | 6484fa94-5d8b-429a-99c6-779b300bc336 |
| DB BlogID | 27604f05-86ad-47ef-9e05-950bb762570c |
| DB PostRowID | 151 |
| Current DB DateModified | 2026-04-27T10:30:04.773 |
| Preserved export timestamp | 2026-05-16T20:56:27.197Z |
| Preserved export DateModified | 2026-04-27T10:30:04.773 |
| Canonical repo post path | Y:\vs-mcp-bridge\docs\blogs\posts\understanding-a-named-pipe-listener |
| Current DB matches preserved export | True |
| Current DB content matches preserved export content | True |
| Current DB metadata matches preserved export metadata | True |
| Canonical content differs from current DB content | True |
| Canonical stale direct links found | 0 |
| Recommended safety decision | Safe for human draft-publish review: current DB still matches preserved export, and canonical content intentionally differs from runtime baseline. |

## Content Hashes

| Source | SHA-256 |
| --- | --- |
| Current live DB content | 4ffa8737bc86ca440f45b3ba6de2b15defbdc836f658b82196828217ccc25bd7 |
| Preserved export content | 4ffa8737bc86ca440f45b3ba6de2b15defbdc836f658b82196828217ccc25bd7 |
| Canonical repo content | aff9f4371a4c77d3796b89566028348a692f61d5ce95ff20c211d0dc6d0f80ed |

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
| Title | Understanding a Named Pipe Listener |
| Description | [Page:NamedPipeListener]<br>Source of Truth: Derived / Educational<br>Understanding a Named Pipe Listener |
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
| Title | Understanding a Named Pipe Listener |
| Description | An educational walkthrough of the VS MCP Bridge named-pipe boundary, PipeClient/PipeServer responsibilities, activation diagnostics, correlation, and relationship to MCP stdio and BridgeToolExecutor. |
| Author | AI Systems Author |
| Slug | understanding-a-named-pipe-listener |
| BlogID | 27604F05-86AD-47EF-9E05-950BB762570C |
| PostID | 6484FA94-5D8B-429A-99C6-779B300BC336 |
| IsPublished | True |
| AllowComments | True |
| Categories | AI Systems Author, MCP Bridge |
| Tags | VS MCP Bridge, MCP, Named Pipes, Visual Studio, VSIX, AI Tooling, Diagnostics, Architecture |

## Intentional BlogEngine Tokens

| Source | Tokens |
| --- | --- |
| Current DB content | [Page:NamedPipeListener], [VS MCP Bridge\|VsMcpBridge] |
| Canonical repo content | [Page:NamedPipeListener] |

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