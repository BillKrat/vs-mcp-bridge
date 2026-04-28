using Adventures.ChatEngine.Abstractions;
using Adventures.ChatEngine.Models;

namespace Adventures.ChatEngine.Tests.Fakes;

internal sealed class FakePingPongProvider : IAiChatProvider
{
    private readonly Queue<Exception> exceptionsToThrow = new();
    private readonly Queue<string> streamChunks = new();

    public bool WasCalled { get; private set; }
    public int CallCount { get; private set; }

    public void FailNext(Exception exception)
    {
        this.exceptionsToThrow.Enqueue(exception);
    }

    public void SetStreamChunks(params string[] chunks)
    {
        this.streamChunks.Clear();

        foreach (string chunk in chunks)
        {
            this.streamChunks.Enqueue(chunk);
        }
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

    public async IAsyncEnumerable<ChatResponse> StreamAsync(ChatRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        this.WasCalled = true;
        this.CallCount++;

        if (this.exceptionsToThrow.Count > 0)
        {
            throw this.exceptionsToThrow.Dequeue();
        }

        if (this.streamChunks.Count > 0)
        {
            foreach (string chunk in this.streamChunks)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return new ChatResponse(chunk);
            }

            yield break;
        }

        string message = request.Message == "ping" ? "pong" : "unknown";
        yield return new ChatResponse(message);
        await Task.CompletedTask;
    }
}
