using System;
using System.Collections.Generic;

namespace VsMcpBridge.Shared.Security
{
    public sealed class CapabilityToolExecutionPolicyOptions
    {
        public ISet<string> AllowedCapabilities { get; } = new HashSet<string>(StringComparer.Ordinal);

        public ISet<string> DeniedCapabilities { get; } = new HashSet<string>(StringComparer.Ordinal);

        public bool DenyUnknownRequiredCapabilities { get; set; }
    }
}
