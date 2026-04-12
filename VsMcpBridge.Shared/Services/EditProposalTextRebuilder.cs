using System;
using System.Collections.Generic;

namespace VsMcpBridge.Shared.Services;

public static class EditProposalTextRebuilder
{
    public static (string OriginalText, string UpdatedText) Rebuild(string diff)
    {
        var originalLines = new List<string>();
        var updatedLines = new List<string>();

        var rawLines = (diff ?? string.Empty).Split('\n');
        foreach (var segment in rawLines)
        {
            var rawLine = segment.EndsWith("\r", StringComparison.Ordinal)
                ? segment.Substring(0, segment.Length - 1)
                : segment;

            if (string.IsNullOrEmpty(rawLine))
                continue;

            if (rawLine.StartsWith("--- ", StringComparison.Ordinal) || rawLine.StartsWith("+++ ", StringComparison.Ordinal))
                continue;

            var prefix = rawLine[0];
            var content = rawLine.Length > 1 ? rawLine.Substring(1) : string.Empty;

            switch (prefix)
            {
                case ' ':
                    originalLines.Add(content);
                    updatedLines.Add(content);
                    break;
                case '+':
                    updatedLines.Add(content);
                    break;
                case '-':
                    originalLines.Add(content);
                    break;
                default:
                    throw new InvalidOperationException("Unsupported diff format for edit proposal.");
            }
        }

        return (string.Join("\n", originalLines), string.Join("\n", updatedLines));
    }
}
