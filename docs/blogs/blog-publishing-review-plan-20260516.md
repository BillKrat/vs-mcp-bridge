# Blog Publishing Review Plan - 2026-05-16

## Scope

This document prepares the cleaned canonical BlogAI post set for publishing review.
It does not publish content, write to the database, call reload endpoints, or change public site behavior.

Inputs reviewed:

- `docs/ARCHITECTURE.md`
- `docs/blogs/README.md`
- `docs/blogs/blog-cleanup-status-20260516.md`
- `docs/blogs/blog-alignment-inventory-20260516.md`
- `docs/blogs/blog-source-reconciliation-20260516.md`
- `docs/blogs/source-of-truth/db-export-20260516/manifest.json`
- canonical posts under `docs/blogs/posts/`
- `scripts/blog-publishing/README.md`

## Readiness Summary

| Disposition | Count | Notes |
| --- | ---: | --- |
| Ready for publishing review | 14 | Cleaned canonical posts map to preserved DB export rows and are ready for human publishing review. |
| Repo-only draft/trial | 2 | Keep separate from live DB posts unless explicitly promoted. |
| Export-only active records | 6 | Preserved from the DB export but not part of this BlogAI publishing set. |
| Export-only deleted records | 2 | Preserved for history only. |

All 14 ready posts have `post.json` metadata that matches the preserved DB export identifiers by slug, BlogID, PostID, and source row.
The canonical bodies differ from the DB export because the cleanup work updated repo source only.
Treat the DB export as the preserved runtime baseline and the canonical post folders as the intended publishing-review candidates.

## Posts Ready For Publishing Review

| Publish order | Canonical slug | DB row | BlogID | PostID | DB status | Metadata comparison | Notes |
| ---: | --- | ---: | --- | --- | --- | --- | --- |
| 1 | `vs-mcp-bridge-blog-series-part-1` | 143 | `27604f05-86ad-47ef-9e05-950bb762570c` | `f0c7a958-f41a-4143-b601-82ce84fd4af0` | published | matches DB export | Opens the architecture series and contains intentional BlogEngine tokens. |
| 2 | `vs-mcp-bridge-blog-series-part-2` | 144 | `27604f05-86ad-47ef-9e05-950bb762570c` | `cad63f28-5739-40be-b8f6-0288a1e3da20` | published | matches DB export | Establishes observable AI workflow and durable trace narrative. |
| 3 | `vs-mcp-bridge-blog-series-part-3` | 145 | `27604f05-86ad-47ef-9e05-950bb762570c` | `78dbd347-397e-4185-b6d5-d67558cc06be` | published | matches DB export | Covers host correctness, UI state, and proposal lifecycle. |
| 4 | `vs-mcp-bridge-blog-series-part-4` | 146 | `27604f05-86ad-47ef-9e05-950bb762570c` | `f62f7756-269a-4d49-a87d-c0394c7627d9` | published | matches DB export | Live Part 4 target; do not confuse with `vs-mcp-bridge-blog-series-part-4-repo-trial`. |
| 5 | `vs-mcp-bridge-blog-series-part-5` | 147 | `27604f05-86ad-47ef-9e05-950bb762570c` | `bd97e5de-4b4e-4660-98f1-465bd53eddec` | published | matches DB export | Covers discovery, compiled tools, and MEF as discovery-only. |
| 6 | `vs-mcp-bridge-blog-series-part-6` | 148 | `27604f05-86ad-47ef-9e05-950bb762570c` | `12db1be9-4143-476d-a12a-04c7ca045a71` | published | matches DB export | Covers security seams, policy, approval, audit, redaction, and secret-reference boundaries. |
| 7 | `vs-mcp-bridge-blog-series-part-7` | 149 | `27604f05-86ad-47ef-9e05-950bb762570c` | `5520e2d5-c597-492d-8b41-e467152364cd` | published | matches DB export | Closes the core series with durable evidence and reconstructable diagnostics. |
| 8 | `how-stdio-works-in-vs-mcp-bridge` | 155 | `27604f05-86ad-47ef-9e05-950bb762570c` | `d0541943-0de1-4c25-a7af-9950c55f1591` | published | matches DB export | Transport deep dive for MCP stdio and diagnostic isolation. |
| 9 | `understanding-a-named-pipe-listener` | 151 | `27604f05-86ad-47ef-9e05-950bb762570c` | `6484fa94-5d8b-429a-99c6-779b300bc336` | published | matches DB export | Transport deep dive for the local named-pipe listener. |
| 10 | `understanding-local-mcp-server-over-stdio-and-local-only-communication-over-a-named-pipe` | 142 | `27604f05-86ad-47ef-9e05-950bb762570c` | `43bf6aae-15c9-4c90-b3b2-66ac51c4a7c8` | published | matches DB export | Connects stdio and named-pipe boundaries in one runtime narrative. |
| 11 | `wpf-vsix-threading-understanding-ui-switching-async-behavior-and-pipe-safety` | 135 | `27604f05-86ad-47ef-9e05-950bb762570c` | `46828793-1f3d-4031-906e-87c1c31dce7e` | published | matches DB export | Host correctness background for WPF, async work, and pipe safety. |
| 12 | `why-vsix-project-should-target-net-framework-4-7-2` | 141 | `27604f05-86ad-47ef-9e05-950bb762570c` | `ae00d3f4-7c9a-4084-b690-d974e945d69e` | published | matches DB export | VSIX runtime targeting background. |
| 13 | `inference-driven-software-design-with-copilot-pros-and-cons` | 156 | `27604f05-86ad-47ef-9e05-950bb762570c` | `b3da6b1c-a955-4ec2-afda-b281bd5d46fd` | published | matches DB export | BlogAI narrative on inference-driven development. |
| 14 | `understanding-ai-chat-sessions-models-and-agents` | 150 | `27604f05-86ad-47ef-9e05-950bb762570c` | `5465cc54-65ab-4c4f-b6ac-4539de01c365` | published | matches DB export | BlogAI narrative on sessions, models, agents, tools, and context. |

