using System.Threading;
using System.Threading.Tasks;

namespace VsMcpBridge.Shared.Interfaces;

public interface IChatRequestService
{
    Task<string> SendAsync(string message, string? requestId = null, CancellationToken cancellationToken = default);
}
