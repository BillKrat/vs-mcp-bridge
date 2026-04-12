using System.Threading.Tasks;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.Models;

namespace VsMcpBridge.Shared.Tests.Support;

public sealed class SkippingEditApplier : IEditApplier
{
    public int Calls { get; private set; }

    public Task<EditApplyResult> ApplyAsync(EditProposal proposal)
    {
        Calls++;
        return Task.FromResult(EditApplyResult.SkippedAlreadyMatchesApprovedUpdatedContent);
    }
}
