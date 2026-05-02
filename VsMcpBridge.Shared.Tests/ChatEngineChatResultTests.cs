using System.Text.Json;
using VsMcpBridge.Shared.Models;
using Xunit;

namespace VsMcpBridge.Shared.Tests;

public sealed class ChatEngineChatResultTests
{
    [Fact]
    public void ChatEngineChatResult_round_trips_through_json_serialization()
    {
        var result = new ChatEngineChatResult
        {
            Success = true,
            Content = "hello",
            Error = null,
            RequestId = "req-123"
        };

        var json = JsonSerializer.Serialize(result);
        var deserialized = JsonSerializer.Deserialize<ChatEngineChatResult>(json);

        Assert.NotNull(deserialized);
        Assert.True(deserialized.Success);
        Assert.Equal("hello", deserialized.Content);
        Assert.Null(deserialized.Error);
        Assert.Equal("req-123", deserialized.RequestId);
    }

    [Fact]
    public void ChatEngineChatResult_supports_success_shape()
    {
        var result = new ChatEngineChatResult
        {
            Success = true,
            Content = "pong",
            Error = null,
            RequestId = "req-success"
        };

        Assert.True(result.Success);
        Assert.Equal("pong", result.Content);
        Assert.Null(result.Error);
        Assert.Equal("req-success", result.RequestId);
    }

    [Fact]
    public void ChatEngineChatResult_supports_failure_shape()
    {
        var result = new ChatEngineChatResult
        {
            Success = false,
            Content = null,
            Error = "chat_engine_chat failed",
            RequestId = "req-failure"
        };

        Assert.False(result.Success);
        Assert.Null(result.Content);
        Assert.Equal("chat_engine_chat failed", result.Error);
        Assert.Equal("req-failure", result.RequestId);
    }
}
