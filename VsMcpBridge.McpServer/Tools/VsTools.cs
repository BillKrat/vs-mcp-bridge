using ModelContextProtocol.Server;
using System.ComponentModel;
using VsMcpBridge.Shared.Models;
using VsMcpBridge.McpServer.Pipe;
using VsMcpBridge.Shared.Interfaces;

namespace VsMcpBridge.McpServer.Tools;

/// <summary>
/// MCP tool definitions that forward requests to the VSIX via named pipe.
/// All tools are read-only or diff-producing; they never write files directly.
/// </summary>
[McpServerToolType]
public sealed class VsTools
{
    private readonly IPipeClient _pipe;

    public VsTools(IPipeClient pipe) => _pipe = pipe;

    [McpServerTool(Name = "vs_get_active_document")]
    [Description("Returns the file path, language, and full text of the document currently active in Visual Studio.")]
    public async Task<string> GetActiveDocumentAsync(CancellationToken ct)
    {
        var response = await _pipe.GetActiveDocumentAsync(ct);
        if (!response.Success)
            return $"Error: {response.ErrorMessage}";

        return $"File: {response.FilePath}\nLanguage: {response.Language}\n\n{response.Content}";
    }

    [McpServerTool(Name = "vs_get_selected_text")]
    [Description("Returns the text currently selected in the active Visual Studio editor.")]
    public async Task<string> GetSelectedTextAsync(CancellationToken ct)
    {
        var response = await _pipe.GetSelectedTextAsync(ct);
        if (!response.Success)
            return $"Error: {response.ErrorMessage}";

        return string.IsNullOrEmpty(response.SelectedText)
            ? "(no selection)"
            : $"File: {response.FilePath}\nSelected:\n{response.SelectedText}";
    }

    [McpServerTool(Name = "vs_list_solution_projects")]
    [Description("Lists all projects in the currently open Visual Studio solution.")]
    public async Task<string> ListSolutionProjectsAsync(CancellationToken ct)
    {
        var response = await _pipe.ListSolutionProjectsAsync(ct);
        if (!response.Success)
            return $"Error: {response.ErrorMessage}";

        if (response.Projects.Count == 0)
            return "(no projects found)";

        var lines = response.Projects.Select(p =>
            $"- {p.Name} ({p.TargetFramework})\n  {p.FullPath}");

        return string.Join("\n", lines);
    }

    [McpServerTool(Name = "vs_get_error_list")]
    [Description("Returns the current errors, warnings, and messages from the Visual Studio Error List.")]
    public async Task<string> GetErrorListAsync(CancellationToken ct)
    {
        var response = await _pipe.GetErrorListAsync(ct);
        if (!response.Success)
            return $"Error: {response.ErrorMessage}";

        if (response.Diagnostics.Count == 0)
            return "(no diagnostics)";

        var lines = response.Diagnostics.Select(d =>
            $"[{d.Severity}] {d.Code}: {d.Description}\n  {d.File}({d.Line},{d.Column}) in {d.Project}");

        return string.Join("\n", lines);
    }

    [McpServerTool(Name = "vs_propose_text_edit")]
    [Description("Produces a unified diff showing proposed changes to a file. Does NOT write to disk; the user must approve changes via the VSIX UI.")]
    public async Task<string> ProposeTextEditAsync(
        [Description("Absolute path to the file to edit.")] string filePath,
        [Description("The original file content (before edits).")] string originalText,
        [Description("The proposed new file content (after edits).")] string proposedText,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return "Error: filePath must not be empty.";

        var response = await _pipe.ProposeTextEditAsync(filePath, originalText, proposedText, ct);
        if (!response.Success)
            return $"Error: {response.ErrorMessage}";

        return string.IsNullOrEmpty(response.Diff)
            ? "(no changes)"
            : $"Proposed diff for {response.FilePath}:\n\n{response.Diff}";
    }

    [McpServerTool(Name = "vs_propose_text_edits")]
    [Description("Produces a unified diff showing proposed changes across multiple files. Does NOT write to disk; the user must approve changes via the VSIX UI.")]
    public async Task<string> ProposeTextEditsAsync(
        [Description("The file edits to include in a single proposal.")] IReadOnlyList<ProposalFileEditRequest> fileEdits,
        CancellationToken ct)
    {
        if (fileEdits == null || fileEdits.Count == 0)
            return "Error: fileEdits must contain at least one file edit.";

        if (fileEdits.Any(fileEdit => fileEdit == null || string.IsNullOrWhiteSpace(fileEdit.FilePath)))
            return "Error: each file edit must include a non-empty filePath.";

        var response = await _pipe.ProposeTextEditsAsync(fileEdits, ct);
        if (!response.Success)
            return $"Error: {response.ErrorMessage}";

        return string.IsNullOrEmpty(response.Diff)
            ? "(no changes)"
            : $"Proposed diff for {fileEdits.Count} files:\n\n{response.Diff}";
    }
}
