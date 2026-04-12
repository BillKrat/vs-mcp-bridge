using System;
using System.IO;
using System.Threading.Tasks;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.Models;

namespace VsMcpBridge.Shared.Services;

public sealed class FileEditApplier : IEditApplier
{
    public Task ApplyAsync(EditProposal proposal)
    {
        if (proposal == null)
            throw new ArgumentNullException(nameof(proposal));

        if (string.IsNullOrWhiteSpace(proposal.FilePath))
            throw new InvalidOperationException("Edit proposal does not specify a file path.");

        var (originalText, updatedText) = EditProposalTextRebuilder.Rebuild(proposal.Diff);
        var currentText = File.ReadAllText(proposal.FilePath);

        if (string.Equals(currentText, updatedText, StringComparison.Ordinal))
            return Task.CompletedTask;

        if (!string.Equals(currentText, originalText, StringComparison.Ordinal))
            throw new InvalidOperationException("Target document no longer matches the approved proposal.");

        File.WriteAllText(proposal.FilePath, updatedText);
        return Task.CompletedTask;
    }
}
