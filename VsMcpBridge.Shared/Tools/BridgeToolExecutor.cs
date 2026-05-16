using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        private readonly ISecretBroker _secretBroker;

        public BridgeToolExecutor(IBridgeToolCatalog catalog, ILogger logger)
            : this(catalog, logger, new BridgeSecurityRedactor(), new NoOpAuditSink(), new AllowToolExecutionPolicy(), new AllowToolExecutionApprovalService(), new NoOpSecretBroker())
        {
        }

        public BridgeToolExecutor(
            IBridgeToolCatalog catalog,
            ILogger logger,
            ISecurityRedactor redactor,
            IAuditSink auditSink,
            IToolExecutionPolicy policy)
            : this(catalog, logger, redactor, auditSink, policy, new AllowToolExecutionApprovalService(), new NoOpSecretBroker())
        {
        }

        public BridgeToolExecutor(
            IBridgeToolCatalog catalog,
            ILogger logger,
            ISecurityRedactor redactor,
            IAuditSink auditSink,
            IToolExecutionPolicy policy,
            IToolExecutionApprovalService approvalService)
            : this(catalog, logger, redactor, auditSink, policy, approvalService, new NoOpSecretBroker())
        {
        }

        public BridgeToolExecutor(
            IBridgeToolCatalog catalog,
            ILogger logger,
            ISecurityRedactor redactor,
            IAuditSink auditSink,
            IToolExecutionPolicy policy,
            IToolExecutionApprovalService approvalService,
            ISecretBroker secretBroker)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _redactor = redactor ?? throw new ArgumentNullException(nameof(redactor));
            _auditSink = auditSink ?? throw new ArgumentNullException(nameof(auditSink));
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
            _approvalService = approvalService ?? throw new ArgumentNullException(nameof(approvalService));
            _secretBroker = secretBroker ?? throw new ArgumentNullException(nameof(secretBroker));
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
            var initialContext = new ToolExecutionSecurityContext(request, descriptor: null);
            var secretReferences = initialContext.SecretReferences;
            var secretReferenceMetadata = FormatSecretReferences(secretReferences);
            _logger.LogTrace(
                $"Bridge tool secret reference metadata [ToolId={request.ToolId}] [RequestId={request.RequestId}] [OperationId={request.OperationId}] [SecretReferences={RedactValue(secretReferenceMetadata)}].");

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
                    requiredCapabilities: "None",
                    secretReferences: secretReferenceMetadata,
                    secretResolution: "NotEvaluated",
                    approvalRequirement: ToolExecutionApprovalRequirement.NotRequired,
                    approvalDecision: "NotEvaluated",
                    approvalReason: "Unknown tool",
                    stopwatch.ElapsedMilliseconds,
                    cancellationToken).ConfigureAwait(false);
                return result;
            }

            var requiredCapabilities = FormatCapabilities(tool.Descriptor.RequiredCapabilities);
            _logger.LogTrace(
                $"Bridge tool capability metadata [ToolId={request.ToolId}] [RequestId={request.RequestId}] [OperationId={request.OperationId}] [RequiredCapabilities={RedactValue(requiredCapabilities)}].");
            var securityContext = new ToolExecutionSecurityContext(request, tool.Descriptor);
            var policyDecision = await _policy.EvaluateAsync(securityContext, cancellationToken).ConfigureAwait(false)
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
                    requiredCapabilities: requiredCapabilities,
                    secretReferences: secretReferenceMetadata,
                    secretResolution: "NotEvaluated",
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
                    requiredCapabilities: requiredCapabilities,
                    secretReferences: secretReferenceMetadata,
                    secretResolution: "NotEvaluated",
                    approvalRequirement: tool.Descriptor.ApprovalRequirement,
                    approvalDecision: "Denied",
                    approvalReason: approvalDecision.Reason,
                    stopwatch.ElapsedMilliseconds,
                    cancellationToken).ConfigureAwait(false);
                return result;
            }

            var secretResolution = await ResolveSecretReferencesAsync(secretReferences, cancellationToken).ConfigureAwait(false);
            if (!secretResolution.Resolved)
            {
                stopwatch.Stop();
                var result = BridgeToolResult.Failed(request, "SecretReferenceUnresolved", RedactValue(secretResolution.Reason));
                _logger.LogWarning(
                    $"Bridge tool secret reference resolution failed [ToolId={request.ToolId}] [RequestId={request.RequestId}] [OperationId={request.OperationId}] [Reason={RedactValue(secretResolution.Reason)}] [ElapsedMs={stopwatch.ElapsedMilliseconds}].");
                await EmitAuditAsync(
                    request,
                    result,
                    allowed: false,
                    policyReason: policyDecision.Reason,
                    requiredCapabilities: requiredCapabilities,
                    secretReferences: secretReferenceMetadata,
                    secretResolution: secretResolution.Reason,
                    approvalRequirement: tool.Descriptor.ApprovalRequirement,
                    approvalDecision: GetApprovalDecisionName(tool.Descriptor, approvalDecision),
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
                    requiredCapabilities: requiredCapabilities,
                    secretReferences: secretReferenceMetadata,
                    secretResolution: secretResolution.Reason,
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
                    requiredCapabilities: requiredCapabilities,
                    secretReferences: secretReferenceMetadata,
                    secretResolution: secretResolution.Reason,
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
                    requiredCapabilities: requiredCapabilities,
                    secretReferences: secretReferenceMetadata,
                    secretResolution: secretResolution.Reason,
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
            string requiredCapabilities,
            string secretReferences,
            string secretResolution,
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
                            ["requiredCapabilities"] = RedactValue(requiredCapabilities),
                            ["secretReferences"] = RedactValue(secretReferences),
                            ["secretResolution"] = RedactValue(secretResolution),
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

        private async Task<SecretResolutionResult> ResolveSecretReferencesAsync(
            IReadOnlyList<ISecretReference> secretReferences,
            CancellationToken cancellationToken)
        {
            if (secretReferences.Count == 0)
                return SecretResolutionResult.ResolvedReference("No secret references");

            foreach (var reference in secretReferences)
            {
                var result = await _secretBroker.ResolveAsync(reference, cancellationToken).ConfigureAwait(false)
                    ?? SecretResolutionResult.Unresolved("Secret broker returned no result.");
                if (!result.Resolved)
                    return SecretResolutionResult.Unresolved($"Secret reference '{reference.ReferenceId}' was not resolved: {result.Reason}");
            }

            return SecretResolutionResult.ResolvedReference("Secret references resolved");
        }

        private static string FormatCapabilities(IReadOnlyList<BridgeCapability>? capabilities)
        {
            if (capabilities == null || capabilities.Count == 0)
                return "None";

            var names = capabilities
                .Where(capability => capability != null && !string.IsNullOrWhiteSpace(capability.Name))
                .Select(capability => capability.Name)
                .ToArray();

            return names.Length == 0 ? "None" : string.Join(",", names);
        }

        private static string FormatSecretReferences(IReadOnlyList<ISecretReference>? secretReferences)
        {
            if (secretReferences == null || secretReferences.Count == 0)
                return "None";

            var metadata = secretReferences
                .Where(reference => reference != null && !string.IsNullOrWhiteSpace(reference.ReferenceId))
                .Select(reference =>
                {
                    var redactionMetadata = new SecretRedactionMetadata(reference);
                    return string.IsNullOrWhiteSpace(redactionMetadata.Provider)
                        ? $"{redactionMetadata.Kind}:{redactionMetadata.ReferenceId}"
                        : $"{redactionMetadata.Kind}:{redactionMetadata.Provider}:{redactionMetadata.ReferenceId}";
                })
                .ToArray();

            return metadata.Length == 0 ? "None" : string.Join(",", metadata);
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
