using Adventures.ChatEngine.Events;
using Adventures.ChatEngine.Models;
using Adventures.ChatEngine.Tests.Fakes;
using ChatEngineService = Adventures.ChatEngine.Services.ChatEngine;
using Microsoft.Extensions.Logging.Abstractions;

namespace Adventures.ChatEngine.Tests;

public sealed class ChatEngineTests
{
    [Fact]
    public async Task SendAsync_WhenPing_ReturnsPong_And_EmitsEvents()
    {
        var provider = new FakePingPongProvider();
        var engine = new ChatEngineService(provider, NullLogger<ChatEngineService>.Instance);

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

    [Fact]
    public async Task StreamAsync_WhenPing_YieldsEventsInOrder()
    {
        var provider = new FakePingPongProvider();
        var engine = new ChatEngineService(provider, NullLogger<ChatEngineService>.Instance);

        var events = new List<ChatEvent>();

        await foreach (ChatEvent chatEvent in engine.StreamAsync(
            new ChatRequest("ping"),
            CancellationToken.None))
        {
            events.Add(chatEvent);
        }

        Assert.Collection(
            events,
            first => Assert.Equal(ChatEventType.RequestStarted, first.Type),
            second => Assert.Equal(ChatEventType.ResponseCompleted, second.Type));
    }
}
