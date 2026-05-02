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
    private const string ChatEngineSummarizeInvalidInputError = "Error: chat_engine_summarize requires a non-empty message no longer than 4000 characters.";
    private const string ChatEngineSummarizeInvalidMaxLengthError = "Error: chat_engine_summarize max_length must be greater than 0 and less than or equal to 1000.";
    private const string ChatEngineSummarizeFailureError = "Error: chat_engine_summarize failed.";
    private const string ChatEngineRewriteInvalidInputError = "Error: chat_engine_rewrite requires a non-empty message no longer than 4000 characters.";
    private const string ChatEngineRewriteInvalidToneError = "Error: chat_engine_rewrite tone must be one of: formal, casual, technical.";
    private const string ChatEngineRewriteFailureError = "Error: chat_engine_rewrite failed.";
    private const string ChatEngineSuggestFixesInvalidInputError = "Error: chat_engine_suggest_fixes requires a non-empty message no longer than 4000 characters.";
    private const string ChatEngineSuggestFixesFailureError = "Error: chat_engine_suggest_fixes failed.";

    private readonly IPipeClient _pipe;
    private readonly IChatEngine _chatEngine;
    private readonly ILogger _logger;

    public VsTools(IPipeClient pipe, IChatEngine chatEngine, ILogger logger)
    {
        _pipe = pipe;
        _chatEngine = chatEngine;
        _logger = logger;
    }

    private static string? NormalizeChatEngineChatField(string? value)
    {
        return value?.Trim();
    }

    private static bool IsValidRewriteTone(string? tone)
    {
        return string.Equals(tone, "formal", StringComparison.OrdinalIgnoreCase)
            || string.Equals(tone, "casual", StringComparison.OrdinalIgnoreCase)
            || string.Equals(tone, "technical", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildChatEngineResultJson(
        bool success,
        string? content,
        string? error,
        string? errorCode,
        string requestId)
    {
        return JsonSerializer.Serialize(new ChatEngineChatResult
        {
            Success = success,
            Content = NormalizeChatEngineChatField(content),
            Error = NormalizeChatEngineChatField(error),
            ErrorCode = errorCode,
            RequestId = requestId
        });
    }

    private async Task<string> ExecuteChatEngineToolAsync(
        string input,
        CancellationToken ct,
        string invalidInputError,
        string failureError,
        string logToolName,
        Func<string, string> requestMessageFactory)
    {
        var requestId = Guid.NewGuid().ToString("N");

        if (string.IsNullOrWhiteSpace(input) || input.Length > MaxChatEngineChatMessageLength)
        {
            return BuildChatEngineResultJson(
                success: false,
                content: null,
                error: invalidInputError,
                errorCode: ChatEngineChatInvalidInputErrorCode,
                requestId: requestId);
        }

        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation(
            "MCP {ToolName} started [RequestId={RequestId}] [MessageLength={MessageLength}].",
            logToolName,
            requestId,
            input?.Length ?? 0);

        try
        {
            var response = await _chatEngine.SendAsync(new ChatRequest(requestMessageFactory(input)), ct).ConfigureAwait(false);
            stopwatch.Stop();
            _logger.LogInformation(
                "MCP {ToolName} completed [RequestId={RequestId}] [ElapsedMs={ElapsedMs}] [ResponseLength={ResponseLength}].",
                logToolName,
                requestId,
                stopwatch.ElapsedMilliseconds,
                response.Message?.Length ?? 0);
            return BuildChatEngineResultJson(
                success: true,
                content: response.Message,
                error: null,
                errorCode: null,
                requestId: requestId);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "MCP {ToolName} canceled [RequestId={RequestId}] [ElapsedMs={ElapsedMs}].",
                logToolName,
                requestId,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "MCP {ToolName} failed [RequestId={RequestId}] [ElapsedMs={ElapsedMs}].",
                logToolName,
                requestId,
                stopwatch.ElapsedMilliseconds);
            return BuildChatEngineResultJson(
                success: false,
                content: null,
                error: failureError,
                errorCode: ChatEngineChatFailureErrorCode,
                requestId: requestId);
        }
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
        return await ExecuteChatEngineToolAsync(
            input: message,
            ct: ct,
            invalidInputError: ChatEngineChatInvalidInputError,
            failureError: ChatEngineChatFailureError,
            logToolName: "chat_engine_chat",
            requestMessageFactory: static input => input).ConfigureAwait(false);
    }

    [McpServerTool(Name = "chat_engine_summarize")]
    [Description("Sends text through Adventures.ChatEngine with a summarization prompt and returns the response message.")]
    public async Task<string> ChatEngineSummarizeAsync(
        [Description("The text to summarize through Adventures.ChatEngine.")] string text,
        [Description("Optional maximum summary length in words.")] int? max_length,
        CancellationToken ct)
    {
        if (max_length.HasValue && (max_length.Value <= 0 || max_length.Value > 1000))
        {
            return BuildChatEngineResultJson(
                success: false,
                content: null,
                error: ChatEngineSummarizeInvalidMaxLengthError,
                errorCode: ChatEngineChatInvalidInputErrorCode,
                requestId: Guid.NewGuid().ToString("N"));
        }

        return await ExecuteChatEngineToolAsync(
            input: text,
            ct: ct,
            invalidInputError: ChatEngineSummarizeInvalidInputError,
            failureError: ChatEngineSummarizeFailureError,
            logToolName: "chat_engine_summarize",
            requestMessageFactory: input =>
            {
                if (!max_length.HasValue)
                {
                    return $"Summarize the following text:\n\n{input}";
                }

                return $"Summarize the following text in no more than {max_length.Value} words:\n\n{input}";
            }).ConfigureAwait(false);
    }

    [McpServerTool(Name = "chat_engine_rewrite")]
    [Description("Sends text through Adventures.ChatEngine with a rewrite prompt and returns the response message.")]
    public async Task<string> ChatEngineRewriteAsync(
        [Description("The text to rewrite through Adventures.ChatEngine.")] string text,
        [Description("Optional tone for the rewrite: formal, casual, or technical.")] string? tone,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(tone) && !IsValidRewriteTone(tone))
        {
            return BuildChatEngineResultJson(
                success: false,
                content: null,
                error: ChatEngineRewriteInvalidToneError,
                errorCode: ChatEngineChatInvalidInputErrorCode,
                requestId: Guid.NewGuid().ToString("N"));
        }

        return await ExecuteChatEngineToolAsync(
            input: text,
            ct: ct,
            invalidInputError: ChatEngineRewriteInvalidInputError,
            failureError: ChatEngineRewriteFailureError,
            logToolName: "chat_engine_rewrite",
            requestMessageFactory: input =>
            {
                if (string.IsNullOrWhiteSpace(tone))
                {
                    return $"Rewrite the following text to be clearer and more concise:\n\n{input}";
                }

                return $"Rewrite the following text to be clearer and more concise in a {tone!.Trim().ToLowerInvariant()} tone:\n\n{input}";
            }).ConfigureAwait(false);
    }

    [McpServerTool(Name = "chat_engine_suggest_fixes")]
    [Description("Sends text through Adventures.ChatEngine with a review prompt and returns suggested improvements or fixes.")]
    public async Task<string> ChatEngineSuggestFixesAsync(
        [Description("The text to review for suggested improvements or fixes.")] string text,
        CancellationToken ct)
    {
        return await ExecuteChatEngineToolAsync(
            input: text,
            ct: ct,
            invalidInputError: ChatEngineSuggestFixesInvalidInputError,
            failureError: ChatEngineSuggestFixesFailureError,
            logToolName: "chat_engine_suggest_fixes",
            requestMessageFactory: static input => $"Review the following text and suggest improvements or fixes:\n\n{input}").ConfigureAwait(false);
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
