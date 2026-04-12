using EnvDTE;
using EnvDTE80;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
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
            var proposal = _approvalWorkflowService.CreateProposal(requestId, filePath, diff);
            _logger.LogInformation($"Created edit proposal [RequestId={proposal.RequestId}] [ProposalId={proposal.ProposalId}] for '{proposal.FilePath}'.");
            _logger.LogInformation($"Proposal pending approval [RequestId={proposal.RequestId}] [ProposalId={proposal.ProposalId}] for '{proposal.FilePath}'.");
            _logToolWindowPresenter.ShowApprovalPrompt(
                BuildProposalDescription(proposal),
                () => _threadHelper.Run(() => ApproveAndApplyAsync(proposal.ProposalId)),
                () => RejectProposal(proposal.ProposalId));
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
        return $"Pending proposal for '{proposal.FilePath}'{Environment.NewLine}{proposal.Diff}";
    }

    private async Task ApproveAndApplyAsync(string proposalId)
    {
        var proposal = _approvalWorkflowService.Approve(proposalId);
        _logger.LogInformation($"Proposal approved [RequestId={proposal.RequestId}] [ProposalId={proposal.ProposalId}] for '{proposal.FilePath}'.");

        try
        {
            var applyResult = await _editApplier.ApplyAsync(proposal);
            _approvalWorkflowService.MarkApplied(proposalId);
            if (applyResult == EditApplyResult.SkippedAlreadyMatchesApprovedUpdatedContent)
            {
                _logger.LogInformation($"Apply skipped because target already matches approved updated content [RequestId={proposal.RequestId}] [ProposalId={proposal.ProposalId}] for '{proposal.FilePath}'.");
            }
            else
            {
                _logger.LogInformation($"Apply succeeded [RequestId={proposal.RequestId}] [ProposalId={proposal.ProposalId}] for '{proposal.FilePath}'.");
            }
        }
        catch (TargetDocumentDriftException ex)
        {
            _approvalWorkflowService.MarkFailed(proposalId);
            _logToolWindowPresenter.ShowStatusMessage($"Apply failed for '{proposal.FilePath}': the document changed after proposal creation.");
            _logger.LogWarning(ex, $"Apply failed because target no longer matches approved original content [RequestId={proposal.RequestId}] [ProposalId={proposal.ProposalId}] for '{proposal.FilePath}'.");
        }
        catch (Exception ex)
        {
            _approvalWorkflowService.MarkFailed(proposalId);
            _logToolWindowPresenter.ShowStatusMessage($"Apply failed for '{proposal.FilePath}'. Review the bridge log for details.");
            _logger.LogError(ex, $"Apply failed [RequestId={proposal.RequestId}] [ProposalId={proposal.ProposalId}] for '{proposal.FilePath}'.");
        }
    }

    private void RejectProposal(string proposalId)
    {
        var proposal = _approvalWorkflowService.Reject(proposalId);
        _logger.LogInformation($"Proposal rejected [RequestId={proposal.RequestId}] [ProposalId={proposal.ProposalId}] for '{proposal.FilePath}'.");
    }

    private static string GenerateUnifiedDiff(string filePath, string original, string proposed)
    {
        var originalLines = original.Split('\n');
        var proposedLines = proposed.Split('\n');

        if (original == proposed)
            return string.Empty;

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
