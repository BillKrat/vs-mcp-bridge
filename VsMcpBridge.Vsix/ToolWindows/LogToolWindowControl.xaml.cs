using Microsoft.VisualStudio.Shell;
using System;
using System.Windows.Controls;
using VsMcpBridge.Vsix.MvpVm;

namespace VsMcpBridge.Vsix.ToolWindows;

/// <summary>
/// Code-behind for <see cref="LogToolWindowControl"/>.
/// Provides methods to append log entries and surface pending approvals.
/// </summary>
public partial class LogToolWindowControl : UserControl, ILogToolWindowControl
{
    public LogToolWindowControl()
    {
        InitializeComponent();
        this.DataContext = this;
    }

    /// <summary>Appends a line to the log text box.</summary>
    public void AppendLog(string message)
    {
        if (Dispatcher.CheckAccess())
        {
            LogTextBox.Text += $"\n{message}";
            return;
        }

        var updateTask = ThreadHelper.JoinableTaskFactory.RunAsync(async delegate
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            LogTextBox.Text += $"\n{message}";
        });
    }

    /// <summary>
    /// Shows an approval prompt for a pending text edit diff.
    /// The caller provides callbacks for the approve and reject actions.
    /// </summary>
    public void ShowApprovalPrompt(string description, Action onApprove, Action onReject)
    {
        if (Dispatcher.CheckAccess())
        {
            ApplyApprovalPrompt(description, onApprove, onReject);
            return;
        }

        var updateTask = ThreadHelper.JoinableTaskFactory.RunAsync(async delegate
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            ApplyApprovalPrompt(description, onApprove, onReject);
        });
    }

    private void ApproveButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var action = ApproveButton.Tag as Action;
        ClearApproval();
        action?.Invoke();
    }

    private void RejectButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var action = RejectButton.Tag as Action;
        ClearApproval();
        action?.Invoke();
    }

    private void ApplyApprovalPrompt(string description, Action onApprove, Action onReject)
    {
        PendingLabel.Text = description;
        ApproveButton.IsEnabled = true;
        RejectButton.IsEnabled = true;

        ApproveButton.Tag = onApprove;
        RejectButton.Tag = onReject;
    }

    private void ClearApproval()
    {
        PendingLabel.Text = string.Empty;
        ApproveButton.IsEnabled = false;
        RejectButton.IsEnabled = false;
        ApproveButton.Tag = null;
        RejectButton.Tag = null;
    }
}
