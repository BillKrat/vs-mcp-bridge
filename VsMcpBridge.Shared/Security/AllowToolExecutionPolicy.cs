using System.Threading;
using System.Threading.Tasks;

namespace VsMcpBridge.Shared.Security
{
    public sealed class AllowToolExecutionPolicy : IToolExecutionPolicy
    {
        public Task<ToolExecutionPolicyDecision> EvaluateAsync(ToolExecutionSecurityContext context, CancellationToken cancellationToken)
            => Task.FromResult(ToolExecutionPolicyDecision.Allow());
    }
}
