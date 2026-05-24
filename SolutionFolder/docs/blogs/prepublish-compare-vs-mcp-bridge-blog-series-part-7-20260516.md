# Pre-Publish Compare - vs-mcp-bridge-blog-series-part-7 - 2026-05-16

## Scope

This report compares the current live BlogEngine database row, the preserved `db-export-20260516` baseline, and the canonical repo post before any publish operation.
The compare script is read-only: it performs a parameterized `SELECT`, reads repository files, and writes this report.
It does not update the database, call reload endpoints, or change public site behavior.

## Result Summary

| Check | Result |
| --- | --- |
| Slug | vs-mcp-bridge-blog-series-part-7 |
| DB PostID | 5520e2d5-c597-492d-8b41-e467152364cd |
| DB BlogID | 27604f05-86ad-47ef-9e05-950bb762570c |
| DB PostRowID | 149 |
| Current DB DateModified | 2026-04-23T06:19:04.573 |
| Preserved export timestamp | 2026-05-16T20:56:27.197Z |
| Preserved export DateModified | 2026-04-23T06:19:04.573 |
| Canonical repo post path | Y:\vs-mcp-bridge\docs\blogs\posts\vs-mcp-bridge-blog-series-part-7 |
| Current DB matches preserved export | True |
| Current DB content matches preserved export content | True |
| Current DB metadata matches preserved export metadata | True |
| Canonical content differs from current DB content | True |
| Canonical stale direct links found | 0 |
| Recommended safety decision | Safe for human draft-publish review: current DB still matches preserved export, and canonical content intentionally differs from runtime baseline. |

## Content Hashes

| Source | SHA-256 |
| --- | --- |
| Current live DB content | ec732acbf8bf3791041c9a602a6934b986b664ed2e849aef3a14d535a16633a4 |
| Preserved export content | ec732acbf8bf3791041c9a602a6934b986b664ed2e849aef3a14d535a16633a4 |
| Canonical repo content | fb04e6e7507a6dd5d425e06f72b020cf74cd129cc7efc19df0f4734e03446bdf |

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
| Title | VS MCP Bridge Blog Series: Part 7 |
| Description | Signal Map for the Current Bridge. This post maps the current VS MCP Bridge to the signals it already exposes, the signals that need tightening, and the missing evidence that still makes validation harder than it should be. |
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
| Title | VS MCP Bridge Blog Series: Part 7 |
| Description | How VS MCP Bridge uses durable logs, metadata, Mermaid traces, workflow documents, and session handoffs to make architecture validation and AI-assisted troubleshooting reconstructable. |
| Author | AI Systems Author |
| Slug | vs-mcp-bridge-blog-series-part-7 |
| BlogID | 27604F05-86AD-47EF-9E05-950BB762570C |
| PostID | 5520E2D5-C597-492D-8B41-E467152364CD |
| IsPublished | True |
| AllowComments | True |
| Categories | None |
| Tags | VS MCP Bridge, MCP, Diagnostics, Observability, Mermaid, Trace Artifacts, Validation, AI Assisted Development, Architecture |

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