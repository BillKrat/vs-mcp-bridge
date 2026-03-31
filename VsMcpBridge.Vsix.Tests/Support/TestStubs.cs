using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using VsMcpBridge.Shared.Models;
using VsMcpBridge.Vsix.Diagnostics;
using VsMcpBridge.Vsix.Services;

namespace VsMcpBridge.Vsix.Tests.Support;

internal sealed class FakeAsyncPackage : AsyncPackage
{
}

internal static class TestPackageFactory
{
    internal static FakeAsyncPackage CreatePackage()
    {
        return (FakeAsyncPackage)FormatterServices.GetUninitializedObject(typeof(FakeAsyncPackage));
    }
}

internal sealed class StubVsService : IVsService
{
    public int GetActiveDocumentCalls { get; private set; }
    public int GetSelectedTextCalls { get; private set; }
    public int ListSolutionProjectsCalls { get; private set; }
    public int GetErrorListCalls { get; private set; }
    public int ProposeTextEditCalls { get; private set; }

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

    public Task<ProposeTextEditResponse> ProposeTextEditAsync(string filePath, string originalText, string proposedText)
    {
        ProposeTextEditCalls++;
        return Task.FromResult(new ProposeTextEditResponse
        {
            Success = true,
            FilePath = filePath,
            Diff = $"--- a/{filePath}\n+++ b/{filePath}\n-{originalText}\n+{proposedText}\n"
        });
    }
}

internal sealed class ThrowingVsService : IVsService
{
    public Task<GetActiveDocumentResponse> GetActiveDocumentAsync() => throw new InvalidOperationException("Boom from GetActiveDocumentAsync.");
    public Task<GetSelectedTextResponse> GetSelectedTextAsync() => throw new InvalidOperationException("Boom from GetSelectedTextAsync.");
    public Task<ListSolutionProjectsResponse> ListSolutionProjectsAsync() => throw new InvalidOperationException("Boom from ListSolutionProjectsAsync.");
    public Task<GetErrorListResponse> GetErrorListAsync() => throw new InvalidOperationException("Boom from GetErrorListAsync.");
    public Task<ProposeTextEditResponse> ProposeTextEditAsync(string filePath, string originalText, string proposedText) => throw new InvalidOperationException("Boom from ProposeTextEditAsync.");
}

internal sealed class RecordingUnhandledExceptionSink : IUnhandledExceptionSink
{
    public List<(string Source, Exception Exception)> Entries { get; } = new();

    public void Save(string source, Exception exception)
    {
        Entries.Add((source, exception));
    }
}
