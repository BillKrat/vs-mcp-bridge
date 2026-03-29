using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using VsMcpBridge.Vsix.Logging;
using VsMcpBridge.Vsix.Pipe;
using VsMcpBridge.Vsix.Services;
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
public sealed class VsMcpBridgePackage : AsyncPackage
{
    public const string PackageGuidString = "a1b2c3d4-e5f6-7890-abcd-ef1234567890";

    private Microsoft.Extensions.DependencyInjection.ServiceProvider? _serviceProvider;
    private IPipeServer? _pipeServer;

    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        await Commands.ShowLogToolWindowCommand.InitializeAsync(this);
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        _serviceProvider = new ServiceCollection()
            .AddSingleton<AsyncPackage>(this)
            .AddSingleton(this)
            .AddSingleton<IBridgeLogger, ActivityLogBridgeLogger>()
            .AddSingleton<IVsService, VsService>()
            .AddSingleton<IPipeServer, PipeServer>()
            .BuildServiceProvider();

        var logger = _serviceProvider.GetRequiredService<IBridgeLogger>();
        logger.LogInformation("Initializing VS MCP Bridge package.");

        _pipeServer = _serviceProvider.GetRequiredService<IPipeServer>();
        _pipeServer.Start();

        logger.LogInformation("VS MCP Bridge package initialized.");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _pipeServer?.Stop();
            _serviceProvider?.Dispose();
        }

        base.Dispose(disposing);
    }
}
