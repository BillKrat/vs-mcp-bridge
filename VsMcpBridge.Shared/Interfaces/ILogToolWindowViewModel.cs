using CommunityToolkit.Mvvm.Input;
using System;

namespace VsMcpBridge.Shared.Interfaces
{
    public interface ILogToolWindowViewModel
    {
        string LogText { get; set; }
        string ProposalFilePath { get; set; }
        string ProposalOriginalText { get; set; }
        string ProposalProposedText { get; set; }
        string PendingApprovalDescription { get; set; }
        bool HasPendingApproval { get; set; }
        IRelayCommand SubmitProposalCommand { get; }
        IRelayCommand ApproveCommand { get; }
        IRelayCommand RejectCommand { get; }
        void SetProposalSubmissionHandler(Action<string, string, string>? onSubmitProposalRequested);
        void SetApprovalRequestHandlers(Action? onApproveRequested, Action? onRejectRequested);
    }
}
