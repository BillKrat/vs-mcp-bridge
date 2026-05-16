using System;
using System.IO;
using System.IO.Pipes;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using VsMcpBridge.McpServer.Pipe;
using VsMcpBridge.Shared.Loggers;
using VsMcpBridge.Shared.Models;
using Xunit;

namespace VsMcpBridge.Shared.Tests;

public sealed class PipeClientTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    [Fact]
    public async Task Vs_backed_tool_when_pipe_is_unavailable_returns_activation_diagnostic()
    {
        var logger = new RecordingBridgeLogger();
        var pipeName = $"VsMcpBridge-Test-token=raw-pipe-secret-{Guid.NewGuid():N}";
        var client = new PipeClient(logger, pipeName, connectTimeoutMilliseconds: 50);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        var response = await client.ProposeTextEditAsync(
            filePath: "token=raw-file-secret.cs",
            originalText: "password=raw-original-secret",
            proposedText: "secret=raw-proposed-secret",
            ct: cts.Token);

        Assert.False(response.Success);
        Assert.False(string.IsNullOrWhiteSpace(response.RequestId));
        Assert.Contains("VS MCP Bridge is not active", response.ErrorMessage, StringComparison.Ordinal);
        Assert.Contains("Visual Studio Experimental Instance", response.ErrorMessage, StringComparison.Ordinal);
        Assert.Contains("View -> Other Windows -> VS MCP Bridge", response.ErrorMessage, StringComparison.Ordinal);
        Assert.Contains(response.RequestId, response.ErrorMessage, StringComparison.Ordinal);
        Assert.DoesNotContain("raw-pipe-secret", response.ErrorMessage, StringComparison.Ordinal);
        Assert.DoesNotContain("raw-file-secret", response.ErrorMessage, StringComparison.Ordinal);
        Assert.DoesNotContain("raw-original-secret", response.ErrorMessage, StringComparison.Ordinal);
        Assert.DoesNotContain("raw-proposed-secret", response.ErrorMessage, StringComparison.Ordinal);
        Assert.Contains(logger.VerboseMessages, message => message.Contains($"Attempting pipe connection to '{pipeName}'", StringComparison.Ordinal));
        Assert.Contains(logger.VerboseMessages, message => message.Contains("Pipe activation preflight failed", StringComparison.Ordinal));
        Assert.Contains(logger.InformationMessages, message => message.Contains("Pipe request started", StringComparison.Ordinal));
        Assert.Contains(logger.WarningMessages, message => message.Contains("Pipe activation preflight failed", StringComparison.Ordinal)
            && message.Contains("View -> Other Windows -> VS MCP Bridge", StringComparison.Ordinal));
        Assert.Empty(logger.Errors);
    }

    [Fact]
    public async Task GetActiveDocumentAsync_preserves_successful_pipe_call_behavior()
    {
        var logger = new RecordingBridgeLogger();
        var pipeName = $"VsMcpBridge-Test-{Guid.NewGuid():N}";
        var client = new PipeClient(logger, pipeName, connectTimeoutMilliseconds: 1000);
        PipeMessage? observedEnvelope = null;
        GetActiveDocumentRequest? observedRequest = null;
        var serverTask = RunSingleResponsePipeServerAsync(
            pipeName,
            envelope =>
            {
                observedEnvelope = envelope;
                observedRequest = JsonSerializer.Deserialize<GetActiveDocumentRequest>(envelope.Payload, JsonOptions);
                return new GetActiveDocumentResponse
                {
                    RequestId = envelope.RequestId,
                    Success = true,
                    FilePath = "active.cs",
                    Language = "C#",
                    Content = "class Active { }"
                };
            });

        var response = await client.GetActiveDocumentAsync(CancellationToken.None);
        await serverTask;

        Assert.True(response.Success);
        Assert.Equal("active.cs", response.FilePath);
        Assert.Equal("C#", response.Language);
        Assert.Equal("class Active { }", response.Content);
        Assert.NotNull(observedEnvelope);
        Assert.Equal(PipeCommands.GetActiveDocument, observedEnvelope!.Command);
        Assert.NotNull(observedRequest);
        Assert.Equal(response.RequestId, observedRequest!.RequestId);
        Assert.Contains(logger.VerboseMessages, message => message.Contains($"Pipe connection established to '{pipeName}'", StringComparison.Ordinal));
        Assert.Contains(logger.InformationMessages, message => message.Contains("Pipe request completed", StringComparison.Ordinal)
            && message.Contains("[Success=True]", StringComparison.Ordinal));
        Assert.Empty(logger.WarningMessages);
        Assert.Empty(logger.Errors);
    }

    private static async Task RunSingleResponsePipeServerAsync<TResponse>(
        string pipeName,
        Func<PipeMessage, TResponse> responseFactory)
    {
        using var server = new NamedPipeServerStream(
            pipeName,
            PipeDirection.InOut,
            maxNumberOfServerInstances: 1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous);

        await server.WaitForConnectionAsync();
        using var reader = new StreamReader(server, leaveOpen: true);
        using var writer = new StreamWriter(server, leaveOpen: true) { AutoFlush = true };
        var requestJson = await reader.ReadLineAsync();
        var envelope = JsonSerializer.Deserialize<PipeMessage>(requestJson!, JsonOptions)
            ?? throw new InvalidOperationException("Pipe request envelope was missing.");
        var response = responseFactory(envelope);
        await writer.WriteLineAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}
