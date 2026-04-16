using System;
using System.Collections.Generic;
using System.Linq;
using VsMcpBridge.Shared.Models;

namespace VsMcpBridge.Shared.Services;

public static class RangeEditApplier
{
    public static EditApplyResult Apply(EditProposal proposal, string currentText, Action<string> writeUpdatedText)
    {
        var rangeEdits = GetRangeEdits(proposal);
        if (rangeEdits.Count == 0)
            throw new InvalidOperationException("Range edit metadata is required.");

        ValidateRangeLayout(rangeEdits);

        if (rangeEdits.All(rangeEdit => MatchesUpdatedRange(currentText, rangeEdit)))
            return EditApplyResult.SkippedAlreadyMatchesApprovedUpdatedContent;

        foreach (var rangeEdit in rangeEdits)
        {
            if (CountRangeMatches(currentText, rangeEdit.OriginalSegment ?? string.Empty, rangeEdit.PrefixContext ?? string.Empty, rangeEdit.SuffixContext ?? string.Empty) > 1)
                throw new TargetDocumentDriftException();

            if (!MatchesOriginalRange(currentText, rangeEdit))
                throw new TargetDocumentDriftException();
        }

        var updatedText = currentText;
        foreach (var rangeEdit in rangeEdits.OrderByDescending(range => range.StartIndex))
        {
            updatedText = updatedText.Substring(0, rangeEdit.StartIndex)
                + (rangeEdit.UpdatedSegment ?? string.Empty)
                + updatedText.Substring(rangeEdit.StartIndex + (rangeEdit.OriginalSegment?.Length ?? 0));
        }

        writeUpdatedText(updatedText);
        return EditApplyResult.Applied;
    }

    private static IReadOnlyList<RangeEdit> GetRangeEdits(EditProposal proposal)
    {
        if (proposal.RangeEdits != null && proposal.RangeEdits.Count > 0)
            return proposal.RangeEdits;

        if (proposal.RangeEdit != null)
            return new[] { proposal.RangeEdit };

        return Array.Empty<RangeEdit>();
    }

    private static void ValidateRangeLayout(IReadOnlyList<RangeEdit> rangeEdits)
    {
        var ordered = rangeEdits.OrderBy(range => range.StartIndex).ToList();
        var previousEnd = -1;

        foreach (var rangeEdit in ordered)
        {
            if (rangeEdit.StartIndex < 0)
                throw new InvalidOperationException("Range edit start index cannot be negative.");

            if (rangeEdit.StartIndex < previousEnd)
                throw new InvalidOperationException("Range edits cannot overlap.");

            previousEnd = rangeEdit.StartIndex + (rangeEdit.OriginalSegment?.Length ?? 0);
        }
    }

    private static bool MatchesOriginalRange(string currentText, RangeEdit rangeEdit)
    {
        return MatchesRange(
            currentText,
            rangeEdit.StartIndex,
            rangeEdit.OriginalSegment ?? string.Empty,
            rangeEdit.PrefixContext ?? string.Empty,
            rangeEdit.SuffixContext ?? string.Empty);
    }

    private static bool MatchesUpdatedRange(string currentText, RangeEdit rangeEdit)
    {
        return MatchesRange(
            currentText,
            rangeEdit.StartIndex,
            rangeEdit.UpdatedSegment ?? string.Empty,
            rangeEdit.PrefixContext ?? string.Empty,
            rangeEdit.SuffixContext ?? string.Empty);
    }

    private static int CountRangeMatches(string currentText, string segment, string prefixContext, string suffixContext)
    {
        var maxStartIndex = currentText.Length - segment.Length - suffixContext.Length;
        if (maxStartIndex < 0)
            return 0;

        var matches = 0;
        for (var startIndex = 0; startIndex <= maxStartIndex; startIndex++)
        {
            if (MatchesRange(currentText, startIndex, segment, prefixContext, suffixContext))
                matches++;
        }

        return matches;
    }

    private static bool MatchesRange(string currentText, int startIndex, string segment, string prefixContext, string suffixContext)
    {
        if (startIndex < 0 || startIndex > currentText.Length)
            return false;

        if (startIndex < prefixContext.Length)
            return false;

        if (startIndex + segment.Length + suffixContext.Length > currentText.Length)
            return false;

        var prefixStart = startIndex - prefixContext.Length;
        if (!string.Equals(currentText.Substring(prefixStart, prefixContext.Length), prefixContext, StringComparison.Ordinal))
            return false;

        if (!string.Equals(currentText.Substring(startIndex, segment.Length), segment, StringComparison.Ordinal))
            return false;

        var suffixStart = startIndex + segment.Length;
        return string.Equals(currentText.Substring(suffixStart, suffixContext.Length), suffixContext, StringComparison.Ordinal);
    }
}
