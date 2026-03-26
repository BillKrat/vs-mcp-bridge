using System.IO.Pipes;
using System.Text.Json;
using VsMcpBridge.Shared.Models;

namespace VsMcpBridge.McpServer.Pipe;

/// <summary>
/// Sends requests to the VSIX named pipe server and awaits the response.
/// Each public method corresponds to one VS operation.
/// </summary>
public sealed class PipeClient
{
    private const string PipeName = "VsMcpBridge";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private async Task<TResponse> SendAsync<TRequest, TResponse>(
        string command,
        TRequest request,
        CancellationToken cancellationToken)
        where TRequest : VsRequestBase
        where TResponse : VsResponseBase, new()
    {
        using var pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await pipe.ConnectAsync(timeout: 5000, cancellationToken);

        var envelope = new PipeMessage
        {
            Command = command,
            Payload = JsonSerializer.Serialize(request, JsonOptions)
        };

        using var writer = new StreamWriter(pipe, leaveOpen: true) { AutoFlush = true };
        using var reader = new StreamReader(pipe, leaveOpen: true);

        await writer.WriteLineAsync(JsonSerializer.Serialize(envelope, JsonOptions));
        var responseJson = await reader.ReadLineAsync(cancellationToken);

        if (string.IsNullOrEmpty(responseJson))
            return new TResponse { Success = false, ErrorMessage = "Empty response from VSIX." };

        return JsonSerializer.Deserialize<TResponse>(responseJson, JsonOptions)
               ?? new TResponse { Success = false, ErrorMessage = "Failed to deserialize response." };
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
