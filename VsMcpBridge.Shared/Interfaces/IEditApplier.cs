using System.Threading.Tasks;
using VsMcpBridge.Shared.Models;

namespace VsMcpBridge.Shared.Interfaces;

public interface IEditApplier
{
    Task ApplyAsync(EditProposal proposal);
}
