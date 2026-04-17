using System;
using System.Collections.Generic;
using VsMcpBridge.Shared.Models;

namespace VsMcpBridge.Shared.Interfaces
{
    public interface ILogToolWindowPresenter
    {
        ILogToolWindowControl LogToolWindowControl { get; set; }
        ILogToolWindowViewModel LogToolWindowViewModel { get; set; }

        void Initialize();
        void AppendLog(string message);
        void ShowApprovalPrompt(string description, string? originalSegment, string? updatedSegment, IReadOnlyList<ProposalReviewedChange>? reviewedChanges, Action onApprove, Action onReject);
        void ShowStatusMessage(string message);
        void CompleteProposalCycle(string statusMessage);
    }
}
