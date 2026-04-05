using System.Collections.Generic;
using System.Threading.Tasks;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.Models;

namespace VsMcpBridge.Shared.Tests.Support;

public sealed class RecordingEditApplier : IEditApplier
{
    public List<EditProposal> AppliedProposals { get; } = new List<EditProposal>();

    public Task ApplyAsync(EditProposal proposal)
    {
        AppliedProposals.Add(proposal);
        return Task.CompletedTask;
    }
}
