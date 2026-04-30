using Adventures.ChatEngine.Abstractions;
using Adventures.ChatEngine.Models;

namespace VsMcpBridge.McpServer.ChatEngine;

internal sealed class HostPingPongChatProvider : IAiChatProvider
{
    public Task<ChatResponse> SendAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        string message = request.Message == "ping" ? "pong" : "unknown";
        return Task.FromResult(new ChatResponse(message));
    }

    public async IAsyncEnumerable<ChatResponse> StreamAsync(
        ChatRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return await this.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
