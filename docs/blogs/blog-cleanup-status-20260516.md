# Blog Cleanup Status - 2026-05-16

## Scope

This status document records the canonical blog alignment work completed after the database preservation and source reconciliation slices.
It is an index for future BlogAI publishing work, not a publishing artifact.

No database records were changed.
No public site behavior was changed.
No article bodies were rewritten in this slice.

Authoritative inputs remain:

- `docs/ARCHITECTURE.md`
- `docs/blogs/source-of-truth/db-export-20260516/manifest.json`
- `docs/blogs/blog-alignment-inventory-20260516.md`
- `docs/blogs/blog-source-reconciliation-20260516.md`
- canonical posts under `docs/blogs/posts/`

## Cleanup Narrative

The blog cleanup started by preserving every BlogEngine database row into repo-tracked source-of-truth artifacts.
The follow-up reconciliation materialized the active BlogAI candidate posts under `docs/blogs/posts/`, separated the live Part 4 post from the repo trial draft, and documented that old-domain rendered inter-post links are produced by `GwnWikiExtension` token expansion rather than direct article-body links.

After that baseline was safe, the cleanup moved through focused single-post slices.
Those slices updated canonical repo sources only, using `docs/ARCHITECTURE.md` as the source of truth.
The updated posts now form a coherent narrative from transport boundaries, to Visual Studio host correctness, to compiled tool execution, to discovery, security seams, durable evidence, and broader BlogAI/AI-assisted development themes.

## Covered Architecture Themes

- MCP/stdio boundary: clean stdout, stdio request flow, and why diagnostics must stay out of protocol output.
- Named-pipe isolation: local-only communication between the MCP server and the VSIX side.
- Host correctness: Visual Studio UI-thread switching, async host behavior, proposal state, and VSIX runtime constraints.
- Approval-aware execution: descriptor metadata, approval decisions, structured denial, audit metadata, and unchanged default behavior.
- Tool catalogs/discovery: compiled bridge tools, catalog/executor boundaries, empty/unknown catalog behavior, and MEF discovery as discovery only.
- Security seams: policy checks, capability metadata, secret references, redaction, audit envelopes, and classification metadata without overstating production security.
- Durable traces/evidence: logs, metadata JSON, Mermaid diagrams, and handoffs as reconstructable evidence.
- AI sessions/models/agents: context continuity, tool-backed workflows, orchestration layers, and the limits of chat inference.
- Inference-driven development: prompt-to-evidence practices, human review, and anti-black-box AI-assisted coding.
- VSIX framework/runtime constraints: why the VSIX targets .NET Framework 4.7.2 while shared logic remains separately testable.

## Status Summary

| Status | Posts | Notes |
| --- | ---: | --- |
| Aligned canonical source | 14 | Active BlogAI/VS MCP Bridge candidate posts have been updated in repo source only. |
| Partially reviewed active candidates | 0 | No active BlogAI candidate is intentionally left mid-cleanup as of this status document. |
| Repo-only draft/trial | 2 | Kept separate from live DB posts until explicitly promoted. |
| Export-only active legacy/support rows | 6 | Preserved from DB export, not yet canonicalized for the BlogAI narrative. |
| Export-only deleted rows | 2 | Preserved for history only. |

## Canonical Post Status

