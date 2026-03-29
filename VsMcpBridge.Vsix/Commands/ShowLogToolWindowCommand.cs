using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace VsMcpBridge.Vsix.Commands;

internal sealed class ShowLogToolWindowCommand
{
    public const int CommandId = 0x0100;
    public static readonly Guid CommandSet = new("f3d8d4b3-6a41-4bc6-8fdd-89bf4cbe0b6b");

    private readonly AsyncPackage _package;

    private ShowLogToolWindowCommand(AsyncPackage package, OleMenuCommandService commandService)
    {
        _package = package;

        var menuCommandId = new CommandID(CommandSet, CommandId);
        var menuItem = new OleMenuCommand(Execute, menuCommandId);
        commandService.AddCommand(menuItem);
    }

    public static async Task InitializeAsync(AsyncPackage package)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService
            ?? throw new InvalidOperationException("Command service unavailable.");

        _ = new ShowLogToolWindowCommand(package, commandService);
    }

    private void Execute(object sender, EventArgs e)
    {
        var showWindowTask = _package.JoinableTaskFactory.RunAsync(async delegate
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            ToolWindowPane window = await _package.ShowToolWindowAsync(
                typeof(ToolWindows.LogToolWindow),
                id: 0,
                create: true,
                cancellationToken: _package.DisposalToken);

            if (window.Frame is not IVsWindowFrame windowFrame)
                throw new NotSupportedException("Unable to create the VS MCP Bridge tool window.");

            ErrorHandler.ThrowOnFailure(windowFrame.Show());
        });
    }
}
