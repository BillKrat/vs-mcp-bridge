# Pre-Publish Compare - understanding-ai-chat-sessions-models-and-agents - 2026-05-16

## Scope

This report compares the current live BlogEngine database row, the preserved `db-export-20260516` baseline, and the canonical repo post before any publish operation.
The compare script is read-only: it performs a parameterized `SELECT`, reads repository files, and writes this report.
It does not update the database, call reload endpoints, or change public site behavior.

## Result Summary

| Check | Result |
| --- | --- |
| Slug | understanding-ai-chat-sessions-models-and-agents |
| DB PostID | 5465cc54-65ab-4c4f-b6ac-4539de01c365 |
| DB BlogID | 27604f05-86ad-47ef-9e05-950bb762570c |
| DB PostRowID | 150 |
| Current DB DateModified | 2026-04-23T05:17:24.893 |
| Preserved export timestamp | 2026-05-16T20:56:27.197Z |
| Preserved export DateModified | 2026-04-23T05:17:24.893 |
| Canonical repo post path | Y:\vs-mcp-bridge\docs\blogs\posts\understanding-ai-chat-sessions-models-and-agents |
| Current DB matches preserved export | False |
| Current DB content matches preserved export content | True |
| Current DB metadata matches preserved export metadata | False |
| Canonical content differs from current DB content | True |
| Canonical stale direct links found | 0 |
| Recommended safety decision | Do not publish yet: current DB no longer matches the preserved export baseline. Re-export or review live edits before overwrite. |

## Content Hashes

| Source | SHA-256 |
| --- | --- |
| Current live DB content | 01ac45e9ad9ade1ec1c395888f85153b6c60ff4eaa7e3fba7b7f760db89c70d8 |
| Preserved export content | 01ac45e9ad9ade1ec1c395888f85153b6c60ff4eaa7e3fba7b7f760db89c70d8 |
| Canonical repo content | 8bbfed2cadbb457da4be57f52fc1b01f473499611995bb3e0949fdfd7f9da8d2 |

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
| Title | Understanding AI Chat Sessions, Models, and Agents |
| Description | For readers who want a practical understanding of AI session behavior without marketing terminology. |
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
| Title | Understanding AI Chat Sessions, Models, and Agents |
| Description | A practical explanation of AI chat sessions, models, agents, tools, orchestration, context loss, and why VS MCP Bridge and BlogAI use source-of-truth docs, durable traces, handoffs, and approval-aware boundaries. |
| Author | AI Systems Author |
| Slug | understanding-ai-chat-sessions-models-and-agents |
| BlogID | 27604F05-86AD-47EF-9E05-950BB762570C |
| PostID | 5465CC54-65AB-4C4F-B6AC-4539DE01C365 |
| IsPublished | True |
| AllowComments | True |
| Categories | AI Systems Author, MCP Bridge |
| Tags | AI Assisted Development, Agents, Chat Sessions, Models, MCP, VS MCP Bridge, BlogAI, Observability, Architecture |

## Intentional BlogEngine Tokens

| Source | Tokens |
| --- | --- |
| Current DB content | [Application Layer], [Display:inference-driven\|InferenceDriven], [Model], [Output], [Page:ChatSessionsModelsAndAgents], [Stateless Model], [Temporary Context] |
| Canonical repo content | [Display:inference-driven\|InferenceDriven], [Page:ChatSessionsModelsAndAgents] |

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