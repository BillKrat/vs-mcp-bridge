using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VsMcpBridge.Shared.Models;
using VsMcpBridge.Vsix.Logging;
using Task = System.Threading.Tasks.Task;

namespace VsMcpBridge.Vsix.Services;

/// <summary>
/// Wraps Visual Studio's DTE and error-list APIs to provide strongly-typed
/// responses for each supported MCP tool operation.
/// </summary>
public sealed class VsService : IVsService
{
    private readonly AsyncPackage _package;
    private readonly IBridgeLogger _logger;

    public VsService(AsyncPackage package, IBridgeLogger logger)
    {
        _package = package;
        _logger = logger;
        _logger.LogVerbose("Bridge service startup complete.");
    }

    public async Task<GetActiveDocumentResponse> GetActiveDocumentAsync()
    {
        _logger.LogInformation("Running VS service operation 'GetActiveDocument'.");

        try
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var dte = await GetDteAsync();
            Document? doc = dte.ActiveDocument;
            if (doc == null)
                return new GetActiveDocumentResponse { Success = false, ErrorMessage = "No active document." };

            var textDoc = doc.Object("TextDocument") as TextDocument;
            var content = textDoc?.StartPoint?.CreateEditPoint().GetText(textDoc.EndPoint) ?? string.Empty;

            return new GetActiveDocumentResponse
            {
                Success = true,
                FilePath = doc.FullName,
                Language = doc.Language,
                Content = content
            };
        }
        catch (Exception ex)
        {
            _logger.LogError("VS service operation 'GetActiveDocument' failed.", ex);
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
            Document? doc = dte.ActiveDocument;
            if (doc == null)
                return new GetSelectedTextResponse { Success = false, ErrorMessage = "No active document." };

            var textDoc = doc.Object("TextDocument") as TextDocument;
            var selection = textDoc?.Selection as TextSelection;
            var text = selection?.Text ?? string.Empty;

            return new GetSelectedTextResponse
            {
                Success = true,
                FilePath = doc.FullName,
                SelectedText = text
            };
        }
        catch (Exception ex)
        {
            _logger.LogError("VS service operation 'GetSelectedText' failed.", ex);
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
            Solution? solution = dte.Solution;
            if (solution == null)
                return new ListSolutionProjectsResponse { Success = false, ErrorMessage = "No solution open." };

            var projects = new List<ProjectInfo>();
            foreach (Project project in solution.Projects)
            {
                if (project.Kind == EnvDTE.Constants.vsProjectKindSolutionItems)
                    continue;

                projects.Add(new ProjectInfo
                {
                    Name = project.Name,
                    FullPath = project.FullName,
                    TargetFramework = GetTargetFramework(project)
                });
            }

            return new ListSolutionProjectsResponse { Success = true, Projects = projects };
        }
        catch (Exception ex)
        {
            _logger.LogError("VS service operation 'ListSolutionProjects' failed.", ex);
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
                dynamic? errorItems = dte.ToolWindows?.ErrorList?.ErrorItems;
                if (errorItems != null)
                {
                    int count = errorItems.Count;
                    for (int index = 1; index <= count; index++)
                    {
                        dynamic item = errorItems.Item(index);
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
                _logger.LogError("Failed to read the Visual Studio Error List.", ex);
                return new GetErrorListResponse { Success = false, ErrorMessage = "Failed to read the Visual Studio Error List." };
            }

            return new GetErrorListResponse { Success = true, Diagnostics = diagnostics };
        }
        catch (Exception ex)
        {
            _logger.LogError("VS service operation 'GetErrorList' failed.", ex);
            throw;
        }
    }

    public Task<ProposeTextEditResponse> ProposeTextEditAsync(
        string filePath,
        string originalText,
        string proposedText)
    {
        _logger.LogInformation($"Generating proposed diff for '{filePath}'.");

        var diff = GenerateUnifiedDiff(filePath, originalText, proposedText);
        return Task.FromResult(new ProposeTextEditResponse
        {
            Success = true,
            FilePath = filePath,
            Diff = diff
        });
    }

    private async Task<DTE2> GetDteAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        var dte = await _package.GetServiceAsync(typeof(DTE)) as DTE2;
        if (dte == null)
            throw new InvalidOperationException("DTE service unavailable.");

        return dte;
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
