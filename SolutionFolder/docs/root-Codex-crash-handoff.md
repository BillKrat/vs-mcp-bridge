# Codex Crash Handoff

## Situation

Codex Desktop showed "Oops, an error has occurred" when using Git/GitHub-related UI/session flows. The issue appeared after GitHub audit logs showed a ChatGPT Codex Connector token regeneration and an older Visual Studio OAuth token revocation.

## Current Operating Rule

Codex may run explicit terminal `git` and `gh` commands from the shared workspace.

Codex should avoid Codex Desktop Git/GitHub UI controls or connector-driven Git UI flows until the crash is resolved.

The user should not be expected to run PowerShell commands manually. Codex should run required PowerShell, `git`, and `gh` commands unless there is a blocker requiring user interaction, such as browser authentication, MFA, or explicit confirmation for a risky operation.

Codex must not run destructive or high-impact Git actions such as `git reset --hard`, force pushes, or branch deletion unless the user explicitly requests that exact action.

## Verified State

- Workspace: `Y:\vs-mcp-bridge`
- Internal path resolution: `//Mac/Dev/vs-mcp-bridge`
- Repository: `BillKrat/vs-mcp-bridge`
- Branch before handoff work: `main`
- Status before handoff work: `main...origin/main [ahead 1]`
- Current status after follow-up triage: `main...origin/main`; `.mcp.json` and `SolutionFolder/docs/Codex-crash-handoff.md` are untracked/modified.
- Latest commit on `main` and `origin/main`: `edf819d Add compiled BM25 text search tool`
- Plain shell Git works.
- GitHub CLI `gh` works after Windows-side login.
- Codex GitHub connector tool calls worked from this session.
- Visual Studio Insiders MCP trust/tool confirmation is not blocking `vs-mcp-bridge` when run from the Experimental Instance.
- GitHub Copilot Chat in the Visual Studio Experimental Instance successfully invoked `vs_get_active_document`.
- GitHub Copilot Chat in the Visual Studio Experimental Instance successfully invoked `vs_list_solution_projects`.
- Codex-side direct MCP stdio validation succeeded: initialize, tools/list, and `chat_engine_ping`.
- Codex-side live VS-backed MCP validation succeeded against the Experimental Instance: `vs_get_active_document` and `vs_list_solution_projects`.
- The repo script `scripts\validate-mcp.ps1` hung while building its temporary helper, before MCP initialization; this appears to be a helper-build issue, not a server activation failure.
- The successful VS Copilot path required the VSIX side to be initialized in the Experimental Instance; opening the `VS MCP Bridge` tool window first is the recommended activation step.
- A failed Copilot activation previously left an orphaned `VsMcpBridge.McpServer.exe` process locking `VsMcpBridge.McpServer\bin\Debug\net10.0\VsMcpBridge.Shared.dll`; stopping that process restored clean builds.
- `README.md` and `docs\LOGGING_DIAGNOSTIC_RUNBOOK.md` were updated so future sessions do not repeat the same Visual Studio Copilot MCP activation and validation triage.

## Verified Commands

```powershell
git --version
# git version 2.49.0.windows.1

where.exe git
# C:\Program Files\Git\cmd\git.exe

git status --short --branch
# ## main...origin/main [ahead 1]

git status --short --branch
# ## main...origin/main
# ?? .mcp.json
# ?? SolutionFolder/docs/Codex-crash-handoff.md

git ls-remote --heads origin
# succeeded

git fetch --dry-run --verbose origin
# succeeded

git push --dry-run origin main
# succeeded

git fsck --no-progress
# exited cleanly; only dangling objects, no corruption
```

## GitHub CLI

Installed with:

```powershell
winget install --id GitHub.cli --exact --accept-package-agreements --accept-source-agreements
```

Verified:

```powershell
gh auth status
# Logged in to github.com account BillKrat (keyring)
# Git operations protocol: https
# Token scopes: gist, read:org, repo, workflow

gh repo view BillKrat/vs-mcp-bridge --json nameWithOwner,defaultBranchRef,viewerPermission,isPrivate
# {"defaultBranchRef":{"name":"main"},"isPrivate":false,"nameWithOwner":"BillKrat/vs-mcp-bridge","viewerPermission":"ADMIN"}
```

Codex GitHub connector verification also succeeded:

