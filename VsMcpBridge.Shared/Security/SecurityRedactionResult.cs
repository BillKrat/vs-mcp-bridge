namespace VsMcpBridge.Shared.Security
{
    public sealed class SecurityRedactionResult
    {
        public SecurityRedactionResult(string value, bool wasRedacted)
        {
            Value = value;
            WasRedacted = wasRedacted;
        }

        public string Value { get; }

        public bool WasRedacted { get; }
    }
}
