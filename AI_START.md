# AI Start

This is the canonical entry point for starting or resuming an AI-assisted session in `vs-mcp-bridge`.

If you are an AI assistant starting fresh, read this file first.

For session closeout, use `AI_STOP.md`.

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
2. `docs/ARCHITECTURE.md`
3. `docs/VS_MCP_BRIDGE_TECHNICAL_ANALYSIS.md`
4. `docs/CODING_STANDARDS.md`
5. `docs/AI_WORKFLOW.md`

Use these as needed after that:

- `docs/MVPVM_OVERVIEW.md` for UI/presenter/viewmodel boundaries
- `docs/LOGGING_DIAGNOSTIC_RUNBOOK.md` for logging/diagnostic validation and triage
- `docs/gated_turn-based_workflow-Codex.txt` for gated collaboration workflow expectations

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

- `docs/session-handoffs/2026-05-16-mef-discovery-trace-validation.md`

That handoff captures the completed MEF discovery-only trace validation. The related compiled tool execution path now has durable observed artifacts:

- `docs/tool-execution-trace-workflow.md`
- `artifacts/logs/tool-regex-search-trace-20260509.log`
- `artifacts/logs/tool-security-trace-20260509.log`
- `artifacts/logs/mef-discovery-trace-20260516.log`
- `artifacts/logs/tool-approval-trace-20260516.log`
- `artifacts/logs/vsix-activation-diagnostic-trace-20260516.log`
- `docs/diagrams/tool-regex-search-trace-20260509.mmd`
- `docs/diagrams/tool-security-trace-20260509.mmd`
- `docs/diagrams/mef-discovery-trace-20260516.mmd`
- `docs/diagrams/tool-approval-trace-20260516.mmd`
- `docs/diagrams/vsix-activation-diagnostic-trace-20260516.mmd`

Use those artifacts when reconstructing or triaging the compiled bridge tool execution path before relying on chat history. For VSIX selected-text work, use `docs/session-handoffs/2026-05-09-selected-text-validation.md`.

For the security-aware compiled tool boundary, also read `docs/session-handoffs/2026-05-09-tool-security-validation.md`.
For the MEF discovery-only boundary, also read `docs/session-handoffs/2026-05-16-mef-discovery-trace-validation.md`.
For the approval-aware tool execution boundary, also read `docs/session-handoffs/2026-05-16-tool-approval-validation.md`.
For the current foundational security architecture seams and deferred scope, also read `docs/session-handoffs/2026-05-16-security-architecture-foundation.md`.
For the latest full validation checkpoint, also read `docs/session-handoffs/2026-05-16-full-validation-checkpoint.md`.
For inactive VSIX named-pipe activation diagnostics, also read `docs/session-handoffs/2026-05-16-vsix-activation-diagnostic-validation.md`.

## Current Working Guidance

Keep these repo-specific themes in mind:

- preserve the decoupled host pattern with no App↔VSIX coupling
- prefer `IConfiguration`-based runtime configuration over scattered direct environment reads
- keep logging useful to operators: `Trace` for verbose diagnostics, `Information` for meaningful user-facing output
- do not remove problematic logging blindly; prefer disabling, suppressing, or relocating it until the correct surfacing is understood
- keep MVP/VM boundaries intact in shared WPF UI work
- keep MCP stdout clean and route diagnostics through approved channels
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
