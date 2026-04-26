using Adventures.ChatEngine.Events;
using Adventures.ChatEngine.Models;

namespace Adventures.ChatEngine.Abstractions;

public interface IChatEngine
{
    Task<ChatResponse> SendAsync(
        ChatRequest request,
        CancellationToken cancellationToken,
        Action<ChatEvent>? onEvent = null);

    IAsyncEnumerable<ChatEvent> StreamAsync(
        ChatRequest request,
        CancellationToken cancellationToken);
}
