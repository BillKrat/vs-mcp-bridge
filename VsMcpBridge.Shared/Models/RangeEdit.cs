namespace VsMcpBridge.Shared.Models;

public sealed class RangeEdit
{
    public int StartIndex { get; set; }
    public string OriginalSegment { get; set; } = string.Empty;
    public string UpdatedSegment { get; set; } = string.Empty;
    public string PrefixContext { get; set; } = string.Empty;
    public string SuffixContext { get; set; } = string.Empty;
}
