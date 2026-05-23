using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using VsMcpBridge.Shared.Security;

namespace VsMcpBridge.Shared.Tools
{
    public sealed class GatedHandoffPreviewTool : IBridgeTool
    {
        public const string ToolId = "bridge.gatedHandoffPreview";

        private const string RedactedValue = "[REDACTED]";

        private static readonly Regex SecretAssignmentPattern = new Regex(
            @"\b(password|passwd|pwd|token|secret|api[-_ ]?key|authorization|cookie)\b\s*[:=]\s*([^\r\n,;]+)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex BearerTokenPattern = new Regex(
            @"\bBearer\s+[A-Za-z0-9._~+/=\-]+",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public BridgeToolDescriptor Descriptor { get; } = new BridgeToolDescriptor
        {
            Id = ToolId,
            Name = "Gated Handoff Preview",
            Description = "Generates a structured preview for a gated ChatGPT to Codex handoff without executing Codex, running commands, or mutating state.",
            Category = "CodexHandoffPreview",
            Source = "Compiled",
            Host = "Shared",
            RequiredCapabilities = new[] { new BridgeCapability("codex.gatedHandoffPreview") },
            ApprovalRequirement = ToolExecutionApprovalRequirement.NotRequired,
            RiskProfile = new BridgeToolRiskProfile
            {
                AuditCategoryHint = AuditEventCategory.CodexHandoffPreview,
                SeverityHint = AuditSeverity.Informational,
                RiskLevelHint = AuditRiskLevel.Low
            }
        };

        public Task<BridgeToolResult> ExecuteAsync(BridgeToolRequest request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            cancellationToken.ThrowIfCancellationRequested();
            EnsureCorrelationMetadata(request);

            var redactionApplied = false;
            var previewRequest = ReadPreviewRequest(request, ref redactionApplied);
            if (string.IsNullOrWhiteSpace(previewRequest.SliceObjective))
                return Task.FromResult(Failed(request, "InvalidRequest", "Gated handoff preview requires a non-empty 'sliceObjective'."));

            if (string.IsNullOrWhiteSpace(previewRequest.RepoPath))
                return Task.FromResult(Failed(request, "InvalidRequest", "Gated handoff preview requires a non-empty 'repoPath'."));

            if (previewRequest.ValidationRequirements.Count == 0)
                return Task.FromResult(Failed(request, "InvalidRequest", "Gated handoff preview requires at least one validation requirement."));

            var result = BuildPreviewResult(request, previewRequest, redactionApplied);
            return Task.FromResult(BridgeToolResult.Succeeded(
                request,
                "Gated handoff preview generated. No commands were run and no state was mutated.",
                result.ToData()));
        }

        private static GatedHandoffPreviewRequest ReadPreviewRequest(BridgeToolRequest request, ref bool redactionApplied)
        {
            var arguments = request.Arguments ?? new Dictionary<string, object?>();
            return new GatedHandoffPreviewRequest
            {
                SliceObjective = ReadString(arguments, ref redactionApplied, "sliceObjective", "objective", "taskObjective"),
                RepoPath = ReadString(arguments, ref redactionApplied, "repoPath", "repositoryPath", "repoTarget"),
                Constraints = ReadStringList(arguments, ref redactionApplied, "constraints", "nonGoalsAndConstraints"),
                NonGoals = ReadStringList(arguments, ref redactionApplied, "nonGoals", "non-goals"),
                ValidationRequirements = ReadStringList(arguments, ref redactionApplied, "validationRequirements", "validationChecklist", "validationPlan"),
                ExpectedArtifacts = ReadStringList(arguments, ref redactionApplied, "expectedArtifacts", "artifacts"),
                DeploymentRestrictions = ReadStringList(arguments, ref redactionApplied, "deploymentRestrictions"),
                RiskFlags = ReadStringList(arguments, ref redactionApplied, "riskFlags"),
                Notes = ReadString(arguments, ref redactionApplied, "notes", "context")
            };
        }

        private static GatedHandoffPreviewResult BuildPreviewResult(
            BridgeToolRequest request,
            GatedHandoffPreviewRequest previewRequest,
            bool redactionApplied)
        {
            var riskFlags = DetectRiskFlags(previewRequest).ToList();
            foreach (var providedFlag in previewRequest.RiskFlags)
            {
                if (!riskFlags.Contains(providedFlag, StringComparer.Ordinal))
                    riskFlags.Add(providedFlag);
            }

            var stopConditions = BuildStopConditions(riskFlags).ToArray();
            var approvalReminder = "User approval is required outside this preview before any Codex execution, command execution, repo mutation, deployment, or next slice proceeds.";
            var redactionNotice = redactionApplied
                ? "Secret-shaped input was redacted in the preview result."
                : "No secret-shaped input was detected by the preview tool.";

            return new GatedHandoffPreviewResult
            {
                Status = GatedHandoffPreviewStatus.PreviewGenerated.ToString(),
                PreviewOnly = true,
                CorrelationId = request.RequestId,
                RequestId = request.RequestId,
                OperationId = request.OperationId,
                TaskSummary = previewRequest.SliceObjective,
                ScopedTaskText = BuildScopedTaskText(previewRequest, stopConditions, approvalReminder, redactionNotice),
                RepoPath = previewRequest.RepoPath,
                Constraints = previewRequest.Constraints,
                NonGoals = previewRequest.NonGoals,
                ValidationChecklist = previewRequest.ValidationRequirements,
                ExpectedArtifacts = previewRequest.ExpectedArtifacts,
                DeploymentRestrictions = previewRequest.DeploymentRestrictions,
                StopConditions = stopConditions,
                RiskFlags = riskFlags.ToArray(),
                ApprovalReminder = approvalReminder,
                RedactionNotice = redactionNotice,
                RedactionApplied = redactionApplied,
                CodexExecutionInvoked = false,
                CommandExecutionPerformed = false,
                RepoMutationPerformed = false,
                DeploymentPerformed = false,
                BackgroundWorkflowStarted = false,
                ApprovalRequiredBeforeExecution = true,
                AuditCategory = AuditEventCategory.CodexHandoffPreview.ToString()
            };
        }

        private static BridgeToolResult Failed(BridgeToolRequest request, string errorCode, string message)
        {
            var result = BridgeToolResult.Failed(request, errorCode, message);
            result.Data = new Dictionary<string, object?>
            {
                ["status"] = GatedHandoffPreviewStatus.InvalidRequest.ToString(),
                ["previewOnly"] = true,
                ["codexExecutionInvoked"] = false,
                ["commandExecutionPerformed"] = false,
                ["repoMutationPerformed"] = false,
                ["deploymentPerformed"] = false,
                ["backgroundWorkflowStarted"] = false
            };
            return result;
        }

        private static void EnsureCorrelationMetadata(BridgeToolRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RequestId))
                request.RequestId = Guid.NewGuid().ToString("N");

