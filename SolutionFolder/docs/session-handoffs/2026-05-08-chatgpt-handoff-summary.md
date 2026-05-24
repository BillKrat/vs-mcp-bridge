# ChatGPT Handoff Summary

Date: 2026-05-08
Repo: `Y:\vs-mcp-bridge`
Branch: `feature/approval-apply-ui-slice`
HEAD at summary time: `12b4cbf`
Reviewed range: `6f997f98..12b4cbf`

## How To Use This Handoff

External ChatGPT will not have direct access to this repository or these local files unless the user pastes content or uploads files manually.

If you give this handoff to ChatGPT outside the repo workspace, include this file's contents directly in the prompt or upload the referenced files.

## Recommended Read Order

1. `AI_START.md`
2. `README.md`
3. `SolutionFolder/docs/ARCHITECTURE.md`
4. `SolutionFolder/docs/VS_MCP_BRIDGE_TECHNICAL_ANALYSIS.md`
5. `SolutionFolder/docs/session-handoffs/2026-05-08-selected-text-prompt-investigation-handoff.md`

## Executive Summary

Recent work in `vs-mcp-bridge` was primarily a cross-host hardening pass focused on:

- standardizing runtime configuration through shared `IConfiguration`
- improving logging and diagnostics so host/process boundaries are observable
- creating repeatable automation/workflows and durable handoff artifacts so the bridge is no longer a black box to AI sessions or operators

The newest tip-of-branch change also fixes a VSIX active-document edge case where placeholder pseudo-path values such as `<no document>` were being treated as real document paths.

## What Changed

### 1. Shared runtime configuration bootstrap

The App host, VSIX host, and MCP host now share the same configuration bootstrap approach instead of reading runtime settings ad hoc from scattered environment-variable paths.

Key implementation points:

- `VsMcpBridge.Shared/Configuration/BridgeConfigurationFactory.cs`
- `VsMcpBridge.Shared/Constants/ConfigurationKeys.cs`
- `VsMcpBridge.App/App.xaml.cs`
- `VsMcpBridge.Vsix/VsMcpBridgePackage.cs`
- `VsMcpBridge.McpServer/Program.cs`

Shared source order now is:

1. environment variables
2. `VSMCPBRIDGE_`-prefixed environment variables
3. `appsettings.json`
4. `%LocalAppData%\VsMcpBridge\appsettings.user.json`

Because later sources override earlier ones, the user settings file currently has the highest precedence.

This was done so runtime behavior can evolve consistently across hosts and so AI debugging sessions do not need to reverse-engineer three different configuration models.

### 2. Centralized configuration keys and constants

Configuration and runtime string constants were centralized so the code no longer depends on repeated literal strings for key paths and bridge names.

Examples:

- `ConfigurationKeys.LoggingProvider`
- `ConfigurationKeys.LoggingMinimumLevel`
- `ConfigurationKeys.ChatEngineProvider`
- `BridgeRuntimeConstants.PipeName`

This reduced drift between the App host, VSIX host, MCP host, and tests.

### 3. End-to-end logging correlation and better diagnostics

The bridge now emits more useful boundary-focused logs with request correlation and elapsed timing across:

- shared presenter prompt submission/routing
- App host chat requests
- VSIX host chat requests
- MCP chat-tool and pipe-client boundaries
- shared named-pipe dispatch
- VSIX read operations such as `GetActiveDocument`, `GetSelectedText`, `ListSolutionProjects`, and `GetErrorList`

Key files:

- `VsMcpBridge.Shared/MvpVm/LogToolWindowPresenter.cs`
- `VsMcpBridge.App/Services/AppChatRequestService.cs`
- `VsMcpBridge.Vsix/Services/VsService.cs`
- `VsMcpBridge.McpServer/Pipe/PipeClient.cs`
- `VsMcpBridge.Shared/Services/PipeServer.cs`

The shared presenter now creates a request id before routing a prompt-box request, passes that id through `IChatRequestService`, and logs request/response lifecycle boundaries around prompt routing and UI state updates.

This makes a small observed request such as `ping -> pong` traceable across components instead of opaque.

### 4. Cleaner operator-facing log surfacing

When raw prompt/response audit logging is disabled, the tool window no longer emits placeholder chatter such as:

- `Prompt submitted. Raw prompt logging is disabled.`
- `Response received. Raw response logging is disabled.`

That noise had been mixing with useful request-boundary logs and reducing operator signal during manual validation.

The logging direction remains:

