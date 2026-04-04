using EnvDTE;
using EnvDTE80;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.Models;

namespace VsMcpBridge.Vsix.Services;

internal sealed class VsixEditApplier : IEditApplier
{
    private readonly IAsyncPackage _package;
    private readonly IThreadHelper _threadHelper;

    public VsixEditApplier(IAsyncPackage package, IThreadHelper threadHelper)
    {
        _package = package;
        _threadHelper = threadHelper;
    }

    public async Task ApplyAsync(EditProposal proposal)
    {
        await _threadHelper.SwitchToMainThreadAsync();

        var dte = await _package.GetServiceAsync<DTE2>(typeof(DTE));
        if (dte == null)
            throw new InvalidOperationException("DTE service unavailable.");

        var document = dte.Documents.Open(proposal.FilePath);
        var textDocument = document.Object("TextDocument") as TextDocument;
        if (textDocument == null)
            throw new InvalidOperationException($"Document '{proposal.FilePath}' does not support text edits.");

        var updatedText = BuildUpdatedText(proposal.Diff);
        var editPoint = textDocument.StartPoint.CreateEditPoint();
        editPoint.Delete(textDocument.EndPoint);
        editPoint.Insert(updatedText);
        document.Save();
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
