using System;
using VsMcpBridge.Shared.Interfaces;

namespace VsMcpBridge.Shared.MvpVm
{
    public class LogToolWindowPresenter : ILogToolWindowPresenter
    {
        private const string InitialLogMessage = "VS MCP Bridge log will appear here.";

        private readonly IBridgeLogger _logger;
        private readonly IThreadHelper _threadHelper;
        private Action? _pendingApproveAction;
        private Action? _pendingRejectAction;

        public LogToolWindowPresenter(IBridgeLogger logger, IThreadHelper threadHelper, ILogToolWindowViewModel logToolWindowViewModel)
        {
            _logger = logger;
            _threadHelper = threadHelper;
            LogToolWindowViewModel = logToolWindowViewModel;
            LogToolWindowViewModel.SetApprovalRequestHandlers(OnApproveRequested, OnRejectRequested);
        }

        public ILogToolWindowControl LogToolWindowControl { get; set; } = null!;

        public ILogToolWindowViewModel LogToolWindowViewModel { get; set; }

        public void Initialize()
        {
            _logger.LogInformation("Initializing VS MCP Bridge tool window...");

            LogToolWindowControl.DataContext = LogToolWindowViewModel;

            _logger.LogInformation("VS MCP Bridge tool window Initialized.");
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

        public void SetProposalSubmissionHandler(Action<string, string, string> onSubmitProposal)
        {
            LogToolWindowViewModel.SetProposalSubmissionHandler(onSubmitProposal);
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

        public void RunOnUiThread(Action action)
        {
            if (_threadHelper.CheckAccess())
            {
                action();
                return;
            }

            _threadHelper.Run(async delegate
            {
                await _threadHelper.SwitchToMainThreadAsync();
                action();
            });
        }
    }
}