            if (string.IsNullOrWhiteSpace(request.OperationId))
                request.OperationId = Guid.NewGuid().ToString("N");
        }

        private static string ReadString(IReadOnlyDictionary<string, object?> arguments, ref bool redactionApplied, params string[] names)
        {
            if (!TryGetArgument(arguments, names, out var key, out var value))
                return string.Empty;

            return SanitizeValue(key, ConvertToString(value), ref redactionApplied);
        }

        private static IReadOnlyList<string> ReadStringList(IReadOnlyDictionary<string, object?> arguments, ref bool redactionApplied, params string[] names)
        {
            if (!TryGetArgument(arguments, names, out var key, out var value))
                return Array.Empty<string>();

            var values = new List<string>();
            foreach (var item in ConvertToStringList(value))
            {
                var sanitized = SanitizeValue(key, item, ref redactionApplied);
                if (!string.IsNullOrWhiteSpace(sanitized))
                    values.Add(sanitized);
            }

            return values;
        }

        private static bool TryGetArgument(IReadOnlyDictionary<string, object?> arguments, IReadOnlyList<string> names, out string key, out object? value)
        {
            foreach (var name in names)
            {
                foreach (var pair in arguments)
                {
                    if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
                    {
                        key = pair.Key;
                        value = pair.Value;
                        return true;
                    }
                }
            }

            key = string.Empty;
            value = null;
            return false;
        }

        private static string ConvertToString(object? value)
        {
            if (value == null)
                return string.Empty;

            if (value is string text)
                return text;

            if (value is JsonElement element)
                return ConvertJsonElementToString(element);

            return value.ToString() ?? string.Empty;
        }

        private static IReadOnlyList<string> ConvertToStringList(object? value)
        {
            if (value == null)
                return Array.Empty<string>();

            if (value is string text)
                return new[] { text };

            if (value is JsonElement element)
                return ConvertJsonElementToStringList(element);

            if (value is IEnumerable enumerable)
            {
                var values = new List<string>();
                foreach (var item in enumerable)
                    values.Add(ConvertToString(item));

                return values;
            }

            return new[] { ConvertToString(value) };
        }

        private static string ConvertJsonElementToString(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.String)
                return element.GetString() ?? string.Empty;

            if (element.ValueKind == JsonValueKind.Null || element.ValueKind == JsonValueKind.Undefined)
                return string.Empty;

