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

    [Fact]
    public async Task StreamAsync_WhenAlreadyCancelled_YieldsRequestStartedThenCancelled()
    {
        var provider = new FakePingPongProvider();
        var engine = new ChatEngineService(provider, NullLogger<ChatEngineService>.Instance);
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        var events = new List<ChatEvent>();

        await foreach (ChatEvent chatEvent in engine.StreamAsync(
            new ChatRequest("ping"),
            cancellationTokenSource.Token))
        {
            events.Add(chatEvent);
        }

        Assert.Collection(
            events,
            first => Assert.Equal(ChatEventType.RequestStarted, first.Type),
            second => Assert.Equal(ChatEventType.Cancelled, second.Type));

        Assert.DoesNotContain(events, chatEvent => chatEvent.Type == ChatEventType.ResponseCompleted);
        Assert.False(provider.WasCalled);
    }

    [Fact]
    public async Task StreamAsync_WhenProviderFailsThenSucceeds_YieldsRetryEventsAndCompletes()
    {
        var provider = new FakePingPongProvider();
        provider.FailNext(new InvalidOperationException("transient"));
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
            second => Assert.Equal(ChatEventType.RetryScheduled, second.Type),
            third => Assert.Equal(ChatEventType.RetryAttempt, third.Type),
            fourth => Assert.Equal(ChatEventType.ResponseCompleted, fourth.Type));

        Assert.Equal(2, provider.CallCount);
    }

    [Fact]
    public async Task StreamAsync_WhenProviderAlwaysFails_YieldsRetryExhausted()
    {
        var provider = new FakePingPongProvider();
        provider.FailNext(new InvalidOperationException("failure-1"));
        provider.FailNext(new InvalidOperationException("failure-2"));
        provider.FailNext(new InvalidOperationException("failure-3"));
        var engine = new ChatEngineService(provider, NullLogger<ChatEngineService>.Instance);

        var events = new List<ChatEvent>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (ChatEvent chatEvent in engine.StreamAsync(
                new ChatRequest("ping"),
                CancellationToken.None))
            {
                events.Add(chatEvent);
            }
        });

        Assert.Equal("failure-3", exception.Message);
        Assert.Collection(
            events,
            first => Assert.Equal(ChatEventType.RequestStarted, first.Type),
            second => Assert.Equal(ChatEventType.RetryScheduled, second.Type),
            third => Assert.Equal(ChatEventType.RetryAttempt, third.Type),
            fourth => Assert.Equal(ChatEventType.RetryScheduled, fourth.Type),
            fifth => Assert.Equal(ChatEventType.RetryAttempt, fifth.Type),
            sixth => Assert.Equal(ChatEventType.RetryExhausted, sixth.Type));

        Assert.Equal(3, provider.CallCount);
    }
}
