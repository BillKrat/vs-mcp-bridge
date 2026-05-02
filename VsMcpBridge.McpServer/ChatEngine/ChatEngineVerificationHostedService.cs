using Adventures.ChatEngine.Abstractions;
using Adventures.ChatEngine.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace VsMcpBridge.McpServer.ChatEngine;

internal sealed class ChatEngineVerificationHostedService(IChatEngine chatEngine, IConfiguration configuration, ILogger logger) : IHostedService
{
    private const string ProviderConfigurationKey = "Adventures:ChatEngine:Provider";

    private readonly IChatEngine chatEngine = chatEngine;
    private readonly IConfiguration configuration = configuration;
    private readonly ILogger logger = logger;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var requestId = Guid.NewGuid().ToString("N");
        var stopwatch = Stopwatch.StartNew();
        this.logger.LogInformation("ChatEngine verification started [RequestId={RequestId}].", requestId);

        string? provider = this.configuration[ProviderConfigurationKey];
        if (string.Equals(provider, "OpenAI", StringComparison.OrdinalIgnoreCase))
        {
            stopwatch.Stop();
            this.logger.LogInformation(
                "ChatEngine integration verification skipped because provider is OpenAI [RequestId={RequestId}] [ElapsedMs={ElapsedMs}].",
                requestId,
                stopwatch.ElapsedMilliseconds);
            return;
        }

        try
        {
            this.logger.LogInformation(
                "ChatEngine verification sending ping [RequestId={RequestId}] [Provider={Provider}].",
                requestId,
                provider ?? "(missing)");

            ChatResponse response = await this.chatEngine
                .SendAsync(new ChatRequest("ping"), cancellationToken)
                .ConfigureAwait(false);

            stopwatch.Stop();
            this.logger.LogInformation(
                "ChatEngine integration verification succeeded [RequestId={RequestId}] [ElapsedMs={ElapsedMs}] [Response={Response}].",
                requestId,
                stopwatch.ElapsedMilliseconds,
                response.Message);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            this.logger.LogWarning(
                "ChatEngine integration verification canceled [RequestId={RequestId}] [ElapsedMs={ElapsedMs}].",
                requestId,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            this.logger.LogError(
                exception,
                "ChatEngine integration verification failed [RequestId={RequestId}] [ElapsedMs={ElapsedMs}].",
                requestId,
                stopwatch.ElapsedMilliseconds);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
