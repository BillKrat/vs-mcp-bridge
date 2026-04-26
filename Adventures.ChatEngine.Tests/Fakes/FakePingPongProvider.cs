using Adventures.ChatEngine.Abstractions;
using Adventures.ChatEngine.Models;

namespace Adventures.ChatEngine.Tests.Fakes;

internal sealed class FakePingPongProvider : IAiChatProvider
{
    public Task<ChatResponse> SendAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        string message = request.Message == "ping" ? "pong" : "unknown";
        return Task.FromResult(new ChatResponse(message));
    }
}
