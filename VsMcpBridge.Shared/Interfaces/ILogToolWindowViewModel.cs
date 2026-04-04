using CommunityToolkit.Mvvm.Input;
using System;

namespace VsMcpBridge.Shared.Interfaces
{
    public interface ILogToolWindowViewModel
    {
        string LogText { get; set; }
        string PendingApprovalDescription { get; set; }
        bool HasPendingApproval { get; set; }
        IRelayCommand ApproveCommand { get; }
        IRelayCommand RejectCommand { get; }
        void SetApprovalRequestHandlers(Action? onApproveRequested, Action? onRejectRequested);
    }
}
