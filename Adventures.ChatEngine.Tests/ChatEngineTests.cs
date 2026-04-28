using Adventures.ChatEngine.Abstractions;
using Adventures.ChatEngine.Events;
using Adventures.ChatEngine.Extensions;
using Adventures.ChatEngine.Models;
using Adventures.ChatEngine.OpenAI.Configuration;
using Adventures.ChatEngine.OpenAI.Extensions;
using Adventures.ChatEngine.OpenAI.Services;
using Adventures.ChatEngine.Tests.Fakes;
using ChatEngineService = Adventures.ChatEngine.Services.ChatEngine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http;
using System.Text;

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
            second =>
            {
                Assert.Equal(ChatEventType.TokenGenerated, second.Type);
                Assert.Equal("pong", second.Content);
            },
            third => Assert.Equal(ChatEventType.ResponseCompleted, third.Type));
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
            second =>
            {
                Assert.Equal(ChatEventType.TokenGenerated, second.Type);
                Assert.Equal("pong", second.Content);
            },
            third => Assert.Equal(ChatEventType.ResponseCompleted, third.Type));
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
            fourth =>
            {
                Assert.Equal(ChatEventType.TokenGenerated, fourth.Type);
                Assert.Equal("pong", fourth.Content);
            },
            fifth => Assert.Equal(ChatEventType.ResponseCompleted, fifth.Type));

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
    public async Task StreamAsync_WhenProviderStreamsChunks_YieldsTokenGeneratedEventsAndResponseCompleted()
    {
        var provider = new FakePingPongProvider();
        provider.SetStreamChunks("po", "ng");
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
            second =>
            {
                Assert.Equal(ChatEventType.TokenGenerated, second.Type);
                Assert.Equal("po", second.Content);
            },
            third =>
            {
                Assert.Equal(ChatEventType.TokenGenerated, third.Type);
                Assert.Equal("ng", third.Content);
            },
            fourth => Assert.Equal(ChatEventType.ResponseCompleted, fourth.Type));
    }

    [Fact]
    public async Task SendAsync_WhenProviderStreamsChunks_ReturnsAggregatedResponse()
    {
        var provider = new FakePingPongProvider();
        provider.SetStreamChunks("po", "ng");
        var engine = new ChatEngineService(provider, NullLogger<ChatEngineService>.Instance, CreateConfiguration());

        ChatResponse response = await engine.SendAsync(
            new ChatRequest("ping"),
            CancellationToken.None);

        Assert.Equal("pong", response.Message);
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

    [Fact]
    public async Task OpenAiProvider_WhenUseRealApiTrue_ReturnsHttpResponseContent()
    {
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                data: {"choices":[{"delta":{"content":"hello "}}]}
                data: {"choices":[{"delta":{"content":"from fake openai"}}]}
                data: [DONE]

                """,
                Encoding.UTF8,
                "text/event-stream"),
        });
        using ServiceProvider serviceProvider = CreateOpenAiServiceProvider(
            new Dictionary<string, string?>
            {
                ["Adventures:ChatEngine:OpenAI:ApiKey"] = "stub-key",
                ["Adventures:ChatEngine:OpenAI:Model"] = "stub-model",
                ["Adventures:ChatEngine:OpenAI:UseRealApi"] = "true",
            },
            handler);

        IChatEngine? engine = serviceProvider.GetService<IChatEngine>();

        ChatResponse response = await engine!.SendAsync(
            new ChatRequest("ping"),
            CancellationToken.None);

        Assert.Equal("hello from fake openai", response.Message);
    }

    [Fact]
    public async Task OpenAiProvider_WhenUseRealApiTrue_AndHttpFails_ThrowsSafeError()
    {
        const string apiKey = "stub-key";
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("unauthorized", Encoding.UTF8, "text/plain"),
        });
        using ServiceProvider serviceProvider = CreateOpenAiServiceProvider(
            new Dictionary<string, string?>
            {
                ["Adventures:ChatEngine:OpenAI:ApiKey"] = apiKey,
                ["Adventures:ChatEngine:OpenAI:Model"] = "stub-model",
                ["Adventures:ChatEngine:OpenAI:UseRealApi"] = "true",
            },
            handler);

        IChatEngine? engine = serviceProvider.GetService<IChatEngine>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            engine!.SendAsync(new ChatRequest("ping"), CancellationToken.None));

        Assert.Contains("401", exception.Message);
        Assert.DoesNotContain(apiKey, exception.Message);
    }

    [Fact]
    public async Task OpenAiProvider_WhenUseRealApiTrue_AndContentMissing_ThrowsClearError()
    {
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                data: {"choices":[]}
                data: [DONE]

                """,
                Encoding.UTF8,
                "text/event-stream"),
        });
        using ServiceProvider serviceProvider = CreateOpenAiServiceProvider(
            new Dictionary<string, string?>
            {
                ["Adventures:ChatEngine:OpenAI:ApiKey"] = "stub-key",
                ["Adventures:ChatEngine:OpenAI:Model"] = "stub-model",
                ["Adventures:ChatEngine:OpenAI:UseRealApi"] = "true",
            },
            handler);

        IChatEngine? engine = serviceProvider.GetService<IChatEngine>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            engine!.SendAsync(new ChatRequest("ping"), CancellationToken.None));

        Assert.Equal("OpenAI streaming response did not contain choices[0].delta.content.", exception.Message);
    }

    [Fact]
    public async Task OpenAiProvider_StreamAsync_ParsesSseAndEmitsChunks()
    {
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                data: {"choices":[{"delta":{"content":"hel"}}]}
                data: {"choices":[{"delta":{"content":"lo"}}]}
                data: [DONE]

                """,
                Encoding.UTF8,
                "text/event-stream"),
        });
        using ServiceProvider serviceProvider = CreateOpenAiServiceProvider(
            new Dictionary<string, string?>
            {
                ["Adventures:ChatEngine:OpenAI:ApiKey"] = "stub-key",
                ["Adventures:ChatEngine:OpenAI:Model"] = "stub-model",
                ["Adventures:ChatEngine:OpenAI:UseRealApi"] = "true",
            },
            handler);

        IAiChatProvider provider = serviceProvider.GetRequiredService<IAiChatProvider>();
        var chunks = new List<string>();

        await foreach (ChatResponse chunk in provider.StreamAsync(
            new ChatRequest("ping"),
            CancellationToken.None))
        {
            chunks.Add(chunk.Message);
        }

        Assert.Collection(
            chunks,
            first => Assert.Equal("hel", first),
            second => Assert.Equal("lo", second));
    }

    private static IConfiguration CreateConfiguration(IDictionary<string, string?>? values = null)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private static ServiceProvider CreateOpenAiServiceProvider(
        IDictionary<string, string?> values,
        HttpMessageHandler? handler = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(CreateConfiguration(values));
        services.AddSingleton<ILogger<ChatEngineService>>(NullLogger<ChatEngineService>.Instance);
        services.AddSingleton<ILogger<OpenAiChatProvider>>(NullLogger<OpenAiChatProvider>.Instance);
        services.AddChatEngine();
        services.AddOpenAiProvider(CreateConfiguration(values));

        if (handler is not null)
        {
            services
                .AddHttpClient<OpenAiChatProvider>()
                .ConfigurePrimaryHttpMessageHandler(() => handler);
        }

        return services.BuildServiceProvider();
    }

    [Fact]
    public void OpenAiProviderOptions_AreBoundFromConfiguration()
    {
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Adventures:ChatEngine:OpenAI:ApiKey"] = "stub-key",
            ["Adventures:ChatEngine:OpenAI:Model"] = "stub-model",
            ["Adventures:ChatEngine:OpenAI:UseRealApi"] = "true",
        });

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddSingleton<ILogger<OpenAiChatProvider>>(NullLogger<OpenAiChatProvider>.Instance);
        services.AddOpenAiProvider(configuration);

        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        IOptions<OpenAiChatProviderOptions>? options = serviceProvider.GetService<IOptions<OpenAiChatProviderOptions>>();

        Assert.NotNull(options);
        Assert.Equal("stub-key", options!.Value.ApiKey);
        Assert.Equal("stub-model", options.Value.Model);
        Assert.True(options.Value.UseRealApi);
    }

    private sealed class TestHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(handler(request));
        }
    }
}
