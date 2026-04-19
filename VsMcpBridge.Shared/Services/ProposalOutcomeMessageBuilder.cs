using System;
using VsMcpBridge.Shared.Models;

namespace VsMcpBridge.Shared.Services;

public static class ProposalOutcomeMessageBuilder
{
    public static string BuildSuccessMessage(EditProposal proposal)
        => $"Apply succeeded for {DescribeScope(proposal)}. All approved changes were applied.";

    public static string BuildSkipMessage(EditProposal proposal)
        => $"Apply skipped for {DescribeScope(proposal)} because all targets already match the approved content.";

    public static string BuildDriftFailureMessage(EditProposal proposal)
        => $"Apply failed for {DescribeScope(proposal)} because at least one target no longer matches the approved content. No changes were applied.";

    public static string BuildAmbiguityFailureMessage(EditProposal proposal)
        => $"Apply failed for {DescribeScope(proposal)} because at least one target location is ambiguous. No changes were applied.";

    public static string BuildGenericFailureMessage(EditProposal proposal)
        => $"Apply failed for {DescribeScope(proposal)}. No changes were applied. Review the bridge log for details.";

    public static string BuildRejectedMessage(EditProposal proposal)
        => $"Proposal rejected for {DescribeScope(proposal)}. No changes were applied.";

    public static string DescribeScope(EditProposal proposal)
    {
        if (proposal == null)
            throw new ArgumentNullException(nameof(proposal));

        var fileCount = proposal.FileEdits?.Count > 0 ? proposal.FileEdits.Count : 1;
        return fileCount == 1 ? "1 file" : $"{fileCount} files";
    }
}
