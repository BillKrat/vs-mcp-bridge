using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using VsMcpBridge.Shared.Models;
using Task = System.Threading.Tasks.Task;

namespace VsMcpBridge.Vsix.Services;

/// <summary>
/// Wraps Visual Studio's DTE and error-list APIs to provide strongly-typed
/// responses for each supported MCP tool operation.
/// </summary>
public sealed class VsService
{
    private readonly AsyncPackage _package;

    public VsService(AsyncPackage package) => _package = package;

    private async Task<DTE2> GetDteAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        var dte = await _package.GetServiceAsync(typeof(DTE)) as DTE2
                  ?? throw new InvalidOperationException("DTE service unavailable.");
        return dte;
    }

    public async Task<GetActiveDocumentResponse> GetActiveDocumentAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        var dte = await GetDteAsync();
        var doc = dte.ActiveDocument;
        if (doc == null)
            return new GetActiveDocumentResponse { Success = false, ErrorMessage = "No active document." };

        var textDoc = doc.Object("TextDocument") as TextDocument;
        var content = textDoc?.StartPoint?.CreateEditPoint()
                               .GetText(textDoc.EndPoint) ?? string.Empty;

        return new GetActiveDocumentResponse
        {
            Success = true,
            FilePath = doc.FullName,
            Language = doc.Language,
            Content = content
        };
    }

    public async Task<GetSelectedTextResponse> GetSelectedTextAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        var dte = await GetDteAsync();
        var doc = dte.ActiveDocument;
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

    public async Task<ListSolutionProjectsResponse> ListSolutionProjectsAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        var dte = await GetDteAsync();
        var solution = dte.Solution;
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

    public async Task<GetErrorListResponse> GetErrorListAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        var errorList = await _package.GetServiceAsync(typeof(SVsErrorList)) as IVsTaskList2;
        var diagnostics = new List<DiagnosticItem>();

        if (errorList != null)
        {
            errorList.EnumTaskItems(out var enumItems);
            var items = new IVsTaskItem[1];

            while (enumItems.Next(1, items, out var fetched) == 0 && fetched == 1)
            {
                var item = items[0];
                item.get_Text(out var text);
                item.get_Document(out var file);
                item.get_Line(out var line);
                item.get_Column(out var col);
                item.get_Category(out var category);

                diagnostics.Add(new DiagnosticItem
                {
                    Severity = CategoryToSeverity(category),
                    Description = text ?? string.Empty,
                    File = file ?? string.Empty,
                    Line = line + 1,
                    Column = col + 1
                });
            }
        }

        return new GetErrorListResponse { Success = true, Diagnostics = diagnostics };
    }

    private static string CategoryToSeverity(VSTASKCATEGORY category) => category switch
    {
        VSTASKCATEGORY.CAT_BUILDCOMPILE => "Error",
        VSTASKCATEGORY.CAT_CODESENSE => "Warning",
        _ => "Message"
    };

    public Task<ProposeTextEditResponse> ProposeTextEditAsync(
        string filePath,
        string originalText,
        string proposedText)
    {
        var diff = GenerateUnifiedDiff(filePath, originalText, proposedText);
        return Task.FromResult(new ProposeTextEditResponse
        {
            Success = true,
            FilePath = filePath,
            Diff = diff
        });
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

        int i = 0, j = 0;
        while (i < originalLines.Length || j < proposedLines.Length)
        {
            if (i < originalLines.Length && j < proposedLines.Length &&
                originalLines[i] == proposedLines[j])
            {
                sb.AppendLine($" {originalLines[i]}");
                i++; j++;
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
