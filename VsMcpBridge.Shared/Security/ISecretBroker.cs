using System.Threading;
using System.Threading.Tasks;

namespace VsMcpBridge.Shared.Security
{
    public interface ISecretBroker
    {
        Task<string?> ResolveAsync(ISecretReference reference, CancellationToken cancellationToken);
    }
}
