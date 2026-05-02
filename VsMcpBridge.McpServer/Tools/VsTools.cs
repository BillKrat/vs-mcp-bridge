using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using Adventures.ChatEngine.Abstractions;
using Adventures.ChatEngine.Models;
using Microsoft.Extensions.Logging;
using VsMcpBridge.Shared.Models;
using VsMcpBridge.McpServer.Pipe;
using VsMcpBridge.Shared.Interfaces;

namespace VsMcpBridge.McpServer.Tools;

/// <summary>
/// MCP tool definitions that forward requests to the VSIX via named pipe.
/// All tools are read-only or diff-producing; they never write files directly.
/// </summary>
[McpServerToolType]
public sealed class VsTools
{
    private const int MaxChatEngineChatMessageLength = 4000;
    private const string ChatEngineChatInvalidInputError = "Error: chat_engine_chat requires a non-empty message no longer than 4000 characters.";
    private const string ChatEngineChatInvalidInputErrorCode = "InvalidInput";
    private const string ChatEngineChatFailureError = "Error: chat_engine_chat failed.";
    private const string ChatEngineChatFailureErrorCode = "ProviderFailure";

    private readonly IPipeClient _pipe;
    private readonly IChatEngine _chatEngine;
    private readonly ILogger _logger;

    public VsTools(IPipeClient pipe, IChatEngine chatEngine, ILogger logger)
    {
        _pipe = pipe;
        _chatEngine = chatEngine;
        _logger = logger;
    }

