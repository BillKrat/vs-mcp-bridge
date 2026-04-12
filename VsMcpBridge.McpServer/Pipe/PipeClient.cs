using Microsoft.Extensions.Logging;
using System.IO.Pipes;
using System.Text.Json;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.Models;

namespace VsMcpBridge.McpServer.Pipe;

/// <summary>
/// Sends requests to the VSIX named pipe server and awaits the response.
/// Each public method corresponds to one VS operation.
/// </summary>
public sealed class PipeClient(ILogger logger) : IPipeClient
{
    private const string DefaultPipeName = "VsMcpBridge";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly ILogger _logger = logger;
    private readonly string _pipeName = DefaultPipeName;

    public PipeClient(ILogger logger, string pipeName)
        : this(logger)
    {
        _pipeName = pipeName;
    }

    private async Task<TResponse> SendAsync<TRequest, TResponse>(
        string command,
        TRequest request,
        CancellationToken cancellationToken)
        where TRequest : VsRequestBase
        where TResponse : VsResponseBase, new()
    {
        var requestId = string.IsNullOrWhiteSpace(request.RequestId) ? "(none)" : request.RequestId;
        _logger.LogTrace($"Attempting pipe connection to '{_pipeName}' [Command={command}] [RequestId={requestId}].");

        try
        {
            using var pipe = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await pipe.ConnectAsync(timeout: 5000, cancellationToken);
            _logger.LogTrace($"Pipe connection established to '{_pipeName}' [Command={command}] [RequestId={requestId}].");

            var envelope = new PipeMessage
            {
                Command = command,
                RequestId = requestId,
                Payload = JsonSerializer.Serialize(request, JsonOptions)
            };

            using var writer = new StreamWriter(pipe, leaveOpen: true) { AutoFlush = true };
            using var reader = new StreamReader(pipe, leaveOpen: true);

            _logger.LogTrace($"Sending pipe request [Command={command}] [RequestId={requestId}] [PayloadLength={envelope.Payload.Length}].");
            await writer.WriteLineAsync(JsonSerializer.Serialize(envelope, JsonOptions));
            var responseJson = await reader.ReadLineAsync(cancellationToken);

            if (string.IsNullOrEmpty(responseJson))
            {
                _logger.LogTrace($"Received empty pipe response [Command={command}] [RequestId={requestId}].");
                return new TResponse
                {
                    RequestId = requestId,
                    Success = false,
                    ErrorMessage = $"Empty response from VSIX for '{command}' [RequestId={requestId}]."
                };
            }

            _logger.LogTrace($"Received pipe response [Command={command}] [RequestId={requestId}] [Length={responseJson.Length}].");
            var response = JsonSerializer.Deserialize<TResponse>(responseJson, JsonOptions);
            if (response == null)
            {
                _logger.LogTrace($"Failed to deserialize pipe response [Command={command}] [RequestId={requestId}].");
                return new TResponse
                {
                    RequestId = requestId,
                    Success = false,
                    ErrorMessage = $"Failed to deserialize response for '{command}' [RequestId={requestId}]."
                };
            }

            response.RequestId = string.IsNullOrWhiteSpace(response.RequestId) ? requestId : response.RequestId;
            _logger.LogTrace($"Pipe request completed [Command={command}] [RequestId={requestId}] [Success={response.Success}].");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogTrace($"Pipe request failed [Command={command}] [RequestId={requestId}] [Error={ex.GetType().Name}: {ex.Message}]");
            throw;
        }
    }

    public Task<GetActiveDocumentResponse> GetActiveDocumentAsync(CancellationToken ct = default)
        => SendAsync<GetActiveDocumentRequest, GetActiveDocumentResponse>(
            PipeCommands.GetActiveDocument,
            new GetActiveDocumentRequest { RequestId = Guid.NewGuid().ToString() },
            ct);

    public Task<GetSelectedTextResponse> GetSelectedTextAsync(CancellationToken ct = default)
        => SendAsync<GetSelectedTextRequest, GetSelectedTextResponse>(
            PipeCommands.GetSelectedText,
            new GetSelectedTextRequest { RequestId = Guid.NewGuid().ToString() },
            ct);

    public Task<ListSolutionProjectsResponse> ListSolutionProjectsAsync(CancellationToken ct = default)
        => SendAsync<ListSolutionProjectsRequest, ListSolutionProjectsResponse>(
            PipeCommands.ListSolutionProjects,
            new ListSolutionProjectsRequest { RequestId = Guid.NewGuid().ToString() },
            ct);

    public Task<GetErrorListResponse> GetErrorListAsync(CancellationToken ct = default)
        => SendAsync<GetErrorListRequest, GetErrorListResponse>(
            PipeCommands.GetErrorList,
            new GetErrorListRequest { RequestId = Guid.NewGuid().ToString() },
            ct);

    public Task<ProposeTextEditResponse> ProposeTextEditAsync(
        string filePath,
        string originalText,
        string proposedText,
        CancellationToken ct = default)
        => SendAsync<ProposeTextEditRequest, ProposeTextEditResponse>(
            PipeCommands.ProposeTextEdit,
            new ProposeTextEditRequest
            {
                RequestId = Guid.NewGuid().ToString(),
                FilePath = filePath,
                OriginalText = originalText,
                ProposedText = proposedText
            },
            ct);
}
