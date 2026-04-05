using System;
using System.Threading.Tasks;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.Models;

namespace VsMcpBridge.Shared.Tests.Support;

public sealed class ThrowingVsService : IVsService
{
    public Task<GetActiveDocumentResponse> GetActiveDocumentAsync() => throw new InvalidOperationException("Boom from GetActiveDocumentAsync.");
    public Task<GetSelectedTextResponse> GetSelectedTextAsync() => throw new InvalidOperationException("Boom from GetSelectedTextAsync.");
    public Task<ListSolutionProjectsResponse> ListSolutionProjectsAsync() => throw new InvalidOperationException("Boom from ListSolutionProjectsAsync.");
    public Task<GetErrorListResponse> GetErrorListAsync() => throw new InvalidOperationException("Boom from GetErrorListAsync.");
    public Task<ProposeTextEditResponse> ProposeTextEditAsync(string requestId, string filePath, string originalText, string proposedText) => throw new InvalidOperationException("Boom from ProposeTextEditAsync.");
}
