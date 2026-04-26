using Adventures.ChatEngine.Abstractions;
using Adventures.ChatEngine.Models;

namespace Adventures.ChatEngine.Tests.Fakes;

internal sealed class FakePingPongProvider : IAiChatProvider
{
    private readonly Queue<Exception> exceptionsToThrow = new();

    public bool WasCalled { get; private set; }
    public int CallCount { get; private set; }

    public void FailNext(Exception exception)
    {
        this.exceptionsToThrow.Enqueue(exception);
    }

    public Task<ChatResponse> SendAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        this.WasCalled = true;
        this.CallCount++;

        if (this.exceptionsToThrow.Count > 0)
        {
            throw this.exceptionsToThrow.Dequeue();
        }

        string message = request.Message == "ping" ? "pong" : "unknown";
        return Task.FromResult(new ChatResponse(message));
    }
}
