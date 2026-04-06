using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using VsMcpBridge.App.Composition;
using VsMcpBridge.Shared.Composition;
using VsMcpBridge.Shared.Interfaces;

namespace VsMcpBridge.App;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    private IUnhandledExceptionSink? _exceptionSink;
    private IPipeServer? _pipeServer;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _serviceProvider = new ServiceCollection()
            .AddVsMcpBridgeAppServices()
            .AddMvpVmServices()
            .BuildServiceProvider();

        _exceptionSink = _serviceProvider.GetRequiredService<IUnhandledExceptionSink>();
        _pipeServer = _serviceProvider.GetRequiredService<IPipeServer>();
        RegisterUnhandledExceptionHandlers();
        _pipeServer.Start();

        var mainWindow = new Windows.MainWindow(_serviceProvider);
        MainWindow = mainWindow;
        mainWindow.Show();
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
    }

    private void UnregisterUnhandledExceptionHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException -= OnCurrentDomainUnhandledException;
    }

    private void OnCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs args)
    {
        if (args.ExceptionObject is Exception exception)
            _exceptionSink?.Save("App.UnhandledException", exception);
    }
}