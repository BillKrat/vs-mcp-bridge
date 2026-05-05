namespace VsMcpBridge.McpServer.ChatEngine;

internal sealed class ProposalReadyChatResult
{
    public string? ToolName { get; set; }
    public string? Summary { get; set; }
    public string? ShortDescription { get; set; }
    public string? SuggestedText { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorCode { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