## Tokens And Mermaid References

These bracket-style BlogEngine tokens are intentional and should be preserved unless a later token-mapping slice explicitly changes `GwnWikiExtension` behavior.
Mermaid source references point at checked-in `.mmd` files and should remain source links, not generated image requirements.

| Canonical slug | Intentional BlogEngine tokens | Mermaid references |
| --- | --- | --- |
| `vs-mcp-bridge-blog-series-part-1` | `[Page:VS MCP Bridge\|VsMcpBridge]`, `[NamedPipeListener]`, `[Stdio]` | None |
| `vs-mcp-bridge-blog-series-part-2` | None | `docs/diagrams/vsix-host-selected-text-trace-20260509.mmd`, `docs/diagrams/tool-regex-search-trace-20260509.mmd`, `docs/diagrams/tool-approval-trace-20260516.mmd`, `docs/diagrams/vsix-activation-diagnostic-trace-20260516.mmd` |
| `vs-mcp-bridge-blog-series-part-3` | None | `docs/diagrams/vsix-host-selected-text-trace-20260509.mmd`, `docs/diagrams/tool-approval-trace-20260516.mmd`, `docs/diagrams/vsix-activation-diagnostic-trace-20260516.mmd` |
| `vs-mcp-bridge-blog-series-part-4` | None | `docs/diagrams/tool-regex-search-trace-20260509.mmd`, `docs/diagrams/tool-security-trace-20260509.mmd`, `docs/diagrams/tool-approval-trace-20260516.mmd`, `docs/diagrams/mef-discovery-trace-20260516.mmd` |
| `vs-mcp-bridge-blog-series-part-5` | `[Page:Playbook]` | `docs/diagrams/mef-discovery-trace-20260516.mmd`, `docs/diagrams/tool-regex-search-trace-20260509.mmd`, `docs/diagrams/tool-security-trace-20260509.mmd`, `docs/diagrams/tool-approval-trace-20260516.mmd` |
| `vs-mcp-bridge-blog-series-part-6` | `[Page:Evidence]` | `docs/diagrams/tool-security-trace-20260509.mmd`, `docs/diagrams/tool-approval-trace-20260516.mmd`, `docs/diagrams/tool-regex-search-trace-20260509.mmd`, `docs/diagrams/mef-discovery-trace-20260516.mmd` |
| `vs-mcp-bridge-blog-series-part-7` | None | `docs/diagrams/tool-regex-search-trace-20260509.mmd`, `docs/diagrams/tool-security-trace-20260509.mmd`, `docs/diagrams/tool-approval-trace-20260516.mmd`, `docs/diagrams/mef-discovery-trace-20260516.mmd`, `docs/diagrams/vsix-activation-diagnostic-trace-20260516.mmd`, `docs/diagrams/vsix-host-selected-text-trace-20260509.mmd` |
| `how-stdio-works-in-vs-mcp-bridge` | `[Page:Stdio]` | `docs/blogs/diagrams/vs-mcp-bridge-bootstrap-sequence.mmd`, `docs/diagrams/vsix-activation-diagnostic-trace-20260516.mmd` |
| `understanding-a-named-pipe-listener` | `[Page:NamedPipeListener]` | `docs/blogs/diagrams/vs-mcp-bridge-bootstrap-sequence.mmd`, `docs/diagrams/vsix-activation-diagnostic-trace-20260516.mmd`, `docs/diagrams/vsix-host-selected-text-trace-20260509.mmd` |
| `understanding-local-mcp-server-over-stdio-and-local-only-communication-over-a-named-pipe` | None | `docs/blogs/diagrams/vs-mcp-bridge-bootstrap-sequence.mmd`, `docs/diagrams/vsix-activation-diagnostic-trace-20260516.mmd`, `docs/diagrams/tool-approval-trace-20260516.mmd`, `docs/diagrams/tool-security-trace-20260509.mmd` |
| `wpf-vsix-threading-understanding-ui-switching-async-behavior-and-pipe-safety` | None | `docs/diagrams/vsix-host-selected-text-trace-20260509.mmd`, `docs/diagrams/vsix-activation-diagnostic-trace-20260516.mmd`, `docs/diagrams/tool-approval-trace-20260516.mmd`, `docs/diagrams/tool-regex-search-trace-20260509.mmd` |
| `why-vsix-project-should-target-net-framework-4-7-2` | None | `docs/diagrams/vsix-host-ping-trace-20260508.mmd`, `docs/diagrams/vsix-host-selected-text-trace-20260509.mmd`, `docs/diagrams/vsix-activation-diagnostic-trace-20260516.mmd` |
| `inference-driven-software-design-with-copilot-pros-and-cons` | `[Page:InferenceDriven]`, `[Display:ChatSessionsModelsAndAgents]` | `docs/diagrams/vsix-host-selected-text-trace-20260509.mmd`, `docs/diagrams/tool-regex-search-trace-20260509.mmd`, `docs/diagrams/tool-approval-trace-20260516.mmd`, `docs/diagrams/vsix-activation-diagnostic-trace-20260516.mmd` |
| `understanding-ai-chat-sessions-models-and-agents` | `[Page:ChatSessionsModelsAndAgents]`, `[Display:inference-driven\|InferenceDriven]` | `docs/diagrams/vsix-host-selected-text-trace-20260509.mmd`, `docs/diagrams/tool-regex-search-trace-20260509.mmd`, `docs/diagrams/tool-approval-trace-20260516.mmd`, `docs/diagrams/vsix-activation-diagnostic-trace-20260516.mmd` |

