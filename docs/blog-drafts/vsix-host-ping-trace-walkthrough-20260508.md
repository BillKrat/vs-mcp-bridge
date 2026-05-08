# VSIX Host Ping Trace Walkthrough

## Summary

This note explains the observed end-to-end `ping` flow through the VSIX host inside the Visual Studio Experimental Instance and why it matters for future diagnostics work.

The run proved that AI can launch the Experimental Instance with Trace enabled, exercise the live VS MCP Bridge tool window, capture correlated logs, and translate those logs into a Mermaid sequence diagram that matches the current code.

## Why this matters

The bridge now has observed end-to-end host workflows for both the App host and the VSIX host.

For the VSIX host path, one request id flowed through:

- prompt submission in the shared presenter
- route evaluation in the shared presenter
- host chat dispatch through `IChatRequestService`
- host-side execution in `VsixChatRequestService`
- visible response application back into the shared viewmodel

That makes `ping` a practical low-risk diagnostic slice for future VSIX-host validation.

## What was exercised

Observed environment:

- repo: `Y:\vs-mcp-bridge`
- branch: `feature/approval-apply-ui-slice`
- commit: `224b554`
- host: `VsMcpBridge.Vsix`
- environment: Visual Studio Experimental Instance
- logging provider: `StdErr`
- logging level: `Trace`
- observed provider path: `OpenAI`
- prompt: `ping`
- visible result: `pong`

The request used the live VS MCP Bridge tool window. The operator interaction was automated, but the runtime path matched normal UI behavior.

## Observed flow

The important sequence was:

1. the presenter created a request id
2. the presenter decided the prompt was not a built-in operational command
3. the presenter routed the request to `IChatRequestService`
4. `VsixChatRequestService` started the host chat dispatch with the observed `OpenAI` provider path
5. the host returned a visible `pong`
6. the presenter applied `pong` to the visible result surface

## Why the diagram is trustworthy

The Mermaid sequence was derived from the observed logs, then checked against:

- `VsMcpBridge.Shared/MvpVm/LogToolWindowPresenter.cs`
- `VsMcpBridge.Vsix/Services/VsService.cs`

For this host path, the logs and code matched.

## Limits

This run proves the observed VSIX-host prompt path only.

It does not replace deeper MCP named-pipe runtime validation or broader provider validation beyond the exercised `ping` request.

## Reusable outputs

- workflow: `docs/vsix-host-ping-trace-workflow.md`
- diagram: `docs/diagrams/vsix-host-ping-trace-20260508.mmd`
- log transcript: `artifacts/logs/vsix-host-ping-trace-20260508.log`
- metadata: `artifacts/logs/vsix-host-ping-trace-20260508.metadata.json`

## Suggested next use

Repeat this same VSIX-host ping workflow after any future changes to:

- presenter prompt routing
- VSIX chat provider wiring
- request/result UI surfacing
- correlation-id propagation
- Experimental Instance logging setup
