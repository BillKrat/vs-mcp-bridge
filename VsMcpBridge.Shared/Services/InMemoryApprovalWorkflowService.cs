using System;
using System.Collections.Generic;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.Models;

namespace VsMcpBridge.Shared.Services;

public sealed class InMemoryApprovalWorkflowService : IApprovalWorkflowService
{
    private readonly Dictionary<string, EditProposal> _proposals = new(StringComparer.Ordinal);
    private readonly object _sync = new();

    public EditProposal CreateProposal(string requestId, string filePath, string diff, RangeEdit? rangeEdit = null, IReadOnlyList<RangeEdit>? rangeEdits = null)
    {
        var proposal = new EditProposal
        {
            RequestId = requestId,
            ProposalId = Guid.NewGuid().ToString("N"),
            FilePath = filePath,
            Diff = diff,
            RangeEdit = rangeEdit,
            RangeEdits = rangeEdits == null ? null : new List<RangeEdit>(rangeEdits),
            Status = ProposalStatus.Pending
        };

        lock (_sync)
        {
            _proposals[proposal.ProposalId] = proposal;
        }

        return proposal;
    }

    public EditProposal? Get(string proposalId)
    {
        lock (_sync)
        {
            return _proposals.TryGetValue(proposalId, out var proposal) ? proposal : null;
        }
    }

    public EditProposal Approve(string proposalId)
    {
        lock (_sync)
        {
            var proposal = GetRequiredProposal(proposalId);
            proposal.Status = ProposalStatus.Approved;
            return proposal;
        }
    }

    public EditProposal Reject(string proposalId)
    {
        lock (_sync)
        {
            var proposal = GetRequiredProposal(proposalId);
            proposal.Status = ProposalStatus.Rejected;
            return proposal;
        }
    }

    public EditProposal MarkApplied(string proposalId)
    {
        lock (_sync)
        {
            var proposal = GetRequiredProposal(proposalId);
            proposal.Status = ProposalStatus.Applied;
            return proposal;
        }
    }

    public EditProposal MarkFailed(string proposalId)
    {
        lock (_sync)
        {
            var proposal = GetRequiredProposal(proposalId);
            proposal.Status = ProposalStatus.Failed;
            return proposal;
        }
    }

    private EditProposal GetRequiredProposal(string proposalId)
    {
        if (!_proposals.TryGetValue(proposalId, out var proposal))
            throw new InvalidOperationException($"Unknown proposal '{proposalId}'.");

        return proposal;
    }
}
