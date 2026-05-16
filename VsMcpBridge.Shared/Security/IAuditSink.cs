using System.Threading;
using System.Threading.Tasks;

namespace VsMcpBridge.Shared.Security
{
    public interface IAuditSink
    {
        Task RecordAsync(BridgeAuditEnvelope envelope, CancellationToken cancellationToken);
    }
}
