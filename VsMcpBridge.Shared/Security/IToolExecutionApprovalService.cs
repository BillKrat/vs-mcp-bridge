using System.Threading;
using System.Threading.Tasks;

namespace VsMcpBridge.Shared.Security
{
    public interface IToolExecutionApprovalService
    {
        Task<ToolExecutionApprovalDecision> EvaluateAsync(ToolExecutionApprovalContext context, CancellationToken cancellationToken);
    }
}
