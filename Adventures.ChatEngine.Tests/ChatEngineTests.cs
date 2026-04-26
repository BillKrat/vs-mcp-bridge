using Adventures.ChatEngine.Events;
using Adventures.ChatEngine.Models;
using Adventures.ChatEngine.Tests.Fakes;
using ChatEngineService = Adventures.ChatEngine.Services.ChatEngine;

namespace Adventures.ChatEngine.Tests;

public sealed class ChatEngineTests
{
    [Fact]
    public async Task SendAsync_WhenPing_ReturnsPong_And_EmitsEvents()
    {
        var provider = new FakePingPongProvider();
        var engine = new ChatEngineService(provider);

        var events = new List<ChatEvent>();

        var response = await engine.SendAsync(
            new ChatRequest("ping"),
            CancellationToken.None,
            events.Add);

        Assert.Equal("pong", response.Message);

        Assert.Collection(
            events,
            first => Assert.Equal(ChatEventType.RequestStarted, first.Type),
            second => Assert.Equal(ChatEventType.ResponseCompleted, second.Type));
    }
}
