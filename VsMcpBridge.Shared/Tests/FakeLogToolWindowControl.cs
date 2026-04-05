using VsMcpBridge.Shared.Interfaces;

namespace VsMcpBridge.Shared.Tests.Support;

public sealed class FakeLogToolWindowControl : ILogToolWindowControl
{
    public object DataContext { get; set; } = null!;
}
