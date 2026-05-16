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
        private readonly IToolExecutionApprovalService _approvalService;

        public BridgeToolExecutor(IBridgeToolCatalog catalog, ILogger logger)
            : this(catalog, logger, new BridgeSecurityRedactor(), new NoOpAuditSink(), new AllowToolExecutionPolicy(), new AllowToolExecutionApprovalService())
        {
        }

        public BridgeToolExecutor(
            IBridgeToolCatalog catalog,
            ILogger logger,
            ISecurityRedactor redactor,
            IAuditSink auditSink,
            IToolExecutionPolicy policy)
            : this(catalog, logger, redactor, auditSink, policy, new AllowToolExecutionApprovalService())
        {
        }

        public BridgeToolExecutor(
            IBridgeToolCatalog catalog,
            ILogger logger,
            ISecurityRedactor redactor,
            IAuditSink auditSink,
            IToolExecutionPolicy policy,
            IToolExecutionApprovalService approvalService)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _redactor = redactor ?? throw new ArgumentNullException(nameof(redactor));
            _auditSink = auditSink ?? throw new ArgumentNullException(nameof(auditSink));
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
            _approvalService = approvalService ?? throw new ArgumentNullException(nameof(approvalService));
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
                await EmitAuditAsync(
                    request,
                    result,
                    allowed: false,
                    policyReason: "Unknown tool",
                    approvalRequirement: ToolExecutionApprovalRequirement.NotRequired,
                    approvalDecision: "NotEvaluated",
                    approvalReason: "Unknown tool",
                    stopwatch.ElapsedMilliseconds,
                    cancellationToken).ConfigureAwait(false);
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
                await EmitAuditAsync(
                    request,
                    result,
                    allowed: false,
                    policyReason: policyDecision.Reason,
                    approvalRequirement: tool.Descriptor.ApprovalRequirement,
                    approvalDecision: "NotEvaluated",
                    approvalReason: "Policy denied before approval",
                    stopwatch.ElapsedMilliseconds,
                    cancellationToken).ConfigureAwait(false);
                return result;
            }

            var approvalDecision = await EvaluateApprovalAsync(request, tool.Descriptor, policyDecision, cancellationToken).ConfigureAwait(false);
            if (!approvalDecision.Approved)
            {
                stopwatch.Stop();
                var result = BridgeToolResult.Failed(request, "ApprovalDenied", approvalDecision.Reason);
                _logger.LogWarning(
                    $"Bridge tool execution approval denied [ToolId={request.ToolId}] [RequestId={request.RequestId}] [OperationId={request.OperationId}] [Reason={RedactValue(approvalDecision.Reason)}] [ElapsedMs={stopwatch.ElapsedMilliseconds}].");
                await EmitAuditAsync(
                    request,
                    result,
                    allowed: false,
                    policyReason: policyDecision.Reason,
                    approvalRequirement: tool.Descriptor.ApprovalRequirement,
                    approvalDecision: "Denied",
                    approvalReason: approvalDecision.Reason,
                    stopwatch.ElapsedMilliseconds,
                    cancellationToken).ConfigureAwait(false);
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
                await EmitAuditAsync(
                    request,
                    result,
                    allowed: true,
                    policyReason: policyDecision.Reason,
                    approvalRequirement: tool.Descriptor.ApprovalRequirement,
                    approvalDecision: GetApprovalDecisionName(tool.Descriptor, approvalDecision),
                    approvalReason: approvalDecision.Reason,
                    stopwatch.ElapsedMilliseconds,
                    cancellationToken).ConfigureAwait(false);
                return result;
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                var result = BridgeToolResult.Failed(request, "Canceled", $"Bridge tool '{request.ToolId}' execution was canceled.");
                _logger.LogWarning(
                    $"Bridge tool execution canceled [ToolId={request.ToolId}] [RequestId={request.RequestId}] [OperationId={request.OperationId}] [ElapsedMs={stopwatch.ElapsedMilliseconds}].");
                await EmitAuditAsync(
                    request,
                    result,
                    allowed: true,
                    policyReason: policyDecision.Reason,
                    approvalRequirement: tool.Descriptor.ApprovalRequirement,
                    approvalDecision: GetApprovalDecisionName(tool.Descriptor, approvalDecision),
                    approvalReason: approvalDecision.Reason,
                    stopwatch.ElapsedMilliseconds,
                    cancellationToken).ConfigureAwait(false);
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                var result = BridgeToolResult.Failed(request, "ExecutionFailed", $"Bridge tool '{request.ToolId}' execution failed. Review the bridge log for details.");
                _logger.LogError(
                    new InvalidOperationException(RedactValue(ex.Message)),
                    $"Bridge tool execution failed [ToolId={request.ToolId}] [RequestId={request.RequestId}] [OperationId={request.OperationId}] [ErrorCode={result.ErrorCode}] [ElapsedMs={stopwatch.ElapsedMilliseconds}].");
                await EmitAuditAsync(
                    request,
                    result,
                    allowed: true,
                    policyReason: policyDecision.Reason,
                    approvalRequirement: tool.Descriptor.ApprovalRequirement,
                    approvalDecision: GetApprovalDecisionName(tool.Descriptor, approvalDecision),
                    approvalReason: approvalDecision.Reason,
                    stopwatch.ElapsedMilliseconds,
                    cancellationToken).ConfigureAwait(false);
                return result;
            }
        }

        private async Task<ToolExecutionApprovalDecision> EvaluateApprovalAsync(
            BridgeToolRequest request,
            BridgeToolDescriptor descriptor,
            ToolExecutionPolicyDecision policyDecision,
            CancellationToken cancellationToken)
        {
            if (descriptor.ApprovalRequirement != ToolExecutionApprovalRequirement.Required)
                return ToolExecutionApprovalDecision.Approve("Not required");

            _logger.LogInformation(
                $"Bridge tool execution approval required [ToolId={request.ToolId}] [RequestId={request.RequestId}] [OperationId={request.OperationId}].");
            return await _approvalService.EvaluateAsync(
                    new ToolExecutionApprovalContext(request, descriptor, policyDecision),
                    cancellationToken).ConfigureAwait(false)
                ?? ToolExecutionApprovalDecision.Deny("Approval service returned no decision");
        }

        private async Task EmitAuditAsync(
            BridgeToolRequest request,
            BridgeToolResult result,
            bool allowed,
            string policyReason,
            ToolExecutionApprovalRequirement approvalRequirement,
            string approvalDecision,
            string approvalReason,
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
                            ["policyReason"] = RedactValue(policyReason),
                            ["approvalRequirement"] = approvalRequirement.ToString(),
                            ["approvalDecision"] = approvalDecision,
                            ["approvalReason"] = RedactValue(approvalReason)
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

        private static string GetApprovalDecisionName(BridgeToolDescriptor descriptor, ToolExecutionApprovalDecision decision)
        {
            if (descriptor.ApprovalRequirement != ToolExecutionApprovalRequirement.Required)
                return "NotRequired";

            return decision.Approved ? "Approved" : "Denied";
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
