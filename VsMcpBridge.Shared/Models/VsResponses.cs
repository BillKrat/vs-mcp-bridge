using System.Collections.Generic;

namespace VsMcpBridge.Shared.Models;

/// <summary>Base class for all responses sent from VSIX back to McpServer.</summary>
public abstract class VsResponseBase
{
    public string RequestId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed class GetActiveDocumentResponse : VsResponseBase
{
    public string FilePath { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
}

public sealed class GetSelectedTextResponse : VsResponseBase
{
    public string SelectedText { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
}

public sealed class ListSolutionProjectsResponse : VsResponseBase
{
    public List<ProjectInfo> Projects { get; set; } = new();
}

public sealed class ProjectInfo
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public string TargetFramework { get; set; } = string.Empty;
}

public sealed class GetErrorListResponse : VsResponseBase
{
    public List<DiagnosticItem> Diagnostics { get; set; } = new();
}

public sealed class DiagnosticItem
{
    public string Severity { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string File { get; set; } = string.Empty;
    public int Line { get; set; }
    public int Column { get; set; }
    public string Project { get; set; } = string.Empty;
}

public sealed class ProposeTextEditResponse : VsResponseBase
{
    public string Diff { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
}
