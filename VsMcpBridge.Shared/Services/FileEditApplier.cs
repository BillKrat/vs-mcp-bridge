using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.Models;

namespace VsMcpBridge.Shared.Services;

public sealed class FileEditApplier : IEditApplier
{
    public Task<EditApplyResult> ApplyAsync(EditProposal proposal)
    {
        if (proposal == null)
            throw new ArgumentNullException(nameof(proposal));

        var fileEdits = EditProposalPlanner.GetFileEdits(proposal);
        var plannedEdits = new List<PlannedFileEdit>(fileEdits.Count);

        foreach (var fileEdit in fileEdits)
        {
            var currentText = File.ReadAllText(fileEdit.FilePath);
            plannedEdits.Add(EditProposalPlanner.Plan(fileEdit, currentText));
        }

        if (plannedEdits.All(edit => edit.Result == EditApplyResult.SkippedAlreadyMatchesApprovedUpdatedContent))
            return Task.FromResult(EditApplyResult.SkippedAlreadyMatchesApprovedUpdatedContent);

        var appliedEdits = new List<PlannedFileEdit>();
        try
        {
            foreach (var plannedEdit in plannedEdits.Where(edit => edit.Result == EditApplyResult.Applied))
            {
                File.WriteAllText(plannedEdit.FilePath, plannedEdit.UpdatedText);
                appliedEdits.Add(plannedEdit);
            }
        }
        catch
        {
            RestoreAppliedEdits(appliedEdits);
            throw;
        }

        return Task.FromResult(EditApplyResult.Applied);
    }

    private static void RestoreAppliedEdits(IEnumerable<PlannedFileEdit> appliedEdits)
    {
        foreach (var appliedEdit in appliedEdits.Reverse())
        {
            File.WriteAllText(appliedEdit.FilePath, appliedEdit.OriginalText);
        }
    }
}
