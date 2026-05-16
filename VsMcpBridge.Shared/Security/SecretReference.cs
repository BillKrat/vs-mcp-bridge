namespace VsMcpBridge.Shared.Security
{
    public sealed class SecretReference : ISecretReference
    {
        public SecretReference(string referenceId, SecretReferenceKind kind = SecretReferenceKind.Named, string provider = "")
        {
            ReferenceId = referenceId ?? string.Empty;
            Kind = kind;
            Provider = provider ?? string.Empty;
        }

        public string ReferenceId { get; }

        public SecretReferenceKind Kind { get; }

        public string Provider { get; }
    }
}
