using Adventures.ChatEngine.Abstractions;
using Adventures.ChatEngine.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace VsMcpBridge.McpServer.ChatEngine;

internal sealed class ChatEngineVerificationHostedService(IChatEngine chatEngine, IConfiguration configuration, ILogger logger) : IHostedService
{
    private const string ProviderConfigurationKey = "Adventures:ChatEngine:Provider";

    private readonly IChatEngine chatEngine = chatEngine;
    private readonly IConfiguration configuration = configuration;
    private readonly ILogger logger = logger;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        string? provider = this.configuration[ProviderConfigurationKey];
        if (string.Equals(provider, "OpenAI", StringComparison.OrdinalIgnoreCase))
        {
            this.logger.LogInformation("ChatEngine integration verification skipped because provider is OpenAI.");
            return;
        }

        try
        {
            ChatResponse response = await this.chatEngine
                .SendAsync(new ChatRequest("ping"), cancellationToken)
                .ConfigureAwait(false);

            this.logger.LogInformation($"ChatEngine integration verification succeeded [Response={response.Message}].");
        }
        catch (Exception exception)
        {
            this.logger.LogError(exception, "ChatEngine integration verification failed.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
