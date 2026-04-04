using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
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
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        var dte = await _package.GetServiceAsync<DTE2>(typeof(DTE));
        if (dte == null)
            throw new InvalidOperationException("DTE service unavailable.");

        var document = OpenDocument(dte, proposal.FilePath);
        var textDocument = GetTextDocument(document);
        if (textDocument == null)
            throw new InvalidOperationException($"Document '{proposal.FilePath}' does not support text edits.");

        var updatedText = BuildUpdatedText(proposal.Diff);
        ApplyUpdatedText(textDocument, updatedText);
        SaveDocument(document);
    }

    private static TextDocument? GetTextDocument(Document document)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        return document.Object("TextDocument") as TextDocument;
    }

    private static Document OpenDocument(DTE2 dte, string filePath)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        return dte.Documents.Open(filePath);
    }

    private static void ApplyUpdatedText(TextDocument textDocument, string updatedText)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var editPoint = textDocument.StartPoint.CreateEditPoint();
        editPoint.Delete(textDocument.EndPoint);
        editPoint.Insert(updatedText);
    }

    private static void SaveDocument(Document document)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
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
