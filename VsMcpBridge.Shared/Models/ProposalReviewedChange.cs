namespace VsMcpBridge.Shared.Models;

public sealed class ProposalReviewedChange
{
    public int SequenceNumber { get; set; }
    public string OriginalSegment { get; set; } = string.Empty;
    public string UpdatedSegment { get; set; } = string.Empty;
}
