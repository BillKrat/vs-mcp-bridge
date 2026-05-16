using System.Threading;
using System.Threading.Tasks;

namespace VsMcpBridge.Shared.Security
{
    public interface ISecretBroker
    {
        Task<SecretResolutionResult> ResolveAsync(ISecretReference reference, CancellationToken cancellationToken);
    }
}
