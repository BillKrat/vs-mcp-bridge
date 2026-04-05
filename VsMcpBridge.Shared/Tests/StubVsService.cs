using System.Collections.Generic;
using System.Threading.Tasks;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.Models;

namespace VsMcpBridge.Shared.Tests.Support;

public sealed class StubVsService : IVsService
{
    public int GetActiveDocumentCalls { get; private set; }
    public int GetSelectedTextCalls { get; private set; }
    public int ListSolutionProjectsCalls { get; private set; }
    public int GetErrorListCalls { get; private set; }
    public int ProposeTextEditCalls { get; private set; }
    public string? LastProposeRequestId { get; private set; }

    public Task<GetActiveDocumentResponse> GetActiveDocumentAsync()
    {
        GetActiveDocumentCalls++;
        return Task.FromResult(new GetActiveDocumentResponse
        {
            Success = true,
            FilePath = "active.cs",
            Language = "C#",
            Content = "class C {}"
        });
    }

    public Task<GetSelectedTextResponse> GetSelectedTextAsync()
    {
        GetSelectedTextCalls++;
        return Task.FromResult(new GetSelectedTextResponse
        {
            Success = true,
            FilePath = "active.cs",
            SelectedText = "selected"
        });
    }

    public Task<ListSolutionProjectsResponse> ListSolutionProjectsAsync()
    {
        ListSolutionProjectsCalls++;
        return Task.FromResult(new ListSolutionProjectsResponse
        {
            Success = true,
            Projects = new List<ProjectInfo>
            {
                new ProjectInfo { Name = "VsMcpBridge", FullPath = "VsMcpBridge.sln", TargetFramework = ".NETFramework,Version=v4.7.2" }
            }
        });
    }

    public Task<GetErrorListResponse> GetErrorListAsync()
    {
        GetErrorListCalls++;
        return Task.FromResult(new GetErrorListResponse
        {
            Success = true,
            Diagnostics = new List<DiagnosticItem>
            {
                new DiagnosticItem { Severity = "Warning", Description = "Something happened." }
            }
        });
    }

    public Task<ProposeTextEditResponse> ProposeTextEditAsync(string requestId, string filePath, string originalText, string proposedText)
    {
        ProposeTextEditCalls++;
        LastProposeRequestId = requestId;
        return Task.FromResult(new ProposeTextEditResponse
        {
            RequestId = requestId,
            Success = true,
            FilePath = filePath,
            Diff = $"--- a/{filePath}\n+++ b/{filePath}\n-{originalText}\n+{proposedText}\n"
        });
    }
}
