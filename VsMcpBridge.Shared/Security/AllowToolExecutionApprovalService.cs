using System.Threading;
using System.Threading.Tasks;

namespace VsMcpBridge.Shared.Security
{
    public sealed class AllowToolExecutionApprovalService : IToolExecutionApprovalService
    {
        public Task<ToolExecutionApprovalDecision> EvaluateAsync(ToolExecutionApprovalContext context, CancellationToken cancellationToken)
            => Task.FromResult(ToolExecutionApprovalDecision.Approve());
    }
}
