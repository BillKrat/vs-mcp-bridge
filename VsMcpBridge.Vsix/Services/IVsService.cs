using System.Threading.Tasks;
using VsMcpBridge.Shared.Models;

namespace VsMcpBridge.Vsix.Services;

public interface IVsService
{
    Task<GetActiveDocumentResponse> GetActiveDocumentAsync();
    Task<GetSelectedTextResponse> GetSelectedTextAsync();
    Task<ListSolutionProjectsResponse> ListSolutionProjectsAsync();
    Task<GetErrorListResponse> GetErrorListAsync();
    Task<ProposeTextEditResponse> ProposeTextEditAsync(string filePath, string originalText, string proposedText);
}
