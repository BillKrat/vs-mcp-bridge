using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.Models;

namespace VsMcpBridge.App.Services;

internal sealed class FileEditApplier : IEditApplier
{
    public Task ApplyAsync(EditProposal proposal)
    {
        if (proposal == null)
            throw new ArgumentNullException(nameof(proposal));

        if (string.IsNullOrWhiteSpace(proposal.FilePath))
            throw new InvalidOperationException("Edit proposal does not specify a file path.");

        var updatedText = BuildUpdatedText(proposal.Diff);
        File.WriteAllText(proposal.FilePath, updatedText);
        return Task.CompletedTask;
    }

    private static string BuildUpdatedText(string diff)
    {
        var lines = diff.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var updatedLines = new List<string>();

        foreach (var rawLine in lines)
        {
            if (string.IsNullOrEmpty(rawLine))
                continue;

            if (rawLine.StartsWith("--- ", StringComparison.Ordinal) || rawLine.StartsWith("+++ ", StringComparison.Ordinal))
                continue;

            var prefix = rawLine[0];
            var content = rawLine.Length > 1 ? rawLine.Substring(1) : string.Empty;

            switch (prefix)
            {
                case ' ':
                case '+':
                    updatedLines.Add(content);
                    break;
                case '-':
                    break;
                default:
                    throw new InvalidOperationException("Unsupported diff format for edit proposal.");
            }
        }

        return string.Join("\n", updatedLines);
    }
}
