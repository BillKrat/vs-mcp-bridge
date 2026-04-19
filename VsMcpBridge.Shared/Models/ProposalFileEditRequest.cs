namespace VsMcpBridge.Shared.Models;

public sealed class ProposalFileEditRequest
{
    public string FilePath { get; set; } = string.Empty;
    public string OriginalText { get; set; } = string.Empty;
    public string ProposedText { get; set; } = string.Empty;
}
