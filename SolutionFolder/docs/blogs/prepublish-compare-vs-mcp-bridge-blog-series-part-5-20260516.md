# Pre-Publish Compare - vs-mcp-bridge-blog-series-part-5 - 2026-05-16

## Scope

This report compares the current live BlogEngine database row, the preserved `db-export-20260516` baseline, and the canonical repo post before any publish operation.
The compare script is read-only: it performs a parameterized `SELECT`, reads repository files, and writes this report.
It does not update the database, call reload endpoints, or change public site behavior.

## Result Summary

| Check | Result |
| --- | --- |
| Slug | vs-mcp-bridge-blog-series-part-5 |
| DB PostID | bd97e5de-4b4e-4660-98f1-465bd53eddec |
| DB BlogID | 27604f05-86ad-47ef-9e05-950bb762570c |
| DB PostRowID | 147 |
| Current DB DateModified | 2026-04-23T06:18:01.040 |
| Preserved export timestamp | 2026-05-16T20:56:27.197Z |
| Preserved export DateModified | 2026-04-23T06:18:01.040 |
| Canonical repo post path | Y:\vs-mcp-bridge\docs\blogs\posts\vs-mcp-bridge-blog-series-part-5 |
| Current DB matches preserved export | False |
| Current DB content matches preserved export content | True |
| Current DB metadata matches preserved export metadata | False |
| Canonical content differs from current DB content | True |
| Canonical stale direct links found | 0 |
| Recommended safety decision | Do not publish yet: current DB no longer matches the preserved export baseline. Re-export or review live edits before overwrite. |

## Content Hashes

| Source | SHA-256 |
| --- | --- |
| Current live DB content | 7a7b85913f888b3f21ee2a641ef1b836fcff8da3fb3137fd1ad640cd43bc1007 |
| Preserved export content | 7a7b85913f888b3f21ee2a641ef1b836fcff8da3fb3137fd1ad640cd43bc1007 |
| Canonical repo content | d2f944be69eb510f626b02eba7f850b2abf8f22ac1bdbd727eb76a54ed283811 |

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
| Title | VS MCP Bridge Blog Series: Part 5 |
| Description | Validation Playbook for the Current Bridge Slice. This post turns runtime validation into a practical sequence of checks for the current VS MCP Bridge, showing what to test first, what signals to watch, and how the playbook also shapes implementation quality. |
| Author | AI Systems Author |
| Status | published |
| IsPublished | True |
| IsDeleted | False |
| AllowComments | True |
| Categories | AI Systems Author, MCP Bridge |
| Tags | AI Tooling, Failure Recovery, MCP, Runtime validation, testing, Visual Studio, VS MCP Bridge, VSIX |

## Canonical Repo Metadata

| Field | Value |
| --- | --- |
| Title | VS MCP Bridge Blog Series: Part 5 |
| Description | How VS MCP Bridge keeps tool discovery and future extensibility observable through compiled discovery, opt-in MEF discovery, catalog metadata, executor-owned policy/audit boundaries, and durable Mermaid traces. |
| Author | AI Systems Author |
| Slug | vs-mcp-bridge-blog-series-part-5 |
| BlogID | 27604F05-86AD-47EF-9E05-950BB762570C |
| PostID | BD97E5DE-4B4E-4660-98F1-465BD53EDDEC |
| IsPublished | True |
| AllowComments | True |
| Categories | AI Systems Author, MCP Bridge |
| Tags | VS MCP Bridge, MCP, Bridge Tools, MEF, Discovery, Extensibility, Diagnostics, Mermaid, Architecture |

## Intentional BlogEngine Tokens

| Source | Tokens |
| --- | --- |
| Current DB content | [Page:Playbook] |
| Canonical repo content | [Page:Playbook] |

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