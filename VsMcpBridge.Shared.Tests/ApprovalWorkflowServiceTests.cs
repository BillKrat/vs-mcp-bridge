using System;
using System.Collections.Generic;
using VsMcpBridge.Shared.Models;
using VsMcpBridge.Shared.Services;
using Xunit;

namespace VsMcpBridge.Shared.Tests;

public sealed class ApprovalWorkflowServiceTests
{
    [Fact]
    public void CreateProposal_creates_pending_proposal_with_generated_id()
    {
        var service = new InMemoryApprovalWorkflowService();

        var proposal = service.CreateProposal("request-123", "sample.cs", "--- a/sample.cs");

        Assert.Equal("request-123", proposal.RequestId);
        Assert.False(string.IsNullOrWhiteSpace(proposal.ProposalId));
        Assert.Equal("sample.cs", proposal.FilePath);
        Assert.Equal("--- a/sample.cs", proposal.Diff);
        Assert.Equal(ProposalStatus.Pending, proposal.Status);
        Assert.Same(proposal, service.Get(proposal.ProposalId));
    }

    [Fact]
    public void Approve_updates_existing_proposal_status()
    {
        var service = new InMemoryApprovalWorkflowService();
        var proposal = service.CreateProposal("request-123", "sample.cs", "diff");

        var approved = service.Approve(proposal.ProposalId);

        Assert.Same(proposal, approved);
        Assert.Equal(ProposalStatus.Approved, approved.Status);
        Assert.Equal(ProposalStatus.Approved, service.Get(proposal.ProposalId)!.Status);
    }

    [Fact]
    public void Reject_updates_existing_proposal_status()
    {
        var service = new InMemoryApprovalWorkflowService();
        var proposal = service.CreateProposal("request-123", "sample.cs", "diff");

        var rejected = service.Reject(proposal.ProposalId);

        Assert.Same(proposal, rejected);
        Assert.Equal(ProposalStatus.Rejected, rejected.Status);
        Assert.Equal(ProposalStatus.Rejected, service.Get(proposal.ProposalId)!.Status);
    }

    [Fact]
    public void MarkApplied_updates_existing_proposal_status()
    {
        var service = new InMemoryApprovalWorkflowService();
        var proposal = service.CreateProposal("request-123", "sample.cs", "diff");

        service.Approve(proposal.ProposalId);
        var applied = service.MarkApplied(proposal.ProposalId);

        Assert.Same(proposal, applied);
        Assert.Equal(ProposalStatus.Applied, applied.Status);
        Assert.Equal(ProposalStatus.Applied, service.Get(proposal.ProposalId)!.Status);
    }

    [Fact]
    public void MarkFailed_updates_existing_proposal_status()
    {
        var service = new InMemoryApprovalWorkflowService();
        var proposal = service.CreateProposal("request-123", "sample.cs", "diff");

        service.Approve(proposal.ProposalId);
        var failed = service.MarkFailed(proposal.ProposalId);

        Assert.Same(proposal, failed);
        Assert.Equal(ProposalStatus.Failed, failed.Status);
        Assert.Equal(ProposalStatus.Failed, service.Get(proposal.ProposalId)!.Status);
    }

    [Fact]
    public void Approve_throws_for_unknown_proposal()
    {
        var service = new InMemoryApprovalWorkflowService();

        var exception = Assert.Throws<InvalidOperationException>(() => service.Approve("missing"));

        Assert.Contains("Unknown proposal", exception.Message);
    }

    [Fact]
    public void CreateProposal_preserves_multi_range_metadata()
    {
        var service = new InMemoryApprovalWorkflowService();
        var rangeEdits = new List<RangeEdit>
        {
            new() { StartIndex = 1, OriginalSegment = "b", UpdatedSegment = "B" },
            new() { StartIndex = 3, OriginalSegment = "d", UpdatedSegment = "D" }
        };

        var proposal = service.CreateProposal("request-123", "sample.cs", "diff", null, rangeEdits);

        Assert.Null(proposal.RangeEdit);
        Assert.NotNull(proposal.RangeEdits);
        Assert.Equal(2, proposal.RangeEdits!.Count);
        Assert.Equal(1, proposal.RangeEdits[0].StartIndex);
        Assert.Equal(3, proposal.RangeEdits[1].StartIndex);
    }
}