## Not Ready Or Untouched

| Slug | Disposition | Reason |
| --- | --- | --- |
| `vs-mcp-bridge-blog-series-part-4-repo-trial` | Repo-only draft/trial | Separate from the live Part 4 DB row; do not publish over `vs-mcp-bridge-blog-series-part-4`. |
| `vs-mcp-bridge-publish-create-trial` | Repo-only draft/trial | Publishing workflow trial material; promote only by explicit decision. |
| `Welcome-to-Developments-blog` | Export-only active record | Older multi-blog/support content, outside the current VS MCP Bridge publishing set. |
| `welcome-to-our-site` | Export-only active record | Legacy site content with a large embedded data URI in the preserved DB export. |
| `this-one-will-only-be-seen-by-subscribers` | Export-only active record | Subscriber-oriented content; needs an explicit visibility decision before canonicalization. |
| `blog-engine-billkrat-fork-documentation` | Export-only active record | BlogEngine documentation content, outside the current architecture narrative. |
| `how-to-publish-your-own-blog-smarterasp` | Export-only active record | Hosting/tutorial content, outside the current architecture narrative. |
| `understanding-dependency-injection` | Export-only active record | Potential future background article, but not part of this publishing set. |
| `creating-a-post` | Export-only deleted record | Historical deleted row; do not canonicalize without a history requirement. |
| `difference-between-post-and-page` | Export-only deleted record | Historical deleted row; do not canonicalize without a history requirement. |

