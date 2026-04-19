using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using VsMcpBridge.Shared.Models;

namespace VsMcpBridge.Shared.Interfaces
{
    public interface ILogToolWindowViewModel
    {
        string LogText { get; set; }
        string RequestInputText { get; set; }
        bool HasRequestInputText { get; }
        string LastSubmittedRequestText { get; set; }
        bool HasLastSubmittedRequest { get; }
        bool IsRequestInProgress { get; set; }
        string ActivityTitle { get; }
        string ActivitySummary { get; }
        string ActivityMetricsSummary { get; }
        bool HasActivityMetricsSummary { get; }
        bool CanOpenGitChanges { get; }
        string ProposalFilePath { get; set; }
        IReadOnlyList<string> ProposalSelectedFiles { get; set; }
        string ProposalSelectionSummary { get; }
        string ProposalActiveFileSummary { get; }
        string RequestPhaseSummary { get; }
        bool HasRequestPhaseSummary { get; }
        string ProposalOriginalText { get; set; }
        string ProposalProposedText { get; set; }
        string PendingApprovalDescription { get; set; }
        string PendingApprovalOriginalSegment { get; set; }
        string PendingApprovalUpdatedSegment { get; set; }
        IReadOnlyList<ProposalReviewedChange> PendingApprovalReviewedChanges { get; set; }
        IReadOnlyList<string> PendingApprovalIncludedFiles { get; set; }
        string LastCompletedProposalOriginalText { get; set; }
        string LastCompletedProposalUpdatedText { get; set; }
        string LastCompletedProposalOriginalSegment { get; set; }
        string LastCompletedProposalUpdatedSegment { get; set; }
        IReadOnlyList<ProposalReviewedChange> LastCompletedProposalReviewedChanges { get; set; }
        IReadOnlyList<string> LastCompletedProposalIncludedFiles { get; set; }
        string StatusMessage { get; set; }
        bool IsProposalFileLoaded { get; set; }
        bool HasPendingApproval { get; set; }
        bool HasPendingApprovalRangePreview { get; }
        bool HasPendingApprovalReviewedChanges { get; }
        bool HasPendingApprovalIncludedFiles { get; }
        string PendingApprovalIncludedFilesHeader { get; }
        bool HasLastCompletedProposalPreview { get; }
        bool HasLastCompletedProposalRangePreview { get; }
        bool HasLastCompletedProposalReviewedChanges { get; }
        bool HasLastCompletedProposalIncludedFiles { get; }
        string LastCompletedProposalIncludedFilesHeader { get; }
        bool IsReviewFocusedLayoutActive { get; }
        bool ShowProposalEditor { get; }
        bool ShowSubmitProposalButton { get; }
        bool IsProposalOriginalTextReadOnly { get; }
        bool IsProposalProposedTextReadOnly { get; }
        bool HasProposalDrafts { get; set; }
        bool HasSubmittableProposal { get; set; }
        bool HasResettableProposalState { get; }
        bool HasSessionState { get; }
        bool CanBrowseProposalFile { get; set; }
        IRelayCommand SubmitProposalCommand { get; }
        IRelayCommand BrowseProposalFileCommand { get; }
        IRelayCommand RemoveProposalFileCommand { get; }
        IRelayCommand ResetProposalCommand { get; }
        IRelayCommand NewChatCommand { get; }
        IRelayCommand OpenGitChangesCommand { get; }
        IRelayCommand ApproveCommand { get; }
        IRelayCommand RejectCommand { get; }
        LogLevel SelectedLogLevel { get; set; }
        IReadOnlyList<LogLevel> AvailableLogLevels { get; }
        void SetProposalBrowseHandler(Action? onBrowseProposalFileRequested);
        void SetProposalRemoveHandler(Action? onRemoveProposalFileRequested);
        void SetProposalResetHandler(Action? onResetProposalRequested);
        void SetNewChatHandler(Action? onNewChatRequested);
        void SetProposalSubmissionHandler(Action? onSubmitProposalRequested);
        void SetOpenGitChangesHandler(Action? onOpenGitChangesRequested);
        void SetApprovalRequestHandlers(Action? onApproveRequested, Action? onRejectRequested);
    }
}
