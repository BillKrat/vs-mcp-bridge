using System;
using CommunityToolkit.Mvvm.Input;

namespace VsMcpBridge.Vsix.MvpVm
{
    internal interface ILogToolWindowViewModel
    {
        string LogText { get; set; }
        string PendingApprovalDescription { get; set; }
        bool HasPendingApproval { get; set; }
        IRelayCommand ApproveCommand { get; }
        IRelayCommand RejectCommand { get; }
        void SetApprovalRequestHandlers(Action? onApproveRequested, Action? onRejectRequested);
    }
}