    [McpServerTool(Name = "chat_engine_ping")]
    [Description("Sends a ping request through Adventures.ChatEngine and returns the response message.")]
    public async Task<string> ChatEnginePingAsync(CancellationToken ct)
    {
        var requestId = Guid.NewGuid().ToString("N");
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("MCP chat_engine_ping started [RequestId={RequestId}].", requestId);

        try
        {
            var response = await _chatEngine.SendAsync(new ChatRequest("ping"), ct).ConfigureAwait(false);
            stopwatch.Stop();
            _logger.LogInformation(
                "MCP chat_engine_ping completed [RequestId={RequestId}] [ElapsedMs={ElapsedMs}] [ResponseLength={ResponseLength}].",
                requestId,
                stopwatch.ElapsedMilliseconds,
                response.Message?.Length ?? 0);
            return response.Message;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "MCP chat_engine_ping canceled [RequestId={RequestId}] [ElapsedMs={ElapsedMs}].",
                requestId,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "MCP chat_engine_ping failed [RequestId={RequestId}] [ElapsedMs={ElapsedMs}].",
                requestId,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    [McpServerTool(Name = "chat_engine_chat")]
    [Description("Sends a message through Adventures.ChatEngine and returns the response message.")]
    public async Task<string> ChatEngineChatAsync(
        [Description("The message to send through Adventures.ChatEngine.")] string message,
        CancellationToken ct)
    {
        var requestId = Guid.NewGuid().ToString("N");

        if (string.IsNullOrWhiteSpace(message) || message.Length > MaxChatEngineChatMessageLength)
        {
            return JsonSerializer.Serialize(new ChatEngineChatResult
            {
                Success = false,
                Content = null,
                Error = ChatEngineChatInvalidInputError,
                ErrorCode = ChatEngineChatInvalidInputErrorCode,
                RequestId = requestId
            });
        }

        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation(
            "MCP chat_engine_chat started [RequestId={RequestId}] [MessageLength={MessageLength}].",
            requestId,
            message?.Length ?? 0);

        try
        {
            var response = await _chatEngine.SendAsync(new ChatRequest(message), ct).ConfigureAwait(false);
            stopwatch.Stop();
            _logger.LogInformation(
                "MCP chat_engine_chat completed [RequestId={RequestId}] [ElapsedMs={ElapsedMs}] [ResponseLength={ResponseLength}].",
                requestId,
                stopwatch.ElapsedMilliseconds,
                response.Message?.Length ?? 0);
            return JsonSerializer.Serialize(new ChatEngineChatResult
            {
                Success = true,
                Content = response.Message,
                Error = null,
                ErrorCode = null,
                RequestId = requestId
            });
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "MCP chat_engine_chat canceled [RequestId={RequestId}] [ElapsedMs={ElapsedMs}].",
                requestId,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "MCP chat_engine_chat failed [RequestId={RequestId}] [ElapsedMs={ElapsedMs}].",
                requestId,
                stopwatch.ElapsedMilliseconds);
            return JsonSerializer.Serialize(new ChatEngineChatResult
            {
                Success = false,
                Content = null,
                Error = ChatEngineChatFailureError,
                ErrorCode = ChatEngineChatFailureErrorCode,
                RequestId = requestId
            });
        }
    }

    [McpServerTool(Name = "vs_get_active_document")]
    [Description("Returns the file path, language, and full text of the document currently active in Visual Studio.")]
    public async Task<string> GetActiveDocumentAsync(CancellationToken ct)
    {
        var response = await _pipe.GetActiveDocumentAsync(ct);
        if (!response.Success)
            return $"Error: {response.ErrorMessage}";

        return $"File: {response.FilePath}\nLanguage: {response.Language}\n\n{response.Content}";
    }

    [McpServerTool(Name = "vs_get_selected_text")]
    [Description("Returns the text currently selected in the active Visual Studio editor.")]
    public async Task<string> GetSelectedTextAsync(CancellationToken ct)
    {
        var response = await _pipe.GetSelectedTextAsync(ct);
        if (!response.Success)
            return $"Error: {response.ErrorMessage}";

        return string.IsNullOrEmpty(response.SelectedText)
            ? "(no selection)"
            : $"File: {response.FilePath}\nSelected:\n{response.SelectedText}";
    }

    [McpServerTool(Name = "vs_list_solution_projects")]
    [Description("Lists all projects in the currently open Visual Studio solution.")]
    public async Task<string> ListSolutionProjectsAsync(CancellationToken ct)
    {
        var response = await _pipe.ListSolutionProjectsAsync(ct);
        if (!response.Success)
            return $"Error: {response.ErrorMessage}";

        if (response.Projects.Count == 0)
            return "(no projects found)";

        var lines = response.Projects.Select(p =>
            $"- {p.Name} ({p.TargetFramework})\n  {p.FullPath}");

        return string.Join("\n", lines);
    }

    [McpServerTool(Name = "vs_get_error_list")]
    [Description("Returns the current errors, warnings, and messages from the Visual Studio Error List.")]
    public async Task<string> GetErrorListAsync(CancellationToken ct)
    {
        var response = await _pipe.GetErrorListAsync(ct);
        if (!response.Success)
            return $"Error: {response.ErrorMessage}";

        if (response.Diagnostics.Count == 0)
            return "(no diagnostics)";

        var lines = response.Diagnostics.Select(d =>
            $"[{d.Severity}] {d.Code}: {d.Description}\n  {d.File}({d.Line},{d.Column}) in {d.Project}");

        return string.Join("\n", lines);
    }

    [McpServerTool(Name = "vs_propose_text_edit")]
    [Description("Produces a unified diff showing proposed changes to a file. Does NOT write to disk; the user must approve changes via the VSIX UI.")]
    public async Task<string> ProposeTextEditAsync(
        [Description("Absolute path to the file to edit.")] string filePath,
        [Description("The original file content (before edits).")] string originalText,
        [Description("The proposed new file content (after edits).")] string proposedText,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return "Error: filePath must not be empty.";

        var response = await _pipe.ProposeTextEditAsync(filePath, originalText, proposedText, ct);
        if (!response.Success)
            return $"Error: {response.ErrorMessage}";

        return string.IsNullOrEmpty(response.Diff)
            ? "(no changes)"
            : $"Proposed diff for {response.FilePath}:\n\n{response.Diff}";
    }

    [McpServerTool(Name = "vs_propose_text_edits")]
    [Description("Produces a unified diff showing proposed changes across multiple files. Does NOT write to disk; the user must approve changes via the VSIX UI.")]
    public async Task<string> ProposeTextEditsAsync(
        [Description("The file edits to include in a single proposal.")] IReadOnlyList<ProposalFileEditRequest> fileEdits,
        CancellationToken ct)
    {
        if (fileEdits == null || fileEdits.Count == 0)
            return "Error: fileEdits must contain at least one file edit.";

        if (fileEdits.Any(fileEdit => fileEdit == null || string.IsNullOrWhiteSpace(fileEdit.FilePath)))
            return "Error: each file edit must include a non-empty filePath.";

        var response = await _pipe.ProposeTextEditsAsync(fileEdits, ct);
        if (!response.Success)
            return $"Error: {response.ErrorMessage}";

        return string.IsNullOrEmpty(response.Diff)
            ? "(no changes)"
            : $"Proposed diff for {fileEdits.Count} files:\n\n{response.Diff}";
    }
}
