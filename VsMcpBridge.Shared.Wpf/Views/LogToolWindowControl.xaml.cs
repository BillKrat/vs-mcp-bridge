using System.Windows.Controls;
using VsMcpBridge.Shared.Interfaces;

namespace VsMcpBridge.Shared.Wpf.Views;

/// <summary>
/// Passive view for <see cref="LogToolWindowControl"/>.
/// </summary>
public partial class LogToolWindowControl : UserControl, ILogToolWindowControl
{
    public LogToolWindowControl()
    {
        InitializeComponent();
    }
}
