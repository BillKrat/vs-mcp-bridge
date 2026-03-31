using System;
using Microsoft.VisualStudio.Shell;
using VsMcpBridge.Vsix.Logging;

namespace VsMcpBridge.Vsix.MvpVm
{
    internal class LogToolWindowPresenter(IBridgeLogger logger) : ILogToolWindowPresenter
    {
        private const string InitialLogMessage = "VS MCP Bridge log will appear here.";

        private Action? _pendingApproveAction;
        private Action? _pendingRejectAction;

        public ILogToolWindowControl LogToolWindowControl { get; set; } = null!;

        public ILogToolWindowViewModel LogToolWindowViewModel { get; set; } = null!;

        public void Initialize()
        {
            logger.LogInformation("Initializing VS MCP Bridge tool window...");

            LogToolWindowControl.DataContext = LogToolWindowViewModel;
            LogToolWindowViewModel.SetApprovalRequestHandlers(OnApproveRequested, OnRejectRequested);

            logger.LogInformation("VS MCP Bridge tool window Initialized.");
        }

        public void AppendLog(string message)
        {
            RunOnUiThread(() =>
            {
                var existingLog = LogToolWindowViewModel.LogText;
                LogToolWindowViewModel.LogText =
                    string.IsNullOrWhiteSpace(existingLog) || string.Equals(existingLog, InitialLogMessage, StringComparison.Ordinal)
                        ? message
                        : $"{existingLog}{Environment.NewLine}{message}";
            });
        }

        public void ShowApprovalPrompt(string description, Action onApprove, Action onReject)
        {
            RunOnUiThread(() =>
            {
                _pendingApproveAction = onApprove;
                _pendingRejectAction = onReject;
                LogToolWindowViewModel.PendingApprovalDescription = description;
                LogToolWindowViewModel.HasPendingApproval = true;
            });
        }

        private void OnApproveRequested()
        {
            Action? approvalAction = null;

            RunOnUiThread(() =>
            {
                approvalAction = _pendingApproveAction;
                ClearApproval();
            });

            approvalAction?.Invoke();
        }

        private void OnRejectRequested()
        {
            Action? rejectionAction = null;

            RunOnUiThread(() =>
            {
                rejectionAction = _pendingRejectAction;
                ClearApproval();
            });

            rejectionAction?.Invoke();
        }

        private void ClearApproval()
        {
            _pendingApproveAction = null;
            _pendingRejectAction = null;
            LogToolWindowViewModel.PendingApprovalDescription = string.Empty;
            LogToolWindowViewModel.HasPendingApproval = false;
        }

        private static void RunOnUiThread(Action action)
        {
            if (ThreadHelper.CheckAccess())
            {
                action();
                return;
            }

            ThreadHelper.JoinableTaskFactory.Run(async delegate
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                action();
            });
        }
    }
}
