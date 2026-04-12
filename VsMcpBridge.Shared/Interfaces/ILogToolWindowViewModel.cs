using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace VsMcpBridge.Shared.Interfaces
{
    public interface ILogToolWindowViewModel
    {
        string LogText { get; set; }
        string ProposalFilePath { get; set; }
        string ProposalOriginalText { get; set; }
        string ProposalProposedText { get; set; }
        string PendingApprovalDescription { get; set; }
        string StatusMessage { get; set; }
        bool HasPendingApproval { get; set; }
        bool IsProposalOriginalTextReadOnly { get; }
        bool IsProposalProposedTextReadOnly { get; }
        IRelayCommand SubmitProposalCommand { get; }
        IRelayCommand ApproveCommand { get; }
        IRelayCommand RejectCommand { get; }
        LogLevel SelectedLogLevel { get; set; }
        IReadOnlyList<LogLevel> AvailableLogLevels { get; }
        void SetProposalSubmissionHandler(Action<string, string, string>? onSubmitProposalRequested);
        void SetApprovalRequestHandlers(Action? onApproveRequested, Action? onRejectRequested);
    }
}
