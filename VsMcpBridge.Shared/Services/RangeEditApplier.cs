using System;
using VsMcpBridge.Shared.Models;

namespace VsMcpBridge.Shared.Services;

public static class RangeEditApplier
{
    public static EditApplyResult Apply(EditProposal proposal, string currentText, Action<string> writeUpdatedText)
    {
        if (proposal.RangeEdit == null)
            throw new InvalidOperationException("Range edit metadata is required.");

        var rangeEdit = proposal.RangeEdit;
        var startIndex = rangeEdit.StartIndex;
        var originalSegment = rangeEdit.OriginalSegment ?? string.Empty;
        var updatedSegment = rangeEdit.UpdatedSegment ?? string.Empty;
        var prefixContext = rangeEdit.PrefixContext ?? string.Empty;
        var suffixContext = rangeEdit.SuffixContext ?? string.Empty;

        var expectedUpdatedText = currentText;
        if (MatchesUpdatedRange(currentText, startIndex, updatedSegment, prefixContext, suffixContext))
            return EditApplyResult.SkippedAlreadyMatchesApprovedUpdatedContent;

        if (CountRangeMatches(currentText, originalSegment, prefixContext, suffixContext) > 1)
            throw new TargetDocumentDriftException();

        if (!MatchesOriginalRange(currentText, startIndex, originalSegment, prefixContext, suffixContext))
            throw new TargetDocumentDriftException();

        expectedUpdatedText = currentText.Substring(0, startIndex)
            + updatedSegment
            + currentText.Substring(startIndex + originalSegment.Length);

        writeUpdatedText(expectedUpdatedText);
        return EditApplyResult.Applied;
    }

    private static bool MatchesOriginalRange(string currentText, int startIndex, string originalSegment, string prefixContext, string suffixContext)
    {
        return MatchesRange(currentText, startIndex, originalSegment, prefixContext, suffixContext);
    }

    private static bool MatchesUpdatedRange(string currentText, int startIndex, string updatedSegment, string prefixContext, string suffixContext)
    {
        return MatchesRange(currentText, startIndex, updatedSegment, prefixContext, suffixContext);
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
