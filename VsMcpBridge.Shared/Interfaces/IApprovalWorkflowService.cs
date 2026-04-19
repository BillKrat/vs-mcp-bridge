using System.Collections.Generic;
using VsMcpBridge.Shared.Models;

namespace VsMcpBridge.Shared.Interfaces;

public interface IApprovalWorkflowService
{
    EditProposal CreateProposal(string requestId, string filePath, string diff, RangeEdit? rangeEdit = null, IReadOnlyList<RangeEdit>? rangeEdits = null);
    EditProposal CreateProposal(string requestId, IReadOnlyList<ProposedFileEdit> fileEdits);
    EditProposal? Get(string proposalId);
    EditProposal Approve(string proposalId);
    EditProposal Reject(string proposalId);
    EditProposal MarkApplied(string proposalId);
    EditProposal MarkFailed(string proposalId);
}
