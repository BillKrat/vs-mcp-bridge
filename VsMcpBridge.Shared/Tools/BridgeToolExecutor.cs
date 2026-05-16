using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using VsMcpBridge.Shared.Security;

namespace VsMcpBridge.Shared.Tools
{
    public sealed class BridgeToolExecutor : IBridgeToolExecutor
    {
        private readonly IBridgeToolCatalog _catalog;
        private readonly ILogger _logger;
        private readonly ISecurityRedactor _redactor;
        private readonly IAuditSink _auditSink;
        private readonly IToolExecutionPolicy _policy;

        public BridgeToolExecutor(IBridgeToolCatalog catalog, ILogger logger)
            : this(catalog, logger, new BridgeSecurityRedactor(), new NoOpAuditSink(), new AllowToolExecutionPolicy())
        {
        }

        public BridgeToolExecutor(
            IBridgeToolCatalog catalog,
            ILogger logger,
            ISecurityRedactor redactor,
            IAuditSink auditSink,
            IToolExecutionPolicy policy)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _redactor = redactor ?? throw new ArgumentNullException(nameof(redactor));
            _auditSink = auditSink ?? throw new ArgumentNullException(nameof(auditSink));
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        }

        public async Task<BridgeToolResult> ExecuteAsync(BridgeToolRequest request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation(
                $"Bridge tool execution started [ToolId={request.ToolId}] [RequestId={request.RequestId}] [OperationId={request.OperationId}].");
            _logger.LogTrace(
                $"Bridge tool request payload [ToolId={request.ToolId}] [RequestId={request.RequestId}] [OperationId={request.OperationId}] [Arguments={RedactPayload(request.Arguments)}].");

            if (!_catalog.TryGetTool(request.ToolId, out var tool))
            {
                stopwatch.Stop();
                var result = BridgeToolResult.Failed(request, "UnknownTool", $"Unknown bridge tool '{request.ToolId}'.");
                _logger.LogWarning(
                    $"Bridge tool execution failed [ToolId={request.ToolId}] [RequestId={request.RequestId}] [OperationId={request.OperationId}] [ErrorCode={result.ErrorCode}] [ElapsedMs={stopwatch.ElapsedMilliseconds}].");
                await EmitAuditAsync(request, result, allowed: false, policyReason: "Unknown tool", stopwatch.ElapsedMilliseconds, cancellationToken).ConfigureAwait(false);
                return result;
            }

            var policyDecision = await _policy.EvaluateAsync(new ToolExecutionSecurityContext(request, tool.Descriptor), cancellationToken).ConfigureAwait(false)
                ?? ToolExecutionPolicyDecision.Deny("Policy returned no decision");
            if (!policyDecision.Allowed)
            {
                stopwatch.Stop();
                var result = BridgeToolResult.Failed(request, "PolicyDenied", policyDecision.Reason);
                _logger.LogWarning(
                    $"Bridge tool execution denied [ToolId={request.ToolId}] [RequestId={request.RequestId}] [OperationId={request.OperationId}] [Reason={RedactValue(policyDecision.Reason)}] [ElapsedMs={stopwatch.ElapsedMilliseconds}].");
                await EmitAuditAsync(request, result, allowed: false, policyReason: policyDecision.Reason, stopwatch.ElapsedMilliseconds, cancellationToken).ConfigureAwait(false);
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
                _logger.LogTrace(
                    $"Bridge tool result payload [ToolId={request.ToolId}] [RequestId={request.RequestId}] [OperationId={request.OperationId}] [Data={RedactPayload(result.Data)}].");
                await EmitAuditAsync(request, result, allowed: true, policyReason: policyDecision.Reason, stopwatch.ElapsedMilliseconds, cancellationToken).ConfigureAwait(false);
                return result;
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                var result = BridgeToolResult.Failed(request, "Canceled", $"Bridge tool '{request.ToolId}' execution was canceled.");
                _logger.LogWarning(
                    $"Bridge tool execution canceled [ToolId={request.ToolId}] [RequestId={request.RequestId}] [OperationId={request.OperationId}] [ElapsedMs={stopwatch.ElapsedMilliseconds}].");
                await EmitAuditAsync(request, result, allowed: true, policyReason: policyDecision.Reason, stopwatch.ElapsedMilliseconds, cancellationToken).ConfigureAwait(false);
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                var result = BridgeToolResult.Failed(request, "ExecutionFailed", $"Bridge tool '{request.ToolId}' execution failed. Review the bridge log for details.");
                _logger.LogError(
                    new InvalidOperationException(RedactValue(ex.Message)),
                    $"Bridge tool execution failed [ToolId={request.ToolId}] [RequestId={request.RequestId}] [OperationId={request.OperationId}] [ErrorCode={result.ErrorCode}] [ElapsedMs={stopwatch.ElapsedMilliseconds}].");
                await EmitAuditAsync(request, result, allowed: true, policyReason: policyDecision.Reason, stopwatch.ElapsedMilliseconds, cancellationToken).ConfigureAwait(false);
                return result;
            }
        }

        private async Task EmitAuditAsync(
            BridgeToolRequest request,
            BridgeToolResult result,
            bool allowed,
            string policyReason,
            long elapsedMilliseconds,
            CancellationToken cancellationToken)
        {
            try
            {
                await _auditSink.RecordAsync(
                    new BridgeAuditEnvelope
                    {
                        EventName = "BridgeToolExecution",
                        ToolId = request.ToolId,
                        RequestId = request.RequestId,
                        OperationId = request.OperationId,
                        Success = result.Success,
                        Allowed = allowed,
                        ErrorCode = result.ErrorCode,
                        ElapsedMilliseconds = elapsedMilliseconds,
                        Metadata = new Dictionary<string, string>
                        {
                            ["policyReason"] = RedactValue(policyReason)
                        }
                    },
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    $"Bridge tool audit sink failed [ToolId={request.ToolId}] [RequestId={request.RequestId}] [OperationId={request.OperationId}] [Error={RedactValue(ex.Message)}].");
            }
        }

        private string RedactPayload(object? value)
            => RedactValue(SerializePayload(value));

        private string RedactValue(string? value)
            => _redactor.Redact(value).Value;

        private static string SerializePayload(object? value)
        {
            if (value == null)
                return string.Empty;

            try
            {
                return JsonSerializer.Serialize(value);
            }
            catch (Exception ex)
            {
                return $"<Unserializable payload: {ex.GetType().Name}>";
            }
        }
    }
}
