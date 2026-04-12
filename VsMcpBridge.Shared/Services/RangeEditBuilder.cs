using System;
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
        var startIndex = prefixLength;

        return new RangeEdit
        {
            StartIndex = startIndex,
            OriginalSegment = originalText.Substring(startIndex, originalSegmentLength),
            UpdatedSegment = updatedText.Substring(startIndex, updatedSegmentLength),
            PrefixContext = GetPrefixContext(originalText, startIndex),
            SuffixContext = GetSuffixContext(originalText, startIndex + originalSegmentLength)
        };
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
}
