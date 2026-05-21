# Platform Self-Description Handoff

## Checkpoint

- branch: `main`
- repository state: `main == origin/main`
- latest HEAD: `6fe6478 Document MCP tool inventory discovery guidance`
- working tree expectation: clean

This is the concise resume point for the current MCP/tool-platform milestone. Use it with `AI_START.md`, `AGENTS.md`, and targeted `SKILL.md` files instead of reconstructing the platform history from chat.

## Completed Platform Capabilities

- `BridgeToolExecutor` is the shared execution, policy, approval, redaction, audit, and trace boundary for executable bridge tools.
- Bridge tools expose lightweight descriptor-derived manifests with stable identity, version, category, discovery/source, host affinity, capability, approval, risk, and execution metadata.
- `IBridgeToolInventoryService` returns deterministic catalog snapshots ordered by tool id without executing tools.
- MCP exposes `bridge_get_tool_inventory` as a safe read-only inventory diagnostic for AI triage.
- MEF bridge tool support exists as a discovery-only seam; compiled tools remain the default path.
- Approval-aware execution, capability-aware policy, structured secret references, and audit classification metadata are in place as minimal seams, not full auth, RBAC, vault, or compliance systems.
- Durable trace artifacts cover compiled tool execution, security, approval, manifest metadata, catalog inventory, MCP inventory diagnostics, MEF discovery, and VSIX activation diagnostics.
- `AGENTS.md`, `AI_START.md`, and `.agents/skills/` now point future agents toward progressive-disclosure workflows, including early use of `bridge_get_tool_inventory`.
- MCP validation has covered direct stdio initialization, tool listing, `chat_engine_ping`, and live `bridge_get_tool_inventory`; VS/Copilot and VSIX activation diagnostics are documented for pipe-backed tools.

## Current Operating Pattern

- Keep Codex sessions short and focused around one slice.
- Prefer terminal `git`/`gh` commands over Codex Desktop Git/GitHub UI flows.
- Start from `AGENTS.md`, `AI_START.md`, `docs/ARCHITECTURE.md`, and the newest relevant handoff; use `.agents/skills/` for task-specific detail.
- Preserve anti-black-box discipline: important workflows need durable logs, metadata where useful, Mermaid diagrams, and handoffs.
- Use `bridge_get_tool_inventory` as the first safe MCP inventory check when MCP is reachable. It returns deterministic manifest metadata, currently including `bridge.bm25TextSearch` and `bridge.regexTextSearch`, without executing bridge tools.

## Stable Validation State

- latest user-provided shared-test checkpoint: 249 passing
- latest observed shared-test run after inventory diagnostics: 253 passing
- MCP inventory live validation succeeded through direct stdio: initialize passed, `tools/list` returned 17 tools, and `bridge_get_tool_inventory` returned two compiled inventory items
- latest requested repo checkpoint before this handoff: `main == origin/main`, HEAD `6fe6478`, clean working tree

## Durable Evidence

- architecture source of truth: `docs/ARCHITECTURE.md`
- tool execution and inventory workflow: `docs/tool-execution-trace-workflow.md`
- live MCP inventory validation: `docs/session-handoffs/2026-05-16-mcp-tool-inventory-live-validation.md`
- package boundary plan: `docs/tool-package-boundary-plan.md`
- VSIX activation diagnostics: `docs/session-handoffs/2026-05-16-vsix-activation-diagnostic-validation.md`
- full validation checkpoint: `docs/session-handoffs/2026-05-16-full-validation-checkpoint.md`

## Next-Step Options

Choose one in a future slice; do not implement from this handoff alone.

- Integrate a user-facing approval prompt path while preserving the existing approval service seam.
- Add richer tool manifest/schema export for diagnostics and documentation.
- Expose a read-only MCP trace inventory if trace discovery becomes a repeated AI triage need.
- Start executing the package extraction plan only after seams and tests are stable enough to move.
- Harden VS-backed live validation automation for Experimental Instance/tool-window activation.
- Clean up nullable warnings in `VsService.cs` and related VS service code.
- Prepare future `Adventures.Mcp` extraction once external reuse is real.

## Resume Guidance

For the next platform/tooling session, start with:

1. `AI_START.md`
2. `AGENTS.md`
3. `docs/ARCHITECTURE.md`
4. this handoff
5. `.agents/skills/mcp-validation/SKILL.md` or `.agents/skills/trace-artifact-workflow/SKILL.md` as needed

If MCP tooling behavior is unclear, first call `bridge_get_tool_inventory`, then decide whether the issue is MCP stdio, inventory/catalog discovery, executable bridge tool execution, named-pipe activation, or VS service behavior.
