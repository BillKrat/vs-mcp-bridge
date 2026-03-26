using System;
using System.IO;
using System.IO.Pipes;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using VsMcpBridge.Shared.Models;
using VsMcpBridge.Vsix.Services;

namespace VsMcpBridge.Vsix.Pipe;

/// <summary>
/// Named pipe server that listens for requests from VsMcpBridge.McpServer.
/// Each incoming connection is handled on a background thread and dispatched
/// to <see cref="VsService"/>.
/// </summary>
public sealed class PipeServer
{
    private const string PipeName = "VsMcpBridge";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly VsService _vsService;
    private CancellationTokenSource? _cts;
    private Thread? _listenThread;

    public PipeServer(VsService vsService) => _vsService = vsService;

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _listenThread = new Thread(() => ListenLoop(_cts.Token)) { IsBackground = true, Name = "VsMcpBridge-PipeServer" };
        _listenThread.Start();
    }

    public void Stop()
    {
        _cts?.Cancel();
        _listenThread?.Join(millisecondsTimeout: 2000);
    }

    private void ListenLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var pipe = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                pipe.WaitForConnectionAsync(ct).GetAwaiter().GetResult();
                Task.Run(() => HandleConnectionAsync(pipe, ct), ct);
            }
            catch (OperationCanceledException) { break; }
            catch { /* log and continue */ }
        }
    }

    private async Task HandleConnectionAsync(NamedPipeServerStream pipe, CancellationToken ct)
    {
        try
        {
            using var reader = new StreamReader(pipe, leaveOpen: true);
            using var writer = new StreamWriter(pipe, leaveOpen: true) { AutoFlush = true };

            var requestJson = await reader.ReadLineAsync();
            if (string.IsNullOrEmpty(requestJson)) return;

            var envelope = JsonSerializer.Deserialize<PipeMessage>(requestJson, JsonOptions);
            if (envelope == null) return;

            var responseJson = await DispatchAsync(envelope, ct);
            await writer.WriteLineAsync(responseJson);
        }
        catch { /* log and swallow */ }
        finally
        {
            if (pipe.IsConnected) pipe.Disconnect();
        }
    }

    private async Task<string> DispatchAsync(PipeMessage envelope, CancellationToken ct)
    {
        object response = envelope.Command switch
        {
            PipeCommands.GetActiveDocument => await _vsService.GetActiveDocumentAsync(),
            PipeCommands.GetSelectedText => await _vsService.GetSelectedTextAsync(),
            PipeCommands.ListSolutionProjects => await _vsService.ListSolutionProjectsAsync(),
            PipeCommands.GetErrorList => await _vsService.GetErrorListAsync(),
            PipeCommands.ProposeTextEdit => await DispatchProposeEditAsync(envelope.Payload),
            _ => new VsResponseBase_Unknown { Success = false, ErrorMessage = $"Unknown command: {envelope.Command}" }
        };

        return JsonSerializer.Serialize(response, response.GetType(), JsonOptions);
    }

    private async Task<ProposeTextEditResponse> DispatchProposeEditAsync(string payload)
    {
        var request = JsonSerializer.Deserialize<ProposeTextEditRequest>(payload, JsonOptions);
        if (request == null)
            return new ProposeTextEditResponse { Success = false, ErrorMessage = "Invalid request payload." };

        return await _vsService.ProposeTextEditAsync(request.FilePath, request.OriginalText, request.ProposedText);
    }

    private sealed class VsResponseBase_Unknown : VsResponseBase { }
}
