# Full Validation Checkpoint

## Summary

Full validation was run after the foundational security architecture phase.

Validated checkpoint:

- branch: `main`
- starting commit: `fc8b035 Document foundational security architecture seams`
- working tree at start: clean
- scope: validation only; no runtime code changes

## Commands Run

Repo sanity:

```powershell
git status --short --branch
git log --oneline -8
```

Result:

- `main...origin/main`
- HEAD was `fc8b035 Document foundational security architecture seams`

Shared tests:

```powershell
dotnet test .\VsMcpBridge.Shared.Tests\VsMcpBridge.Shared.Tests.csproj
```

Result:

- passed: 242
- failed: 0
- skipped: 0
- existing warnings observed in shared nullable annotations, MCP server nullable annotations, and one xUnit analyzer warning

App build:

```powershell
dotnet build .\VsMcpBridge.App\VsMcpBridge.App.csproj
```

Result:

- build succeeded
- warnings: 0
- errors: 0

VSIX build:

```powershell
.\scripts\build-vsix.ps1 -Restore -Project 'VsMcpBridge.Vsix.Tests\VsMcpBridge.Vsix.Tests.csproj'
```

Result:

- build succeeded through Visual Studio Insiders MSBuild
- VSIX package produced
- existing nullable warnings observed in `VsMcpBridge.Vsix\Services\VsService.cs`

VSIX tests:

```powershell
vstest.console.exe .\VsMcpBridge.Vsix.Tests\bin\Debug\net472\VsMcpBridge.Vsix.Tests.dll
```

Result:

- passed: 25
- failed: 0

MCP process check:

```powershell
Get-Process VsMcpBridge.McpServer -ErrorAction SilentlyContinue
```

Result:

- no orphaned `VsMcpBridge.McpServer` process was present before the MCP server build

MCP server build:

```powershell
dotnet build .\VsMcpBridge.McpServer\VsMcpBridge.McpServer.csproj
```

Result:

- build succeeded
- warnings: 0
- errors: 0

MCP stdio validation:

```powershell
.\scripts\validate-mcp.ps1 -TimeoutSeconds 20
```

Result:

- initialize passed
- `tools/list` passed with 16 tools
- `vs_get_active_document` was listed
- downstream VS-backed call was unavailable
- script exited successfully because MCP transport validation succeeded

Strict VS-backed MCP validation:

```powershell
.\scripts\validate-mcp.ps1 -TimeoutSeconds 20 -RequireVsToolSuccess
```

Result:

- initialize passed
- `tools/list` passed with 16 tools
- `vs_get_active_document` failed with generic tool invocation output
- `pipe-client.log` showed a named-pipe connection timeout to `VsMcpBridge`

## Findings

No regression was found in shared tests, app build, VSIX build/tests, MCP server build, or MCP stdio startup/tool discovery.

The only meaningful finding was operational:

- MCP stdio validation is healthy.
- `vs_get_active_document` is exposed by `tools/list`.
- strict live VS-backed validation requires the VSIX named-pipe side to be active.
- during this run, `PipeClient` timed out connecting to pipe `VsMcpBridge`, so the live VS-backed tool call could not be completed from automation.

Relevant log evidence:

- `%LocalAppData%\VsMcpBridge\Logs\McpServer\pipe-client.log`
- timeout occurred while connecting to `VsMcpBridge` for `vs_get_active_document`

This is consistent with the known operational rule: opening the `VS MCP Bridge` tool window in the Experimental Instance initializes the VSIX/named-pipe side. Future live MCP validation should explicitly activate the tool window before requiring pipe-backed tools to succeed.

## Scope Notes

No runtime code changes were made.

No OAuth, authentication, RBAC, vault integration, sandboxing, remote authorization, plugin signing, audit export, or SIEM work was introduced.

The foundational security seams remain validated at the shared executor boundary through automated shared tests. Live VS-backed validation still depends on an activated Visual Studio Experimental Instance and should be treated as an environment step, not a shared-code regression, unless the pipe server is active and the same call still fails.
