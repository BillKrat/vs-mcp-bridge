using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VsMcpBridge.Shared.Models;

namespace VsMcpBridge.Shared.Interfaces;

public interface IPipeClient
{
    Task<GetActiveDocumentResponse> GetActiveDocumentAsync(CancellationToken ct = default);
    Task<GetErrorListResponse> GetErrorListAsync(CancellationToken ct = default);
    Task<GetSelectedTextResponse> GetSelectedTextAsync(CancellationToken ct = default);
    Task<ListSolutionProjectsResponse> ListSolutionProjectsAsync(CancellationToken ct = default);
    Task<ProposeTextEditResponse> ProposeTextEditAsync(string filePath, string originalText, string proposedText, CancellationToken ct = default);
    Task<ProposeTextEditResponse> ProposeTextEditsAsync(IReadOnlyList<ProposalFileEditRequest> fileEdits, CancellationToken ct = default);
}
