# Pre-Publish Compare - inference-driven-software-design-with-copilot-pros-and-cons - 2026-05-16

## Scope

This report compares the current live BlogEngine database row, the preserved `db-export-20260516` baseline, and the canonical repo post before any publish operation.
The compare script is read-only: it performs a parameterized `SELECT`, reads repository files, and writes this report.
It does not update the database, call reload endpoints, or change public site behavior.

## Result Summary

| Check | Result |
| --- | --- |
| Slug | inference-driven-software-design-with-copilot-pros-and-cons |
| DB PostID | b3da6b1c-a955-4ec2-afda-b281bd5d46fd |
| DB BlogID | 27604f05-86ad-47ef-9e05-950bb762570c |
| DB PostRowID | 156 |
| Current DB DateModified | 2026-04-23T06:05:58.807 |
| Preserved export timestamp | 2026-05-16T20:56:27.197Z |
| Preserved export DateModified | 2026-04-23T06:05:58.807 |
| Canonical repo post path | Y:\vs-mcp-bridge\docs\blogs\posts\inference-driven-software-design-with-copilot-pros-and-cons |
| Current DB matches preserved export | True |
| Current DB content matches preserved export content | True |
| Current DB metadata matches preserved export metadata | True |
| Canonical content differs from current DB content | True |
| Canonical stale direct links found | 0 |
| Recommended safety decision | Safe for human draft-publish review: current DB still matches preserved export, and canonical content intentionally differs from runtime baseline. |

## Content Hashes

| Source | SHA-256 |
| --- | --- |
| Current live DB content | 8629963fb03b5ccd82cd9304aaccce582a9642d6a4c235e9c4c375023ae14bdb |
| Preserved export content | 8629963fb03b5ccd82cd9304aaccce582a9642d6a4c235e9c4c375023ae14bdb |
| Canonical repo content | 37d3c11e745b78997fb758f2c0848491a4f8693f3ce47775b9ce758dea3d4fd4 |

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
| Title | Inference-driven development with Copilot; pros and cons |
| Description | Copilot used by itself could leave a developer with a lack of confidence in AI ability to complete software development, this is not the case; you have to use the correct tool for the job. |
| Author | BillKrat |
| Status | published |
| IsPublished | True |
| IsDeleted | False |
| AllowComments | True |
| Categories | None |
| Tags | Copilot |

## Canonical Repo Metadata

| Field | Value |
| --- | --- |
| Title | Inference-driven development with Copilot; pros and cons |
| Description | A practical look at inference-driven software design, where Copilot and AI-assisted coding help most, where they create risk, and why observable workflows, approvals, logs, diagrams, and source-of-truth docs keep BlogAI development grounded. |
| Author | BillKrat |
| Slug | inference-driven-software-design-with-copilot-pros-and-cons |
| BlogID | 27604F05-86AD-47EF-9E05-950BB762570C |
| PostID | B3DA6B1C-A955-4EC2-AFDA-B281BD5D46FD |
| IsPublished | True |
| AllowComments | True |
| Categories | None |
| Tags | Copilot, AI Assisted Development, Inference, BlogAI, VS MCP Bridge, Observability, Architecture, Diagnostics |

## Intentional BlogEngine Tokens

| Source | Tokens |
| --- | --- |
| Current DB content | [Display:ChatSessionsModelsAndAgents], [Page:InferenceDriven] |
| Canonical repo content | [Display:ChatSessionsModelsAndAgents], [Page:InferenceDriven] |

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