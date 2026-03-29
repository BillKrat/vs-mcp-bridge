using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using VsMcpBridge.Shared.Models;
using VsMcpBridge.Vsix.Logging;
using VsMcpBridge.Vsix.Services;

namespace VsMcpBridge.Vsix.Pipe;

/// <summary>
/// Named pipe server that listens for requests from VsMcpBridge.McpServer.
/// Each incoming connection is dispatched to the Visual Studio service layer.
/// </summary>
public sealed class PipeServer : IPipeServer
{
    private const string PipeName = "VsMcpBridge";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly IVsService _vsService;
    private readonly IBridgeLogger _logger;
    private readonly object _sync = new();
    private CancellationTokenSource? _cts;
    private Thread? _listenThread;

    public PipeServer(IVsService vsService, IBridgeLogger logger)
    {
        _vsService = vsService;
        _logger = logger;
    }

    public void Start()
    {
        lock (_sync)
        {
            if (_listenThread != null)
            {
                _logger.LogInformation("Pipe server start requested, but it is already running.");
                return;
            }

            _cts = new CancellationTokenSource();
            _listenThread = new Thread(() => ListenLoop(_cts.Token))
            {
                IsBackground = true,
                Name = "VsMcpBridge-PipeServer"
            };
            _listenThread.Start();
        }

        _logger.LogInformation($"Pipe server started on '{PipeName}'.");
    }

    public void Stop()
    {
        CancellationTokenSource? cts;
        Thread? listenThread;

        lock (_sync)
        {
            cts = _cts;
            listenThread = _listenThread;
            _cts = null;
            _listenThread = null;
        }

        if (cts == null || listenThread == null)
            return;

        try
        {
            cts.Cancel();
            listenThread.Join(millisecondsTimeout: 2000);
            _logger.LogInformation("Pipe server stopped.");
        }
        finally
        {
            cts.Dispose();
        }
    }

    private void ListenLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            NamedPipeServerStream? pipe = null;

            try
            {
                pipe = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                pipe.WaitForConnection();
                _ = Task.Run(() => HandleConnectionAsync(pipe, ct), CancellationToken.None);
                pipe = null;
            }
            catch (OperationCanceledException)
            {
                pipe?.Dispose();
                break;
            }
            catch (Exception ex)
            {
                pipe?.Dispose();
                if (ct.IsCancellationRequested)
                    break;

                _logger.LogError("Pipe server listen loop failed.", ex);
            }
        }
    }

    private async Task HandleConnectionAsync(NamedPipeServerStream pipe, CancellationToken ct)
    {
        try
        {
            using var reader = new StreamReader(pipe, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
            using var writer = new StreamWriter(pipe, Encoding.UTF8, bufferSize: 1024, leaveOpen: true) { AutoFlush = true };

            var requestJson = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(requestJson))
            {
                _logger.LogWarning("Received an empty pipe request.");
                return;
            }

            var envelope = JsonSerializer.Deserialize<PipeMessage>(requestJson, JsonOptions);
            if (envelope == null)
            {
                _logger.LogWarning("Received a pipe request that could not be deserialized.");
                return;
            }

            _logger.LogInformation($"Dispatching pipe command '{envelope.Command}'.");
            var responseJson = await DispatchAsync(envelope);
            await writer.WriteLineAsync(responseJson);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Pipe connection handling canceled.");
        }
        catch (Exception ex)
        {
            _logger.LogError("Pipe connection handling failed.", ex);
        }
        finally
        {
            try
            {
                if (pipe.IsConnected)
                    pipe.Disconnect();
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to disconnect pipe cleanly.", ex);
            }

            pipe.Dispose();
        }
    }

    private async Task<string> DispatchAsync(PipeMessage envelope)
    {
        object response = envelope.Command switch
        {
            PipeCommands.GetActiveDocument => await _vsService.GetActiveDocumentAsync(),
            PipeCommands.GetSelectedText => await _vsService.GetSelectedTextAsync(),
            PipeCommands.ListSolutionProjects => await _vsService.ListSolutionProjectsAsync(),
            PipeCommands.GetErrorList => await _vsService.GetErrorListAsync(),
            PipeCommands.ProposeTextEdit => await DispatchProposeEditAsync(envelope.Payload),
            _ => new VsResponseBaseUnknown { Success = false, ErrorMessage = $"Unknown command: {envelope.Command}" }
        };

        return JsonSerializer.Serialize(response, response.GetType(), JsonOptions);
    }

    private async Task<ProposeTextEditResponse> DispatchProposeEditAsync(string payload)
    {
        var request = JsonSerializer.Deserialize<ProposeTextEditRequest>(payload, JsonOptions);
        if (request == null)
        {
            _logger.LogWarning("Received an invalid ProposeTextEdit request payload.");
            return new ProposeTextEditResponse { Success = false, ErrorMessage = "Invalid request payload." };
        }

        return await _vsService.ProposeTextEditAsync(request.FilePath, request.OriginalText, request.ProposedText);
    }

    private sealed class VsResponseBaseUnknown : VsResponseBase { }
}
