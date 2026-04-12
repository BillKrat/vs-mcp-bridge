using VsMcpBridge.Shared.Interfaces;

namespace VsMcpBridge.Shared.Tests.Support;

public sealed class StubProposalFilePicker : IProposalFilePicker
{
    public int Calls { get; private set; }
    public string? SelectedPath { get; set; }

    public string? PickFilePath()
    {
        Calls++;
        return SelectedPath;
    }
}
