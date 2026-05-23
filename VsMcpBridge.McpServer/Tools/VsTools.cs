using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Adventures.ChatEngine.Abstractions;
using Adventures.ChatEngine.Models;
using Microsoft.Extensions.Logging;
using VsMcpBridge.McpServer.ChatEngine;
using VsMcpBridge.Shared.Models;
using VsMcpBridge.McpServer.Pipe;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.Tools;

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
    private const string ChatEngineRewriteWithTargetInvalidInputError = "Error: chat_engine_rewrite_with_target requires a non-empty filePath and originalText.";
    private const string ChatEngineRewriteWithTargetInvalidToneError = "Error: chat_engine_rewrite_with_target tone must be one of: formal, casual, technical.";
    private const string ChatEngineRewriteWithTargetFailureError = "Error: chat_engine_rewrite_with_target failed.";
    private const string ChatEngineSuggestFixesInvalidInputError = "Error: chat_engine_suggest_fixes requires a non-empty message no longer than 4000 characters.";
    private const string ChatEngineSuggestFixesFailureError = "Error: chat_engine_suggest_fixes failed.";
    private const string ChatEngineSuggestFixesWithTargetInvalidInputError = "Error: chat_engine_suggest_fixes_with_target requires a non-empty filePath and originalText.";
    private const string ChatEngineSuggestFixesWithTargetFailureError = "Error: chat_engine_suggest_fixes_with_target failed.";
    private const string ChatEngineExplainCodeInvalidInputError = "Error: chat_engine_explain_code requires a non-empty message no longer than 4000 characters.";
    private const string ChatEngineExplainCodeFailureError = "Error: chat_engine_explain_code failed.";
    private const string ChatEngineExplainErrorInvalidInputError = "Error: chat_engine_explain_error requires a non-empty message no longer than 4000 characters.";
    private const string ChatEngineExplainErrorFailureError = "Error: chat_engine_explain_error failed.";
    private const string ChatEngineSuggestErrorFixInvalidInputError = "Error: chat_engine_suggest_error_fix requires a non-empty message no longer than 4000 characters.";
    private const string ChatEngineSuggestErrorFixFailureError = "Error: chat_engine_suggest_error_fix failed.";

    private readonly IPipeClient _pipe;
    private readonly IChatEngine _chatEngine;
    private readonly IBridgeToolInventoryService _toolInventory;
    private readonly IBridgeToolExecutor _bridgeToolExecutor;
    private readonly ILogger _logger;

    public VsTools(
        IPipeClient pipe,
        IChatEngine chatEngine,
        ILogger logger,
        IBridgeToolInventoryService? toolInventory = null,
        IBridgeToolExecutor? bridgeToolExecutor = null)
    {
        _pipe = pipe;
        _chatEngine = chatEngine;
        _toolInventory = toolInventory ?? EmptyBridgeToolInventoryService.Instance;
        _bridgeToolExecutor = bridgeToolExecutor ?? EmptyBridgeToolExecutor.Instance;
        _logger = logger;
    }

    private static readonly JsonSerializerOptions InventoryJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

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
        string requestId,
        string toolName,
        string shortDescription)
    {
        var result = new ChatEngineChatResult
        {
            Success = success,
            Content = NormalizeChatEngineChatField(content),
            Error = NormalizeChatEngineChatField(error),
            ErrorCode = errorCode,
            RequestId = requestId
        };

        _ = ChatEngineResultAdapter.ToProposalReady(result, toolName, shortDescription);

        return JsonSerializer.Serialize(result);
    }

    private static string GetToolShortDescription(string toolName)
    {
        return toolName switch
        {
            "chat_engine_chat" => "AI chat response",
            "chat_engine_summarize" => "AI summary suggestion",
            "chat_engine_rewrite" => "AI rewrite suggestion",
            "chat_engine_rewrite_with_target" => "AI rewrite proposal",
            "chat_engine_suggest_fixes" => "AI fix suggestion",
            "chat_engine_suggest_fixes_with_target" => "AI fix proposal",
            "chat_engine_explain_code" => "AI code explanation",
            "chat_engine_explain_error" => "AI error explanation",
            "chat_engine_suggest_error_fix" => "AI error fix suggestion",
            _ => "AI suggestion"
        };
    }

    [McpServerTool(Name = "bridge_get_tool_inventory")]
    [Description("Returns deterministic read-only bridge tool manifest metadata for diagnostics. Does not execute tools or trigger approval.")]
    public string GetToolInventory(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var requestId = Guid.NewGuid().ToString("N");
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("MCP bridge_get_tool_inventory started [RequestId={RequestId}].", requestId);

        try
        {
            BridgeToolCatalogSnapshot snapshot = _toolInventory.GetSnapshot();
            stopwatch.Stop();
            _logger.LogInformation(
                "MCP bridge_get_tool_inventory completed [RequestId={RequestId}] [ElapsedMs={ElapsedMs}] [ToolCount={ToolCount}].",
                requestId,
                stopwatch.ElapsedMilliseconds,
                snapshot.Tools.Count);

            return JsonSerializer.Serialize(
                new BridgeToolInventoryDiagnosticResult
                {
                    Success = true,
                    RequestId = requestId,
                    ToolName = "bridge_get_tool_inventory",
                    CapturedAtUtc = snapshot.CapturedAtUtc,
                    ToolCount = snapshot.Tools.Count,
                    Tools = snapshot.Tools
                },
                InventoryJsonOptions);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "MCP bridge_get_tool_inventory canceled [RequestId={RequestId}] [ElapsedMs={ElapsedMs}].",
                requestId,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "MCP bridge_get_tool_inventory failed [RequestId={RequestId}] [ElapsedMs={ElapsedMs}].",
                requestId,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    [McpServerTool(Name = "bridge_regex_text_search")]
    [Description("Executes the compiled bridge.regexTextSearch tool through BridgeToolExecutor against explicit input text or entries. Does not read files or mutate state.")]
    public async Task<string> RegexTextSearchAsync(
        [Description("Regular expression pattern to search for.")] string pattern,
        [Description("Optional single text value to search. Ignored when entries are provided.")] string? inputText = null,
        [Description("Optional explicit text entries to search. No file paths are read.")] string[]? entries = null,
        [Description("Whether pattern matching is case-sensitive. Defaults to false.")] bool caseSensitive = false,
        [Description("Maximum number of matches to return. Must be greater than zero when provided.")] int? maxResults = null,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var requestId = Guid.NewGuid().ToString("N");
        var operationId = Guid.NewGuid().ToString("N");
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation(
            "MCP bridge_regex_text_search started [RequestId={RequestId}] [OperationId={OperationId}].",
            requestId,
            operationId);

        var arguments = new Dictionary<string, object?>
        {
            ["pattern"] = pattern,
            ["caseSensitive"] = caseSensitive
        };

        if (entries is { Length: > 0 })
            arguments["entries"] = entries;
        else if (!string.IsNullOrWhiteSpace(inputText))
            arguments["inputText"] = inputText;

        if (maxResults.HasValue)
            arguments["maxResults"] = maxResults.Value;

        var request = new BridgeToolRequest
        {
            ToolId = RegexTextSearchTool.ToolId,
            RequestId = requestId,
            OperationId = operationId,
            Arguments = arguments
        };

        try
        {
            var result = await _bridgeToolExecutor.ExecuteAsync(request, ct).ConfigureAwait(false);
            stopwatch.Stop();
            _logger.LogInformation(
                "MCP bridge_regex_text_search completed [RequestId={RequestId}] [OperationId={OperationId}] [Success={Success}] [ElapsedMs={ElapsedMs}].",
                requestId,
                operationId,
                result.Success,
                stopwatch.ElapsedMilliseconds);

            return JsonSerializer.Serialize(result, InventoryJsonOptions);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "MCP bridge_regex_text_search canceled [RequestId={RequestId}] [OperationId={OperationId}] [ElapsedMs={ElapsedMs}].",
                requestId,
                operationId,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "MCP bridge_regex_text_search failed [RequestId={RequestId}] [OperationId={OperationId}] [ElapsedMs={ElapsedMs}].",
                requestId,
                operationId,
                stopwatch.ElapsedMilliseconds);

            var result = BridgeToolResult.Failed(
                request,
                "McpWrapperFailed",
                "MCP regex text search failed before the bridge tool completed. Review the MCP log for details.");
            return JsonSerializer.Serialize(result, InventoryJsonOptions);
        }
    }

    [McpServerTool(Name = "bridge_bm25_text_search")]
    [Description("Executes the compiled bridge.bm25TextSearch tool through BridgeToolExecutor against explicit in-memory documents or entries. Does not read files or mutate state.")]
    public async Task<string> Bm25TextSearchAsync(
        [Description("Search query used to rank the supplied documents.")] string query,
        [Description("Optional explicit in-memory document text values to rank. No file paths are read.")] string[]? documents = null,
        [Description("Optional explicit text entries to rank when documents are not provided. No file paths are read.")] string[]? entries = null,
        [Description("Whether token matching is case-sensitive. Defaults to false.")] bool caseSensitive = false,
        [Description("Maximum number of ranked results to return. Must be greater than zero when provided.")] int? maxResults = null,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var requestId = Guid.NewGuid().ToString("N");
        var operationId = Guid.NewGuid().ToString("N");
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation(
            "MCP bridge_bm25_text_search started [RequestId={RequestId}] [OperationId={OperationId}].",
            requestId,
            operationId);

        var arguments = new Dictionary<string, object?>
        {
            ["query"] = query,
            ["caseSensitive"] = caseSensitive
        };

        if (documents is { Length: > 0 })
            arguments["documents"] = documents;
        else if (entries is { Length: > 0 })
            arguments["entries"] = entries;

        if (maxResults.HasValue)
            arguments["maxResults"] = maxResults.Value;

        var request = new BridgeToolRequest
        {
            ToolId = Bm25TextSearchTool.ToolId,
            RequestId = requestId,
            OperationId = operationId,
            Arguments = arguments
        };

        try
        {
            var result = await _bridgeToolExecutor.ExecuteAsync(request, ct).ConfigureAwait(false);
            stopwatch.Stop();
            _logger.LogInformation(
                "MCP bridge_bm25_text_search completed [RequestId={RequestId}] [OperationId={OperationId}] [Success={Success}] [ElapsedMs={ElapsedMs}].",
                requestId,
                operationId,
                result.Success,
                stopwatch.ElapsedMilliseconds);

            return JsonSerializer.Serialize(result, InventoryJsonOptions);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "MCP bridge_bm25_text_search canceled [RequestId={RequestId}] [OperationId={OperationId}] [ElapsedMs={ElapsedMs}].",
                requestId,
                operationId,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "MCP bridge_bm25_text_search failed [RequestId={RequestId}] [OperationId={OperationId}] [ElapsedMs={ElapsedMs}].",
                requestId,
                operationId,
                stopwatch.ElapsedMilliseconds);

            var result = BridgeToolResult.Failed(
                request,
                "McpWrapperFailed",
                "MCP BM25 text search failed before the bridge tool completed. Review the MCP log for details.");
            return JsonSerializer.Serialize(result, InventoryJsonOptions);
        }
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
                requestId: requestId,
                toolName: logToolName,
                shortDescription: GetToolShortDescription(logToolName));
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
                requestId: requestId,
                toolName: logToolName,
                shortDescription: GetToolShortDescription(logToolName));
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
                requestId: requestId,
                toolName: logToolName,
                shortDescription: GetToolShortDescription(logToolName));
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
                requestId: Guid.NewGuid().ToString("N"),
                toolName: "chat_engine_summarize",
                shortDescription: GetToolShortDescription("chat_engine_summarize"));
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
                requestId: Guid.NewGuid().ToString("N"),
                toolName: "chat_engine_rewrite",
                shortDescription: GetToolShortDescription("chat_engine_rewrite"));
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

    [McpServerTool(Name = "chat_engine_rewrite_with_target")]
    [Description("Rewrites explicit target text through Adventures.ChatEngine and creates an approval-gated proposal.")]
    public async Task<string> ChatEngineRewriteWithTargetAsync(
        [Description("Absolute path to the file to target.")] string filePath,
        [Description("The original text to rewrite and propose.")] string originalText,
        [Description("Optional tone for the rewrite: formal, casual, or technical.")] string? tone,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(filePath) || string.IsNullOrWhiteSpace(originalText))
        {
            return BuildChatEngineResultJson(
                success: false,
                content: null,
                error: ChatEngineRewriteWithTargetInvalidInputError,
                errorCode: ChatEngineChatInvalidInputErrorCode,
                requestId: Guid.NewGuid().ToString("N"),
                toolName: "chat_engine_rewrite_with_target",
                shortDescription: GetToolShortDescription("chat_engine_rewrite_with_target"));
        }

        if (!string.IsNullOrWhiteSpace(tone) && !IsValidRewriteTone(tone))
        {
            return BuildChatEngineResultJson(
                success: false,
                content: null,
                error: ChatEngineRewriteWithTargetInvalidToneError,
                errorCode: ChatEngineChatInvalidInputErrorCode,
                requestId: Guid.NewGuid().ToString("N"),
                toolName: "chat_engine_rewrite_with_target",
                shortDescription: GetToolShortDescription("chat_engine_rewrite_with_target"));
        }

        string resultJson = await ExecuteChatEngineToolAsync(
            input: originalText,
            ct: ct,
            invalidInputError: ChatEngineRewriteWithTargetInvalidInputError,
            failureError: ChatEngineRewriteWithTargetFailureError,
            logToolName: "chat_engine_rewrite_with_target",
            requestMessageFactory: input =>
            {
                if (string.IsNullOrWhiteSpace(tone))
                {
                    return $"Rewrite the following text to be clearer and more concise:\n\n{input}";
                }

                return $"Rewrite the following text to be clearer and more concise in a {tone!.Trim().ToLowerInvariant()} tone:\n\n{input}";
            }).ConfigureAwait(false);

        ChatEngineChatResult? result = JsonSerializer.Deserialize<ChatEngineChatResult>(resultJson);
        if (result is null)
        {
            return BuildChatEngineResultJson(
                success: false,
                content: null,
                error: ChatEngineRewriteWithTargetFailureError,
                errorCode: ChatEngineChatFailureErrorCode,
                requestId: Guid.NewGuid().ToString("N"),
                toolName: "chat_engine_rewrite_with_target",
                shortDescription: GetToolShortDescription("chat_engine_rewrite_with_target"));
        }

        if (!result.Success)
        {
            return resultJson;
        }

        ProposalReadyChatResult proposalReady = ChatEngineResultAdapter.ToProposalReady(
            result,
            "rewrite_with_target",
            GetToolShortDescription("chat_engine_rewrite_with_target"));
        ProposeTextEditResponse proposalResponse = await _pipe.ProposeTextEditAsync(
            filePath,
            originalText,
            proposalReady.SuggestedText ?? string.Empty,
            ct).ConfigureAwait(false);

        if (!proposalResponse.Success)
        {
            return BuildChatEngineResultJson(
                success: false,
                content: null,
                error: ChatEngineRewriteWithTargetFailureError,
                errorCode: ChatEngineChatFailureErrorCode,
                requestId: result.RequestId ?? Guid.NewGuid().ToString("N"),
                toolName: "chat_engine_rewrite_with_target",
                shortDescription: GetToolShortDescription("chat_engine_rewrite_with_target"));
        }

        return BuildChatEngineResultJson(
            success: true,
            content: "Proposal created",
            error: null,
            errorCode: null,
            requestId: result.RequestId ?? Guid.NewGuid().ToString("N"),
            toolName: "chat_engine_rewrite_with_target",
            shortDescription: GetToolShortDescription("chat_engine_rewrite_with_target"));
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

    [McpServerTool(Name = "chat_engine_suggest_fixes_with_target")]
    [Description("Reviews explicit target text through Adventures.ChatEngine and creates an approval-gated proposal.")]
    public async Task<string> ChatEngineSuggestFixesWithTargetAsync(
        [Description("Absolute path to the file to target.")] string filePath,
        [Description("The original text to review and propose improvements for.")] string originalText,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(filePath) || string.IsNullOrWhiteSpace(originalText))
        {
            return BuildChatEngineResultJson(
                success: false,
                content: null,
                error: ChatEngineSuggestFixesWithTargetInvalidInputError,
                errorCode: ChatEngineChatInvalidInputErrorCode,
                requestId: Guid.NewGuid().ToString("N"),
                toolName: "chat_engine_suggest_fixes_with_target",
                shortDescription: GetToolShortDescription("chat_engine_suggest_fixes_with_target"));
        }

        string resultJson = await ExecuteChatEngineToolAsync(
            input: originalText,
            ct: ct,
            invalidInputError: ChatEngineSuggestFixesWithTargetInvalidInputError,
            failureError: ChatEngineSuggestFixesWithTargetFailureError,
            logToolName: "chat_engine_suggest_fixes_with_target",
            requestMessageFactory: static input => $"Review the following text and suggest improvements or fixes:\n\n{input}").ConfigureAwait(false);

        ChatEngineChatResult? result = JsonSerializer.Deserialize<ChatEngineChatResult>(resultJson);
        if (result is null)
        {
            return BuildChatEngineResultJson(
                success: false,
                content: null,
                error: ChatEngineSuggestFixesWithTargetFailureError,
                errorCode: ChatEngineChatFailureErrorCode,
                requestId: Guid.NewGuid().ToString("N"),
                toolName: "chat_engine_suggest_fixes_with_target",
                shortDescription: GetToolShortDescription("chat_engine_suggest_fixes_with_target"));
        }

        if (!result.Success)
        {
            return resultJson;
        }

        ProposalReadyChatResult proposalReady = ChatEngineResultAdapter.ToProposalReady(
            result,
            "suggest_fixes_with_target",
            GetToolShortDescription("chat_engine_suggest_fixes_with_target"));
        ProposeTextEditResponse proposalResponse = await _pipe.ProposeTextEditAsync(
            filePath,
            originalText,
            proposalReady.SuggestedText ?? string.Empty,
            ct).ConfigureAwait(false);

        if (!proposalResponse.Success)
        {
            return BuildChatEngineResultJson(
                success: false,
                content: null,
                error: ChatEngineSuggestFixesWithTargetFailureError,
                errorCode: ChatEngineChatFailureErrorCode,
                requestId: result.RequestId ?? Guid.NewGuid().ToString("N"),
                toolName: "chat_engine_suggest_fixes_with_target",
                shortDescription: GetToolShortDescription("chat_engine_suggest_fixes_with_target"));
        }

        return BuildChatEngineResultJson(
            success: true,
            content: "Proposal created",
            error: null,
            errorCode: null,
            requestId: result.RequestId ?? Guid.NewGuid().ToString("N"),
            toolName: "chat_engine_suggest_fixes_with_target",
            shortDescription: GetToolShortDescription("chat_engine_suggest_fixes_with_target"));
    }

    [McpServerTool(Name = "chat_engine_explain_code")]
    [Description("Sends code or text through Adventures.ChatEngine with an explanation prompt and returns a concise explanation.")]
    public async Task<string> ChatEngineExplainCodeAsync(
        [Description("The code or text to explain.")] string text,
        CancellationToken ct)
    {
        return await ExecuteChatEngineToolAsync(
            input: text,
            ct: ct,
            invalidInputError: ChatEngineExplainCodeInvalidInputError,
            failureError: ChatEngineExplainCodeFailureError,
            logToolName: "chat_engine_explain_code",
            requestMessageFactory: static input => $"Explain the following code clearly and concisely:\n\n{input}").ConfigureAwait(false);
    }

    [McpServerTool(Name = "chat_engine_explain_error")]
    [Description("Sends compiler or diagnostic text through Adventures.ChatEngine with an explanation prompt and returns a concise explanation of the likely cause.")]
    public async Task<string> ChatEngineExplainErrorAsync(
        [Description("The compiler or diagnostic error text to explain.")] string text,
        CancellationToken ct)
    {
        return await ExecuteChatEngineToolAsync(
            input: text,
            ct: ct,
            invalidInputError: ChatEngineExplainErrorInvalidInputError,
            failureError: ChatEngineExplainErrorFailureError,
            logToolName: "chat_engine_explain_error",
            requestMessageFactory: static input => $"Explain the following compiler or diagnostic error and suggest the likely cause:\n\n{input}").ConfigureAwait(false);
    }

    [McpServerTool(Name = "chat_engine_suggest_error_fix")]
    [Description("Sends compiler or diagnostic text through Adventures.ChatEngine with a likely-fix prompt and returns a suggested fix with explanation.")]
    public async Task<string> ChatEngineSuggestErrorFixAsync(
        [Description("The compiler or diagnostic error text to explain and suggest a fix for.")] string text,
        CancellationToken ct)
    {
        return await ExecuteChatEngineToolAsync(
            input: text,
            ct: ct,
            invalidInputError: ChatEngineSuggestErrorFixInvalidInputError,
            failureError: ChatEngineSuggestErrorFixFailureError,
            logToolName: "chat_engine_suggest_error_fix",
            requestMessageFactory: static input => $"Given the following compiler or diagnostic error, suggest a likely fix and explain why:\n\n{input}").ConfigureAwait(false);
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

    private sealed class BridgeToolInventoryDiagnosticResult
    {
        public bool Success { get; set; }

        public string RequestId { get; set; } = string.Empty;

        public string ToolName { get; set; } = string.Empty;

        public DateTimeOffset CapturedAtUtc { get; set; }

        public int ToolCount { get; set; }

        public IReadOnlyList<BridgeToolInventoryItem> Tools { get; set; } = Array.Empty<BridgeToolInventoryItem>();
    }

    private sealed class EmptyBridgeToolInventoryService : IBridgeToolInventoryService
    {
        public static EmptyBridgeToolInventoryService Instance { get; } = new();

        private EmptyBridgeToolInventoryService()
        {
        }

        public BridgeToolCatalogSnapshot GetSnapshot()
        {
            return new BridgeToolCatalogSnapshot
            {
                Tools = Array.Empty<BridgeToolInventoryItem>()
            };
        }
    }

    private sealed class EmptyBridgeToolExecutor : IBridgeToolExecutor
    {
        public static EmptyBridgeToolExecutor Instance { get; } = new();

        private EmptyBridgeToolExecutor()
        {
        }

        public Task<BridgeToolResult> ExecuteAsync(BridgeToolRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(BridgeToolResult.Failed(
                request,
                "BridgeToolExecutorUnavailable",
                "Bridge tool executor is not registered."));
        }
    }
}
