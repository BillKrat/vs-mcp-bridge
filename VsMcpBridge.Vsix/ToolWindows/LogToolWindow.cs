using Microsoft.VisualStudio.Shell;
using System;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using VsMcpBridge.Shared.Composition;
using VsMcpBridge.Shared.Interfaces;

namespace VsMcpBridge.Vsix.ToolWindows;

/// <summary>
/// Tool window that displays MCP request logs and pending approval prompts.
/// </summary>
[Guid("b2c3d4e5-f6a7-8901-bcde-f12345678901")]
public sealed class LogToolWindow : ToolWindowPane
{
    private ILogToolWindowPresenter? _presenter;

    public LogToolWindow() : base(null)
    {
        Caption = "VS MCP Bridge";
        Content = new LogToolWindowControl();
    }

    public override void OnToolWindowCreated()
    {
        base.OnToolWindowCreated();

        var package = (VsMcpBridgePackage)Package;

        var viewModel = package.ServiceProvider.Resolve<ILogToolWindowViewModel>();
        _presenter = package.ServiceProvider.Resolve<ILogToolWindowPresenter>();
        var logger = package.ServiceProvider.Resolve<IBridgeLogger>();
        var vsService = package.ServiceProvider.Resolve<IVsService>();

        _presenter.LogToolWindowControl = (ILogToolWindowControl)Content;
        _presenter.LogToolWindowViewModel = viewModel;
        _presenter.SetProposalSubmissionHandler((filePath, originalText, proposedText) =>
            _ = SubmitProposalAsync(vsService, logger, filePath, originalText, proposedText));

        _presenter.Initialize();
    }

    private static async Task SubmitProposalAsync(
        IVsService vsService,
        IBridgeLogger logger,
        string filePath,
        string originalText,
        string proposedText)
    {
        try
        {
            await vsService.ProposeTextEditAsync(Guid.NewGuid().ToString("N"), filePath, originalText, proposedText);
        }
        catch (Exception ex)
        {
            logger.LogError($"Manual proposal submission failed for '{filePath}'.", ex);
        }
    }
}
