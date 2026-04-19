using System;
using System.Collections.Generic;
using VsMcpBridge.Shared.Models;

namespace VsMcpBridge.Shared.Services;

public static class EditProposalPlanner
{
    public static IReadOnlyList<ProposedFileEdit> GetFileEdits(EditProposal proposal)
    {
        if (proposal.FileEdits != null && proposal.FileEdits.Count > 0)
            return proposal.FileEdits;

        return new[]
        {
            new ProposedFileEdit
            {
                FilePath = proposal.FilePath,
                Diff = proposal.Diff,
                RangeEdit = proposal.RangeEdit,
                RangeEdits = proposal.RangeEdits
            }
        };
    }

    public static PlannedFileEdit Plan(ProposedFileEdit fileEdit, string currentText)
    {
        if (fileEdit == null)
            throw new ArgumentNullException(nameof(fileEdit));

        if (string.IsNullOrWhiteSpace(fileEdit.FilePath))
            throw new InvalidOperationException("Edit proposal does not specify a file path.");

        if (fileEdit.RangeEdit != null || (fileEdit.RangeEdits != null && fileEdit.RangeEdits.Count > 0))
        {
            var (rangeUpdatedText, result) = RangeEditApplier.BuildUpdatedTextWithResult(fileEdit.RangeEdit, fileEdit.RangeEdits, currentText);
            return new PlannedFileEdit(
                fileEdit.FilePath,
                currentText,
                rangeUpdatedText,
                result);
        }

        var (originalText, updatedText) = EditProposalTextRebuilder.Rebuild(fileEdit.Diff);

        if (string.Equals(currentText, updatedText, StringComparison.Ordinal))
            return new PlannedFileEdit(fileEdit.FilePath, currentText, updatedText, EditApplyResult.SkippedAlreadyMatchesApprovedUpdatedContent);

        if (!string.Equals(currentText, originalText, StringComparison.Ordinal))
            throw new TargetDocumentDriftException();

        return new PlannedFileEdit(fileEdit.FilePath, currentText, updatedText);
    }
}

public sealed class PlannedFileEdit
{
    public PlannedFileEdit(string filePath, string originalText, string updatedText, EditApplyResult result = EditApplyResult.Applied)
    {
        FilePath = filePath;
        OriginalText = originalText;
        UpdatedText = updatedText;
        Result = result;
    }

    public string FilePath { get; }
    public string OriginalText { get; }
    public string UpdatedText { get; }
    public EditApplyResult Result { get; }
}
