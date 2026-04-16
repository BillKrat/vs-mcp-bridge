using System;
using System.Collections.Generic;
using VsMcpBridge.Shared.Models;

namespace VsMcpBridge.Shared.Services;

public static class RangeEditBuilder
{
    private const int ContextWindow = 32;

    public static RangeEdit Build(string originalText, string updatedText)
    {
        originalText ??= string.Empty;
        updatedText ??= string.Empty;

        var prefixLength = GetCommonPrefixLength(originalText, updatedText);
        var originalSuffixLength = originalText.Length - prefixLength;
        var updatedSuffixLength = updatedText.Length - prefixLength;

        var suffixLength = GetCommonSuffixLength(
            originalText,
            updatedText,
            prefixLength,
            originalSuffixLength,
            updatedSuffixLength);

        var originalSegmentLength = originalText.Length - prefixLength - suffixLength;
        var updatedSegmentLength = updatedText.Length - prefixLength - suffixLength;
        return CreateRangeEdit(
            originalText,
            updatedText,
            prefixLength,
            prefixLength,
            originalSegmentLength,
            updatedSegmentLength);
    }

    public static IReadOnlyList<RangeEdit> BuildAll(string originalText, string updatedText)
    {
        originalText ??= string.Empty;
        updatedText ??= string.Empty;

        if (string.Equals(originalText, updatedText, StringComparison.Ordinal))
            return Array.Empty<RangeEdit>();

        var blocks = BuildDifferingBlocks(originalText, updatedText);
        if (blocks.Count <= 1)
            return new[] { Build(originalText, updatedText) };

        var rangeEdits = new List<RangeEdit>(blocks.Count);
        foreach (var block in blocks)
        {
            var blockOriginal = originalText.Substring(block.OriginalStart, block.OriginalLength);
            var blockUpdated = updatedText.Substring(block.UpdatedStart, block.UpdatedLength);
            var refined = Build(blockOriginal, blockUpdated);
            rangeEdits.Add(CreateRangeEdit(
                originalText,
                updatedText,
                block.OriginalStart + refined.StartIndex,
                block.UpdatedStart + refined.StartIndex,
                refined.OriginalSegment.Length,
                refined.UpdatedSegment.Length));
        }

        return rangeEdits;
    }

    private static RangeEdit CreateRangeEdit(string originalText, string updatedText, int originalStartIndex, int updatedStartIndex, int originalSegmentLength, int updatedSegmentLength)
    {
        return new RangeEdit
        {
            StartIndex = originalStartIndex,
            OriginalSegment = originalText.Substring(originalStartIndex, originalSegmentLength),
            UpdatedSegment = updatedText.Substring(updatedStartIndex, updatedSegmentLength),
            PrefixContext = GetPrefixContext(originalText, originalStartIndex),
            SuffixContext = GetSuffixContext(originalText, originalStartIndex + originalSegmentLength)
        };
    }

    private static List<DiffBlock> BuildDifferingBlocks(string originalText, string updatedText)
    {
        var originalLines = SplitLines(originalText);
        var updatedLines = SplitLines(updatedText);
        var matches = BuildLongestCommonSubsequenceMatches(originalLines, updatedLines);
        var blocks = new List<DiffBlock>();

        var previousOriginalLine = 0;
        var previousUpdatedLine = 0;
        foreach (var match in matches)
        {
            if (match.OriginalIndex > previousOriginalLine || match.UpdatedIndex > previousUpdatedLine)
            {
                blocks.Add(new DiffBlock(
                    GetLineStartOffset(originalLines, previousOriginalLine),
                    GetLineStartOffset(originalLines, match.OriginalIndex),
                    GetLineStartOffset(updatedLines, previousUpdatedLine),
                    GetLineStartOffset(updatedLines, match.UpdatedIndex)));
            }

            previousOriginalLine = match.OriginalIndex + 1;
            previousUpdatedLine = match.UpdatedIndex + 1;
        }

        if (previousOriginalLine < originalLines.Count || previousUpdatedLine < updatedLines.Count)
        {
            blocks.Add(new DiffBlock(
                GetLineStartOffset(originalLines, previousOriginalLine),
                GetLineStartOffset(originalLines, originalLines.Count),
                GetLineStartOffset(updatedLines, previousUpdatedLine),
                GetLineStartOffset(updatedLines, updatedLines.Count)));
        }

        return blocks;
    }

