using Adventures.ChatEngine.Abstractions;
using Adventures.ChatEngine.Events;
using Adventures.ChatEngine.Models;

namespace Adventures.ChatEngine.Services;

public sealed class ChatEngine
{
    private readonly IAiChatProvider provider;

    public ChatEngine(IAiChatProvider provider)
    {
        this.provider = provider;
    }

    public async Task<ChatResponse> SendAsync(
        ChatRequest request,
        CancellationToken cancellationToken,
        Action<ChatEvent> emitEvent)
    {
        emitEvent(new ChatEvent(ChatEventType.RequestStarted));

        ChatResponse response = await this.provider
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        emitEvent(new ChatEvent(ChatEventType.ResponseCompleted));

        return response;
    }
}
