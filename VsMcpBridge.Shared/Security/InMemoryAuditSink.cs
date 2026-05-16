using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace VsMcpBridge.Shared.Security
{
    public sealed class InMemoryAuditSink : IAuditSink
    {
        private readonly object _gate = new object();
        private readonly List<BridgeAuditEnvelope> _events = new List<BridgeAuditEnvelope>();

        public IReadOnlyList<BridgeAuditEnvelope> Events
        {
            get
            {
                lock (_gate)
                    return _events.ToArray();
            }
        }

        public Task RecordAsync(BridgeAuditEnvelope envelope, CancellationToken cancellationToken)
        {
            lock (_gate)
                _events.Add(envelope);

            return Task.CompletedTask;
        }
    }
}
