namespace VsMcpBridge.Shared.Models;

/// <summary>Base class for all requests sent from McpServer to VSIX via named pipe.</summary>
public abstract class VsRequestBase
{
    public string RequestId { get; set; } = string.Empty;
}

public sealed class GetActiveDocumentRequest : VsRequestBase { }

public sealed class GetSelectedTextRequest : VsRequestBase { }

public sealed class ListSolutionProjectsRequest : VsRequestBase { }

public sealed class GetErrorListRequest : VsRequestBase { }

public sealed class ProposeTextEditRequest : VsRequestBase
{
    public string FilePath { get; set; } = string.Empty;
    public string OriginalText { get; set; } = string.Empty;
    public string ProposedText { get; set; } = string.Empty;
}
