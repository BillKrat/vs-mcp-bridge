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
        string PendingApprovalOriginalSegment { get; set; }
        string PendingApprovalUpdatedSegment { get; set; }
        string LastCompletedProposalOriginalText { get; set; }
        string LastCompletedProposalUpdatedText { get; set; }
        string LastCompletedProposalOriginalSegment { get; set; }
        string LastCompletedProposalUpdatedSegment { get; set; }
        string StatusMessage { get; set; }
        bool IsProposalFileLoaded { get; set; }
        bool HasPendingApproval { get; set; }
        bool HasPendingApprovalRangePreview { get; }
        bool HasLastCompletedProposalPreview { get; }
        bool HasLastCompletedProposalRangePreview { get; }
        bool ShowProposalEditor { get; }
        bool IsProposalOriginalTextReadOnly { get; }
        bool IsProposalProposedTextReadOnly { get; }
        bool CanBrowseProposalFile { get; set; }
        IRelayCommand SubmitProposalCommand { get; }
        IRelayCommand BrowseProposalFileCommand { get; }
        IRelayCommand ApproveCommand { get; }
        IRelayCommand RejectCommand { get; }
        LogLevel SelectedLogLevel { get; set; }
        IReadOnlyList<LogLevel> AvailableLogLevels { get; }
        void SetProposalBrowseHandler(Action? onBrowseProposalFileRequested);
        void SetProposalSubmissionHandler(Action<string, string, string>? onSubmitProposalRequested);
        void SetApprovalRequestHandlers(Action? onApproveRequested, Action? onRejectRequested);
    }
}