- `Trace` for verbose diagnostic flow
- `Information` for meaningful user-facing operational output
- `StdErr` for transport-safe out-of-band diagnostics that must not pollute MCP stdio JSON

### 5. Shared log-forwarding seam

The shared UI log sink now includes a forwarding seam so persistence can be added without changing core callers.

Key files:

- `VsMcpBridge.Shared/Interfaces/IBridgeLogForwarder.cs`
- `VsMcpBridge.Shared/Loggers/BridgeLogSink.cs`
- `VsMcpBridge.Shared/Loggers/FileBridgeLogForwarder.cs`
- `VsMcpBridge.Shared/Loggers/NullBridgeLogForwarder.cs`

Current first implementation:

- file-backed forwarding

Design intent:

- preserve current UI logging
- allow future persistence targets such as SQL-backed sinks without changing call sites

### 6. Durable AI session entry/exit and operational workflows

The repo now has explicit session bootstrap and closeout docs:

- `AI_START.md`
- `SolutionFolder/docs/AI_STOP.md`

It also now has repeatable trace-capture workflows and durable artifacts for both hosts:

- `SolutionFolder/docs/app-host-ping-trace-workflow.md`
- `SolutionFolder/docs/vsix-host-ping-trace-workflow.md`
- `SolutionFolder/artifacts/logs/...`
- `SolutionFolder/docs/diagrams/...`
- `SolutionFolder/docs/blog-drafts/...`

These workflows are intended to let future AI sessions:

- launch a host
- run a low-risk `ping` request
- capture correlated logs
- build a Mermaid sequence diagram from observed behavior
- compare the observed flow against the code

This is a major part of making the bridge no longer a black box.

### 7. MCP validation automation

The repo now includes a GitHub Actions workflow:

- `.github/workflows/mcp-validation.yml`

This adds lightweight automated validation around the MCP host path.

### 8. Latest VSIX active-document edge-case fix

The latest change at `12b4cbf` adds `VsService.HasUsableDocumentPath(...)` and updates active-document handling so placeholder values such as `<no document>` and similar pseudo-paths are not treated as real active files.

Key files:

- `VsMcpBridge.Vsix/Services/VsService.cs`
- `VsMcpBridge.Vsix.Tests/VsServiceTests.cs`

This aligns with manual validation showing that the built-in VSIX prompt `what is the active file` now behaves correctly in the prompt UI when there is no real document active.

## What Was Validated

Validated or documented during this work:

- App-host `ping` trace workflow with correlated logs and durable artifacts
- VSIX-host `ping` trace workflow with correlated logs and durable artifacts
- shared configuration bootstrap behavior covered by tests
- shared log forwarding seam covered by tests
- latest placeholder-path active-document fix covered by tests
- manual VSIX validation that `what is the active file` works as expected after the placeholder-path fix

## Key Architectural Direction

The main design direction reinforced by this range is:

- preserve decoupled hosts with no App↔VSIX coupling
- prefer shared infrastructure in `VsMcpBridge.Shared`
- prefer `IConfiguration` over scattered direct environment reads
- keep MCP stdout clean
- route diagnostics through approved transport-safe channels
- make host/process boundaries observable with correlation-oriented logging
- leave enough durable artifacts that a fresh AI session can resume from files instead of chat history

## Current Open Issue

The current known next investigation is not configuration bootstrap or ping tracing.

It is the VSIX built-in selected-text prompt path.

Current known state:

- built-in prompt `what is the active file` is confirmed working
- built-in selected-text prompt behavior is still suspect and needs observed reproduction

See:

- `SolutionFolder/docs/session-handoffs/2026-05-08-selected-text-prompt-investigation-handoff.md`

## Recommended Next Step

Resume with a focused observed investigation of the VSIX built-in selected-text path:

1. reproduce `what is the selected text` in the Experimental Instance with a known editor selection
2. capture the visible response and correlated logs
3. compare the observed path against presenter prompt routing and `VsService.GetSelectedTextAsync`
4. implement the minimum fix only if the disconnect is confirmed in repo code
5. preserve the decoupled host pattern, MVP/VM boundaries, and transport-safe logging

## Prompt To Give ChatGPT

Use this prompt with the contents of this handoff pasted in:

`Read this handoff summary first. Then help me reason about the current VSIX selected-text prompt investigation in vs-mcp-bridge. Focus on the shared IConfiguration migration, logging/diagnostic architecture, and the current suspected gap in the built-in selected-text prompt path. Do not assume access to my local repo unless I paste additional files.`