            return element.ToString();
        }

        private static IReadOnlyList<string> ConvertJsonElementToStringList(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Array)
                return element.EnumerateArray().Select(ConvertJsonElementToString).ToArray();

            var value = ConvertJsonElementToString(element);
            return string.IsNullOrWhiteSpace(value) ? Array.Empty<string>() : new[] { value };
        }

        private static string SanitizeValue(string key, string value, ref bool redactionApplied)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            if (IsSecretKey(key))
            {
                redactionApplied = true;
                return RedactedValue;
            }

            var redacted = SecretAssignmentPattern.Replace(value, match => match.Groups[1].Value + "=" + RedactedValue);
            redacted = BearerTokenPattern.Replace(redacted, "Bearer " + RedactedValue);
            if (!string.Equals(redacted, value, StringComparison.Ordinal))
                redactionApplied = true;

            return redacted;
        }

        private static bool IsSecretKey(string key)
            => ContainsAny(key, "password", "passwd", "pwd", "token", "secret", "apikey", "apiKey", "authorization", "cookie");

        private static IEnumerable<string> DetectRiskFlags(GatedHandoffPreviewRequest request)
        {
            var text = string.Join(
                "\n",
                new[]
                {
                    request.SliceObjective,
                    request.RepoPath,
                    request.Notes
                }
                .Concat(request.Constraints)
                .Concat(request.NonGoals)
                .Concat(request.ValidationRequirements)
                .Concat(request.ExpectedArtifacts)
                .Concat(request.DeploymentRestrictions)
                .Concat(request.RiskFlags));

            var flags = new HashSet<string>(StringComparer.Ordinal);
            AddRiskFlag(flags, text, "Deployment", "deploy", "deployment", "publish", "webdeploy");
            AddRiskFlag(flags, text, "DestructiveGit", "git reset", "reset --hard", "git clean", "force push", "--force", "delete branch");
            AddRiskFlag(flags, text, "ProductionAuth", "production auth", "oauth", "openid", "rbac", "auth middleware", "cookies/session", "session topology");
            AddRiskFlag(flags, text, "SecretHandling", "password", "token", "secret", "api key", "apikey", "authorization", "bearer", "cookie");
            AddRiskFlag(flags, text, "CommandExecution", "run command", "execute command", "powershell", "bash", "shell command", "command execution");
            AddRiskFlag(flags, text, "RepoMutation", "repo write", "repo mutation", "write files", "edit files", "create file", "delete file", "commit", "push");
            AddRiskFlag(flags, text, "CodexExecution", "invoke codex", "execute codex", "run codex", "submit to codex", "codex execution");
            AddRiskFlag(flags, text, "BackgroundWorkflow", "background", "wait loop", "polling loop", "auto-continue", "auto continuation");

            return flags.OrderBy(flag => flag, StringComparer.Ordinal);
        }

        private static void AddRiskFlag(ISet<string> flags, string text, string flag, params string[] patterns)
        {
            if (ContainsAny(text, patterns))
                flags.Add(flag);
        }

        private static bool ContainsAny(string text, params string[] patterns)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            foreach (var pattern in patterns)
            {
                if (text.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        private static IReadOnlyList<string> BuildStopConditions(IReadOnlyList<string> riskFlags)
        {
            var stopConditions = new List<string>
            {
                "Stop before any Codex invocation; this tool only returns a preview.",
                "Stop before running commands or starting background work.",
                "Stop before writing, editing, deleting, committing, or pushing repo files.",
                "Stop before deployment; deployment requires a separate explicit approval."
            };

            if (riskFlags.Contains("DestructiveGit"))
                stopConditions.Add("Stop on destructive git scope unless the user explicitly approves the exact command.");

            if (riskFlags.Contains("ProductionAuth"))
                stopConditions.Add("Stop on production auth expansion until a separate design and approval gate exists.");

            if (riskFlags.Contains("SecretHandling"))
                stopConditions.Add("Stop before exposing or persisting secrets; keep secrets externalized and redacted.");

            return stopConditions;
        }

        private static string BuildScopedTaskText(
            GatedHandoffPreviewRequest request,
            IReadOnlyList<string> stopConditions,
            string approvalReminder,
            string redactionNotice)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Scoped gated handoff proposal");
            builder.AppendLine();
            builder.AppendLine("Objective:");
            builder.AppendLine(request.SliceObjective);
            builder.AppendLine();
            builder.AppendLine("Repo path:");
            builder.AppendLine(request.RepoPath);
            AppendSection(builder, "Constraints/non-goals:", request.Constraints.Concat(request.NonGoals));
            AppendSection(builder, "Validation checklist:", request.ValidationRequirements);
            AppendSection(builder, "Expected artifacts:", request.ExpectedArtifacts);
            AppendSection(builder, "Deployment restrictions:", request.DeploymentRestrictions);
            AppendSection(builder, "Stop conditions:", stopConditions);
            builder.AppendLine();
            builder.AppendLine("Approval:");
            builder.AppendLine(approvalReminder);
            builder.AppendLine();
            builder.AppendLine("Redaction:");
            builder.AppendLine(redactionNotice);
            return builder.ToString().TrimEnd();
        }

        private static void AppendSection(StringBuilder builder, string heading, IEnumerable<string> values)
        {
            var list = values.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
            if (list.Length == 0)
                return;

            builder.AppendLine();
            builder.AppendLine(heading);
            foreach (var value in list)
                builder.AppendLine("- " + value);
        }
    }

    public sealed class GatedHandoffPreviewRequest
    {
        public string SliceObjective { get; set; } = string.Empty;

        public string RepoPath { get; set; } = string.Empty;

        public IReadOnlyList<string> Constraints { get; set; } = Array.Empty<string>();

        public IReadOnlyList<string> NonGoals { get; set; } = Array.Empty<string>();

        public IReadOnlyList<string> ValidationRequirements { get; set; } = Array.Empty<string>();

        public IReadOnlyList<string> ExpectedArtifacts { get; set; } = Array.Empty<string>();

        public IReadOnlyList<string> DeploymentRestrictions { get; set; } = Array.Empty<string>();

        public IReadOnlyList<string> RiskFlags { get; set; } = Array.Empty<string>();

        public string Notes { get; set; } = string.Empty;
    }

    public sealed class GatedHandoffPreviewResult
    {
        public string Status { get; set; } = string.Empty;

        public bool PreviewOnly { get; set; }

        public string CorrelationId { get; set; } = string.Empty;

        public string RequestId { get; set; } = string.Empty;

        public string OperationId { get; set; } = string.Empty;

        public string TaskSummary { get; set; } = string.Empty;

        public string ScopedTaskText { get; set; } = string.Empty;

        public string RepoPath { get; set; } = string.Empty;

        public IReadOnlyList<string> Constraints { get; set; } = Array.Empty<string>();

        public IReadOnlyList<string> NonGoals { get; set; } = Array.Empty<string>();

        public IReadOnlyList<string> ValidationChecklist { get; set; } = Array.Empty<string>();

        public IReadOnlyList<string> ExpectedArtifacts { get; set; } = Array.Empty<string>();

        public IReadOnlyList<string> DeploymentRestrictions { get; set; } = Array.Empty<string>();

        public IReadOnlyList<string> StopConditions { get; set; } = Array.Empty<string>();

        public IReadOnlyList<string> RiskFlags { get; set; } = Array.Empty<string>();

        public string ApprovalReminder { get; set; } = string.Empty;

        public string RedactionNotice { get; set; } = string.Empty;

        public bool RedactionApplied { get; set; }

        public bool CodexExecutionInvoked { get; set; }

        public bool CommandExecutionPerformed { get; set; }

        public bool RepoMutationPerformed { get; set; }

        public bool DeploymentPerformed { get; set; }

        public bool BackgroundWorkflowStarted { get; set; }

        public bool ApprovalRequiredBeforeExecution { get; set; }

        public string AuditCategory { get; set; } = string.Empty;

        public IReadOnlyDictionary<string, object?> ToData()
            => new Dictionary<string, object?>
            {
                ["status"] = Status,
                ["previewOnly"] = PreviewOnly,
                ["correlationId"] = CorrelationId,
                ["requestId"] = RequestId,
                ["operationId"] = OperationId,
                ["taskSummary"] = TaskSummary,
                ["scopedTaskText"] = ScopedTaskText,
                ["repoPath"] = RepoPath,
                ["constraints"] = Constraints,
                ["nonGoals"] = NonGoals,
                ["validationChecklist"] = ValidationChecklist,
                ["expectedArtifacts"] = ExpectedArtifacts,
                ["deploymentRestrictions"] = DeploymentRestrictions,
                ["stopConditions"] = StopConditions,
                ["riskFlags"] = RiskFlags,
                ["approvalReminder"] = ApprovalReminder,
                ["redactionNotice"] = RedactionNotice,
                ["redactionApplied"] = RedactionApplied,
                ["codexExecutionInvoked"] = CodexExecutionInvoked,
                ["commandExecutionPerformed"] = CommandExecutionPerformed,
                ["repoMutationPerformed"] = RepoMutationPerformed,
                ["deploymentPerformed"] = DeploymentPerformed,
                ["backgroundWorkflowStarted"] = BackgroundWorkflowStarted,
                ["approvalRequiredBeforeExecution"] = ApprovalRequiredBeforeExecution,
                ["auditCategory"] = AuditCategory
            };
    }

    public enum GatedHandoffPreviewStatus
    {
        PreviewGenerated,
        InvalidRequest
    }
}
