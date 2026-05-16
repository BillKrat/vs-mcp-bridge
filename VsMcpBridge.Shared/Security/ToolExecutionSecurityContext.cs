using System.Collections.Generic;
using System;
using VsMcpBridge.Shared.Tools;

namespace VsMcpBridge.Shared.Security
{
    public sealed class ToolExecutionSecurityContext
    {
        public ToolExecutionSecurityContext(BridgeToolRequest request, BridgeToolDescriptor? descriptor)
        {
            Request = request;
            Descriptor = descriptor;
        }

        public BridgeToolRequest Request { get; }

        public BridgeToolDescriptor? Descriptor { get; }

        public string ToolId => Request.ToolId;

        public string RequestId => Request.RequestId;

        public string OperationId => Request.OperationId;

        public IReadOnlyDictionary<string, object?> Arguments => Request.Arguments;

        public IReadOnlyList<BridgeCapability> RequiredCapabilities => Descriptor?.RequiredCapabilities ?? Array.Empty<BridgeCapability>();
    }
}