| Post slug | Status | Primary topic | Narrative group | Mermaid references | Intentional BlogEngine tokens | Remaining concerns/actions |
| --- | --- | --- | --- | --- | --- | --- |
| `vs-mcp-bridge-blog-series-part-1` | Aligned canonical source | Project origin, VSIX activation, MCP bridge shape | Core architecture series | None | `[Page:VS MCP Bridge\|VsMcpBridge]`, `[NamedPipeListener]`, `[Stdio]` | Needs runtime/render verification after publish to `https://www.global-webnet.com`. |
| `how-stdio-works-in-vs-mcp-bridge` | Aligned canonical source | MCP stdio boundary and clean stdout | Transport/runtime deep dive | `docs/diagrams/vsix-activation-diagnostic-trace-20260516.mmd` | `[Page:Stdio]` | Needs runtime/render verification after publish. |
| `understanding-a-named-pipe-listener` | Aligned canonical source | VSIX named-pipe listener and local-only isolation | Transport/runtime deep dive | `docs/diagrams/vsix-activation-diagnostic-trace-20260516.mmd`, `docs/diagrams/vsix-host-selected-text-trace-20260509.mmd` | `[Page:NamedPipeListener]` | Needs runtime/render verification after publish. |
| `understanding-local-mcp-server-over-stdio-and-local-only-communication-over-a-named-pipe` | Aligned canonical source | How stdio and named pipe fit together | Transport/runtime deep dive | `docs/diagrams/vsix-activation-diagnostic-trace-20260516.mmd`, `docs/diagrams/tool-approval-trace-20260516.mmd`, `docs/diagrams/tool-security-trace-20260509.mmd` | None found | Needs runtime/render verification after publish. |
| `vs-mcp-bridge-blog-series-part-2` | Aligned canonical source | Observable AI-assisted workflows and durable traces | Core architecture series | `docs/diagrams/vsix-host-selected-text-trace-20260509.mmd`, `docs/diagrams/tool-regex-search-trace-20260509.mmd`, `docs/diagrams/tool-approval-trace-20260516.mmd`, `docs/diagrams/vsix-activation-diagnostic-trace-20260516.mmd` | None found | Needs runtime/render verification after publish. |
| `vs-mcp-bridge-blog-series-part-3` | Aligned canonical source | Host correctness, UI state, and proposal lifecycle | Core architecture series | `docs/diagrams/vsix-host-selected-text-trace-20260509.mmd`, `docs/diagrams/tool-approval-trace-20260516.mmd`, `docs/diagrams/vsix-activation-diagnostic-trace-20260516.mmd` | None found | Needs runtime/render verification after publish. |
| `vs-mcp-bridge-blog-series-part-4` | Aligned canonical source | Compiled bridge tools, catalog/executor boundary, approval-aware tool execution | Core architecture series | `docs/diagrams/tool-regex-search-trace-20260509.mmd`, `docs/diagrams/tool-security-trace-20260509.mmd`, `docs/diagrams/tool-approval-trace-20260516.mmd`, `docs/diagrams/mef-discovery-trace-20260516.mmd` | None found | Live Part 4 only; do not merge with or overwrite the repo trial draft. Needs runtime/render verification after publish. |
| `vs-mcp-bridge-blog-series-part-5` | Aligned canonical source | Tool discovery, extensibility, and MEF discovery-only seam | Core architecture series | `docs/diagrams/mef-discovery-trace-20260516.mmd`, `docs/diagrams/tool-regex-search-trace-20260509.mmd`, `docs/diagrams/tool-security-trace-20260509.mmd`, `docs/diagrams/tool-approval-trace-20260516.mmd` | `[Page:Playbook]` | Needs runtime/render verification after publish. |
| `vs-mcp-bridge-blog-series-part-6` | Aligned canonical source | Security seams, policy, approval, audit, redaction, and secret references | Core architecture series | `docs/diagrams/tool-security-trace-20260509.mmd`, `docs/diagrams/tool-approval-trace-20260516.mmd`, `docs/diagrams/tool-regex-search-trace-20260509.mmd`, `docs/diagrams/mef-discovery-trace-20260516.mmd` | `[Page:Evidence]` | Needs runtime/render verification after publish. |
| `vs-mcp-bridge-blog-series-part-7` | Aligned canonical source | Durable evidence, validation handoffs, and reconstructable troubleshooting | Core architecture series | `docs/diagrams/tool-regex-search-trace-20260509.mmd`, `docs/diagrams/tool-security-trace-20260509.mmd`, `docs/diagrams/tool-approval-trace-20260516.mmd`, `docs/diagrams/mef-discovery-trace-20260516.mmd`, `docs/diagrams/vsix-activation-diagnostic-trace-20260516.mmd`, `docs/diagrams/vsix-host-selected-text-trace-20260509.mmd` | None found | Needs runtime/render verification after publish. |
| `inference-driven-software-design-with-copilot-pros-and-cons` | Aligned canonical source | Inference-driven development, pros/cons, and prompt-to-evidence practice | BlogAI narrative series | `docs/diagrams/vsix-host-selected-text-trace-20260509.mmd`, `docs/diagrams/tool-regex-search-trace-20260509.mmd`, `docs/diagrams/tool-approval-trace-20260516.mmd`, `docs/diagrams/vsix-activation-diagnostic-trace-20260516.mmd` | `[Page:InferenceDriven]`, `[Display:ChatSessionsModelsAndAgents]` | Needs runtime/render verification after publish. |
| `understanding-ai-chat-sessions-models-and-agents` | Aligned canonical source | Sessions, models, agents, tools, orchestration, and context loss | BlogAI narrative series | `docs/diagrams/vsix-host-selected-text-trace-20260509.mmd`, `docs/diagrams/tool-regex-search-trace-20260509.mmd`, `docs/diagrams/tool-approval-trace-20260516.mmd`, `docs/diagrams/vsix-activation-diagnostic-trace-20260516.mmd` | `[Page:ChatSessionsModelsAndAgents]`, `[Display:inference-driven\|InferenceDriven]` | Needs runtime/render verification after publish. |
| `wpf-vsix-threading-understanding-ui-switching-async-behavior-and-pipe-safety` | Aligned canonical source | WPF/VSIX threading, async behavior, pipe safety, and host correctness | Transport/runtime deep dive | `docs/diagrams/vsix-host-selected-text-trace-20260509.mmd`, `docs/diagrams/vsix-activation-diagnostic-trace-20260516.mmd`, `docs/diagrams/tool-approval-trace-20260516.mmd`, `docs/diagrams/tool-regex-search-trace-20260509.mmd` | None found | Needs runtime/render verification after publish. |
| `why-vsix-project-should-target-net-framework-4-7-2` | Aligned canonical source | VSIX target framework, host runtime constraints, and shared/VSIX separation | Transport/runtime deep dive | `docs/diagrams/vsix-host-ping-trace-20260508.mmd`, `docs/diagrams/vsix-host-selected-text-trace-20260509.mmd`, `docs/diagrams/vsix-activation-diagnostic-trace-20260516.mmd` | None found | Needs runtime/render verification after publish. |
| `vs-mcp-bridge-blog-series-part-4-repo-trial` | Repo-only draft/trial | Publishing trial for Part 4 shape | Repo-only artifact | None | None found | Keep separate from live `vs-mcp-bridge-blog-series-part-4`; do not publish over the live DB post without explicit decision. |
| `vs-mcp-bridge-publish-create-trial` | Repo-only draft/trial | Publishing workflow trial | Repo-only artifact | None | None found | Keep as trial material unless explicitly promoted. |

