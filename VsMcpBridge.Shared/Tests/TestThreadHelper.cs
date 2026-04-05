using System;
using System.Threading.Tasks;
using VsMcpBridge.Shared.Interfaces;

namespace VsMcpBridge.Shared.Tests.Support;

public sealed class TestThreadHelper : IThreadHelper
{
    public bool HasAccess { get; set; } = true;
    public int RunCalls { get; private set; }
    public int SwitchCalls { get; private set; }
    public int ThrowIfNotOnUiThreadCalls { get; private set; }

    public bool CheckAccess() => HasAccess;

    public void Run(Func<Task> value)
    {
        RunCalls++;
        value().GetAwaiter().GetResult();
    }

    public Task SwitchToMainThreadAsync()
    {
        SwitchCalls++;
        HasAccess = true;
        return Task.CompletedTask;
    }

    public void ThrowIfNotOnUIThread() => ThrowIfNotOnUiThreadCalls++;
}
