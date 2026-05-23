using System.Collections.Generic;

namespace VsMcpBridge.Shared.AdventuresAuth
{
    public sealed class AdventuresAuthPrincipal
    {
        public AdventuresAuthPrincipal(
            string subject,
            string displayName,
            IReadOnlyDictionary<string, string>? claims = null)
        {
            Subject = subject;
            DisplayName = displayName;
            Claims = claims ?? new Dictionary<string, string>();
        }

        public string Subject { get; }

        public string DisplayName { get; }

        public IReadOnlyDictionary<string, string> Claims { get; }
    }
}