## Export-Only Records

These database rows remain preserved in the source-of-truth export but are not part of the current canonical BlogAI cleanup set.

| DB slug | DB status | Current disposition | Remaining concerns/actions |
| --- | --- | --- | --- |
| `Welcome-to-Developments-blog` | Published | Export-only legacy/support content | Decide later whether older multi-blog content belongs in BlogAI canonical source. |
| `welcome-to-our-site` | Published | Export-only legacy/support content | Contains a large embedded data URI in the DB export; avoid reformatting without an asset plan. |
| `this-one-will-only-be-seen-by-subscribers` | Published | Export-only subscriber-oriented content | Needs an explicit visibility/publishing decision before canonicalization. |
| `blog-engine-billkrat-fork-documentation` | Published | Export-only BlogEngine documentation content | Out of current VS MCP Bridge architecture narrative. |
| `how-to-publish-your-own-blog-smarterasp` | Published | Export-only hosting/tutorial content | Out of current VS MCP Bridge architecture narrative. |
| `understanding-dependency-injection` | Published | Export-only general architecture content | Potential future background post, but not aligned in this cleanup run. |
| `creating-a-post` | Deleted | Export-only historical row | Do not canonicalize unless historical/deleted content becomes a requirement. |
| `difference-between-post-and-page` | Deleted | Export-only historical row | Do not canonicalize unless historical/deleted content becomes a requirement. |

## Recommended Reading Order

Core VS MCP Bridge architecture series:

1. `vs-mcp-bridge-blog-series-part-1`
2. `vs-mcp-bridge-blog-series-part-2`
3. `vs-mcp-bridge-blog-series-part-3`
4. `vs-mcp-bridge-blog-series-part-4`
5. `vs-mcp-bridge-blog-series-part-5`
6. `vs-mcp-bridge-blog-series-part-6`
7. `vs-mcp-bridge-blog-series-part-7`

Transport/runtime deep dives:

1. `how-stdio-works-in-vs-mcp-bridge`
2. `understanding-a-named-pipe-listener`
3. `understanding-local-mcp-server-over-stdio-and-local-only-communication-over-a-named-pipe`
4. `wpf-vsix-threading-understanding-ui-switching-async-behavior-and-pipe-safety`
5. `why-vsix-project-should-target-net-framework-4-7-2`

BlogAI narrative series:

1. `inference-driven-software-design-with-copilot-pros-and-cons`
2. `understanding-ai-chat-sessions-models-and-agents`
3. `vs-mcp-bridge-blog-series-part-2`
4. `vs-mcp-bridge-blog-series-part-7`

## Remaining Gaps Before Broader Publishing

- Canonical content has not been written back to the BlogEngine database.
- Updated canonical posts still need runtime/render verification on `https://www.global-webnet.com` after any publish operation.
- `GwnWikiExtension` token mappings are preserved, but no token/link migration or production-domain normalization has been performed.
- The live Part 4 post and repo-only Part 4 trial draft are separated, but future publish tooling must continue to respect that mapping.
- Export-only legacy/support posts need explicit keep/archive/canonicalize decisions before broader BlogAI promotion.
- A safe compare-and-publish workflow should verify DB/runtime/canonical differences before any overwrite.
- Mermaid references point to source `.mmd` files; image generation remains deferred unless a lightweight repo convention is added.
