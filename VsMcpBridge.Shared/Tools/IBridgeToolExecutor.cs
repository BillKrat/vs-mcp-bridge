using System.Threading;
using System.Threading.Tasks;

namespace VsMcpBridge.Shared.Tools
{
    public interface IBridgeToolExecutor
    {
        Task<BridgeToolResult> ExecuteAsync(BridgeToolRequest request, CancellationToken cancellationToken);
    }
}
