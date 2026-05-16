using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        public IReadOnlyList<ISecretReference> SecretReferences => ExtractSecretReferences(Request.Arguments);

        private static IReadOnlyList<ISecretReference> ExtractSecretReferences(object? value)
        {
            var references = new List<ISecretReference>();
            CollectSecretReferences(value, references);
            return references
                .GroupBy(reference => $"{reference.Kind}:{reference.ReferenceId}", StringComparer.Ordinal)
                .Select(group => group.First())
                .ToArray();
        }

        private static void CollectSecretReferences(object? value, List<ISecretReference> references)
        {
            if (value == null || value is string)
                return;

            if (value is ISecretReference secretReference)
            {
                references.Add(secretReference);
                return;
            }

            if (value is IDictionary<string, object?> objectDictionary)
            {
                foreach (var item in objectDictionary.Values)
                    CollectSecretReferences(item, references);
                return;
            }

            if (value is IDictionary dictionary)
            {
                foreach (DictionaryEntry item in dictionary)
                    CollectSecretReferences(item.Value, references);
                return;
            }

            if (value is IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                    CollectSecretReferences(item, references);
            }
        }
    }
}
