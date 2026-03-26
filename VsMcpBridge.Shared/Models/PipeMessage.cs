namespace VsMcpBridge.Shared.Models;

/// <summary>
/// Named pipe message envelope used for communication between McpServer and VSIX.
/// The <see cref="Command"/> field identifies the operation and the
/// <see cref="Payload"/> carries the JSON-serialised request or response.
/// </summary>
public sealed class PipeMessage
{
    public string Command { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
}

/// <summary>Well-known command names used in <see cref="PipeMessage.Command"/>.</summary>
public static class PipeCommands
{
    public const string GetActiveDocument = "vs_get_active_document";
    public const string GetSelectedText = "vs_get_selected_text";
    public const string ListSolutionProjects = "vs_list_solution_projects";
    public const string GetErrorList = "vs_get_error_list";
    public const string ProposeTextEdit = "vs_propose_text_edit";
}
