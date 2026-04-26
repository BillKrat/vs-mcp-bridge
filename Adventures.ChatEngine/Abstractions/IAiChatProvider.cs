using Adventures.ChatEngine.Models;

namespace Adventures.ChatEngine.Abstractions;

public interface IAiChatProvider
{
    Task<ChatResponse> SendAsync(ChatRequest request, CancellationToken cancellationToken);
}
