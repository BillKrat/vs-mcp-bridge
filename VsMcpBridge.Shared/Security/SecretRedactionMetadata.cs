namespace VsMcpBridge.Shared.Security
{
    public sealed class SecretRedactionMetadata
    {
        public SecretRedactionMetadata(ISecretReference reference)
        {
            ReferenceId = reference?.ReferenceId ?? string.Empty;
            Kind = reference is SecretReference secretReference ? secretReference.Kind : SecretReferenceKind.Unknown;
            Provider = reference is SecretReference providerReference ? providerReference.Provider : string.Empty;
        }

        public string ReferenceId { get; }

        public SecretReferenceKind Kind { get; }

        public string Provider { get; }
    }
}
