# Repository Agent Guide

## Purpose

`vs-mcp-bridge` is a local MCP integration that exposes selected Visual Studio and workspace state to AI tooling through a conservative host bridge.

Use this file as the high-level operating guide for AI agents. It is intentionally small. Follow links to the durable source-of-truth documents instead of expanding this file with copied architecture, runbook, or handoff prose.

## Source Of Truth

Read these first when the task needs repository context:

1. `AI_START.md` for session bootstrap and resume routing.
2. `docs/ARCHITECTURE.md` for current system behavior. This is the primary architecture source of truth.
3. `docs/AI_WORKFLOW.md` for AI role boundaries and execution expectations.
4. The newest relevant file under `docs/session-handoffs/` when resuming or changing an established slice.

Use targeted docs as needed:

- `docs/LOGGING_DIAGNOSTIC_RUNBOOK.md` for logging, diagnostics, activation, and triage.
- `docs/tool-execution-trace-workflow.md` for compiled bridge tool execution evidence.
- `docs/MVPVM_OVERVIEW.md` for shared WPF presenter/viewmodel boundaries.
- `docs/blogs/README.md` for blog source-of-truth and publishing review workflows.
- `docs/gated_turn-based_workflow-Codex.txt` for gated collaboration workflow expectations.

## Operating Rules

- Preserve the architecture described in `docs/ARCHITECTURE.md`; update that file when current system behavior changes.
- Keep MCP stdio clean. Route diagnostics through approved UI, file, Debug, or StdErr channels.
- Preserve the anti-black-box standard: important workflows should be runnable end to end, captured as correlated evidence, and diagrammable from observed logs.
- New or changed boundary-crossing code should preserve request/operation correlation, structured success/failure results, elapsed timing where useful, and redaction before durable logs or audit metadata.
- Keep vertical slices incremental. Do not introduce broad framework, transport, auth, sandboxing, persistence, or UI redesign work inside unrelated slices.
- Keep proposal mutation safety intact: AI tools suggest or propose; validated approval/apply flows mutate.
- Preserve `BridgeToolExecutor` as the shared bridge tool execution, policy, approval, redaction, and audit boundary.
- Treat MEF bridge tool support as discovery-only unless a future explicit design slice changes that.

## Validation Expectations

Choose validation by blast radius:

- Documentation-only changes: run `git diff --check`.
- Shared-layer logic changes: run `dotnet test .\VsMcpBridge.Shared.Tests\VsMcpBridge.Shared.Tests.csproj`.
- VSIX changes: use `.\scripts\build-vsix.ps1 -Restore` and the documented VSIX test runner from `README.md`.
- Runtime workflow changes: capture durable evidence under the established `artifacts/logs/`, `docs/diagrams/`, and handoff patterns.

## Git And Codex Rules

- Check branch and working tree before edits.
- Do not revert unrelated user changes.
- Prefer terminal `git` commands for repository operations.
- Avoid Codex Desktop Git/GitHub UI completion flows.
- Commit messages should describe the completed slice.

## Skills

Task-oriented agent skills live under `.agents/skills/`.

Use them for progressive disclosure:

- `.agents/skills/mcp-validation/SKILL.md`
- `.agents/skills/vsix-validation/SKILL.md`
- `.agents/skills/trace-artifact-workflow/SKILL.md`
- `.agents/skills/architecture-handoff/SKILL.md`
- `.agents/skills/blog-publishing-review/SKILL.md`
- `.agents/skills/security-seam-development/SKILL.md`
