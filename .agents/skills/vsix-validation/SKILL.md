---
name: vsix-validation
description: Build and validate the Visual Studio extension host, tests, and live activation preconditions.
---

# VSIX Validation

## Use When

- Changing `VsMcpBridge.Vsix`, Visual Studio service code, package activation, command tables, or tool-window behavior.
- Validating live VS-backed tools.
- Debugging inactive named-pipe or Experimental Instance behavior.

## Workflow

1. Read `README.md`, `SolutionFolder/docs/ARCHITECTURE.md`, and `SolutionFolder/docs/LOGGING_DIAGNOSTIC_RUNBOOK.md`.
2. Build the VSIX with `.\scripts\build-vsix.ps1 -Restore`.
3. Run VSIX tests with the Visual Studio test runner documented in `README.md` when code changes warrant it.
4. For live validation, launch the Experimental Instance, open `View -> Other Windows -> VS MCP Bridge`, then invoke VS-backed tools.
5. If build output is locked, check for an orphaned `VsMcpBridge.McpServer` process before rebuilding.
6. Preserve tool-window, pipe-server, and activation evidence when the validation result changes the resume point.

## References

- `README.md`
- `SolutionFolder/docs/ARCHITECTURE.md`
- `SolutionFolder/docs/LOGGING_DIAGNOSTIC_RUNBOOK.md`
- `SolutionFolder/docs/vsix-host-ping-trace-workflow.md`
- `SolutionFolder/docs/vsix-host-selected-text-trace-workflow.md`
- `SolutionFolder/docs/session-handoffs/2026-05-16-vsix-activation-diagnostic-validation.md`
