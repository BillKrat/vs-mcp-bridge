namespace VsMcpBridge.Shared.Models;

public sealed class ChatEngineChatResult
{
    public bool Success { get; set; }
    public string? Content { get; set; }
    public string? Error { get; set; }
    public string? RequestId { get; set; }
}
