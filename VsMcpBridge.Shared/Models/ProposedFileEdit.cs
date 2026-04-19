using System.Collections.Generic;

namespace VsMcpBridge.Shared.Models;

public sealed class ProposedFileEdit
{
    public string FilePath { get; set; } = string.Empty;
    public string Diff { get; set; } = string.Empty;
    public RangeEdit? RangeEdit { get; set; }
    public List<RangeEdit>? RangeEdits { get; set; }
}
