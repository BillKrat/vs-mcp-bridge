using Adventures.ChatEngine.Abstractions;
using Adventures.ChatEngine.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace VsMcpBridge.McpServer.ChatEngine;

internal sealed class ChatEngineVerificationHostedService(IChatEngine chatEngine, ILogger logger) : IHostedService
{
    private readonly IChatEngine chatEngine = chatEngine;
    private readonly ILogger logger = logger;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
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
