using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using VsMcpBridge.Shared.Composition;
using VsMcpBridge.Shared.Interfaces;

namespace VsMcpBridge.App.Windows;

/// <summary>
/// Hosts <see cref="VsMcpBridge.Shared.Wpf.Views.LogToolWindowControl"/> as a standalone window.
/// Mirrors the wiring in <c>LogToolWindow.OnToolWindowCreated</c>.
/// </summary>
public partial class MainWindow : Window
{
    private readonly ILogToolWindowPresenter _presenter;

    public MainWindow(ServiceProvider serviceProvider)
    {
        InitializeComponent();

        _presenter = serviceProvider.Resolve<ILogToolWindowPresenter>();
        _presenter.LogToolWindowControl = LogControl;
        _presenter.LogToolWindowViewModel = serviceProvider.Resolve<ILogToolWindowViewModel>();
        _presenter.Initialize();
    }
}
