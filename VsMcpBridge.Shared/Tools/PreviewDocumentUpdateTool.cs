using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VsMcpBridge.Shared.Security;

namespace VsMcpBridge.Shared.Tools
{
    public sealed class PreviewDocumentUpdateTool : IBridgeTool
    {
        public const string ToolId = "bridge.previewDocumentUpdate";

        private readonly string _repositoryRoot;

        public PreviewDocumentUpdateTool()
            : this(Directory.GetCurrentDirectory())
        {
        }

        public PreviewDocumentUpdateTool(string repositoryRoot)
        {
            if (string.IsNullOrWhiteSpace(repositoryRoot))
                throw new ArgumentException("Repository root must not be empty.", nameof(repositoryRoot));

            _repositoryRoot = Path.GetFullPath(repositoryRoot);
        }

        public BridgeToolDescriptor Descriptor { get; } = new BridgeToolDescriptor
        {
            Id = ToolId,
            Name = "Preview Document Update",
            Description = "Generates a deterministic preview and unified diff for an explicit document update without writing files.",
            Category = "DocumentPreview",
            Source = "Compiled",
            Host = "Shared",
            RequiredCapabilities = new[] { new BridgeCapability("workspace.previewDocumentUpdate") },
            ApprovalRequirement = ToolExecutionApprovalRequirement.NotRequired,
            RiskProfile = new BridgeToolRiskProfile
            {
                AuditCategoryHint = AuditEventCategory.DocumentPreview,
                SeverityHint = AuditSeverity.Informational,
                RiskLevelHint = AuditRiskLevel.Low
            }
        };

        public async Task<BridgeToolResult> ExecuteAsync(BridgeToolRequest request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            cancellationToken.ThrowIfCancellationRequested();

            if (!TryGetStringArgument(request, "targetPath", allowEmpty: false, out var targetPath))
                return Failed(request, "InvalidTargetPath", PreviewDocumentUpdateStatus.InvalidRequest, "Preview document update requires a non-empty repo-root-relative 'targetPath'.");

            if (!TryNormalizeTargetPath(targetPath!, out var normalizedTargetPath, out var fullPath, out var pathError))
                return Failed(request, "InvalidTargetPath", PreviewDocumentUpdateStatus.InvalidRequest, pathError);

            if (!File.Exists(fullPath))
                return Failed(request, "TargetNotFound", PreviewDocumentUpdateStatus.InvalidRequest, "Preview document update target was not found.");

            var hasExpectedContent = TryGetStringArgument(request, "expectedContent", allowEmpty: true, out var expectedContent);
            var hasExpectedHash = TryGetStringArgument(request, "expectedContentHash", allowEmpty: false, out var expectedContentHash);
            if (!hasExpectedContent && !hasExpectedHash)
                return Failed(request, "ExpectedContentRequired", PreviewDocumentUpdateStatus.InvalidRequest, "Preview document update requires 'expectedContent' or 'expectedContentHash'.");

            if (!TryGetStringArgument(request, "replacementContent", allowEmpty: true, out var replacementContent)
                && !TryGetStringArgument(request, "updateText", allowEmpty: true, out replacementContent)
                && !TryGetStringArgument(request, "replacementText", allowEmpty: true, out replacementContent))
            {
                return Failed(request, "InvalidRequest", PreviewDocumentUpdateStatus.InvalidRequest, "Preview document update requires 'replacementContent'.");
            }

            var currentContent = await ReadAllTextAsync(fullPath, cancellationToken).ConfigureAwait(false);
            var currentHash = ComputeSha256(currentContent);
            var expectedMatched = true;
            var expectedStateMode = hasExpectedContent ? "content" : "hash";

            if (hasExpectedContent && !string.Equals(currentContent, expectedContent, StringComparison.Ordinal))
                expectedMatched = false;

            if (hasExpectedHash && !string.Equals(currentHash, NormalizeHash(expectedContentHash!), StringComparison.OrdinalIgnoreCase))
                expectedMatched = false;

            if (!expectedMatched)
            {
                return Result(
                    request,
                    success: false,
                    errorCode: "DriftDetected",
                    status: PreviewDocumentUpdateStatus.DriftDetected,
                    message: "Preview document update detected drift between expected and current target content.",
                    normalizedTargetPath,
                    targetExists: true,
                    expectedMatched: false,
                    noOp: false,
                    currentHash,
                    expectedStateMode,
                    replacementContent: replacementContent!,
                    diff: string.Empty,
                    changedLineCount: 0);
            }

            if (string.Equals(currentContent, replacementContent, StringComparison.Ordinal))
            {
                return Result(
                    request,
                    success: true,
                    errorCode: string.Empty,
                    status: PreviewDocumentUpdateStatus.NoOp,
                    message: "No changes detected. No files were written.",
                    normalizedTargetPath,
                    targetExists: true,
                    expectedMatched: true,
                    noOp: true,
                    currentHash,
                    expectedStateMode,
                    replacementContent: replacementContent!,
                    diff: string.Empty,
                    changedLineCount: 0);
            }

            var diff = CreateUnifiedDiff(normalizedTargetPath, currentContent, replacementContent!);
            var changedLineCount = CountChangedDiffLines(diff);

            return Result(
                request,
                success: true,
                errorCode: string.Empty,
                status: PreviewDocumentUpdateStatus.PreviewGenerated,
                message: "Preview generated. No files were written.",
                normalizedTargetPath,
                targetExists: true,
                expectedMatched: true,
                noOp: false,
                currentHash,
                expectedStateMode,
                replacementContent: replacementContent!,
                diff,
                changedLineCount);
        }

