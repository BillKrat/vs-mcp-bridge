using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Adventures.ChatEngine.Abstractions;
using Adventures.ChatEngine.Events;
using Adventures.ChatEngine.Models;
using Microsoft.Extensions.Logging.Abstractions;
using VsMcpBridge.McpServer.Tools;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.Models;
using Xunit;

namespace VsMcpBridge.Shared.Tests;

public sealed class VsToolsTests
{
    [Fact]
    public async Task ProposeTextEditAsync_preserves_backward_compatible_single_file_request_flow()
    {
        var pipeClient = new RecordingPipeClient();
        var tools = new VsTools(pipeClient, new StubChatEngine(), NullLogger.Instance);

        var response = await tools.ProposeTextEditAsync("sample.cs", "before", "after", CancellationToken.None);

        Assert.Equal(1, pipeClient.ProposeTextEditCalls);
        Assert.Equal(0, pipeClient.ProposeTextEditsCalls);
        Assert.Contains("Proposed diff for sample.cs", response, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProposeTextEditsAsync_sends_multi_file_request_through_pipe_client()
    {
        var pipeClient = new RecordingPipeClient();
        var tools = new VsTools(pipeClient, new StubChatEngine(), NullLogger.Instance);
        var fileEdits = new[]
        {
            new ProposalFileEditRequest { FilePath = "first.cs", OriginalText = "before-1", ProposedText = "after-1" },
            new ProposalFileEditRequest { FilePath = "second.cs", OriginalText = "before-2", ProposedText = "after-2" }
        };

        var response = await tools.ProposeTextEditsAsync(fileEdits, CancellationToken.None);

        Assert.Equal(0, pipeClient.ProposeTextEditCalls);
        Assert.Equal(1, pipeClient.ProposeTextEditsCalls);
        Assert.Equal(2, pipeClient.LastFileEdits.Count);
        Assert.Contains("Proposed diff for 2 files", response, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProposeTextEditsAsync_returns_clear_error_for_invalid_payload()
    {
        var pipeClient = new RecordingPipeClient();
        var tools = new VsTools(pipeClient, new StubChatEngine(), NullLogger.Instance);

        var response = await tools.ProposeTextEditsAsync(
            new[] { new ProposalFileEditRequest { FilePath = string.Empty, OriginalText = "before", ProposedText = "after" } },
            CancellationToken.None);

        Assert.Equal("Error: each file edit must include a non-empty filePath.", response);
        Assert.Equal(0, pipeClient.ProposeTextEditCalls);
        Assert.Equal(0, pipeClient.ProposeTextEditsCalls);
    }

    [Fact]
    public async Task ChatEnginePingAsync_routes_ping_through_chat_engine()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new StubChatEngine();
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var response = await tools.ChatEnginePingAsync(CancellationToken.None);

        Assert.Equal("pong", response);
        Assert.Equal("ping", chatEngine.LastRequest?.Message);
    }

    [Fact]
    public async Task ChatEngineChatAsync_routes_input_through_chat_engine()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new StubChatEngine();
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var responseJson = await tools.ChatEngineChatAsync("hello", CancellationToken.None);
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.Equal("echo:hello", response.Content);
        Assert.Null(response.Error);
        Assert.False(string.IsNullOrWhiteSpace(response.RequestId));
        Assert.Equal("hello", chatEngine.LastRequest?.Message);
    }

    [Fact]
    public async Task ChatEngineChatAsync_returns_controlled_error_for_null_input_without_calling_chat_engine()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new StubChatEngine();
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

#pragma warning disable CS8625
        var responseJson = await tools.ChatEngineChatAsync(null, CancellationToken.None);
#pragma warning restore CS8625
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Null(response.Content);
        Assert.Equal("Error: chat_engine_chat requires a non-empty message no longer than 4000 characters.", response.Error);
        Assert.False(string.IsNullOrWhiteSpace(response.RequestId));
        Assert.Null(chatEngine.LastRequest);
    }

    [Fact]
    public async Task ChatEngineChatAsync_returns_controlled_error_for_empty_input_without_calling_chat_engine()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new StubChatEngine();
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var responseJson = await tools.ChatEngineChatAsync(string.Empty, CancellationToken.None);
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Null(response.Content);
        Assert.Equal("Error: chat_engine_chat requires a non-empty message no longer than 4000 characters.", response.Error);
        Assert.False(string.IsNullOrWhiteSpace(response.RequestId));
        Assert.Null(chatEngine.LastRequest);
    }

