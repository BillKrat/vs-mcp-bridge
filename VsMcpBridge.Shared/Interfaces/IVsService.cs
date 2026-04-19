using System.Collections.Generic;
using System.Threading.Tasks;
using VsMcpBridge.Shared.Models;

namespace VsMcpBridge.Shared.Interfaces;

public interface IVsService
{
    Task<GetActiveDocumentResponse> GetActiveDocumentAsync();
    Task<GetSelectedTextResponse> GetSelectedTextAsync();
    Task<ListSolutionProjectsResponse> ListSolutionProjectsAsync();
    Task<GetErrorListResponse> GetErrorListAsync();
    Task OpenGitChangesAsync();
    Task<ProposeTextEditResponse> ProposeTextEditAsync(string requestId, string filePath, string originalText, string proposedText);
    Task<ProposeTextEditResponse> ProposeTextEditsAsync(string requestId, IReadOnlyList<ProposalFileEditRequest> fileEdits);
}
