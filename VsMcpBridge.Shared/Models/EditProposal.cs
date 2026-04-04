namespace VsMcpBridge.Shared.Models;

public sealed class EditProposal
{
    public string RequestId { get; set; } = string.Empty;
    public string ProposalId { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string Diff { get; set; } = string.Empty;
    public ProposalStatus Status { get; set; }
}
