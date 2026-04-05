using System;
using System.Threading.Tasks;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.Models;

namespace VsMcpBridge.Shared.Tests.Support;

public sealed class ThrowingEditApplier : IEditApplier
{
    public int Calls { get; private set; }

    public Task ApplyAsync(EditProposal proposal)
    {
        Calls++;
        throw new InvalidOperationException("Boom from ApplyAsync.");
    }
}
