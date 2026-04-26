using Adventures.ChatEngine.Abstractions;
using Adventures.ChatEngine.Events;
using Adventures.ChatEngine.Models;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

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

    public async IAsyncEnumerable<ChatEvent> StreamAsync(
        ChatRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (ChatEvent chatEvent in this.StreamAsyncCore(request, cancellationToken).ConfigureAwait(false))
        {
            yield return chatEvent;
        }
    }

    public async Task<ChatResponse> SendAsync(
        ChatRequest request,
        CancellationToken cancellationToken,
        Action<ChatEvent>? emitEvent)
    {
        ChatResponse? response = null;

        await foreach (ChatEvent chatEvent in this.StreamAsyncCore(
            request,
            cancellationToken,
            capturedResponse => response = capturedResponse).ConfigureAwait(false))
        {
            emitEvent?.Invoke(chatEvent);
        }

        return response!;
    }

    private async IAsyncEnumerable<ChatEvent> StreamAsyncCore(
        ChatRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken,
        Action<ChatResponse>? captureResponse = null)
    {
        this.logger.LogInformation("Starting request processing for message {Message}.", request.Message);
        yield return new ChatEvent(ChatEventType.RequestStarted);

        this.logger.LogInformation("Calling AI chat provider for message {Message}.", request.Message);

        ChatResponse response = await this.provider
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        captureResponse?.Invoke(response);

        this.logger.LogInformation("AI chat provider returned message {Message}.", response.Message);
        this.logger.LogInformation("Emitting response completed event.");

        yield return new ChatEvent(ChatEventType.ResponseCompleted);
    }
}