        private BridgeToolResult Failed(BridgeToolRequest request, string errorCode, PreviewDocumentUpdateStatus status, string message)
        {
            var result = BridgeToolResult.Failed(request, errorCode, message);
            result.Data = new Dictionary<string, object?>
            {
                ["status"] = status.ToString(),
                ["previewOnly"] = true,
                ["approvalRequiredForApply"] = true
            };
            return result;
        }

        private static BridgeToolResult Result(
            BridgeToolRequest request,
            bool success,
            string errorCode,
            PreviewDocumentUpdateStatus status,
            string message,
            string targetPath,
            bool targetExists,
            bool expectedMatched,
            bool noOp,
            string currentHash,
            string expectedStateMode,
            string replacementContent,
            string diff,
            int changedLineCount)
        {
            var data = new Dictionary<string, object?>
            {
                ["status"] = status.ToString(),
                ["previewOnly"] = true,
                ["targetPath"] = targetPath,
                ["targetExists"] = targetExists,
                ["expectedMatched"] = expectedMatched,
                ["expectedStateMode"] = expectedStateMode,
                ["currentContentHash"] = currentHash,
                ["replacementContentHash"] = ComputeSha256(replacementContent),
                ["noOp"] = noOp,
                ["changedLineCount"] = changedLineCount,
                ["diff"] = diff,
                ["auditCategory"] = "DocumentPreview",
                ["approvalRequiredForApply"] = true
            };

            return new BridgeToolResult
            {
                ToolId = request.ToolId,
                RequestId = request.RequestId,
                OperationId = request.OperationId,
                Success = success,
                ErrorCode = errorCode,
                Message = message,
                Data = data
            };
        }

        private bool TryNormalizeTargetPath(string targetPath, out string normalizedTargetPath, out string fullPath, out string error)
        {
            normalizedTargetPath = string.Empty;
            fullPath = string.Empty;
            error = string.Empty;

            var normalized = targetPath.Trim().Replace('\\', '/').TrimStart('/');
            if (Path.IsPathRooted(targetPath) || normalized.IndexOf(':') >= 0)
            {
                error = "Preview document update targetPath must be repo-root-relative.";
                return false;
            }

            if (normalized.IndexOfAny(new[] { '*', '?' }) >= 0)
            {
                error = "Preview document update targetPath must be explicit and must not contain wildcards.";
                return false;
            }

            var segments = normalized.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0 || segments.Any(segment => segment == "." || segment == ".."))
            {
                error = "Preview document update targetPath must not contain current-directory or parent-directory segments.";
                return false;
            }

            fullPath = Path.GetFullPath(Path.Combine(_repositoryRoot, normalized.Replace('/', Path.DirectorySeparatorChar)));
            if (!IsUnderRepositoryRoot(fullPath))
            {
                error = "Preview document update targetPath must stay under the repository root.";
                return false;
            }

            normalizedTargetPath = normalized;
            return true;
        }

        private bool IsUnderRepositoryRoot(string path)
        {
            var root = _repositoryRoot.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? _repositoryRoot
                : _repositoryRoot + Path.DirectorySeparatorChar;
            return fullEquals(path, _repositoryRoot) || path.StartsWith(root, StringComparison.OrdinalIgnoreCase);

            static bool fullEquals(string left, string right) => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }

        private static string CreateUnifiedDiff(string targetPath, string before, string after)
        {
            var beforeLines = SplitLines(before);
            var afterLines = SplitLines(after);
            var builder = new StringBuilder();
            builder.Append("--- a/").Append(targetPath).Append('\n');
            builder.Append("+++ b/").Append(targetPath).Append('\n');
            builder.Append("@@ -1,").Append(beforeLines.Count).Append(" +1,").Append(afterLines.Count).Append(" @@").Append('\n');

            foreach (var line in beforeLines)
                builder.Append('-').Append(line).Append('\n');

            foreach (var line in afterLines)
                builder.Append('+').Append(line).Append('\n');

            return builder.ToString();
        }

        private static int CountChangedDiffLines(string diff)
            => SplitLines(diff).Count(line =>
                line.Length > 0
                && (line[0] == '-' || line[0] == '+')
                && !line.StartsWith("--- ", StringComparison.Ordinal)
                && !line.StartsWith("+++ ", StringComparison.Ordinal));

        private static IReadOnlyList<string> SplitLines(string text)
        {
            if (text.Length == 0)
                return Array.Empty<string>();

            var normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
            if (normalized.EndsWith("\n", StringComparison.Ordinal))
                normalized = normalized.Substring(0, normalized.Length - 1);

            return normalized.Split('\n');
        }

        private static bool TryGetStringArgument(BridgeToolRequest request, string key, bool allowEmpty, out string? value)
        {
            value = null;
            if (!request.Arguments.TryGetValue(key, out var rawValue) || rawValue == null)
                return false;

            value = rawValue as string ?? rawValue.ToString();
            return allowEmpty ? value != null : !string.IsNullOrWhiteSpace(value);
        }

        private static async Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, useAsync: true))
            using (var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
            {
                var content = await reader.ReadToEndAsync().ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                return content;
            }
        }

        private static string NormalizeHash(string hash)
        {
            var normalized = hash.Trim();
            return normalized.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase)
                ? normalized.Substring("sha256:".Length)
                : normalized;
        }

        private static string ComputeSha256(string text)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
                var builder = new StringBuilder(bytes.Length * 2);
                foreach (var value in bytes)
                    builder.Append(value.ToString("x2"));

                return builder.ToString();
            }
        }
    }

    public enum PreviewDocumentUpdateStatus
    {
        PreviewGenerated = 1,
        NoOp = 2,
        DriftDetected = 3,
        InvalidRequest = 4
    }
}
