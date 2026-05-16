namespace VsMcpBridge.Shared.Security
{
    public interface ISecurityRedactor
    {
        SecurityRedactionResult Redact(string? value);
    }
}
