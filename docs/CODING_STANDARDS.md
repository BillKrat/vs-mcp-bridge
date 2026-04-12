# VS MCP Bridge - Coding Standards & Patterns

> **Purpose**: This document captures the patterns, conventions, and guard-rails that apply across the codebase. All contributors - human and AI - should read this before generating or modifying code. When a pattern here conflicts with a suggestion made elsewhere, this document wins.

---

## Table of Contents

1. [Async / Fire-and-Forget](#1-async--fire-and-forget)
2. [Dependency Injection - Resolve vs GetRequiredService](#2-dependency-injection---resolve-vs-getrequiredservice)
3. [Logging](#3-logging)
4. [VSIX-Specific Rules](#4-vsix-specific-rules)
5. [MVP-VM Responsibilities](#5-mvp-vm-responsibilities)

---

## 1. Async / Fire-and-Forget

### The Rule

Every fire-and-forget task (`_ = SomeAsync()`) **must** catch all exceptions internally. There is no AppDomain-wide safety net in the VSIX host.

### Safe Pattern

```csharp
// The caller discards the Task - the method owns its own error handling.
_ = DoWorkAsync();

private async Task DoWorkAsync()
{
    try
    {
        await SomethingThatMightThrow();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "DoWorkAsync failed.");
    }
}
```

### Dangerous Pattern

```csharp
// If DoWorkAsync throws, the exception is silently swallowed after GC.
_ = DoWorkAsync();

private async Task DoWorkAsync()
{
    await SomethingThatMightThrow(); // no try-catch - exception disappears
}
```

### Why

`TaskScheduler.UnobservedTaskException` is intentionally **not** subscribed to in the VSIX package because it fires for all of Visual Studio's internal async noise, not just bridge tasks. See [VSIX-Specific Rules](#4-vsix-specific-rules).

### Current Fire-and-Forget Sites

| Location | Method | Safe? |
|---|---|---|
| `PipeServer.ListenLoop` | `HandleConnectionAsync` | yes - full try-catch |
| `LogToolWindowPresenter.OnSubmitProposalRequested` | `SubmitProposalAsync` | yes - full try-catch |

When adding new fire-and-forget sites, add a row to this table.

---

## 2. Dependency Injection - Resolve vs GetRequiredService

### The Rule

At composition roots, prefer the project-local `Resolve<T>()` extension from `ServiceProviderExtensions` when you have a concrete `ServiceProvider` and want DI trace breadcrumbs during startup.

```csharp
// Preferred when startup DI trace logging matters and ServiceProvider is available.
_logger = _serviceProvider.Resolve<ILogger>();
_pipeServer = _serviceProvider.Resolve<IPipeServer>();

// Also valid at composition roots when trace logging is not being used there.
_exceptionSink = _serviceProvider.GetRequiredService<IUnhandledExceptionSink>();
_pipeServer = _serviceProvider.GetRequiredService<IPipeServer>();
```

### Current Repository State

- `VsMcpBridge.Vsix.VsMcpBridgePackage.InitializeAsync` uses `Resolve<T>()`.
- `VsMcpBridge.App.App.OnStartup` currently uses `GetRequiredService<T>()`.

If this is standardized later, update this document and the relevant composition roots together.

### Why

`Resolve<T>` emits a `LogTrace("[DI] Resolving {TypeName}")` message automatically, giving both developers and AI a startup trace of which services are being wired. `GetRequiredService<T>` is silent.

### Exceptions

- Inside DI registration lambdas (`services.AddSingleton<T>(sp => sp.GetRequiredService<...>())`) - these receive `IServiceProvider`, not `ServiceProvider`, and `Resolve<T>` is not defined on the interface. Use `GetRequiredService` there.
- In test files asserting DI composition - use `GetRequiredService` to keep test intent explicit.

---

## 3. Logging

### Interface

All injectable loggers implement `Microsoft.Extensions.Logging.ILogger`. The custom `IBridgeLogger` interface has been removed. Do not reintroduce it.

```csharp
// Correct
private readonly ILogger _logger;
public MyService(ILogger logger) => _logger = logger;

// Wrong - IBridgeLogger is gone
private readonly IBridgeLogger _logger;
```

### Logger Hierarchy

```text
LoggerBase (ILogger, template-method base)
|- ConsoleBridgeLogger - Console.WriteLine (App host)
|- DebugBridgeLogger - Debug.WriteLine -> VS Output pane
'- ActivityLogBridgeLogger (VSIX)
   '- AdditionalLogger: DebugBridgeLogger
```

`ActivityLogBridgeLogger` writes to the VS ActivityLog XML file **and** forwards to `DebugBridgeLogger` so messages appear in the VS Output window during debugging. `LoggerBase` does the `IsEnabled` check; subclasses only override `LogMessage`.

### Runtime Level Control

`ILogLevelSettings` is a singleton that both logger implementations and the tool-window `LogLevel` dropdown share. Changing the dropdown updates `ILogLevelSettings.MinimumLevel` immediately.

```csharp
// The IsEnabled check in LoggerBase respects MinimumLevel at call time.
public bool IsEnabled(LogLevel logLevel) =>
    logLevel != LogLevel.None && logLevel >= Settings.MinimumLevel;
```

Default level at startup: `LogLevel.Information`.

### Log Level Guidance

| Level | Use for |
|---|---|
| `Trace` | DI resolution, method entry, low-level pipe framing |
| `Information` | Normal lifecycle events (service started, proposal created) |
| `Warning` | Recoverable anomalies (empty pipe message, unknown command) |
| `Error` | Caught exceptions with context |

### LogError Parameter Order

MEL's `LogError` extension takes `(Exception?, string, params object[])` - **exception first, message second**. This is the opposite of the old `IBridgeLogger.LogError(string, Exception?)` signature.

```csharp
// Correct MEL order
_logger.LogError(ex, "Operation failed for '{FilePath}'.", filePath);

// Old IBridgeLogger order - will not compile but watch for it in new code
_logger.LogError("Operation failed.", ex);
```

---

## 4. VSIX-Specific Rules

### Do Not Subscribe to TaskScheduler.UnobservedTaskException

```csharp
// Never do this in VsMcpBridgePackage or any VSIX code
TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
```

`devenv.exe` loads every installed VS extension into the same AppDomain. This event fires for all of Visual Studio's own internal timeout tasks during startup, drowning real bridge errors in noise. The `AppDomain.CurrentDomain.UnhandledException` handler is sufficient and is already in place.

### ActivityLog vs Output Window

- `ActivityLog.TryLogInformation/Warning/Error` -> writes to `%LocalAppData%\Microsoft\VisualStudio\<ver>\ActivityLog.xml`
- `DebugBridgeLogger` (via `AdditionalLogger`) -> writes to VS Output window (`System.Diagnostics.Debug.WriteLine`)

During development, watch the **Output** window. ActivityLog is for post-mortem diagnostics when the debugger is not attached.

---

## 5. MVP-VM Responsibilities

See `docs/MVPVM_OVERVIEW.md` for the full guide. Quick summary:

| Layer | Owns |
|---|---|
| **View** (XAML/code-behind) | Layout, bindings, no logic |
| **ViewModel** | Observable state, commands, `ILogLevelSettings` binding |
| **Presenter** | Coordination between view, viewmodel, and services |
| **Service** | Host-specific operations (`IVsService`, `IEditApplier`, etc.) |

Do not put service calls in the ViewModel. Do not put UI state in the Presenter. The Presenter calls the service; the ViewModel reflects the result.

---

*This document is maintained by the human developer. Codex should treat it as binding guidance and propose any additions through the normal gated collaboration workflow rather than editing it casually.*