```text
_get_user_login
# {"login":"BillKrat","id":53179043}

_get_repo BillKrat/vs-mcp-bridge
# admin/push/pull permissions returned for BillKrat/vs-mcp-bridge
```

If `gh` is not found in the current Codex process, refresh PATH:

```powershell
$machine = [Environment]::GetEnvironmentVariable('Path','Machine')
$user = [Environment]::GetEnvironmentVariable('Path','User')
$env:Path = "$machine;$user"
```

## Visual Studio / MCP Triage Notes

Visual Studio 2026 Insiders added MCP server trust validation. During follow-up triage, the user selected all Git tools, added the `vs-mcp-bridge` MCP server, selected all `vs-mcp-bridge` tools, and allowed the tool run for the current session.

The first attempt failed with:

```text
Activation of the server 'vs-mcp-bridge' failed.
```

That attempt left an orphaned process:

```text
VsMcpBridge.McpServer.exe
Y:\vs-mcp-bridge\VsMcpBridge.McpServer\bin\Debug\net10.0\VsMcpBridge.McpServer.exe
```

The orphan locked the Debug output and caused `dotnet build .\VsMcpBridge.McpServer\VsMcpBridge.McpServer.csproj` to fail with `MSB3027` / `MSB3021` copying `VsMcpBridge.Shared.dll`. Stopping the orphaned process restored a clean build.

The successful sequence was:

1. Start Visual Studio Insiders Experimental Instance for this repo/VSIX.
2. Open `View -> Other Windows -> VS MCP Bridge` in the Experimental Instance to initialize the VSIX/named-pipe side.
3. Open a real editor document.
4. Use GitHub Copilot Chat in the same Experimental Instance.
5. Use Agent mode and allow `vs-mcp-bridge` tools for the current session.
6. Invoke `vs_get_active_document` and `vs_list_solution_projects`.

Both tool invocations succeeded after this sequence.

Visual Studio created `.mcp.json` in the repository root:

```json
{
  "inputs": [],
  "servers": {
    "vs-mcp-bridge": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "Y:\\\\vs-mcp-bridge\\\\VsMcpBridge.McpServer\\\\VsMcpBridge.McpServer.csproj"
      ],
      "env": {}
    }
  }
}
```

Codex-side direct MCP stdio validation:

```text
initialize: VsMcpBridge.McpServer 1.0.0.0
tools/list: 16 tools
chat_engine_ping: pong
```

Codex-side live VS-backed validation after launching the Experimental Instance:

```text
vs_get_active_document:
File: Y:\vs-mcp-bridge\Adventures.ChatEngine\Events\ChatEvent.cs
Language: CSharp

vs_list_solution_projects:
- VsMcpBridge.App (.NETCoreApp,Version=v8.0)
- VsMcpBridge.Vsix (.NETFramework,Version=v4.7.2)
- Miscellaneous Files
```

Targeted repo tests also passed:

```powershell
dotnet test .\VsMcpBridge.Shared.Tests\VsMcpBridge.Shared.Tests.csproj --no-build --filter "FullyQualifiedName~McpServerHostTests|FullyQualifiedName~VsToolsTests|FullyQualifiedName~Pipe"
# Passed: 70, Failed: 0, Skipped: 0
```

Known validation-script issue:

```powershell
.\scripts\validate-mcp.ps1 -TimeoutSeconds 20
# Hung at: [INFO] Building temporary MCP validation helper
```

Retrying after `dotnet build-server shutdown` and `MSBUILDDISABLENODEREUSE=1` reproduced the same helper-build hang. Direct MCP validation succeeded, so do not treat this script hang as evidence that `VsMcpBridge.McpServer` is broken.

## Current Next Steps

Continue avoiding Codex Desktop Git/GitHub UI controls until the Desktop crash is isolated. Prefer explicit terminal `git` / `gh` commands and Codex GitHub connector calls.

For VS-backed MCP validation, use the Experimental Instance sequence above. If activation fails again, first check for and stop orphaned server processes:

```powershell
Get-Process VsMcpBridge.McpServer -ErrorAction SilentlyContinue
```

Then rebuild:

```powershell
dotnet build .\VsMcpBridge.McpServer\VsMcpBridge.McpServer.csproj
```

The next validation action is to commit and push the triage documentation plus the Visual Studio-created `.mcp.json` with explicit shell `git`. If that succeeds without crashing Codex Desktop, terminal Git remains healthy; it may also indicate the newly enabled Git tooling has resolved or avoided the earlier crash path.
