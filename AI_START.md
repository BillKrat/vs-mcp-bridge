# AI Start

This is the canonical entry point for starting or resuming an AI-assisted session in `vs-mcp-bridge`.

If you are an AI assistant starting fresh, read this file first.

For session closeout, use `AI_STOP.md`.

For repository-level agent operating rules, use `AGENTS.md`. For task-specific progressive disclosure, use focused skills under `.agents/skills/` instead of expanding this file with workflow details.

## Purpose

Use this document to quickly establish the minimum reliable context for a productive session without assuming `README.md` alone contains every current handoff detail.

This file does not replace the detailed docs. It tells you what to read next and in what order.

## Fast Start Rules

1. Treat repository files as the source of truth.
2. Prefer current docs and current code over prior chat history.
3. For a new session, start with the core grounding set below.
4. For a resumed session, read the latest relevant handoff in `docs/session-handoffs/` after the core grounding set.
5. Before changing code, confirm whether there is an active handoff or recent manual-test finding that changes priorities.

## Core Grounding Set

Read these first in order:

1. `README.md`
2. `AGENTS.md`
3. `docs/ARCHITECTURE.md`
4. `docs/VS_MCP_BRIDGE_TECHNICAL_ANALYSIS.md`
5. `docs/CODING_STANDARDS.md`
6. `docs/AI_WORKFLOW.md`

Use these as needed after that:

- `docs/MVPVM_OVERVIEW.md` for UI/presenter/viewmodel boundaries
- `docs/LOGGING_DIAGNOSTIC_RUNBOOK.md` for logging/diagnostic validation and triage
- `docs/tool-package-boundary-plan.md` for future Adventures.Mcp, Adventures.Tools, and host-specific tool-pack planning
- `docs/gated_turn-based_workflow-Codex.txt` for gated collaboration workflow expectations
- `.agents/skills/` for focused task workflows such as MCP validation, MCP search diagnostics, VSIX validation, trace artifacts, architecture handoffs, blog publishing review, and security seam development

## New Session vs Resume Session

### If starting a new session

After reading the core grounding set:

1. inspect `docs/session-handoffs/`
2. read the most recent handoff that matches the current work area
3. check the current branch and working tree state
4. ask or infer the next concrete slice from the user request and current handoff state

### If resuming an existing session

After reading the core grounding set:

1. read the handoff file explicitly named by the user, if one is provided
2. otherwise, inspect `docs/session-handoffs/` and choose the most recent relevant handoff
3. treat that handoff plus the current repository state as the active resume point
4. verify whether any later commits or docs have changed the intended next step

## Current Known Resume Point

If the user does not specify another handoff, start here first:

- `docs/session-handoffs/2026-05-16-platform-self-description-handoff.md`

That handoff summarizes the current MCP/tool-platform self-description milestone. The related compiled tool execution and activation paths now have durable observed artifacts:

- `docs/tool-execution-trace-workflow.md`
- `artifacts/logs/tool-regex-search-trace-20260509.log`
- `artifacts/logs/tool-security-trace-20260509.log`
- `artifacts/logs/mef-discovery-trace-20260516.log`
- `artifacts/logs/tool-approval-trace-20260516.log`
- `artifacts/logs/tool-manifest-trace-20260516.log`
- `artifacts/logs/tool-inventory-trace-20260516.log`
- `artifacts/logs/mcp-tool-inventory-trace-20260516.log`
- `artifacts/logs/mcp-tool-inventory-live-validation-20260516.log`
- `artifacts/logs/vsix-activation-diagnostic-trace-20260516.log`
- `docs/diagrams/tool-regex-search-trace-20260509.mmd`
- `docs/diagrams/tool-security-trace-20260509.mmd`
- `docs/diagrams/mef-discovery-trace-20260516.mmd`
- `docs/diagrams/tool-approval-trace-20260516.mmd`
- `docs/diagrams/tool-manifest-trace-20260516.mmd`
- `docs/diagrams/tool-inventory-trace-20260516.mmd`
- `docs/diagrams/mcp-tool-inventory-trace-20260516.mmd`
- `docs/diagrams/mcp-tool-inventory-live-validation-20260516.mmd`
- `docs/diagrams/vsix-activation-diagnostic-trace-20260516.mmd`

Use those artifacts when reconstructing or triaging the compiled bridge tool execution path before relying on chat history. For VSIX selected-text work, use `docs/session-handoffs/2026-05-09-selected-text-validation.md`.

