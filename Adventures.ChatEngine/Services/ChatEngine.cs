using Adventures.ChatEngine.Abstractions;
using Adventures.ChatEngine.Events;
using Adventures.ChatEngine.Models;
using Microsoft.Extensions.Logging;

namespace Adventures.ChatEngine.Services;

public sealed class ChatEngine
{
    private readonly ILogger<ChatEngine> logger;
    private readonly IAiChatProvider provider;

    public ChatEngine(IAiChatProvider provider, ILogger<ChatEngine> logger)
    {
        this.provider = provider;
        this.logger = logger;
    }

    public async Task<ChatResponse> SendAsync(
        ChatRequest request,
        CancellationToken cancellationToken,
        Action<ChatEvent> emitEvent)
    {
        this.logger.LogInformation("Starting request processing for message {Message}.", request.Message);

        emitEvent(new ChatEvent(ChatEventType.RequestStarted));

        this.logger.LogInformation("Calling AI chat provider for message {Message}.", request.Message);

        ChatResponse response = await this.provider
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        this.logger.LogInformation("AI chat provider returned message {Message}.", response.Message);
        this.logger.LogInformation("Emitting response completed event.");

        emitEvent(new ChatEvent(ChatEventType.ResponseCompleted));

        return response;
    }
}
