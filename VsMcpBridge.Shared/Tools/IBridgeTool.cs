using System.Threading;
using System.Threading.Tasks;

namespace VsMcpBridge.Shared.Tools
{
    public interface IBridgeTool
    {
        BridgeToolDescriptor Descriptor { get; }

        Task<BridgeToolResult> ExecuteAsync(BridgeToolRequest request, CancellationToken cancellationToken);
    }
}
