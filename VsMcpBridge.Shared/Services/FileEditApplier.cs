using System;
using System.IO;
using System.Threading.Tasks;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.Models;

namespace VsMcpBridge.Shared.Services;

public sealed class FileEditApplier : IEditApplier
{
    public Task<EditApplyResult> ApplyAsync(EditProposal proposal)
    {
        if (proposal == null)
            throw new ArgumentNullException(nameof(proposal));

        if (string.IsNullOrWhiteSpace(proposal.FilePath))
            throw new InvalidOperationException("Edit proposal does not specify a file path.");

        var currentText = File.ReadAllText(proposal.FilePath);

        if (proposal.RangeEdit != null || (proposal.RangeEdits != null && proposal.RangeEdits.Count > 0))
        {
            return Task.FromResult(RangeEditApplier.Apply(
                proposal,
                currentText,
                updatedText => File.WriteAllText(proposal.FilePath, updatedText)));
        }

        var (originalText, updatedText) = EditProposalTextRebuilder.Rebuild(proposal.Diff);

        if (string.Equals(currentText, updatedText, StringComparison.Ordinal))
            return Task.FromResult(EditApplyResult.SkippedAlreadyMatchesApprovedUpdatedContent);

        if (!string.Equals(currentText, originalText, StringComparison.Ordinal))
            throw new TargetDocumentDriftException();

        File.WriteAllText(proposal.FilePath, updatedText);
        return Task.FromResult(EditApplyResult.Applied);
    }
}
