using System.Threading;
using System.Threading.Tasks;

namespace VsMcpBridge.Shared.Security
{
    public sealed class NoOpSecretBroker : ISecretBroker
    {
        public Task<SecretResolutionResult> ResolveAsync(ISecretReference reference, CancellationToken cancellationToken)
            => Task.FromResult(SecretResolutionResult.Unresolved("No secret broker is configured."));
    }
}
