using VsMcpBridge.Shared.Models;

namespace VsMcpBridge.McpServer.ChatEngine;

internal static class ChatEngineResultAdapter
{
    public static ProposalReadyChatResult ToProposalReady(
        ChatEngineChatResult result,
        string toolName,
        string shortDescription)
    {
        return new ProposalReadyChatResult
        {
            ToolName = toolName,
            Summary = result.Success
                ? "ChatEngine suggestion prepared."
                : result.Error,
            ShortDescription = shortDescription,
            SuggestedText = result.Content,
            IsSuccess = result.Success,
            ErrorCode = result.ErrorCode,
            Timestamp = DateTimeOffset.UtcNow
        };
    }
}
