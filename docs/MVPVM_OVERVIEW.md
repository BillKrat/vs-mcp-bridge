# MVP/VM In This Repository

Last updated: 2026-04-10

## Purpose

This document explains the MVP/VM split used by the tool-window UI in `vs-mcp-bridge`.

It is intentionally short.

This is supporting guidance, not the main priority of the current phase. The current priority is getting the VSIX host, named-pipe bridge, and MCP server working end to end. While doing that, we still want to avoid collapsing UI responsibilities back together.

## Why This Pattern Exists Here

The tool window has to balance three things:

- WPF bindings and commands
- workflow coordination that does not belong in code-behind
- Visual Studio-specific behavior that should stay out of the shared WPF view layer

This repository uses MVP/VM so that:

- the view stays passive
- the viewmodel owns bindable state and commands
- the presenter coordinates workflow
- host services own Visual Studio or file-system behavior

## Current Mapping

Current files:

- View: `VsMcpBridge.Shared.Wpf/Views/LogToolWindowControl.xaml`
- VSIX host wrapper: `VsMcpBridge.Vsix/ToolWindows/LogToolWindow.cs`
- Presenter: `VsMcpBridge.Shared/MvpVm/LogToolWindowPresenter.cs`
- ViewModel: `VsMcpBridge.Shared/MvpVm/LogToolWindowViewModel.cs`
- Service boundary: `VsMcpBridge.Shared/Interfaces/IVsService.cs`

## Responsibility Split

### View

Owns:

- XAML layout
- bindings
- minimal code-behind required by WPF control hosting

Does not own:

- Visual Studio SDK calls
- workflow logic
- service resolution

### ViewModel

Owns:

- bindable UI state
- command exposure
- simple command enablement logic

Does not own:

- Visual Studio SDK knowledge
- service orchestration
- DI lookups

### Presenter

Owns:

- coordination between commands and services
- approval prompt flow
- UI-thread-sensitive orchestration
- updating viewmodel state as workflow changes

Does not own:

- heavy business logic that belongs in services
- raw control manipulation beyond what is needed to initialize the view

### Service Layer

Owns:

- Visual Studio-backed operations
- proposal creation and approval-related domain behavior
- file-system or host-specific behavior in non-VSIX hosts

## Current Flow

At a high level:

1. The VSIX creates the tool window.
2. The tool window resolves the presenter and viewmodel from DI.
3. The presenter initializes the passive WPF control with the viewmodel.
4. User actions hit viewmodel commands.
5. The presenter handles those commands and calls services.
6. Services perform the host-specific work and feed results back through the presenter/viewmodel.

## Practical Rules

When adding or changing UI behavior:

- put display state in the viewmodel
- put coordination in the presenter
- put host behavior in services
- keep code-behind thin

Avoid these failure modes:

- putting Visual Studio API calls in the viewmodel
- letting code-behind become a second presenter
- moving service orchestration into the view
- using the presenter as a dumping ground for unrelated domain logic

## Current Phase Note

Right now, this pattern matters mainly as a guardrail.

We do not need to expand or perfect the MVP/VM design before the bridge works end to end. We only need to preserve a clean enough split that runtime fixes do not turn the UI layer into a tangle.

## Related Documents

- `README.md`
- `docs/CODING_STANDARDS.md`
- `docs/AI_HANDOFF.md`
- `docs/VS_MCP_BRIDGE_TECHNICAL_ANALYSIS.md`
