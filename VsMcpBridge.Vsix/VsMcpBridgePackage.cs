using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
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

    private Pipe.PipeServer? _pipeServer;

    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        await Commands.ShowLogToolWindowCommand.InitializeAsync(this);
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var vsService = new Services.VsService(this);
        _pipeServer = new Pipe.PipeServer(vsService);
        _pipeServer.Start();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _pipeServer?.Stop();

        base.Dispose(disposing);
    }
}