## Link Review

Canonical post content under `docs/blogs/posts/` was checked for:

- `feature/approval-apply-ui-slice`
- direct `adventuresontheedge.net` URLs
- direct `post.aspx?id=...` URLs

No stale direct links were found in the cleaned canonical post bodies.
The stale feature-branch links remain only in the preserved DB export baseline and are expected differences that publishing the cleaned canonical source would replace.
Rendered old-domain inter-post links may still appear after publishing because bracket tokens are resolved through `GwnWikiExtension` settings, not because direct old-domain URLs are present in article bodies.

## Recommended Publish Order

1. Core architecture series, Parts 1 through 7.
2. Transport/runtime deep dives: stdio, named pipe, combined stdio/named-pipe, WPF/VSIX threading, and .NET Framework 4.7.2 targeting.
3. BlogAI narrative posts: inference-driven development, then AI chat sessions/models/agents.

This order keeps readers on the current architecture path first, then adds supporting runtime detail, then broadens into BlogAI workflow interpretation.

## Publishing Review Checklist

Before publishing any post:

- Confirm the target canonical folder under `docs/blogs/posts/<slug>/`.
- Confirm `post.json` `slug`, `blogId`, `postId`, and `sourceDatabase.postRowId` match the intended DB export row in this plan.
- Confirm the post is not a repo-only trial artifact.
- Preserve intentional BlogEngine tokens unless a separate token-mapping decision has been made.
- Preserve `.mmd` source references as source links.
- Compare the current DB/runtime post against the preserved export if there is any chance of manual runtime edits after `db-export-20260516`.
- Remember that `Publish-BlogPostDraft.ps1` is draft-only for this workflow and overwrites DB content for the target post.

After publishing to BlogAI/global-webnet:

- Verify the post renders at the expected `https://www.global-webnet.com/post/YYYY/MM/DD/<slug>` route or known BlogEngine route for that post.
- Verify title, excerpt/description, categories, tags, author, and publication state.
- Verify BlogEngine token expansion does not break rendering.
- Verify no visible raw bracket token remains unless intentionally left unexpanded.
- Verify `.mmd` references are readable links or intentionally plain source references.
- Verify source-of-truth links point at stable `main`-branch repository docs where applicable.
- Verify no feature-branch GitHub links reappear.
- Verify no unexpected old-domain direct links appear in the article body; plugin-expanded old-domain links remain a separate mapping decision.
- Record any runtime/render differences before publishing the next post.

## Remaining Blockers

- No database write should happen until a human approves the ready list and target identifiers.
- Any manual DB edits made after `db-export-20260516` need a fresh compare/export before overwrite.
- `GwnWikiExtension` token mappings still need a separate keep/migrate decision.
- Export-only active records need explicit keep/archive/canonicalize decisions before broader BlogAI promotion.
- Draft-only publishing should be verified in a controlled BlogAI environment before any published-state overwrite workflow is considered.

## Recommended Next Slice

Create a read-only compare script or report that, for a selected slug, compares current DB content with the canonical repo post immediately before draft publishing.
That slice should still avoid writes, but it would reduce the risk of overwriting runtime edits made after the preserved `db-export-20260516` baseline.
