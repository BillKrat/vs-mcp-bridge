# End-of-Day Architecture Handoff

## Checkpoint

- branch: `main`
- repository state: `main == origin/main`
- HEAD: `5e666fc Add VSIX activation diagnostic trace artifacts`
- working tree expectation: clean

This handoff summarizes the completed architecture, security, and diagnostic slices from the current phase so future sessions do not need to reconstruct them from chat history.

## Completed Work

- `edf819d Add compiled BM25 text search tool`
  Added the compiled BM25 bridge search tool alongside the existing regex search path.
- `b325ecc Copilot: Record MCP activation triage`
  Recorded Visual Studio, Codex, and Copilot MCP activation triage and operating notes.
- `1d2cfc7 Add approval-aware tool execution seam`
  Added minimal approval-aware execution contracts while preserving default auto-run behavior.
- `cf40c7d Add approval-aware tool execution trace artifacts`
  Added durable approval allow/deny trace artifacts.
- `40608d2 Add bridge tool capability metadata seam`
  Let bridge tool descriptors declare required capabilities as metadata.
- `e34f527 Add capability-aware tool execution policy`
  Added optional capability-based policy evaluation without introducing auth, users, OAuth, RBAC, persistence, or prompts.
- `75d347d Add secret reference and broker seam`
  Added structured secret references and a broker seam without real secret storage.
- `7cd6892 Add structured audit classification metadata`
  Added category, severity, risk, and outcome metadata to audit envelopes.
- `fc8b035 Document foundational security architecture seams`
  Added the consolidated security architecture handoff.
- `01a2e27 Add full validation checkpoint handoff`
  Recorded full validation results and the environment-dependent live VS-backed finding.
- `cb0bc6a Improve VSIX activation diagnostics for pipe-backed MCP tools`
  Converted inactive named-pipe timeouts into clear activation diagnostics.
- `5e666fc Add VSIX activation diagnostic trace artifacts`
  Added durable inactive VSIX named-pipe diagnostic artifacts and handoff guidance.

Related MCP activation documentation and trace references now live in:

- `docs/LOGGING_DIAGNOSTIC_RUNBOOK.md`
- `docs/tool-execution-trace-workflow.md`
- `docs/session-handoffs/2026-05-16-full-validation-checkpoint.md`
- `docs/session-handoffs/2026-05-16-vsix-activation-diagnostic-validation.md`

## Stable Validation State

- latest shared test count: 243 passing
- latest MCP server build: passing
- latest App build: passing
- VSIX build/tests passed in the full validation checkpoint
- MCP stdio validation passed initialize and `tools/list` with 16 tools
- live VS-backed validation requires VSIX/tool-window activation before pipe-backed tools can succeed
- inactive VSIX named-pipe diagnostic path is documented with log, metadata, Mermaid, and handoff artifacts

## Operating Rules

- Terminal `git` and `gh` commands are safe for repo work.
- Avoid Codex Desktop Git/GitHub UI flows if instability returns; terminal flows have been stable.
- VS-backed MCP tools require the Visual Studio Experimental Instance and the `VS MCP Bridge` tool window to be active.
- Opening `View -> Other Windows -> VS MCP Bridge` initializes the VSIX/named-pipe side.
- Orphaned `VsMcpBridge.McpServer` processes can lock MCP server build outputs; check with:

```powershell
Get-Process VsMcpBridge.McpServer -ErrorAction SilentlyContinue
```

- Preserve `BridgeToolExecutor` as the execution/security/audit/redaction boundary for shared bridge tools.
- Do not treat capability metadata, approval seams, secret references, or audit classification as full auth, UI prompting, vault integration, or compliance infrastructure.

## Recommended Next-Step Options

Do not start these automatically; choose one based on the next user request.

- Wire a real user-facing approval prompt path while preserving the existing approval service seam.
- Document bridge tool manifest/schema conventions for descriptors, capabilities, approval requirements, and secret references.
- Add a BM25 durable trace artifact if search-ranking evidence becomes important.
- Plan package/namespace extraction for shared tool/security contracts if the repo starts separating plugin/runtime packages.
- Improve VS-backed live validation automation so it can confirm the Experimental Instance/tool-window activation path more reliably.
- Clean up nullable warnings in `VsMcpBridge.Vsix\Services\VsService.cs`.
- Continue tightening MCP activation diagnostics only where they reduce opaque failures without changing transport semantics.

## Resume Guidance

Start future architecture/security sessions from:

1. `AI_START.md`
2. `docs/ARCHITECTURE.md`
3. this handoff
4. `docs/session-handoffs/2026-05-16-security-architecture-foundation.md`
5. `docs/session-handoffs/2026-05-16-full-validation-checkpoint.md`
6. `docs/session-handoffs/2026-05-16-vsix-activation-diagnostic-validation.md`

If live VS-backed tools fail, first decide whether it is MCP stdio, tool discovery, named-pipe activation, or VS service execution. The inactive-pipe diagnostic artifacts are the reference for distinguishing named-pipe activation failure from MCP server failure.
