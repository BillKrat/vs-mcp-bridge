# App Host Ping Trace Walkthrough

## Summary

This note explains the observed end-to-end `ping` flow through `VsMcpBridge.App` and why it matters for future diagnostics work.

The run proved that AI can launch the standalone host, exercise the live WPF request surface, capture correlated logs, and translate those logs into a Mermaid sequence diagram that matches the current code.

## Why this matters

The bridge now has enough correlation data to make a small end-to-end request understandable without guessing.

For the App host fake-provider path, one request id flowed through:

- prompt submission in the shared presenter
- route evaluation in the shared presenter
- host chat dispatch through `IChatRequestService`
- fake-provider execution in `AppChatRequestService`
- visible response application back into the shared viewmodel

That makes `ping -> pong` a good low-risk diagnostic slice for future sessions.

## What was exercised

Observed environment:

- repo: `Y:\vs-mcp-bridge`
- branch: `feature/approval-apply-ui-slice`
- commit: `2ed6e93`
- host: `VsMcpBridge.App`
- logging provider: `StdErr`
- logging level: `Trace`
- chat provider: `Fake`
- prompt: `ping`
- visible result: `pong`

The request used the live WPF window. The operator interaction was automated, but the runtime path matched normal UI behavior.

## Observed flow

The important sequence was:

1. the presenter created a request id
2. the presenter decided the prompt was not a built-in operational command
3. the presenter routed the request to `IChatRequestService`
4. `AppChatRequestService` handled the call in fake-provider mode
5. `ping` returned `pong`
6. the presenter applied `pong` to the visible result surface

## Why the diagram is trustworthy

The Mermaid sequence was derived from the observed logs, then checked against:

- `VsMcpBridge.Shared/MvpVm/LogToolWindowPresenter.cs`
- `VsMcpBridge.App/Services/AppChatRequestService.cs`

For this host path, the logs and code matched.

## Limits

This does not prove the VSIX host path.

The VSIX runtime has its own host-specific implementation and should be documented with a separate observed run if that path becomes the next focus.

## Reusable outputs

- workflow: `SolutionFolder/docs/app-host-ping-trace-workflow.md`
- diagram: `SolutionFolder/docs/diagrams/app-host-ping-trace-20260507.mmd`
- log transcript: `SolutionFolder/artifacts/logs/app-host-ping-trace-20260507.log`
- metadata: `SolutionFolder/artifacts/logs/app-host-ping-trace-20260507.metadata.json`

## Suggested next use

Repeat this same App-host ping workflow after any future changes to:

- prompt routing
- presenter trace behavior
- request/result UI surfacing
- App chat provider wiring
- correlation-id propagation
