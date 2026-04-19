using System.Threading.Tasks;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.Models;
using VsMcpBridge.Shared.Services;

namespace VsMcpBridge.Shared.Tests.Support;

public sealed class AmbiguousEditApplier : IEditApplier
{
    public int Calls { get; private set; }

    public Task<EditApplyResult> ApplyAsync(EditProposal proposal)
    {
        Calls++;
        throw new AmbiguousEditTargetException();
    }
}
