using System;
using VsMcpBridge.McpServer.ChatEngine;
using VsMcpBridge.Shared.Models;
using Xunit;

namespace VsMcpBridge.Shared.Tests;

public sealed class ChatEngineResultAdapterTests
{
    [Fact]
    public void ToProposalReady_maps_success_result()
    {
        var result = new ChatEngineChatResult
        {
            Success = true,
            Content = "suggested text",
            Error = null,
            ErrorCode = null,
            RequestId = "req-1"
        };

        var before = DateTimeOffset.UtcNow;
        var adapted = ChatEngineResultAdapter.ToProposalReady(result, "rewrite_with_target", "AI rewrite suggestion");
        var after = DateTimeOffset.UtcNow;

        Assert.True(adapted.IsSuccess);
        Assert.Equal("rewrite_with_target", adapted.ToolName);
        Assert.Equal("ChatEngine suggestion prepared.", adapted.Summary);
        Assert.Equal("AI rewrite suggestion", adapted.ShortDescription);
        Assert.Equal("suggested text", adapted.SuggestedText);
        Assert.Null(adapted.ErrorCode);
        Assert.InRange(adapted.Timestamp, before, after);
    }

    [Fact]
    public void ToProposalReady_maps_failure_result()
    {
        var result = new ChatEngineChatResult
        {
            Success = false,
            Content = null,
            Error = "Error: chat_engine_chat failed.",
            ErrorCode = "ProviderFailure",
            RequestId = "req-2"
        };

        var before = DateTimeOffset.UtcNow;
        var adapted = ChatEngineResultAdapter.ToProposalReady(result, "suggest_fixes_with_target", "AI fix suggestion");
        var after = DateTimeOffset.UtcNow;

        Assert.False(adapted.IsSuccess);
        Assert.Equal("suggest_fixes_with_target", adapted.ToolName);
        Assert.Equal("Error: chat_engine_chat failed.", adapted.Summary);
        Assert.Equal("AI fix suggestion", adapted.ShortDescription);
        Assert.Null(adapted.SuggestedText);
        Assert.Equal("ProviderFailure", adapted.ErrorCode);
        Assert.InRange(adapted.Timestamp, before, after);
    }

    [Fact]
    public void ToProposalReady_does_not_modify_original_result()
    {
        var result = new ChatEngineChatResult
        {
            Success = true,
            Content = "original",
            Error = null,
            ErrorCode = null,
            RequestId = "req-3"
        };

        _ = ChatEngineResultAdapter.ToProposalReady(result, "chat", "AI chat suggestion");

        Assert.True(result.Success);
        Assert.Equal("original", result.Content);
        Assert.Null(result.Error);
        Assert.Null(result.ErrorCode);
        Assert.Equal("req-3", result.RequestId);
    }
}
