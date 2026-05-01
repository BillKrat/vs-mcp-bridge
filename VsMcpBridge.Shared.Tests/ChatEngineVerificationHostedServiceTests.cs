using Adventures.ChatEngine.Abstractions;
using Adventures.ChatEngine.Events;
using Adventures.ChatEngine.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VsMcpBridge.McpServer.ChatEngine;
using VsMcpBridge.Shared.Loggers;
using Xunit;

namespace VsMcpBridge.Shared.Tests;

public sealed class ChatEngineVerificationHostedServiceTests
{
    [Fact]
    public async Task StartAsync_when_provider_is_missing_runs_verification()
    {
        var chatEngine = new RecordingChatEngine();
        var configuration = new ConfigurationBuilder().Build();
        var logger = new RecordingBridgeLogger();
        var service = new ChatEngineVerificationHostedService(chatEngine, configuration, logger);

        await service.StartAsync(CancellationToken.None);

        Assert.Equal(1, chatEngine.SendCallCount);
        Assert.Equal("ping", chatEngine.LastRequest?.Message);
        Assert.Contains(
            logger.InformationMessages,
            message => message.Contains("verification succeeded", System.StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task StartAsync_when_provider_is_fake_runs_verification()
    {
        var chatEngine = new RecordingChatEngine();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Adventures:ChatEngine:Provider"] = "Fake",
            })
            .Build();
        var logger = new RecordingBridgeLogger();
        var service = new ChatEngineVerificationHostedService(chatEngine, configuration, logger);

        await service.StartAsync(CancellationToken.None);

        Assert.Equal(1, chatEngine.SendCallCount);
        Assert.Equal("ping", chatEngine.LastRequest?.Message);
    }

    [Fact]
    public async Task StartAsync_when_provider_is_openai_skips_verification()
    {
        var chatEngine = new RecordingChatEngine();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Adventures:ChatEngine:Provider"] = "OpenAI",
            })
            .Build();
        var logger = new RecordingBridgeLogger();
        var service = new ChatEngineVerificationHostedService(chatEngine, configuration, logger);

        await service.StartAsync(CancellationToken.None);

        Assert.Equal(0, chatEngine.SendCallCount);
        Assert.Contains(
            logger.InformationMessages,
            message => message.Contains("skipped", System.StringComparison.OrdinalIgnoreCase) &&
                       message.Contains("OpenAI", System.StringComparison.OrdinalIgnoreCase));
    }

    private sealed class RecordingChatEngine : IChatEngine
    {
        public int SendCallCount { get; private set; }
        public ChatRequest? LastRequest { get; private set; }

        public Task<ChatResponse> SendAsync(
            ChatRequest request,
            CancellationToken cancellationToken,
            Action<ChatEvent>? onEvent = null)
        {
            SendCallCount++;
            LastRequest = request;
            return Task.FromResult(new ChatResponse("pong"));
        }

        public async IAsyncEnumerable<ChatEvent> StreamAsync(
            ChatRequest request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            LastRequest = request;
            yield return new ChatEvent(ChatEventType.RequestStarted);
            yield return new ChatEvent(ChatEventType.TokenGenerated, "pong");
            yield return new ChatEvent(ChatEventType.ResponseCompleted);
            await Task.CompletedTask;
        }
    }
}
