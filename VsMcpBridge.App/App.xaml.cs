using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using VsMcpBridge.App.Composition;
using VsMcpBridge.Shared.Composition;
using VsMcpBridge.Shared.Configuration;
using VsMcpBridge.Shared.Interfaces;

namespace VsMcpBridge.App;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    private IUnhandledExceptionSink? _exceptionSink;
    private IPipeServer? _pipeServer;
    private ILogger? _logger;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var configuration = BridgeConfigurationFactory.Create(AppContext.BaseDirectory);

            _serviceProvider = new ServiceCollection()
                .AddVsMcpBridgeAppServices(configuration)
                .AddMvpVmServices()
                .BuildServiceProvider();

            _logger = _serviceProvider.GetService<ILogger>();
            _exceptionSink = _serviceProvider.GetRequiredService<IUnhandledExceptionSink>();
            _pipeServer = _serviceProvider.GetRequiredService<IPipeServer>();
            RegisterUnhandledExceptionHandlers();
            _logger?.LogInformation("Initializing App host.");
            _pipeServer.Start();

            var mainWindow = new Windows.MainWindow(_serviceProvider);
            MainWindow = mainWindow;
            mainWindow.Show();
            _logger?.LogInformation("App host initialized.");
        }
        catch (Exception exception)
        {
            _logger?.LogError(exception, "App host startup failed.");
            _exceptionSink?.Save("App.OnStartup", exception);
            throw;
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        UnregisterUnhandledExceptionHandlers();
        _pipeServer?.Stop();
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }

    private void RegisterUnhandledExceptionHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += OnTaskSchedulerUnobservedTaskException;
        _logger?.LogTrace("Registered App global exception handlers.");
    }

    private void UnregisterUnhandledExceptionHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException -= OnCurrentDomainUnhandledException;
        DispatcherUnhandledException -= OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException -= OnTaskSchedulerUnobservedTaskException;
    }

    private void OnCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs args)
    {
        if (args.ExceptionObject is Exception exception)
        {
            _logger?.LogError(exception, "AppDomain unhandled exception observed in App host.");
            _exceptionSink?.Save("App.UnhandledException", exception);
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs args)
    {
        _logger?.LogError(args.Exception, "Dispatcher unhandled exception observed in App host.");
        _exceptionSink?.Save("App.DispatcherUnhandledException", args.Exception);
    }

    private void OnTaskSchedulerUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
    {
        _logger?.LogError(args.Exception, "Unobserved task exception observed in App host.");
        _exceptionSink?.Save("App.TaskScheduler.UnobservedTaskException", args.Exception);
    }
}