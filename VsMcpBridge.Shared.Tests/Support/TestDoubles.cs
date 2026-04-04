using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.Models;

namespace VsMcpBridge.Shared.Tests.Support;

internal sealed class RecordingBridgeLogger : IBridgeLogger
{
    public List<string> VerboseMessages { get; } = new();
    public List<string> InformationMessages { get; } = new();
    public List<string> WarningMessages { get; } = new();
    public List<(string Message, Exception? Exception)> Errors { get; } = new();

    public void LogVerbose(string message) => VerboseMessages.Add(message);

    public void LogInformation(string message) => InformationMessages.Add(message);

    public void LogWarning(string message) => WarningMessages.Add(message);

    public void LogError(string message, Exception? exception = null) => Errors.Add((message, exception));
}

internal sealed class RecordingUnhandledExceptionSink : IUnhandledExceptionSink
{
    public List<(string Source, Exception Exception)> Entries { get; } = new();

    public void Save(string source, Exception exception) => Entries.Add((source, exception));
}

internal sealed class StubVsService : IVsService
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
            Projects =
            [
                new ProjectInfo { Name = "VsMcpBridge", FullPath = "VsMcpBridge.sln", TargetFramework = ".NETFramework,Version=v4.7.2" }
            ]
        });
    }

    public Task<GetErrorListResponse> GetErrorListAsync()
    {
        GetErrorListCalls++;
        return Task.FromResult(new GetErrorListResponse
        {
            Success = true,
            Diagnostics =
            [
                new DiagnosticItem { Severity = "Warning", Description = "Something happened." }
            ]
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

internal sealed class ThrowingVsService : IVsService
{
    public Task<GetActiveDocumentResponse> GetActiveDocumentAsync() => throw new InvalidOperationException("Boom from GetActiveDocumentAsync.");
    public Task<GetSelectedTextResponse> GetSelectedTextAsync() => throw new InvalidOperationException("Boom from GetSelectedTextAsync.");
    public Task<ListSolutionProjectsResponse> ListSolutionProjectsAsync() => throw new InvalidOperationException("Boom from ListSolutionProjectsAsync.");
    public Task<GetErrorListResponse> GetErrorListAsync() => throw new InvalidOperationException("Boom from GetErrorListAsync.");
    public Task<ProposeTextEditResponse> ProposeTextEditAsync(string requestId, string filePath, string originalText, string proposedText) => throw new InvalidOperationException("Boom from ProposeTextEditAsync.");
}

internal sealed class TestThreadHelper : IThreadHelper
{
    public bool HasAccess { get; set; } = true;
    public int RunCalls { get; private set; }
    public int SwitchCalls { get; private set; }
    public int ThrowIfNotOnUiThreadCalls { get; private set; }

    public bool CheckAccess() => HasAccess;

    public void Run(Func<Task> value)
    {
        RunCalls++;
        value().GetAwaiter().GetResult();
    }

    public Task SwitchToMainThreadAsync()
    {
        SwitchCalls++;
        HasAccess = true;
        return Task.CompletedTask;
    }

    public void ThrowIfNotOnUIThread() => ThrowIfNotOnUiThreadCalls++;
}

internal sealed class FakeLogToolWindowControl : ILogToolWindowControl
{
    public object DataContext { get; set; } = null!;
}
