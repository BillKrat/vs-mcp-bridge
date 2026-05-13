using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using VsMcpBridge.Shared.Models;

namespace VsMcpBridge.Shared.Interfaces
{
    public interface IProposalManager
    {
        int SelectedFileCount { get; }
        string? ActiveManualRequestId { get; }
        bool HasProposalFilePicker { get; }

        void Initialize();
        void ShowApprovalPrompt(string description, string? originalSegment, string? updatedSegment, IReadOnlyList<ProposalReviewedChange>? reviewedChanges, Action onApprove, Action onReject, IReadOnlyList<string>? includedFiles = null, string? requestId = null);
        void CompleteProposalCycle(string statusMessage, string? requestId = null);
        Task SubmitProposalAsync(Func<string, Task<bool>> tryDispatchPromptRequestAsync, Action<string> appendPromptActivityEntry);
        void BrowseProposalFile();
        void RemoveSelectedProposalFile();
        void ResetCurrentRequestState();
        void StartNewChat();
        void ApproveRequested();
        void RejectRequested();
        void HandleViewModelPropertyChanged(PropertyChangedEventArgs e);
    }
}
