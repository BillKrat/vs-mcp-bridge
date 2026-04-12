using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.Threading.Tasks;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.Models;
using VsMcpBridge.Shared.Services;

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

    public async Task<EditApplyResult> ApplyAsync(EditProposal proposal)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        var dte = await _package.GetServiceAsync<DTE2>(typeof(DTE));
        if (dte == null)
            throw new InvalidOperationException("DTE service unavailable.");

        var document = OpenDocument(dte, proposal.FilePath);
        var textDocument = GetTextDocument(document);
        if (textDocument == null)
            throw new InvalidOperationException($"Document '{proposal.FilePath}' does not support text edits.");

        var (originalText, updatedText) = EditProposalTextRebuilder.Rebuild(proposal.Diff);
        var currentText = GetDocumentText(textDocument);

        if (string.Equals(currentText, updatedText, StringComparison.Ordinal))
            return EditApplyResult.SkippedAlreadyMatchesApprovedUpdatedContent;

        if (!string.Equals(currentText, originalText, StringComparison.Ordinal))
            throw new TargetDocumentDriftException();

        ApplyUpdatedText(textDocument, updatedText);
        SaveDocument(document);
        return EditApplyResult.Applied;
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

    private static string GetDocumentText(TextDocument textDocument)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        return textDocument.StartPoint.CreateEditPoint().GetText(textDocument.EndPoint);
    }
}
