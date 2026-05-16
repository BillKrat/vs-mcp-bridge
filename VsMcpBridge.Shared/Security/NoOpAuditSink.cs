using System.Threading;
using System.Threading.Tasks;

namespace VsMcpBridge.Shared.Security
{
    public sealed class NoOpAuditSink : IAuditSink
    {
        public Task RecordAsync(BridgeAuditEnvelope envelope, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
