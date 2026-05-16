using System.Text.RegularExpressions;

namespace VsMcpBridge.Shared.Security
{
    public sealed class BridgeSecurityRedactor : ISecurityRedactor
    {
        private const string Mask = "[REDACTED]";

        private static readonly Regex KeyValueSecretPattern = new Regex(
            @"(?i)(""?'?(?:apiKey|api_key|token|password|secret)""?'?\s*[:=]\s*)(""[^""]*""|'[^']*'|[^\s,;}\]]+)",
            RegexOptions.Compiled);

        private static readonly Regex BearerPattern = new Regex(
            @"(?i)\b(authorization\s*[:=]\s*bearer\s+|bearer\s+)([A-Za-z0-9._~+/=-]+)",
            RegexOptions.Compiled);

        public SecurityRedactionResult Redact(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return new SecurityRedactionResult(value ?? string.Empty, false);

            var redacted = KeyValueSecretPattern.Replace(value, match => $"{match.Groups[1].Value}{Mask}");
            redacted = BearerPattern.Replace(redacted, match => $"{match.Groups[1].Value}{Mask}");

            return new SecurityRedactionResult(redacted, redacted != value);
        }
    }
}
