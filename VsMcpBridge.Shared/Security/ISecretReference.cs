namespace VsMcpBridge.Shared.Security
{
    public interface ISecretReference
    {
        string ReferenceId { get; }

        SecretReferenceKind Kind { get; }
    }
}
