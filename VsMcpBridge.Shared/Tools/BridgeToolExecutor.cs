using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace VsMcpBridge.Shared.Tools
{
    public sealed class BridgeToolExecutor : IBridgeToolExecutor
    {
        private readonly IBridgeToolCatalog _catalog;
        private readonly ILogger _logger;

        public BridgeToolExecutor(IBridgeToolCatalog catalog, ILogger logger)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<BridgeToolResult> ExecuteAsync(BridgeToolRequest request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation(
                $"Bridge tool execution started [ToolId={request.ToolId}] [RequestId={request.RequestId}] [OperationId={request.OperationId}].");

            if (!_catalog.TryGetTool(request.ToolId, out var tool))
            {
                stopwatch.Stop();
                var result = BridgeToolResult.Failed(request, "UnknownTool", $"Unknown bridge tool '{request.ToolId}'.");
                _logger.LogWarning(
                    $"Bridge tool execution failed [ToolId={request.ToolId}] [RequestId={request.RequestId}] [OperationId={request.OperationId}] [ErrorCode={result.ErrorCode}] [ElapsedMs={stopwatch.ElapsedMilliseconds}].");
                return result;
            }

            try
            {
                var result = await tool.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
                stopwatch.Stop();
                result.ToolId = request.ToolId;
                result.RequestId = request.RequestId;
                result.OperationId = request.OperationId;
                _logger.LogInformation(
                    $"Bridge tool execution completed [ToolId={request.ToolId}] [RequestId={request.RequestId}] [OperationId={request.OperationId}] [Success={result.Success}] [ElapsedMs={stopwatch.ElapsedMilliseconds}].");
                return result;
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                var result = BridgeToolResult.Failed(request, "Canceled", $"Bridge tool '{request.ToolId}' execution was canceled.");
                _logger.LogWarning(
                    $"Bridge tool execution canceled [ToolId={request.ToolId}] [RequestId={request.RequestId}] [OperationId={request.OperationId}] [ElapsedMs={stopwatch.ElapsedMilliseconds}].");
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                var result = BridgeToolResult.Failed(request, "ExecutionFailed", $"Bridge tool '{request.ToolId}' execution failed. Review the bridge log for details.");
                _logger.LogError(
                    ex,
                    $"Bridge tool execution failed [ToolId={request.ToolId}] [RequestId={request.RequestId}] [OperationId={request.OperationId}] [ErrorCode={result.ErrorCode}] [ElapsedMs={stopwatch.ElapsedMilliseconds}].");
                return result;
            }
        }
    }
}