For the security-aware compiled tool boundary, also read `docs/session-handoffs/2026-05-09-tool-security-validation.md`.
For the MEF discovery-only boundary, also read `docs/session-handoffs/2026-05-16-mef-discovery-trace-validation.md`.
For the approval-aware tool execution boundary, also read `docs/session-handoffs/2026-05-16-tool-approval-validation.md`.
For bridge tool manifest metadata flow, also read `docs/session-handoffs/2026-05-16-tool-manifest-validation.md`.
For bridge tool catalog inventory behavior, also read `docs/session-handoffs/2026-05-16-tool-inventory-validation.md`.
For MCP-exposed bridge tool inventory diagnostics, also read `docs/session-handoffs/2026-05-16-mcp-tool-inventory-validation.md`.
For live MCP stdio validation of the inventory diagnostic, also read `docs/session-handoffs/2026-05-16-mcp-tool-inventory-live-validation.md`.
For MCP-exposed regex search validation through `BridgeToolExecutor`, also read `docs/session-handoffs/2026-05-16-mcp-regex-search-validation.md`.
For MCP-exposed BM25 search validation through `BridgeToolExecutor`, also read `docs/session-handoffs/2026-05-16-mcp-bm25-search-validation.md`.
For the current foundational security architecture seams and deferred scope, also read `docs/session-handoffs/2026-05-16-security-architecture-foundation.md`.
For the latest full validation checkpoint, also read `docs/session-handoffs/2026-05-16-full-validation-checkpoint.md`.
For inactive VSIX named-pipe activation diagnostics, also read `docs/session-handoffs/2026-05-16-vsix-activation-diagnostic-validation.md`.
For the end-of-day architecture/security summary, also read `docs/session-handoffs/2026-05-16-end-of-day-architecture-handoff.md`.
For the current platform self-description milestone and next-step options, read `docs/session-handoffs/2026-05-16-platform-self-description-handoff.md`.
For BlogAI platform direction and operational philosophy, read `docs/session-handoffs/2026-05-16-blogai-platform-direction.md`.
For the current transition from foundational MCP/tool-platform work into operational BlogAI-assisted development, read `docs/session-handoffs/2026-05-17-platform-to-blogai-transition.md`.
For initial BlogAI authentication/API boundary direction, read `docs/blogai-auth-api-boundary-note.md`.
For the conceptual BlogAI auth trust-boundary flow sketch, read `docs/blogai-auth-trust-boundary-flow.md`.
For the proposed first BlogAI authentication implementation boundary, read `docs/adr/0001-blogai-first-auth-boundary.md`.
Before any BlogAI auth prototype, read `docs/blogai-auth-implementation-gate.md`.
For the BlogAI auth gate review result, read `docs/session-handoffs/2026-05-17-blogai-auth-gate-review.md`.
For the clarified minimal BlogAI auth prototype objective and local validation expectations, read `docs/blogai-minimal-auth-prototype-clarification.md`.
For the reusable Global WebNet auth/API boundary direction with BlogAI as first consumer, read `docs/global-webnet-auth-boundary-direction.md`.
For the local-only `AdventuresAuth` prototype design, read `docs/adventures-auth-local-prototype-design.md`.
For the future local/dev `AdventuresAuth` API boundary design, read `docs/adventures-auth-local-api-boundary-design.md`.
For the final readiness gate before implementing the local-only `AdventuresAuth` prototype, read `docs/session-handoffs/2026-05-17-adventures-auth-readiness-review.md`.
For durable validation evidence of the local-only `AdventuresAuth` prototype skeleton, read `docs/session-handoffs/2026-05-17-adventures-auth-local-prototype-validation.md`.
For the BlogAI consumer boundary for `AdventuresAuth`, read `docs/blogai-adventures-auth-consumer-design.md`.
For the minimal BlogAI-side `AdventuresAuth` consumer prototype plan, read `docs/blogai-minimal-auth-consumer-prototype-plan.md`.
For the readiness gate before implementing the minimal BlogAI-side `AdventuresAuth` consumer prototype, read `docs/session-handoffs/2026-05-17-blogai-auth-consumer-readiness-review.md`.
For the implemented local/dev-only BlogAI-side `AdventuresAuth` consumer adapter, inspect `VsMcpBridge.Shared/BlogAI/Auth`, `AddBlogAiAuthConsumerServices`, and `VsMcpBridge.Shared.Tests/BlogAiAuthConsumerTests.cs`.
For durable validation evidence of the local/dev-only BlogAI-side `AdventuresAuth` consumer boundary, read `docs/session-handoffs/2026-05-17-blogai-auth-consumer-validation.md`.
For the first practical BlogAI usage path that should pressure-test the MCP/tool platform without premature auth, API, deployment, or package work, read `docs/blogai-functional-pressure-test-plan.md`.
For the concrete first-session checklist for applying current MCP/tools to BlogAI work, read `docs/blogai-first-pressure-test-session.md`.
For findings from the first BlogAI pressure-test pass, read `docs/session-handoffs/2026-05-16-blogai-pressure-test-findings.md`.
For the direct MCP inventory validation and BlogAI stale shared chrome search findings, read `docs/session-handoffs/2026-05-16-blogai-stale-chrome-search-findings.md`.
For the MCP regex-tool rerun of the BlogAI stale shared chrome search, read `docs/session-handoffs/2026-05-16-blogai-stale-chrome-mcp-regex-search.md`.
For the first real BlogAI workflow using both MCP regex and BM25 diagnostics, read `docs/session-handoffs/2026-05-16-blogai-mcp-search-workflow-findings.md`.
For the BlogAI workflow using MCP document selection plus regex/BM25 search, read `docs/session-handoffs/2026-05-16-blogai-doc-selection-search-workflow.md`.
For practical MCP search workflow ergonomics gaps and conservative next tooling slices, read `docs/mcp-search-workflow-ergonomics-gap-list.md`.
For distinguishing canonical/current source from preserved historical or diagnostic evidence during MCP searches, read `docs/evidence-classification-guidance.md`.
For the architectural threshold before any future MCP-assisted repository mutation tools, read `docs/mcp-controlled-mutation-threshold.md`.
For the preview-only design of the first future mutation-adjacent document update tool, read `docs/preview-only-document-update-tool-design.md`.
For practical preview diff readability improvements that preserve the no-mutation boundary, read `docs/preview-diff-ergonomics-plan.md`.
For validation of the implemented preview-only MCP document update tool, read `docs/session-handoffs/2026-05-17-preview-document-update-validation.md`.
For MCP-exposed explicit repo document selection validation, also read `docs/session-handoffs/2026-05-16-document-selection-validation.md`.

