using EnvDTE;
using EnvDTE80;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.Models;
using VsMcpBridge.Shared.Services;
using Task = System.Threading.Tasks.Task;

namespace VsMcpBridge.Vsix.Services;

/// <summary>
/// Wraps Visual Studio's DTE and error-list APIs to provide strongly-typed
/// responses for each supported MCP tool operation.
/// </summary>
public sealed class VsService : IVsService
{
    private readonly IAsyncPackage _package;
    private readonly ILogger _logger;
    private readonly IThreadHelper _threadHelper;
    private readonly IApprovalWorkflowService _approvalWorkflowService;
    private readonly IEditApplier _editApplier;
    private readonly ILogToolWindowPresenter _logToolWindowPresenter;

    public VsService(
        IAsyncPackage package,
        ILogger logger,
        IThreadHelper threadHelper,
        IApprovalWorkflowService approvalWorkflowService,
        IEditApplier editApplier,
        ILogToolWindowPresenter logToolWindowPresenter)
    {
        _package = package;
        _logger = logger;
        _threadHelper = threadHelper;
        _approvalWorkflowService = approvalWorkflowService;
        _editApplier = editApplier;
        _logToolWindowPresenter = logToolWindowPresenter;
        _logger.LogTrace("Bridge service startup complete.");
    }

    public async Task<GetActiveDocumentResponse> GetActiveDocumentAsync()
    {
        _logger.LogInformation("Running VS service operation 'GetActiveDocument'.");

        try
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var dte = await GetDteAsync();
            var doc = GetActiveDocumentOnUIThread(dte);
            if (doc == null)
                return new GetActiveDocumentResponse { Success = false, ErrorMessage = "No active document." };

            var textDoc = doc.Object("TextDocument") as TextDocument;
            var content = GetDocumentText(textDoc);
            var filePath = doc.FullName;
            var language = doc.Language;

            return new GetActiveDocumentResponse
            {
                Success = true,
                FilePath = filePath,
                Language = language,
                Content = content
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VS service operation 'GetActiveDocument' failed.");
            throw;
        }
    }

    public async Task<GetSelectedTextResponse> GetSelectedTextAsync()
    {
        _logger.LogInformation("Running VS service operation 'GetSelectedText'.");

        try
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var dte = await GetDteAsync();
            var doc = GetActiveDocumentOnUIThread(dte);
            if (doc == null)
                return new GetSelectedTextResponse { Success = false, ErrorMessage = "No active document." };

            var textDoc = doc.Object("TextDocument") as TextDocument;
            var text = GetSelectedText(textDoc);
            var filePath = doc.FullName;

            return new GetSelectedTextResponse
            {
                Success = true,
                FilePath = filePath,
                SelectedText = text
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VS service operation 'GetSelectedText' failed.");
            throw;
        }
    }

