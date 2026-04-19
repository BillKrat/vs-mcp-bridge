using System;
using System.Collections.Generic;
using System.Linq;
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
    public int OpenGitChangesCalls { get; private set; }
    public int ProposeTextEditCalls { get; private set; }
    public int ProposeTextEditsCalls { get; private set; }
    public string? LastProposeRequestId { get; private set; }
    public IReadOnlyList<ProposalFileEditRequest> LastMultiFileEdits { get; private set; } = Array.Empty<ProposalFileEditRequest>();

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

    public Task OpenGitChangesAsync()
    {
        OpenGitChangesCalls++;
        return Task.CompletedTask;
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

    public Task<ProposeTextEditResponse> ProposeTextEditsAsync(string requestId, IReadOnlyList<ProposalFileEditRequest> fileEdits)
    {
        ProposeTextEditsCalls++;
        LastProposeRequestId = requestId;
        LastMultiFileEdits = fileEdits.ToArray();
        return Task.FromResult(new ProposeTextEditResponse
        {
            RequestId = requestId,
            Success = true,
            FilePath = fileEdits.Count > 0 ? fileEdits[0].FilePath : string.Empty,
            Diff = string.Join("\n", fileEdits.Select(fileEdit => $"--- a/{fileEdit.FilePath}\n+++ b/{fileEdit.FilePath}\n-{fileEdit.OriginalText}\n+{fileEdit.ProposedText}\n"))
        });
    }
}