    [Fact]
    public async Task ChatEngineChatAsync_returns_controlled_error_for_whitespace_input_without_calling_chat_engine()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new StubChatEngine();
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var responseJson = await tools.ChatEngineChatAsync("   ", CancellationToken.None);
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Null(response.Content);
        Assert.Equal("Error: chat_engine_chat requires a non-empty message no longer than 4000 characters.", response.Error);
        Assert.False(string.IsNullOrWhiteSpace(response.RequestId));
        Assert.Null(chatEngine.LastRequest);
    }

    [Fact]
    public async Task ChatEngineChatAsync_returns_controlled_error_for_oversized_input_without_calling_chat_engine()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new StubChatEngine();
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var responseJson = await tools.ChatEngineChatAsync(new string('x', 4001), CancellationToken.None);
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Null(response.Content);
        Assert.Equal("Error: chat_engine_chat requires a non-empty message no longer than 4000 characters.", response.Error);
        Assert.False(string.IsNullOrWhiteSpace(response.RequestId));
        Assert.Null(chatEngine.LastRequest);
    }

    [Fact]
    public async Task ChatEngineChatAsync_returns_controlled_error_when_chat_engine_fails()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new ThrowingChatEngine();
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var responseJson = await tools.ChatEngineChatAsync("hello", CancellationToken.None);
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Null(response.Content);
        Assert.Equal("Error: chat_engine_chat failed.", response.Error);
        Assert.False(string.IsNullOrWhiteSpace(response.RequestId));
        Assert.Equal("hello", chatEngine.LastRequest?.Message);
    }

    private sealed class RecordingPipeClient : IPipeClient
    {
        public int ProposeTextEditCalls { get; private set; }
        public int ProposeTextEditsCalls { get; private set; }
        public IReadOnlyList<ProposalFileEditRequest> LastFileEdits { get; private set; } = Array.Empty<ProposalFileEditRequest>();

        public Task<GetActiveDocumentResponse> GetActiveDocumentAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<GetErrorListResponse> GetErrorListAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<GetSelectedTextResponse> GetSelectedTextAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<ListSolutionProjectsResponse> ListSolutionProjectsAsync(CancellationToken ct = default) => throw new NotSupportedException();

        public Task<ProposeTextEditResponse> ProposeTextEditAsync(string filePath, string originalText, string proposedText, CancellationToken ct = default)
        {
            ProposeTextEditCalls++;
            return Task.FromResult(new ProposeTextEditResponse
            {
                Success = true,
                FilePath = filePath,
                Diff = $"--- a/{filePath}\n+++ b/{filePath}\n-{originalText}\n+{proposedText}\n"
            });
        }

        public Task<ProposeTextEditResponse> ProposeTextEditsAsync(IReadOnlyList<ProposalFileEditRequest> fileEdits, CancellationToken ct = default)
        {
            ProposeTextEditsCalls++;
            LastFileEdits = fileEdits;
            return Task.FromResult(new ProposeTextEditResponse
            {
                Success = true,
                FilePath = fileEdits[0].FilePath,
                Diff = string.Join("\n", fileEdits.Select(fileEdit => $"--- a/{fileEdit.FilePath}\n+++ b/{fileEdit.FilePath}\n-{fileEdit.OriginalText}\n+{fileEdit.ProposedText}\n"))
            });
        }
    }

    private sealed class StubChatEngine : IChatEngine
    {
        public ChatRequest? LastRequest { get; private set; }

        public Task<ChatResponse> SendAsync(ChatRequest request, CancellationToken cancellationToken, Action<ChatEvent>? onEvent = null)
        {
            LastRequest = request;
            var response = request.Message == "ping" ? "pong" : $"echo:{request.Message}";
            return Task.FromResult(new ChatResponse(response));
        }

        public async IAsyncEnumerable<ChatEvent> StreamAsync(ChatRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            LastRequest = request;
            yield return new ChatEvent(ChatEventType.RequestStarted);
            var response = request.Message == "ping" ? "pong" : $"echo:{request.Message}";
            yield return new ChatEvent(ChatEventType.TokenGenerated, response);
            yield return new ChatEvent(ChatEventType.ResponseCompleted);
            await Task.CompletedTask;
        }
    }

    private sealed class ThrowingChatEngine : IChatEngine
    {
        public ChatRequest? LastRequest { get; private set; }

        public Task<ChatResponse> SendAsync(ChatRequest request, CancellationToken cancellationToken, Action<ChatEvent>? onEvent = null)
        {
            LastRequest = request;
            throw new InvalidOperationException("provider failed");
        }

        public async IAsyncEnumerable<ChatEvent> StreamAsync(ChatRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            LastRequest = request;
            await Task.CompletedTask;
            yield break;
            throw new InvalidOperationException("provider failed");
        }
    }
}
