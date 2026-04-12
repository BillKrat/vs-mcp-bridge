using System;
using System.Threading.Tasks;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.Models;

namespace VsMcpBridge.Shared.Tests.Support;

public sealed class DriftingEditApplier : IEditApplier
{
    public int Calls { get; private set; }

    public Task ApplyAsync(EditProposal proposal)
    {
        Calls++;
        throw new InvalidOperationException("Target document no longer matches the approved proposal.");
    }
}
