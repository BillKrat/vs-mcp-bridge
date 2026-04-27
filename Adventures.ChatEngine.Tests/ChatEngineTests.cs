using Adventures.ChatEngine.Abstractions;
using Adventures.ChatEngine.Events;
using Adventures.ChatEngine.Extensions;
using Adventures.ChatEngine.Models;
using Adventures.ChatEngine.OpenAI.Extensions;
using Adventures.ChatEngine.OpenAI.Services;
using Adventures.ChatEngine.Tests.Fakes;
using ChatEngineService = Adventures.ChatEngine.Services.ChatEngine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Adventures.ChatEngine.Tests;

public sealed class ChatEngineTests
{
    [Fact]
    public async Task SendAsync_WhenPing_ReturnsPong_And_EmitsEvents()
    {
        var provider = new FakePingPongProvider();
        var engine = new ChatEngineService(provider, NullLogger<ChatEngineService>.Instance, CreateConfiguration());

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
        var engine = new ChatEngineService(provider, NullLogger<ChatEngineService>.Instance, CreateConfiguration());

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
        var engine = new ChatEngineService(provider, NullLogger<ChatEngineService>.Instance, CreateConfiguration());
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
        var engine = new ChatEngineService(provider, NullLogger<ChatEngineService>.Instance, CreateConfiguration());

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
        var engine = new ChatEngineService(provider, NullLogger<ChatEngineService>.Instance, CreateConfiguration());

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

    [Fact]
    public async Task AddChatEngine_RegistersChatEngine()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IAiChatProvider, FakePingPongProvider>();
        services.AddSingleton<ILogger<ChatEngineService>>(NullLogger<ChatEngineService>.Instance);
        services.AddSingleton<IConfiguration>(CreateConfiguration());
        services.AddChatEngine();

        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        IChatEngine? engine = serviceProvider.GetService<IChatEngine>();

        Assert.NotNull(engine);

        ChatResponse response = await engine!.SendAsync(
            new ChatRequest("ping"),
            CancellationToken.None);

        Assert.Equal("pong", response.Message);
    }

    [Fact]
    public async Task StreamAsync_WhenRetryMaxAttemptsConfigured_UsesConfiguredValue()
    {
        var provider = new FakePingPongProvider();
        provider.FailNext(new InvalidOperationException("failure-1"));
        provider.FailNext(new InvalidOperationException("failure-2"));
        provider.FailNext(new InvalidOperationException("failure-3"));
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Adventures:ChatEngine:Retry:MaxAttempts"] = "2",
        });
        var engine = new ChatEngineService(provider, NullLogger<ChatEngineService>.Instance, configuration);

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

        Assert.Equal("failure-2", exception.Message);
        Assert.Equal(2, provider.CallCount);
        Assert.Contains(events, chatEvent => chatEvent.Type == ChatEventType.RetryExhausted);
    }

    [Fact]
    public async Task OpenAiProvider_WhenRegistered_EngineUsesProvider()
    {
        using ServiceProvider serviceProvider = CreateOpenAiServiceProvider(new Dictionary<string, string?>
        {
            ["Adventures:ChatEngine:OpenAI:ApiKey"] = "stub-key",
            ["Adventures:ChatEngine:OpenAI:Model"] = "stub-model",
        });

        IChatEngine? engine = serviceProvider.GetService<IChatEngine>();

        Assert.NotNull(engine);

        ChatResponse response = await engine!.SendAsync(
            new ChatRequest("ping"),
            CancellationToken.None);

        Assert.Equal("pong-from-openai", response.Message);
    }

    [Fact]
    public async Task OpenAiProvider_WhenApiKeyMissing_ThrowsClearConfigurationError()
    {
        using ServiceProvider serviceProvider = CreateOpenAiServiceProvider(new Dictionary<string, string?>
        {
            ["Adventures:ChatEngine:OpenAI:Model"] = "stub-model",
        });

        IChatEngine? engine = serviceProvider.GetService<IChatEngine>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            engine!.SendAsync(new ChatRequest("ping"), CancellationToken.None));

        Assert.Contains("Adventures:ChatEngine:OpenAI:ApiKey", exception.Message);
        Assert.DoesNotContain("stub-key", exception.Message);
    }

    [Fact]
    public async Task OpenAiProvider_WhenModelMissing_ThrowsClearConfigurationError()
    {
        using ServiceProvider serviceProvider = CreateOpenAiServiceProvider(new Dictionary<string, string?>
        {
            ["Adventures:ChatEngine:OpenAI:ApiKey"] = "stub-key",
        });

        IChatEngine? engine = serviceProvider.GetService<IChatEngine>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            engine!.SendAsync(new ChatRequest("ping"), CancellationToken.None));

        Assert.Contains("Adventures:ChatEngine:OpenAI:Model", exception.Message);
        Assert.DoesNotContain("stub-key", exception.Message);
    }

    private static IConfiguration CreateConfiguration(IDictionary<string, string?>? values = null)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private static ServiceProvider CreateOpenAiServiceProvider(IDictionary<string, string?> values)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(CreateConfiguration(values));
        services.AddSingleton<ILogger<ChatEngineService>>(NullLogger<ChatEngineService>.Instance);
        services.AddSingleton<ILogger<OpenAiChatProvider>>(NullLogger<OpenAiChatProvider>.Instance);
        services.AddChatEngine();
        services.AddOpenAiProvider();
        return services.BuildServiceProvider();
    }
}