    private static List<LineSegment> SplitLines(string text)
    {
        var lines = new List<LineSegment>();
        var index = 0;

        while (index < text.Length)
        {
            var lineStart = index;
            while (index < text.Length && text[index] != '\r' && text[index] != '\n')
                index++;

            if (index < text.Length)
            {
                if (text[index] == '\r' && index + 1 < text.Length && text[index + 1] == '\n')
                    index += 2;
                else
                    index++;
            }

            lines.Add(new LineSegment(lineStart, text.Substring(lineStart, index - lineStart)));
        }

        return lines;
    }

    private static List<LineMatch> BuildLongestCommonSubsequenceMatches(IReadOnlyList<LineSegment> originalLines, IReadOnlyList<LineSegment> updatedLines)
    {
        var lengths = new int[originalLines.Count + 1, updatedLines.Count + 1];
        for (var originalIndex = originalLines.Count - 1; originalIndex >= 0; originalIndex--)
        {
            for (var updatedIndex = updatedLines.Count - 1; updatedIndex >= 0; updatedIndex--)
            {
                lengths[originalIndex, updatedIndex] = string.Equals(originalLines[originalIndex].Text, updatedLines[updatedIndex].Text, StringComparison.Ordinal)
                    ? lengths[originalIndex + 1, updatedIndex + 1] + 1
                    : Math.Max(lengths[originalIndex + 1, updatedIndex], lengths[originalIndex, updatedIndex + 1]);
            }
        }

        var matches = new List<LineMatch>();
        var i = 0;
        var j = 0;
        while (i < originalLines.Count && j < updatedLines.Count)
        {
            if (string.Equals(originalLines[i].Text, updatedLines[j].Text, StringComparison.Ordinal))
            {
                matches.Add(new LineMatch(i, j));
                i++;
                j++;
            }
            else if (lengths[i + 1, j] >= lengths[i, j + 1])
            {
                i++;
            }
            else
            {
                j++;
            }
        }

        return matches;
    }

    private static int GetLineStartOffset(IReadOnlyList<LineSegment> lines, int lineIndex)
    {
        if (lineIndex <= 0 || lines.Count == 0)
            return 0;

        if (lineIndex >= lines.Count)
        {
            var lastLine = lines[lines.Count - 1];
            return lastLine.StartIndex + lastLine.Text.Length;
        }

        return lines[lineIndex].StartIndex;
    }

    private static int GetCommonPrefixLength(string originalText, string updatedText)
    {
        var max = Math.Min(originalText.Length, updatedText.Length);
        var index = 0;
        while (index < max && originalText[index] == updatedText[index])
            index++;

        return index;
    }

    private static int GetCommonSuffixLength(string originalText, string updatedText, int prefixLength, int originalSuffixLength, int updatedSuffixLength)
    {
        var max = Math.Min(originalSuffixLength, updatedSuffixLength);
        var suffix = 0;
        while (suffix < max
            && originalText[originalText.Length - 1 - suffix] == updatedText[updatedText.Length - 1 - suffix])
        {
            suffix++;
        }

        return suffix;
    }

    private static string GetPrefixContext(string originalText, int startIndex)
    {
        var contextStart = Math.Max(0, startIndex - ContextWindow);
        return originalText.Substring(contextStart, startIndex - contextStart);
    }

    private static string GetSuffixContext(string originalText, int endIndex)
    {
        var contextLength = Math.Min(ContextWindow, originalText.Length - endIndex);
        return originalText.Substring(endIndex, contextLength);
    }

    private readonly struct LineSegment
    {
        public LineSegment(int startIndex, string text)
        {
            StartIndex = startIndex;
            Text = text;
        }

        public int StartIndex { get; }
        public string Text { get; }
    }

    private readonly struct LineMatch
    {
        public LineMatch(int originalIndex, int updatedIndex)
        {
            OriginalIndex = originalIndex;
            UpdatedIndex = updatedIndex;
        }

        public int OriginalIndex { get; }
        public int UpdatedIndex { get; }
    }

    private readonly struct DiffBlock
    {
        public DiffBlock(int originalStart, int originalEnd, int updatedStart, int updatedEnd)
        {
            OriginalStart = originalStart;
            OriginalLength = originalEnd - originalStart;
            UpdatedStart = updatedStart;
            UpdatedLength = updatedEnd - updatedStart;
        }

        public int OriginalStart { get; }
        public int OriginalLength { get; }
        public int UpdatedStart { get; }
        public int UpdatedLength { get; }
    }
}
