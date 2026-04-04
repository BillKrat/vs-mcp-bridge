using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using VsMcpBridge.Shared.Interfaces;

namespace VsMcpBridge.Shared.MvpVm
{
    public class LogToolWindowViewModel : ObservableObject, ILogToolWindowViewModel
    {
        private const string InitialLogMessage = "VS MCP Bridge log will appear here.";

        private string _logText = InitialLogMessage;
        private string _pendingApprovalDescription = string.Empty;
        private bool _hasPendingApproval;
        private Action? _onApproveRequested;
        private Action? _onRejectRequested;

        public LogToolWindowViewModel()
        {
            ApproveCommand = new RelayCommand(
                execute: () => _onApproveRequested?.Invoke(),
                canExecute: () => HasPendingApproval);

            RejectCommand = new RelayCommand(
                execute: () => _onRejectRequested?.Invoke(),
                canExecute: () => HasPendingApproval);
        }

        public string LogText
        {
            get => _logText;
            set => SetProperty(ref _logText, value);
        }

        public string PendingApprovalDescription
        {
            get => _pendingApprovalDescription;
            set => SetProperty(ref _pendingApprovalDescription, value);
        }

        public bool HasPendingApproval
        {
            get => _hasPendingApproval;
            set
            {
                if (SetProperty(ref _hasPendingApproval, value))
                {
                    ApproveCommand.NotifyCanExecuteChanged();
                    RejectCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public IRelayCommand ApproveCommand { get; }

        public IRelayCommand RejectCommand { get; }

        public void SetApprovalRequestHandlers(Action? onApproveRequested, Action? onRejectRequested)
        {
            _onApproveRequested = onApproveRequested;
            _onRejectRequested = onRejectRequested;
            ApproveCommand.NotifyCanExecuteChanged();
            RejectCommand.NotifyCanExecuteChanged();
        }
    }
}