    public async Task<ListSolutionProjectsResponse> ListSolutionProjectsAsync()
    {
        _logger.LogInformation("Running VS service operation 'ListSolutionProjects'.");

        try
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var dte = await GetDteAsync();
            var solution = GetSolution(dte);
            if (solution == null)
                return new ListSolutionProjectsResponse { Success = false, ErrorMessage = "No solution open." };

            var projects = new List<ProjectInfo>();
            foreach (var project in EnumerateSolutionProjects(solution))
            {
                projects.Add(CreateProjectInfo(project));
            }

            return new ListSolutionProjectsResponse { Success = true, Projects = projects };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VS service operation 'ListSolutionProjects' failed.");
            throw;
        }
    }

    public async Task<GetErrorListResponse> GetErrorListAsync()
    {
        _logger.LogInformation("Running VS service operation 'GetErrorList'.");

        try
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var dte = await GetDteAsync();
            var diagnostics = new List<DiagnosticItem>();

            try
            {
                var errorItems = dte.ToolWindows?.ErrorList?.ErrorItems as ErrorItems;
                if (errorItems != null)
                {
                    int count = errorItems.Count;
                    for (int index = 1; index <= count; index++)
                    {
                        var item = errorItems.Item(index);
                        diagnostics.Add(new DiagnosticItem
                        {
                            Severity = ErrorLevelToSeverity(item.ErrorLevel),
                            Code = string.Empty,
                            Description = item.Description ?? string.Empty,
                            File = item.FileName ?? string.Empty,
                            Line = item.Line,
                            Column = 0,
                            Project = item.Project ?? string.Empty
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read the Visual Studio Error List.");
                return new GetErrorListResponse { Success = false, ErrorMessage = "Failed to read the Visual Studio Error List." };
            }

            return new GetErrorListResponse { Success = true, Diagnostics = diagnostics };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VS service operation 'GetErrorList' failed.");
            throw;
        }
    }

    public async Task OpenGitChangesAsync()
    {
        _logger.LogInformation("Running VS service operation 'OpenGitChanges'.");

        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        var dte = await GetDteAsync();
        var commands = new[]
        {
            "View.GitChanges",
            "Git.ViewGitChanges",
            "GitHub.OpenGitChanges"
        };

        foreach (var command in commands)
        {
            try
            {
                dte.ExecuteCommand(command);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, $"Failed to execute Visual Studio command '{command}' while opening Git Changes.");
            }
        }

        throw new InvalidOperationException("Unable to open the Git Changes window in Visual Studio.");
    }

    public Task<ProposeTextEditResponse> ProposeTextEditAsync(
        string requestId,
        string filePath,
        string originalText,
        string proposedText)
    {
        _logger.LogInformation($"Generating proposed diff for '{filePath}' [RequestId={requestId}].");

        var diff = GenerateUnifiedDiff(filePath, originalText, proposedText);
        if (!string.IsNullOrEmpty(diff))
        {
            var rangeEdits = RangeEditBuilder.BuildAll(originalText, proposedText);
            var rangeEdit = rangeEdits.Count == 1 ? rangeEdits[0] : null;
            var proposal = _approvalWorkflowService.CreateProposal(
                requestId,
                filePath,
                diff,
                rangeEdit,
                rangeEdits.Count > 1 ? rangeEdits : null);
            _logger.LogInformation($"Created edit proposal [RequestId={proposal.RequestId}] [ProposalId={proposal.ProposalId}] for '{proposal.FilePath}'.");
            _logger.LogInformation($"Proposal pending approval [RequestId={proposal.RequestId}] [ProposalId={proposal.ProposalId}] for '{proposal.FilePath}'.");
            _logToolWindowPresenter.ShowApprovalPrompt(
                BuildProposalDescription(proposal),
                proposal.RangeEdit?.OriginalSegment,
                proposal.RangeEdit?.UpdatedSegment,
                BuildReviewedChanges(proposal),
                () => _threadHelper.Run(() => ApproveAndApplyAsync(proposal.ProposalId)),
                () => RejectProposal(proposal.ProposalId),
                BuildIncludedFiles(proposal),
                proposal.RequestId);
        }
        else
        {
            _logger.LogInformation($"No edit proposal created because the proposed text matches the original text [RequestId={requestId}] for '{filePath}'.");
        }

        return Task.FromResult(new ProposeTextEditResponse
        {
            RequestId = requestId,
            Success = true,
            FilePath = filePath,
            Diff = diff
        });
    }

    public Task<ProposeTextEditResponse> ProposeTextEditsAsync(string requestId, IReadOnlyList<ProposalFileEditRequest> fileEdits)
    {
        if (fileEdits == null)
            throw new ArgumentNullException(nameof(fileEdits));

        if (fileEdits.Count == 1)
            return ProposeTextEditAsync(requestId, fileEdits[0].FilePath, fileEdits[0].OriginalText, fileEdits[0].ProposedText);

        _logger.LogInformation($"Generating proposed diff for {fileEdits.Count} files [RequestId={requestId}].");

        var combinedDiff = new StringBuilder();
        var proposalFileEdits = new List<ProposedFileEdit>(fileEdits.Count);
        var hasMeaningfulChange = false;

        foreach (var fileEdit in fileEdits)
        {
            var hasFileChange = !string.Equals(fileEdit.OriginalText, fileEdit.ProposedText, StringComparison.Ordinal);
            var diff = BuildProposalDiff(fileEdit.FilePath, fileEdit.OriginalText, fileEdit.ProposedText, includeNoOp: true);
            hasMeaningfulChange |= hasFileChange;

            if (combinedDiff.Length > 0)
                combinedDiff.AppendLine();

            combinedDiff.Append(diff);

            var rangeEdits = RangeEditBuilder.BuildAll(fileEdit.OriginalText, fileEdit.ProposedText);
            proposalFileEdits.Add(new ProposedFileEdit
            {
                FilePath = fileEdit.FilePath,
                Diff = diff,
                RangeEdit = rangeEdits.Count == 1 ? rangeEdits[0] : null,
                RangeEdits = rangeEdits.Count > 1 ? rangeEdits.ToList() : null
            });
        }

        if (proposalFileEdits.Count > 0 && hasMeaningfulChange)
        {
            var proposal = _approvalWorkflowService.CreateProposal(requestId, proposalFileEdits);
            var proposalTarget = DescribeProposalTarget(proposal);
            _logger.LogInformation($"Created edit proposal [RequestId={proposal.RequestId}] [ProposalId={proposal.ProposalId}] for {proposalTarget}.");
            _logger.LogInformation($"Proposal pending approval [RequestId={proposal.RequestId}] [ProposalId={proposal.ProposalId}] for {proposalTarget}.");
            _logToolWindowPresenter.ShowApprovalPrompt(
                BuildProposalDescription(proposal),
                null,
                null,
                Array.Empty<ProposalReviewedChange>(),
                () => _threadHelper.Run(() => ApproveAndApplyAsync(proposal.ProposalId)),
                () => RejectProposal(proposal.ProposalId),
                BuildIncludedFiles(proposal),
                proposal.RequestId);
        }
        else
        {
            _logger.LogInformation($"No multi-file edit proposal created because every selected file already matches the proposed text [RequestId={requestId}].");
        }

        return Task.FromResult(new ProposeTextEditResponse
        {
            RequestId = requestId,
            Success = true,
            FilePath = proposalFileEdits.FirstOrDefault()?.FilePath ?? string.Empty,
            Diff = combinedDiff.ToString()
        });
    }

    private async Task<DTE2> GetDteAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        var dte = await _package.GetServiceAsync<DTE2>(typeof(DTE));
        if (dte == null)
            throw new InvalidOperationException("DTE service unavailable.");

        return dte;
    }

    private static Document? GetActiveDocumentOnUIThread(DTE2 dte)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        return dte.ActiveDocument;
    }

    private static Solution? GetSolution(DTE2 dte)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        return dte.Solution;
    }

    private static IEnumerable<Project> EnumerateSolutionProjects(Solution solution)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        foreach (Project project in solution.Projects)
        {
            if (project.Kind == EnvDTE.Constants.vsProjectKindSolutionItems)
                continue;

            yield return project;
        }
    }

    private static ProjectInfo CreateProjectInfo(Project project)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        return new ProjectInfo
        {
            Name = project.Name,
            FullPath = project.FullName,
            TargetFramework = GetTargetFramework(project)
        };
    }

    private static string GetDocumentText(TextDocument? textDocument)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        return textDocument?.StartPoint?.CreateEditPoint().GetText(textDocument.EndPoint) ?? string.Empty;
    }

    private static string GetSelectedText(TextDocument? textDocument)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var selection = textDocument?.Selection as TextSelection;
        return selection?.Text ?? string.Empty;
    }

    private static string GetTargetFramework(Project project)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        try
        {
            return project.Properties?.Item("TargetFrameworkMoniker")?.Value?.ToString() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string ErrorLevelToSeverity(vsBuildErrorLevel level) => level switch
    {
        vsBuildErrorLevel.vsBuildErrorLevelHigh => "Error",
        vsBuildErrorLevel.vsBuildErrorLevelMedium => "Warning",
        _ => "Message"
    };

    private static string BuildProposalDescription(EditProposal proposal)
    {
        if (proposal.FileEdits != null && proposal.FileEdits.Count > 1)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"Pending proposal for {proposal.FileEdits.Count} files");
            foreach (var fileEdit in proposal.FileEdits)
            {
                builder.AppendLine($"File: '{fileEdit.FilePath}'");
                builder.AppendLine(fileEdit.Diff);
            }

            return builder.ToString().TrimEnd();
        }

        return $"Pending proposal for '{proposal.FilePath}'{Environment.NewLine}{proposal.Diff}";
    }

    private static IReadOnlyList<ProposalReviewedChange> BuildReviewedChanges(EditProposal proposal)
    {
        if (proposal.RangeEdits == null || proposal.RangeEdits.Count == 0)
            return Array.Empty<ProposalReviewedChange>();

        var changes = new List<ProposalReviewedChange>(proposal.RangeEdits.Count);
        for (var index = 0; index < proposal.RangeEdits.Count; index++)
        {
            var rangeEdit = proposal.RangeEdits[index];
            changes.Add(new ProposalReviewedChange
            {
                SequenceNumber = index + 1,
                OriginalSegment = rangeEdit.OriginalSegment ?? string.Empty,
                UpdatedSegment = rangeEdit.UpdatedSegment ?? string.Empty
            });
        }

        return changes;
    }

    private static IReadOnlyList<string> BuildIncludedFiles(EditProposal proposal)
    {
        if (proposal.FileEdits != null && proposal.FileEdits.Count > 0)
            return proposal.FileEdits.Select(fileEdit => fileEdit.FilePath).ToArray();

        return string.IsNullOrWhiteSpace(proposal.FilePath)
            ? Array.Empty<string>()
            : new[] { proposal.FilePath };
    }

    private async Task ApproveAndApplyAsync(string proposalId)
    {
        var proposal = _approvalWorkflowService.Approve(proposalId);
        var proposalTarget = DescribeProposalTarget(proposal);
        _logger.LogInformation($"Proposal approved [RequestId={proposal.RequestId}] [ProposalId={proposal.ProposalId}] for {proposalTarget}.");

        try
        {
            var applyResult = await _editApplier.ApplyAsync(proposal);
            _approvalWorkflowService.MarkApplied(proposalId);
            if (applyResult == EditApplyResult.SkippedAlreadyMatchesApprovedUpdatedContent)
            {
                var message = ProposalOutcomeMessageBuilder.BuildSkipMessage(proposal);
                _logToolWindowPresenter.CompleteProposalCycle(message, proposal.RequestId);
                _logger.LogInformation($"{message} [RequestId={proposal.RequestId}] [ProposalId={proposal.ProposalId}]");
            }
            else
            {
                var message = ProposalOutcomeMessageBuilder.BuildSuccessMessage(proposal);
                _logToolWindowPresenter.CompleteProposalCycle(message, proposal.RequestId);
                _logger.LogInformation($"{message} [RequestId={proposal.RequestId}] [ProposalId={proposal.ProposalId}]");
            }
        }
        catch (TargetDocumentDriftException ex)
        {
            _approvalWorkflowService.MarkFailed(proposalId);
            var message = ProposalOutcomeMessageBuilder.BuildDriftFailureMessage(proposal);
            _logToolWindowPresenter.CompleteProposalCycle(message, proposal.RequestId);
            _logger.LogWarning(ex, $"{message} [RequestId={proposal.RequestId}] [ProposalId={proposal.ProposalId}]");
        }
        catch (AmbiguousEditTargetException ex)
        {
            _approvalWorkflowService.MarkFailed(proposalId);
            var message = ProposalOutcomeMessageBuilder.BuildAmbiguityFailureMessage(proposal);
            _logToolWindowPresenter.CompleteProposalCycle(message, proposal.RequestId);
            _logger.LogWarning(ex, $"{message} [RequestId={proposal.RequestId}] [ProposalId={proposal.ProposalId}]");
        }
        catch (Exception ex)
        {
            _approvalWorkflowService.MarkFailed(proposalId);
            var message = ProposalOutcomeMessageBuilder.BuildGenericFailureMessage(proposal);
            _logToolWindowPresenter.CompleteProposalCycle(message);
            _logger.LogError(ex, $"{message} [RequestId={proposal.RequestId}] [ProposalId={proposal.ProposalId}]");
        }
    }

    private void RejectProposal(string proposalId)
    {
        var proposal = _approvalWorkflowService.Reject(proposalId);
        var message = ProposalOutcomeMessageBuilder.BuildRejectedMessage(proposal);
        _logToolWindowPresenter.CompleteProposalCycle(message, proposal.RequestId);
        _logger.LogInformation($"{message} [RequestId={proposal.RequestId}] [ProposalId={proposal.ProposalId}]");
    }

    private static string DescribeProposalTarget(EditProposal proposal)
    {
        return proposal.FileEdits != null && proposal.FileEdits.Count > 1
            ? $"{proposal.FileEdits.Count} files"
            : $"'{proposal.FilePath}'";
    }

    private static string GenerateUnifiedDiff(string filePath, string original, string proposed)
    {
        return BuildProposalDiff(filePath, original, proposed, includeNoOp: false);
    }

    private static string BuildProposalDiff(string filePath, string original, string proposed, bool includeNoOp)
    {
        var originalLines = original.Split('\n');
        var proposedLines = proposed.Split('\n');

        if (original == proposed)
        {
            if (!includeNoOp)
                return string.Empty;

            var noOpBuilder = new StringBuilder();
            noOpBuilder.AppendLine($"--- a/{filePath}");
            noOpBuilder.AppendLine($"+++ b/{filePath}");
            foreach (var originalLine in originalLines)
                noOpBuilder.AppendLine($" {originalLine}");

            return noOpBuilder.ToString();
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"--- a/{filePath}");
        sb.AppendLine($"+++ b/{filePath}");

        int i = 0;
        int j = 0;
        while (i < originalLines.Length || j < proposedLines.Length)
        {
            if (i < originalLines.Length && j < proposedLines.Length && originalLines[i] == proposedLines[j])
            {
                sb.AppendLine($" {originalLines[i]}");
                i++;
                j++;
            }
            else if (i < originalLines.Length)
            {
                sb.AppendLine($"-{originalLines[i]}");
                i++;
            }
            else
            {
                sb.AppendLine($"+{proposedLines[j]}");
                j++;
            }
        }

        return sb.ToString();
    }
}
