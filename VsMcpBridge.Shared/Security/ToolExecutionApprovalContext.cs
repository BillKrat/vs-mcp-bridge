using System.Collections.Generic;
using VsMcpBridge.Shared.Tools;

namespace VsMcpBridge.Shared.Security
{
    public sealed class ToolExecutionApprovalContext
    {
        public ToolExecutionApprovalContext(BridgeToolRequest request, BridgeToolDescriptor descriptor, ToolExecutionPolicyDecision policyDecision)
        {
            Request = request;
            Descriptor = descriptor;
            PolicyDecision = policyDecision;
        }

        public BridgeToolRequest Request { get; }

        public BridgeToolDescriptor Descriptor { get; }

        public BridgeToolManifest Manifest => Descriptor.Manifest;

        public ToolExecutionPolicyDecision PolicyDecision { get; }

        public string ToolId => Request.ToolId;

        public string RequestId => Request.RequestId;

        public string OperationId => Request.OperationId;

        public IReadOnlyDictionary<string, object?> Arguments => Request.Arguments;
    }
}
