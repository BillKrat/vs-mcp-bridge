using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.Models;

namespace VsMcpBridge.Shared.Services;

/// <summary>
/// Named pipe server that listens for requests from VsMcpBridge.McpServer.
/// Each incoming connection is dispatched to the Visual Studio service layer.
/// </summary>
public sealed class PipeServer : IPipeServer
{
    private const string PipeName = "VsMcpBridge";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly IVsService _vsService;
    private readonly ILogger _logger;
    private readonly IUnhandledExceptionSink _exceptionSink;
    private readonly object _sync = new();
    private CancellationTokenSource? _cts;
    private Thread? _listenThread;
    private int _hasHandledFirstRequest;

    public PipeServer(IVsService vsService, ILogger logger, IUnhandledExceptionSink exceptionSink)
    {
        _vsService = vsService;
        _logger = logger;
        _exceptionSink = exceptionSink;
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

            _logger.LogTrace("Starting pipe server listener thread.");
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

    public async Task<string?> ProcessRequestAsync(string? requestJson)
    {
        if (string.IsNullOrWhiteSpace(requestJson))
        {
            _logger.LogWarning("Received an empty pipe request.");
            return null;
        }

        if (Interlocked.CompareExchange(ref _hasHandledFirstRequest, 1, 0) == 0)
            _logger.LogTrace("Handling first bridge request.");

        var envelope = JsonSerializer.Deserialize<PipeMessage>(requestJson!, JsonOptions);
        if (envelope == null)
        {
            _logger.LogWarning("Received a pipe request that could not be deserialized.");
            return null;
        }

        envelope.RequestId = EnsureRequestId(envelope.RequestId);

        _logger.LogInformation($"Dispatching pipe command '{envelope.Command}' [RequestId={envelope.RequestId}].");
        var response = await DispatchAsync(envelope);
        response.RequestId = envelope.RequestId;
        return JsonSerializer.Serialize(response, response.GetType(), JsonOptions);
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

                _logger.LogError(ex, "Pipe server listen loop failed.");
                _exceptionSink.Save("PipeServer.ListenLoop", ex);
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
            var responseJson = await ProcessRequestAsync(requestJson);
            if (responseJson == null)
                return;

            await writer.WriteLineAsync(responseJson);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Pipe connection handling canceled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pipe connection handling failed.");
            _exceptionSink.Save("PipeServer.HandleConnectionAsync", ex);
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
                _logger.LogError(ex, "Failed to disconnect pipe cleanly.");
                _exceptionSink.Save("PipeServer.Disconnect", ex);
            }

            pipe.Dispose();
        }
    }

    private async Task<VsResponseBase> DispatchAsync(PipeMessage envelope)
    {
        VsResponseBase response = envelope.Command switch
        {
            PipeCommands.GetActiveDocument => await _vsService.GetActiveDocumentAsync(),
            PipeCommands.GetSelectedText => await _vsService.GetSelectedTextAsync(),
            PipeCommands.ListSolutionProjects => await _vsService.ListSolutionProjectsAsync(),
            PipeCommands.GetErrorList => await _vsService.GetErrorListAsync(),
            PipeCommands.ProposeTextEdit => await DispatchProposeEditAsync(envelope),
            _ => new VsResponseBaseUnknown { Success = false, ErrorMessage = $"Unknown command: {envelope.Command}" }
        };

        return response;
    }

    private async Task<ProposeTextEditResponse> DispatchProposeEditAsync(PipeMessage envelope)
    {
        var request = JsonSerializer.Deserialize<ProposeTextEditRequest>(envelope.Payload, JsonOptions);
        if (request == null)
        {
            _logger.LogWarning($"Received an invalid ProposeTextEdit request payload [RequestId={envelope.RequestId}].");
            return new ProposeTextEditResponse { RequestId = envelope.RequestId, Success = false, ErrorMessage = "Invalid request payload." };
        }

        request.RequestId = EnsureRequestId(request.RequestId, envelope.RequestId);
        envelope.RequestId = request.RequestId;
        return await _vsService.ProposeTextEditAsync(request.RequestId, request.FilePath, request.OriginalText, request.ProposedText);
    }

    private static string EnsureRequestId(string? requestId, string? fallbackRequestId = null)
    {
        if (!string.IsNullOrWhiteSpace(requestId))
            return requestId!;

        if (!string.IsNullOrWhiteSpace(fallbackRequestId))
            return fallbackRequestId!;

        return Guid.NewGuid().ToString("N");
    }

    private sealed class VsResponseBaseUnknown : VsResponseBase { }
}
