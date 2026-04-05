using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;
using VsMcpBridge.Shared.Composition;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.Wpf.Views;

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

        _presenter.LogToolWindowControl = (ILogToolWindowControl)Content;
        _presenter.LogToolWindowViewModel = viewModel;
        _presenter.Initialize();
    }
}
