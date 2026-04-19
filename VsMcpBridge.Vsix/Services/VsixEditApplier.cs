using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
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

        var fileEdits = EditProposalPlanner.GetFileEdits(proposal);
        var plannedEdits = new List<PlannedTextDocumentEdit>(fileEdits.Count);

        foreach (var fileEdit in fileEdits)
        {
            var document = OpenDocument(dte, fileEdit.FilePath);
            var textDocument = GetTextDocument(document);
            if (textDocument == null)
                throw new InvalidOperationException($"Document '{fileEdit.FilePath}' does not support text edits.");

            var currentText = GetDocumentText(textDocument);
            plannedEdits.Add(new PlannedTextDocumentEdit(textDocument, EditProposalPlanner.Plan(fileEdit, currentText)));
        }

        if (plannedEdits.All(edit => edit.Plan.Result == EditApplyResult.SkippedAlreadyMatchesApprovedUpdatedContent))
            return EditApplyResult.SkippedAlreadyMatchesApprovedUpdatedContent;

        var appliedEdits = new List<PlannedTextDocumentEdit>();
        try
        {
            foreach (var plannedEdit in plannedEdits.Where(edit => edit.Plan.Result == EditApplyResult.Applied))
            {
                ApplyUpdatedText(plannedEdit.TextDocument, plannedEdit.Plan.UpdatedText);
                appliedEdits.Add(plannedEdit);
            }
        }
        catch
        {
            RestoreAppliedEdits(appliedEdits);
            throw;
        }

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
        SaveDocument(textDocument.Parent);
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

    private static void RestoreAppliedEdits(IEnumerable<PlannedTextDocumentEdit> appliedEdits)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        foreach (var appliedEdit in appliedEdits.Reverse())
        {
            ApplyUpdatedText(appliedEdit.TextDocument, appliedEdit.Plan.OriginalText);
        }
    }

    private sealed class PlannedTextDocumentEdit
    {
        public PlannedTextDocumentEdit(TextDocument textDocument, PlannedFileEdit plan)
        {
            TextDocument = textDocument;
            Plan = plan;
        }

        public TextDocument TextDocument { get; }
        public PlannedFileEdit Plan { get; }
    }
}
