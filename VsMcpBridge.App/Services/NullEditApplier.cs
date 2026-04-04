using System.Threading.Tasks;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.Models;

namespace VsMcpBridge.App.Services;

internal sealed class NullEditApplier : IEditApplier
{
    public Task ApplyAsync(EditProposal proposal) => Task.CompletedTask;
}
