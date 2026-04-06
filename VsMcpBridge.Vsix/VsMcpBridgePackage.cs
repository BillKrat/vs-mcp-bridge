using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using VsMcpBridge.Shared.Composition;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Vsix.Composition;
using VsMcpBridge.Vsix.Logging;
using Task = System.Threading.Tasks.Task;

namespace VsMcpBridge.Vsix;

/// <summary>
/// VSIX package entry point. Registers the named pipe server and the log/approval tool window.
/// </summary>
[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[Guid(PackageGuidString)]
[ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
[ProvideMenuResource("Menus.ctmenu", 1)]
[ProvideToolWindow(typeof(ToolWindows.LogToolWindow))]
public sealed class VsMcpBridgePackage : AsyncPackage, IAsyncPackage
{
    public const string PackageGuidString = "a1b2c3d4-e5f6-7890-abcd-ef1234567890";

    private Microsoft.Extensions.DependencyInjection.ServiceProvider? _serviceProvider;
    private IPipeServer? _pipeServer;
    private IUnhandledExceptionSink? _exceptionSink;
    private ILogger? _logger;

    public ILogger Logger { get { return _logger!; } }
    public Microsoft.Extensions.DependencyInjection.ServiceProvider ServiceProvider { get { return _serviceProvider!; } }

    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        var bootstrapLogger = new ActivityLogBridgeLogger();
        bootstrapLogger.LogTrace("Package initialization starting.");

        try
        {
            await Commands.ShowLogToolWindowCommand.InitializeAsync(this);
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            bootstrapLogger.LogTrace("Building VS MCP Bridge service collection.");
            _serviceProvider = new ServiceCollection()
                .AddVsMcpBridgeServices(this)
                .AddMvpVmServices()
                .BuildServiceProvider();

            _logger = _serviceProvider.Resolve<ILogger>();
            _exceptionSink = _serviceProvider.Resolve<IUnhandledExceptionSink>();
            RegisterUnhandledExceptionHandlers();

            _logger.LogInformation("Initializing VS MCP Bridge package.");
            _logger.LogTrace("Starting bridge services.");

            _pipeServer = _serviceProvider.Resolve<IPipeServer>();
            _pipeServer.Start();

            _logger.LogInformation("VS MCP Bridge package initialized.");
            _logger.LogTrace("Package initialization completed.");
        }
        catch (Exception ex)
        {
            bootstrapLogger.LogError(ex, "Package initialization failed.");
            _exceptionSink?.Save("VsMcpBridgePackage.InitializeAsync", ex);
            throw;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            UnregisterUnhandledExceptionHandlers();
            _pipeServer?.Stop();
            _serviceProvider?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void RegisterUnhandledExceptionHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        global::System.Threading.Tasks.TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        _logger?.LogTrace("Registered global unhandled exception handlers.");
    }

    private void UnregisterUnhandledExceptionHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException -= OnCurrentDomainUnhandledException;
        global::System.Threading.Tasks.TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
    }

    private void OnCurrentDomainUnhandledException(object? sender, UnhandledExceptionEventArgs args)
    {
        if (args.ExceptionObject is not Exception exception)
            return;

        _logger?.LogError(exception, "AppDomain unhandled exception observed.");
        _exceptionSink?.Save("AppDomain.CurrentDomain.UnhandledException", exception);
    }

    private void OnUnobservedTaskException(object? sender, global::System.Threading.Tasks.UnobservedTaskExceptionEventArgs args)
    {
        _logger?.LogError(args.Exception, "TaskScheduler unobserved task exception observed.");
        _exceptionSink?.Save("TaskScheduler.UnobservedTaskException", args.Exception);
    }

    public async System.Threading.Tasks.Task<T> GetServiceAsync<T>(Type type)
    {
        var service = await base.GetServiceAsync(type);
        if (service is null)
            return default!;

        return (T)service;
    }
}