## Current Working Guidance

Keep these repo-specific themes in mind:

- preserve the decoupled host pattern with no App↔VSIX coupling
- prefer `IConfiguration`-based runtime configuration over scattered direct environment reads
- keep logging useful to operators: `Trace` for verbose diagnostics, `Information` for meaningful user-facing output
- do not remove problematic logging blindly; prefer disabling, suppressing, or relocating it until the correct surfacing is understood
- keep MVP/VM boundaries intact in shared WPF UI work
- keep MCP stdout clean and route diagnostics through approved channels
- during MCP/tooling triage, use `bridge_get_tool_inventory` early when available; it is a safe read-only MCP diagnostic that returns deterministic bridge tool manifest metadata without executing bridge tools
- for MCP search diagnostics, use `.agents/skills/mcp-search-diagnostics/SKILL.md`; `bridge_select_repo_documents` is for deterministic metadata-only file selection, `bridge_regex_text_search` is for exact/regex searches, `bridge_bm25_text_search` is for ranked relevance over explicit documents, and the search tools do not read paths or crawl files
- when MCP search results include preserved BlogAI evidence, distinguish `canonical-current` sources from `historical-evidence`, `rendered-failure-evidence`, and `diagnostic-trace` sources before recommending action
- do not introduce MCP mutation tools without the threshold documented in `docs/mcp-controlled-mutation-threshold.md`; today Codex repository edits still happen through normal repo workflows, not MCP mutation tools, and `bridge_preview_document_update` remains preview-only with no write/apply path
- preserve the anti-black-box standard: important workflows should be runnable end-to-end with captured evidence that can generate a Mermaid sequence diagram matching the observed application flow
- require new code to participate in the established logging/correlation pattern when it crosses meaningful boundaries or needs future AI triage
- preserve the shared tool execution security seams in `VsMcpBridge.Shared.Security`: policy evaluation, approval evaluation when required, redaction, audit envelope emission, secret-reference hooks, and simple capability hooks are contracts for future hardening, not an invitation to add OAuth, sandboxing, MEF, or enterprise security infrastructure in unrelated slices
- treat MEF bridge tool support as a discovery-only seam: compiled tools remain the default path, directory discovery must be explicitly configured, and all discovered tools still execute through `BridgeToolExecutor`
- treat `Bm25TextSearchTool` as a compiled, request-scoped, in-memory ranking tool only; it is not a persistent index, crawler, external search integration, or directory-loaded plugin model

## When Ending A Session

Before ending a meaningful work session:

1. update durable docs if behavior or priorities changed
2. create or update a handoff in `docs/session-handoffs/` if the next session would benefit from a clean resume point
3. record concrete next steps, validation status, and any known blockers

## Suggested User Prompt

A good session bootstrap prompt is:

- `Read AI_START.md and use it to establish context before doing anything else.`

A good resume prompt is:

- `Read AI_START.md, then resume from docs/session-handoffs/2026-05-09-tool-execution-validation.md.`

A good closeout prompt is:

- `Read AI_STOP.md and do the required closeout updates before ending the session.`
