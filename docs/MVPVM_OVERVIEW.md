# MVP/VM In This Repository

Last updated: 2026-04-04

## Purpose

This document explains how the Model-View-Presenter-ViewModel (MVP/VM, or MVPVM) pattern is used in `vs-mcp-bridge`.

It is not intended to replace broader pattern literature. Its job is to help developers working in this repository understand:

- why this pattern exists here
- what each layer owns
- how the current tool window implementation is split
- where to put new behavior without collapsing responsibilities back together

For the original background and deeper theory, see Bill Kratochvil's MSDN Magazine article: [MVPVM Design Pattern - The Model-View-Presenter-ViewModel Design Pattern for WPF](https://learn.microsoft.com/en-us/archive/msdn-magazine/2011/december/mvpvm-design-pattern-the-model-view-presenter-viewmodel-design-pattern-for-wpf).

## Why MVP/VM Here

The tool window in this solution has three competing needs:

- it must work naturally with WPF binding and commands
- it must coordinate Visual Studio-hosted behavior that does not belong in XAML or code-behind
- it must remain testable without a running Visual Studio UI

Plain MVVM is a good fit for bindable state, but it becomes awkward when orchestration starts to involve host services, thread switching, approval callbacks, and Visual Studio-specific workflows.

This repository uses MVP/VM so that:

- the `ViewModel` owns UI state and commands
- the `Presenter` owns orchestration and interaction logic
- the `View` stays passive
- host-specific services remain outside the WPF layer

## Pattern Mapping In `vs-mcp-bridge`

Current tool-window split:

- `View`: [LogToolWindowControl.xaml](/Y:/vs-mcp-bridge/VsMcpBridge.Shared.Wpf/Views/LogToolWindowControl.xaml)
- View wrapper hosted by Visual Studio: [LogToolWindow.cs](/Y:/vs-mcp-bridge/VsMcpBridge.Vsix/ToolWindows/LogToolWindow.cs)
- `Presenter`: [LogToolWindowPresenter.cs](/Y:/vs-mcp-bridge/VsMcpBridge.Shared/MvpVm/LogToolWindowPresenter.cs)
- `ViewModel`: [LogToolWindowViewModel.cs](/Y:/vs-mcp-bridge/VsMcpBridge.Shared/MvpVm/LogToolWindowViewModel.cs)
- service boundary used by the presenter: [IVsService.cs](/Y:/vs-mcp-bridge/VsMcpBridge.Shared/Interfaces/IVsService.cs)

Conceptual mapping:

- `Model`
  - domain and service state such as edit proposals, proposal status, and Visual Studio operations
  - examples include `EditProposal`, `IApprovalWorkflowService`, and `IVsService`
- `View`
  - WPF markup and minimal code-behind only
  - binds to properties and commands
  - does not decide what application behavior should happen
- `Presenter`
  - wires handlers
  - translates UI intent into service calls
  - coordinates state changes that should be reflected in the viewmodel
  - handles UI-thread concerns
- `ViewModel`
  - exposes bindable state and commands
  - contains no Visual Studio SDK knowledge
  - does not resolve services or orchestrate workflows

## How The Current Flow Works

Startup and wiring:

1. Visual Studio creates [LogToolWindow.cs](/Y:/vs-mcp-bridge/VsMcpBridge.Vsix/ToolWindows/LogToolWindow.cs).
2. The tool window creates the WPF control from `VsMcpBridge.Shared.Wpf`.
3. The tool window resolves `ILogToolWindowPresenter` and `ILogToolWindowViewModel` from DI.
4. The presenter is given the control and the viewmodel.
5. The presenter sets the control `DataContext` to the viewmodel during `Initialize`.

User-driven proposal submission:

1. The view binds text boxes to `ProposalFilePath`, `ProposalOriginalText`, and `ProposalProposedText`.
2. The view binds the submit button to `SubmitProposalCommand`.
3. The viewmodel raises the command through the submission handler registered by the presenter.
4. The presenter calls `IVsService.ProposeTextEditAsync(...)`.
5. `VsService` creates the proposal, generates the preview diff, and asks the presenter to show an approval prompt.

Approval flow:

1. `VsService` calls `ShowApprovalPrompt(...)` on the presenter.
2. The presenter updates `PendingApprovalDescription` and `HasPendingApproval`.
3. The view updates automatically through binding.
4. The approve and reject buttons execute `ApproveCommand` and `RejectCommand`.
5. The viewmodel routes those commands back to presenter-owned handlers.
6. The presenter invokes the callbacks previously supplied by `VsService`.

This is the key MVP/VM point in the current design:

- the viewmodel owns the command surface
- the presenter owns the meaning of those commands
- the service layer owns the business operation being approved

## Concrete Examples From This Codebase

### Example 1: Passive View

In [LogToolWindowControl.xaml](/Y:/vs-mcp-bridge/VsMcpBridge.Shared.Wpf/Views/LogToolWindowControl.xaml), the WPF view is almost entirely binding declarations:

- `Text="{Binding ProposalFilePath, UpdateSourceTrigger=PropertyChanged}"`
- `Command="{Binding SubmitProposalCommand}"`
- `Text="{Binding PendingApprovalDescription}"`
- `Command="{Binding ApproveCommand}"`

That is intentional. The XAML does not decide how proposals are created or approved.

### Example 2: ViewModel Owns State And Commands

In [LogToolWindowViewModel.cs](/Y:/vs-mcp-bridge/VsMcpBridge.Shared/MvpVm/LogToolWindowViewModel.cs):

- bindable properties represent current UI state
- `RelayCommand` instances expose actions to the view
- `CanSubmitProposal()` controls button enablement
- the viewmodel remains unaware of `DTE`, `AsyncPackage`, or Visual Studio services

This is the main ViewModel responsibility in this repository: represent UI state cleanly and expose a command surface that the presenter can hook into.

### Example 3: Presenter Owns Orchestration

In [LogToolWindowPresenter.cs](/Y:/vs-mcp-bridge/VsMcpBridge.Shared/MvpVm/LogToolWindowPresenter.cs), the presenter:

- assigns the `DataContext`
- appends log text
- updates pending-approval state
- resolves `IVsService` when a manual proposal is submitted
- translates `ApproveCommand` and `RejectCommand` into callback execution
- marshals updates onto the UI thread when necessary

This is the main Presenter responsibility in this repository: coordinate between the UI-facing viewmodel and the service/application layer.

### Example 4: View Host Stays Thin

In [LogToolWindow.cs](/Y:/vs-mcp-bridge/VsMcpBridge.Vsix/ToolWindows/LogToolWindow.cs), the Visual Studio tool-window host does very little:

- create the control
- resolve dependencies
- connect presenter, viewmodel, and control
- call `Initialize()`

That is the desired shape. The VSIX host should bootstrap the UI, not contain UI behavior.

## Responsibilities Checklist

When adding functionality, prefer these rules.

Put code in the `View` when:

- it is purely layout, styling, or binding markup
- it is minimal control initialization that cannot reasonably live elsewhere

Put code in the `ViewModel` when:

- it is bindable state
- it is command availability logic
- it is UI-facing data that should notify on change

Put code in the `Presenter` when:

- it coordinates between commands and services
- it owns approval callbacks or workflow transitions
- it needs to update multiple pieces of viewmodel state together
- it has to care about the UI thread

Put code in services or models when:

- it is business logic
- it is Visual Studio integration logic
- it creates proposals, applies edits, or reads IDE state
- it should be reusable without the WPF UI

## What To Avoid

Common failure modes for this repo:

- putting Visual Studio SDK calls in the viewmodel
- letting code-behind grow into an alternate presenter
- storing orchestration callbacks directly in the view
- making the presenter a dumping ground for domain logic that belongs in services
- making the viewmodel resolve dependencies from `IServiceProvider`

If a new feature needs both bindable state and workflow coordination, the default split should be:

- state in the viewmodel
- coordination in the presenter
- business action in a service

## A Small Before/After Mental Model

Avoid this shape:

```text
Button click -> code-behind -> DTE / service calls -> mutate controls directly
```

Prefer this shape:

```text
Button click -> ViewModel command -> Presenter handler -> service call -> ViewModel state update -> binding refresh
```

That second shape is what the current proposal and approval flow is doing.

## Current Limitations

The current MVP/VM usage is directionally good, but not complete:

- log/event forwarding into the presenter is still limited
- the presenter currently resolves `IVsService` through `IServiceProvider`, which is pragmatic but not the cleanest long-term dependency shape
- the approved-edit apply path still needs a stronger structured edit model underneath the UI

Those are implementation issues, not reasons to abandon the pattern.

## Practical Guidance For New Features

If you add a new tool-window feature, start with these questions:

1. What state must the UI display?
2. Which commands should the view expose?
3. Which coordination logic belongs in the presenter?
4. Which service should own the actual operation?

Example:

- a new "refresh diagnostics" button would likely add a command and result state to the viewmodel
- the presenter would handle the command and call a diagnostics-oriented service
- the service would talk to Visual Studio APIs
- the view would only bind to the command and the displayed result

## Related Documents

- [README.md](/Y:/vs-mcp-bridge/README.md)
- [VS_MCP_BRIDGE_TECHNICAL_ANALYSIS.md](/Y:/vs-mcp-bridge/docs/VS_MCP_BRIDGE_TECHNICAL_ANALYSIS.md)
- [CHATGPT_HANDOFF.md](/Y:/vs-mcp-bridge/docs/CHATGPT_HANDOFF.md)
- [MVPVM Design Pattern - The Model-View-Presenter-ViewModel Design Pattern for WPF](https://learn.microsoft.com/en-us/archive/msdn-magazine/2011/december/mvpvm-design-pattern-the-model-view-presenter-viewmodel-design-pattern-for-wpf)
