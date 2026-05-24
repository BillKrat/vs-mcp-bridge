# Platform To BlogAI Transition Handoff

## Purpose

Mark the transition from foundational MCP/tool-platform work into real operational BlogAI-assisted development.

This is a docs-only resume handoff. It adds no runtime code, tools, BlogAI features, authentication, deployment behavior, or apply/write capability.

## Checkpoint

- branch: `main`
- starting HEAD: `f1277b6 Document preview diff ergonomics plan`
- expected working tree: clean and aligned with `origin/main`
- current focus: use the existing MCP/tool platform for BlogAI work instead of adding more speculative infrastructure

## Current MCP/Tool Platform State

The platform now has enough validated surface area to support real BlogAI analysis:

- inventory: `bridge_get_tool_inventory` exposes deterministic compiled bridge-tool metadata for triage without executing tools
- document selection: `bridge_select_repo_documents` selects deterministic repo-root-relative metadata before explicit-input search
- regex search: `bridge_regex_text_search` executes exact/regex search over caller-provided text through `BridgeToolExecutor`
- BM25 search: `bridge_bm25_text_search` ranks caller-provided in-memory documents through `BridgeToolExecutor`
- preview-only update: `bridge_preview_document_update` reads one explicit target, verifies expected state, returns deterministic preview/diff metadata, and writes no files
- security seams: capability metadata, policy, approval metadata, redaction, audit envelope emission, request/operation correlation, and secret-reference contracts are in place as seams, not as full auth infrastructure
- MEF: discovery-only support exists, disabled/empty by default; discovered tools must still execute through `BridgeToolExecutor`
- guidance: `AGENTS.md`, `AI_START.md`, and `.agents/skills/*.md` define progressive disclosure, validation expectations, mutation boundaries, and trace-artifact workflow
- evidence: durable logs, metadata, Mermaid diagrams, and handoffs now cover inventory, regex, BM25, document selection, approval/security/manifest seams, MEF discovery, and preview-only document update

Key platform references:

- `SolutionFolder/docs/ARCHITECTURE.md`
- `SolutionFolder/docs/tool-execution-trace-workflow.md`
- `SolutionFolder/docs/mcp-controlled-mutation-threshold.md`
- `SolutionFolder/docs/preview-only-document-update-tool-design.md`
- `SolutionFolder/docs/preview-diff-ergonomics-plan.md`
- `SolutionFolder/docs/session-handoffs/2026-05-17-preview-document-update-real-workflow.md`

## BlogAI Pressure-Test Results

The BlogAI pressure-test work produced enough evidence to move from platform proving into operational BlogAI development:

- the stale shared chrome/cache issue still appears operational/cache-related, not a canonical post-body problem
- the stale `feature/approval-apply-ui-slice` marker was absent from canonical posts, selected current BlogAI source, and current after-update widget settings
- stale marker matches remained in historical diagnostics, rendered-failure evidence, preserved before-update widget evidence, and handoffs
- MCP regex and BM25 workflows validated the same conclusions without fallback `rg`
- `bridge_select_repo_documents` reduced manual entry assembly by selecting deterministic file metadata before caller-owned file reads and explicit-input search
- preview-only document update validated a real documentation workflow with no MCP mutation; Codex applied the accepted edit later through normal repository editing

Key BlogAI references:

- `SolutionFolder/docs/blogai-functional-pressure-test-plan.md`
- `SolutionFolder/docs/blogai-first-pressure-test-session.md`
- `SolutionFolder/docs/session-handoffs/2026-05-16-blogai-pressure-test-findings.md`
- `SolutionFolder/docs/session-handoffs/2026-05-16-blogai-stale-chrome-mcp-regex-search.md`
- `SolutionFolder/docs/session-handoffs/2026-05-16-blogai-mcp-search-workflow-findings.md`
- `SolutionFolder/docs/session-handoffs/2026-05-16-blogai-doc-selection-search-workflow.md`
- `SolutionFolder/docs/blogs/README.md`

## Safe Boundaries

Current MCP tools can:

- inspect inventory
- select explicit repo document metadata
- search caller-provided text
- rank caller-provided documents
- generate preview-only document update diffs
- preserve request/operation correlation and audit/security metadata

Current MCP tools still do not:

- write files
- apply patches
- crawl hidden file sets
- mutate repository state
- implement BlogAI features
- implement authentication
- clear production caches
- publish or deploy

Codex repository edits remain normal controlled repo edits: inspect diff, validate, commit, and push.
Any future MCP apply/write tool requires a separate approval-required design and implementation slice under `SolutionFolder/docs/mcp-controlled-mutation-threshold.md`.

## Recommended Next BlogAI Work

Start actual BlogAI operational development from `SolutionFolder/docs/blogai-functional-pressure-test-plan.md`.

Recommended first area:

- create an auth/API boundary architecture document for BlogAI
- keep implementation deferred until the boundary is explicit
- avoid OAuth/OpenID, deployment, or `api.global-webnet.com` implementation in the first pass
- use MCP inventory, document selection, regex, BM25, and preview-only update tools during analysis
- preserve durable findings under `SolutionFolder/docs/session-handoffs/` or targeted BlogAI docs rather than chat-only notes

The stale chrome/cache investigation remains a useful operational thread, but it should stay local-first and evidence-driven before any production cache-clear, app recycle, or deployment action.

## MCP/Tooling Work Only If Needed

Do not keep expanding platform infrastructure by default. Add tooling only when real BlogAI work exposes the need.

Reasonable future tooling slices:

- minimal diff ergonomics implementation for `bridge_preview_document_update`
- trace bundle or workflow-runner helper for inventory, document selection, regex, and BM25 evidence capture
- future approval-required apply tool design, still separate from preview
- VS-backed live validation hardening for named-pipe and Visual Studio-hosted workflows

Avoid:

- new MCP mutation tools inside BlogAI analysis
- broad file mutation workflows
- hidden crawlers or indexes
- auth implementation before boundary docs
- production publishing automation before local evidence is clear

## Resume Guidance

For the next BlogAI-assisted session, start with:

1. `AI_START.md`
2. this handoff
3. `SolutionFolder/docs/blogai-functional-pressure-test-plan.md`
4. `SolutionFolder/docs/blogai-first-pressure-test-session.md`
5. `SolutionFolder/docs/blogs/README.md`
6. the newest relevant BlogAI handoff or pressure-test evidence listed above

Suggested next prompt:

`Resume from SolutionFolder/docs/session-handoffs/2026-05-17-platform-to-blogai-transition.md and start a docs-only BlogAI auth/API boundary architecture pass. Do not implement auth or runtime code.`
