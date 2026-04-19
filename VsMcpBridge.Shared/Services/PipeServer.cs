using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
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
    private static readonly object TraceSync = new();

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

        _logger.LogTrace($"Processing pipe request [Command={envelope.Command}] [RequestId={envelope.RequestId}] [PayloadLength={envelope.Payload?.Length ?? 0}].");
        _logger.LogInformation($"Dispatching pipe command '{envelope.Command}' [RequestId={envelope.RequestId}].");
        var response = await DispatchAsync(envelope);
        response.RequestId = envelope.RequestId;
        _logger.LogInformation($"Pipe command '{envelope.Command}' completed [RequestId={envelope.RequestId}] [Success={response.Success}].");
        return JsonSerializer.Serialize(response, response.GetType(), JsonOptions);
    }

    private void ListenLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            NamedPipeServerStream? pipe = null;

            try
            {
                _logger.LogTrace($"Waiting for pipe client connection on '{PipeName}'.");
                pipe = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                pipe.WaitForConnection();
                _logger.LogInformation($"Pipe client connected on '{PipeName}'.");
                var acceptedPipe = pipe;
                _ = Task.Run(() => HandleConnectionAsync(acceptedPipe, ct), CancellationToken.None);
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
        if (pipe == null)
        {
            var ex = new ArgumentNullException(nameof(pipe));
            _logger.LogError(ex, "Pipe connection handling received a null pipe instance.");
            _exceptionSink.Save("PipeServer.HandleConnectionAsync", ex);
            return;
        }

        try
        {
            WritePipeTrace("HandleConnectionAsync: entered.");
            using var reader = new StreamReader(pipe, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
            WritePipeTrace("HandleConnectionAsync: StreamReader created.");

            WritePipeTrace("HandleConnectionAsync: awaiting ReadLineAsync.");
            var requestJson = await reader.ReadLineAsync();
            WritePipeTrace($"HandleConnectionAsync: ReadLineAsync completed [Length={requestJson?.Length ?? 0}].");
            _logger.LogTrace($"Received raw pipe request [Length={requestJson?.Length ?? 0}].");

            string requestId = "(unknown)";
            string command = "(unknown)";

            if (!string.IsNullOrWhiteSpace(requestJson))
            {
                try
                {
                    var envelope = JsonSerializer.Deserialize<PipeMessage>(requestJson!, JsonOptions);
                    if (envelope != null)
                    {
                        requestId = EnsureRequestId(envelope.RequestId);
                        command = string.IsNullOrWhiteSpace(envelope.Command) ? "(unknown)" : envelope.Command;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read request metadata before processing the pipe request.");
                }
            }

            _logger.LogTrace($"Handling pipe connection [Command={command}] [RequestId={requestId}].");
            var responseJson = await ProcessRequestAsync(requestJson);
            if (responseJson == null)
            {
                _logger.LogWarning($"No pipe response will be written because request processing returned null [Command={command}] [RequestId={requestId}].");
                return;
            }

            using var writer = new StreamWriter(pipe, Encoding.UTF8, bufferSize: 1024, leaveOpen: true) { AutoFlush = true };
            WritePipeTrace("HandleConnectionAsync: StreamWriter created.");
            await writer.WriteLineAsync(responseJson);
            _logger.LogTrace($"Wrote pipe response [Command={command}] [RequestId={requestId}] [Length={responseJson.Length}].");
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
            WritePipeTrace("HandleConnectionAsync: entering finally.");
            try
            {
                if (pipe.IsConnected)
                {
                    WritePipeTrace("HandleConnectionAsync: disconnecting pipe.");
                    pipe.Disconnect();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to disconnect pipe cleanly.");
                _exceptionSink.Save("PipeServer.Disconnect", ex);
            }

            pipe.Dispose();
            WritePipeTrace("HandleConnectionAsync: pipe disposed.");
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

        if (request.FileEdits != null && request.FileEdits.Count > 0)
        {
            if (request.FileEdits.Any(fileEdit => fileEdit == null || string.IsNullOrWhiteSpace(fileEdit.FilePath)))
            {
                _logger.LogWarning($"Received an invalid multi-file ProposeTextEdit request payload [RequestId={envelope.RequestId}].");
                return new ProposeTextEditResponse { RequestId = envelope.RequestId, Success = false, ErrorMessage = "Invalid request payload." };
            }

            return await _vsService.ProposeTextEditsAsync(request.RequestId, request.FileEdits);
        }

        if (string.IsNullOrWhiteSpace(request.FilePath))
        {
            _logger.LogWarning($"Received an invalid ProposeTextEdit request payload [RequestId={envelope.RequestId}].");
            return new ProposeTextEditResponse { RequestId = envelope.RequestId, Success = false, ErrorMessage = "Invalid request payload." };
        }

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

    private static void WritePipeTrace(string message)
    {
        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var logDirectory = Path.Combine(localAppData, "VsMcpBridge", "Logs", "Vsix");
            var logPath = Path.Combine(logDirectory, "pipe-server-trace.log");
            var line = $"[{DateTime.Now:HH:mm:ss}] {message}";

            lock (TraceSync)
            {
                Directory.CreateDirectory(logDirectory);
                File.AppendAllText(logPath, line + Environment.NewLine, Encoding.UTF8);
            }
        }
        catch
        {
            // Never let diagnostics interfere with request handling.
        }
    }

    private sealed class VsResponseBaseUnknown : VsResponseBase { }
}
