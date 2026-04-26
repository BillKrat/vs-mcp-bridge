using Adventures.ChatEngine.Abstractions;
using Adventures.ChatEngine.Models;

namespace Adventures.ChatEngine.Tests.Fakes;

internal sealed class FakePingPongProvider : IAiChatProvider
{
    public bool WasCalled { get; private set; }

    public Task<ChatResponse> SendAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        this.WasCalled = true;
        string message = request.Message == "ping" ? "pong" : "unknown";
        return Task.FromResult(new ChatResponse(message));
    }
}
