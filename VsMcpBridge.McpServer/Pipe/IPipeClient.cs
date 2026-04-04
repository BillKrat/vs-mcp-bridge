using VsMcpBridge.Shared.Models;

namespace VsMcpBridge.McpServer.Pipe
{
    public interface IPipeClient
    {
        Task<GetActiveDocumentResponse> GetActiveDocumentAsync(CancellationToken ct = default);
        Task<GetErrorListResponse> GetErrorListAsync(CancellationToken ct = default);
        Task<GetSelectedTextResponse> GetSelectedTextAsync(CancellationToken ct = default);
        Task<ListSolutionProjectsResponse> ListSolutionProjectsAsync(CancellationToken ct = default);
        Task<ProposeTextEditResponse> ProposeTextEditAsync(string filePath, string originalText, string proposedText, CancellationToken ct = default);
    }
}