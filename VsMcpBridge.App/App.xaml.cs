using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using VsMcpBridge.App.Composition;
using VsMcpBridge.Shared.Composition;
using VsMcpBridge.Shared.Diagnostics;
using VsMcpBridge.Shared.Interfaces;

namespace VsMcpBridge.App;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    private IUnhandledExceptionSink? _exceptionSink;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _serviceProvider = new ServiceCollection()
            .AddVsMcpBridgeAppServices()
            .AddMvpVmServices()
            .BuildServiceProvider();

        _exceptionSink = _serviceProvider.GetRequiredService<IUnhandledExceptionSink>();
        RegisterUnhandledExceptionHandlers();

        var mainWindow = new Windows.MainWindow(_serviceProvider);
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        UnregisterUnhandledExceptionHandlers();
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }

    private void RegisterUnhandledExceptionHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private void UnregisterUnhandledExceptionHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException -= OnCurrentDomainUnhandledException;
        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
    }

    private void OnCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs args)
    {
        if (args.ExceptionObject is Exception exception)
            _exceptionSink?.Save("App.UnhandledException", exception);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
    {
        _exceptionSink?.Save("App.UnobservedTaskException", args.Exception);
        args.SetObserved();
    }
}
