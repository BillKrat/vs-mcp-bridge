# Codex Round-Trip Proposal Unblocked

- Codex MCP config path is working.
- Bridge tool visibility is confirmed.
- `vs_propose_text_edit` previously failed with an empty VSIX response.
- Root cause was WPF UI-thread marshalling in the proposal UI path.

## Fix Applied

- [VsMcpBridge.Shared/MvpVm/LogToolWindowPresenter.cs](\\?\UNC\Mac\Dev\vs-mcp-bridge\VsMcpBridge.Shared\MvpVm\LogToolWindowPresenter.cs)
- [VsMcpBridge.Vsix/Services/ThreadHelperAdapter.cs](\\?\UNC\Mac\Dev\vs-mcp-bridge\VsMcpBridge.Vsix\Services\ThreadHelperAdapter.cs)
- [VsMcpBridge.Vsix.Tests/VsServiceTests.cs](\\?\UNC\Mac\Dev\vs-mcp-bridge\VsMcpBridge.Vsix.Tests\VsServiceTests.cs)

## Validation

- A real proposal surfaced in the Experimental Instance.
- The user clicked Keep and the change applied.
- `VsMcpBridge.Shared/Loggers/LogLevelSettings.cs` only needed the applied round-trip validation comment; no further cleanup was required.
- The apparent build/editor issue in `LogLevelSettings.cs` was stale state, not a real compile problem.

## Local Convenience Artifact

- Added local launcher script: [launch-exp.cmd](\\?\UNC\Mac\Dev\vs-mcp-bridge\launch-exp.cmd)
- Purpose: launch Visual Studio 2026 Insiders Experimental Instance directly with `/RootSuffix Exp /Log`.
- This script is intentionally left untracked because it is machine-local convenience glue with a detected local install path.
