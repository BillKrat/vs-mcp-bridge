using System.Threading.Tasks;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.Models;

namespace VsMcpBridge.App.Services;

internal sealed class NullVsService : IVsService
{
    public Task<GetActiveDocumentResponse> GetActiveDocumentAsync() =>
        Task.FromResult(new GetActiveDocumentResponse { Success = true });

    public Task<GetSelectedTextResponse> GetSelectedTextAsync() =>
        Task.FromResult(new GetSelectedTextResponse { Success = true });

    public Task<ListSolutionProjectsResponse> ListSolutionProjectsAsync() =>
        Task.FromResult(new ListSolutionProjectsResponse { Success = true });

    public Task<GetErrorListResponse> GetErrorListAsync() =>
        Task.FromResult(new GetErrorListResponse { Success = true });

    public Task<ProposeTextEditResponse> ProposeTextEditAsync(
        string requestId, string filePath, string originalText, string proposedText) =>
        Task.FromResult(new ProposeTextEditResponse { Success = true });
}
