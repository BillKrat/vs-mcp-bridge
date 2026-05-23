using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
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
    private static readonly string[] IgnoredDocumentSelectionDirectoryNames = [".git", ".vs", "bin", "obj", "node_modules"];

    private readonly IPipeClient _pipe;
    private readonly IChatEngine _chatEngine;
    private readonly IBridgeToolInventoryService _toolInventory;
    private readonly IBridgeToolExecutor _bridgeToolExecutor;
    private readonly ILogger _logger;
    private readonly string _repositoryRoot;

    public VsTools(
        IPipeClient pipe,
        IChatEngine chatEngine,
        ILogger logger,
        IBridgeToolInventoryService? toolInventory = null,
        IBridgeToolExecutor? bridgeToolExecutor = null,
        string? repositoryRoot = null)
    {
        _pipe = pipe;
        _chatEngine = chatEngine;
        _toolInventory = toolInventory ?? EmptyBridgeToolInventoryService.Instance;
        _bridgeToolExecutor = bridgeToolExecutor ?? EmptyBridgeToolExecutor.Instance;
        _logger = logger;
        _repositoryRoot = Path.GetFullPath(repositoryRoot ?? Directory.GetCurrentDirectory());
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

    [McpServerTool(Name = "bridge_select_repo_documents")]
    [Description("Selects deterministic repo-root-relative document metadata from explicit include/exclude patterns. Does not return file contents, search content, execute bridge tools, or mutate state.")]
    public string SelectRepoDocuments(
        [Description("Explicit repo-root-relative include glob patterns, such as docs/blogs/*.md or docs/session-handoffs/2026-05-16-blogai-*.md. Broad root crawls such as **/* are rejected.")] string[] includePatterns,
        [Description("Optional repo-root-relative exclude glob patterns.")] string[]? excludePatterns = null,
        [Description("Optional maximum number of selected files to return. Must be greater than zero when provided.")] int? maxFiles = null,
        [Description("Optional category hints aligned by index with includePatterns.")] string[]? categoryHints = null,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var requestId = Guid.NewGuid().ToString("N");
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation(
            "MCP bridge_select_repo_documents started [RequestId={RequestId}] [IncludePatternCount={IncludePatternCount}].",
            requestId,
            includePatterns?.Length ?? 0);

        try
        {
            if (includePatterns is not { Length: > 0 })
            {
                return SerializeDocumentSelectionFailure(
                    requestId,
                    "InvalidRequest",
                    "bridge_select_repo_documents requires at least one explicit include pattern.",
                    stopwatch);
            }

            if (maxFiles.HasValue && maxFiles.Value <= 0)
            {
                return SerializeDocumentSelectionFailure(
                    requestId,
                    "InvalidRequest",
                    "bridge_select_repo_documents maxFiles must be greater than zero when provided.",
                    stopwatch);
            }

            var includeSpecs = includePatterns
                .Select((pattern, index) => CreateDocumentSelectionPattern(pattern, categoryHints?.ElementAtOrDefault(index), isInclude: true))
                .ToArray();
            var excludeSpecs = (excludePatterns ?? Array.Empty<string>())
                .Select(pattern => CreateDocumentSelectionPattern(pattern, categoryHint: null, isInclude: false))
                .ToArray();
            var selected = new Dictionary<string, BridgeDocumentSelectionCandidate>(StringComparer.OrdinalIgnoreCase);

            foreach (var includeSpec in includeSpecs)
            {
                ct.ThrowIfCancellationRequested();

                foreach (var candidatePath in EnumerateDocumentSelectionCandidates(includeSpec, ct))
                {
                    var relativePath = ToRepoRelativePath(candidatePath);
                    if (string.IsNullOrWhiteSpace(relativePath)
                        || IsIgnoredDocumentSelectionPath(relativePath)
                        || !includeSpec.IsMatch(relativePath)
                        || excludeSpecs.Any(excludeSpec => excludeSpec.IsMatch(relativePath)))
                    {
                        continue;
                    }

                    if (!selected.ContainsKey(relativePath))
                    {
                        selected[relativePath] = new BridgeDocumentSelectionCandidate(
                            candidatePath,
                            relativePath,
                            includeSpec.Pattern,
                            includeSpec.CategoryHint);
                    }
                }
            }

            var orderedCandidates = selected.Values
                .OrderBy(candidate => candidate.RelativePath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(candidate => candidate.RelativePath, StringComparer.Ordinal)
                .ToArray();
            var limited = maxFiles.HasValue && orderedCandidates.Length > maxFiles.Value;
            var returnedCandidates = maxFiles.HasValue
                ? orderedCandidates.Take(maxFiles.Value).ToArray()
                : orderedCandidates;
            var documents = returnedCandidates
                .Select(ToDocumentSelectionItem)
                .ToArray();

            stopwatch.Stop();
            _logger.LogInformation(
                "MCP bridge_select_repo_documents completed [RequestId={RequestId}] [ElapsedMs={ElapsedMs}] [SelectedCount={SelectedCount}] [ReturnedCount={ReturnedCount}] [Limited={Limited}].",
                requestId,
                stopwatch.ElapsedMilliseconds,
                orderedCandidates.Length,
                documents.Length,
                limited);

            return JsonSerializer.Serialize(
                new BridgeDocumentSelection
                {
                    Success = true,
                    RequestId = requestId,
                    ToolName = "bridge_select_repo_documents",
                    CapturedAtUtc = DateTimeOffset.UtcNow,
                    RepositoryRoot = _repositoryRoot,
                    IncludePatterns = includeSpecs.Select(spec => spec.Pattern).ToArray(),
                    ExcludePatterns = excludeSpecs.Select(spec => spec.Pattern).ToArray(),
                    MaxFiles = maxFiles,
                    CandidateCount = orderedCandidates.Length,
                    SelectedCount = documents.Length,
                    Limited = limited,
                    Documents = documents
                },
                InventoryJsonOptions);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "MCP bridge_select_repo_documents canceled [RequestId={RequestId}] [ElapsedMs={ElapsedMs}].",
                requestId,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (ArgumentException ex)
        {
            return SerializeDocumentSelectionFailure(requestId, "InvalidRequest", ex.Message, stopwatch);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "MCP bridge_select_repo_documents failed [RequestId={RequestId}] [ElapsedMs={ElapsedMs}].",
                requestId,
                stopwatch.ElapsedMilliseconds);

            return JsonSerializer.Serialize(
                new BridgeDocumentSelection
                {
                    Success = false,
                    RequestId = requestId,
                    ToolName = "bridge_select_repo_documents",
                    CapturedAtUtc = DateTimeOffset.UtcNow,
                    RepositoryRoot = _repositoryRoot,
                    ErrorCode = "DocumentSelectionFailed",
                    Message = "MCP document selection failed. Review the MCP log for details."
                },
                InventoryJsonOptions);
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

    [McpServerTool(Name = "bridge_preview_document_update")]
    [Description("Executes the compiled bridge.previewDocumentUpdate tool through BridgeToolExecutor for an explicit repo-relative target. Generates preview and diff only; never writes files.")]
    public async Task<string> PreviewDocumentUpdateAsync(
        [Description("Explicit repo-root-relative path for the target document. Absolute paths, parent traversal, and wildcards are rejected.")] string targetPath,
        [Description("Optional exact original/current content expected by the caller. Required when expectedContentHash is not supplied.")] string? expectedContent = null,
        [Description("Optional SHA-256 hash of the expected current content. Required when expectedContent is not supplied.")] string? expectedContentHash = null,
        [Description("Complete proposed replacement content for the full document. No patch is applied.")] string? replacementContent = null,
        [Description("Optional human-readable operation description for audit/correlation context.")] string? description = null,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var requestId = Guid.NewGuid().ToString("N");
        var operationId = Guid.NewGuid().ToString("N");
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation(
            "MCP bridge_preview_document_update started [RequestId={RequestId}] [OperationId={OperationId}].",
            requestId,
            operationId);

        var arguments = new Dictionary<string, object?>
        {
            ["targetPath"] = targetPath
        };

        if (expectedContent != null)
            arguments["expectedContent"] = expectedContent;

        if (!string.IsNullOrWhiteSpace(expectedContentHash))
            arguments["expectedContentHash"] = expectedContentHash;

        if (replacementContent != null)
            arguments["replacementContent"] = replacementContent;

        if (!string.IsNullOrWhiteSpace(description))
            arguments["description"] = description;

        var request = new BridgeToolRequest
        {
            ToolId = PreviewDocumentUpdateTool.ToolId,
            RequestId = requestId,
            OperationId = operationId,
            Arguments = arguments
        };

        try
        {
            var result = await _bridgeToolExecutor.ExecuteAsync(request, ct).ConfigureAwait(false);
            stopwatch.Stop();
            _logger.LogInformation(
                "MCP bridge_preview_document_update completed [RequestId={RequestId}] [OperationId={OperationId}] [Success={Success}] [ElapsedMs={ElapsedMs}].",
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
                "MCP bridge_preview_document_update canceled [RequestId={RequestId}] [OperationId={OperationId}] [ElapsedMs={ElapsedMs}].",
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
                "MCP bridge_preview_document_update failed [RequestId={RequestId}] [OperationId={OperationId}] [ElapsedMs={ElapsedMs}].",
                requestId,
                operationId,
                stopwatch.ElapsedMilliseconds);

            var result = BridgeToolResult.Failed(
                request,
                "McpWrapperFailed",
                "MCP preview document update failed before the bridge tool completed. Review the MCP log for details.");
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

    private string SerializeDocumentSelectionFailure(string requestId, string errorCode, string message, Stopwatch stopwatch)
    {
        stopwatch.Stop();
        _logger.LogWarning(
            "MCP bridge_select_repo_documents returned failure [RequestId={RequestId}] [ElapsedMs={ElapsedMs}] [ErrorCode={ErrorCode}].",
            requestId,
            stopwatch.ElapsedMilliseconds,
            errorCode);

        return JsonSerializer.Serialize(
            new BridgeDocumentSelection
            {
                Success = false,
                RequestId = requestId,
                ToolName = "bridge_select_repo_documents",
                CapturedAtUtc = DateTimeOffset.UtcNow,
                RepositoryRoot = _repositoryRoot,
                ErrorCode = errorCode,
                Message = message
            },
            InventoryJsonOptions);
    }

    private BridgeDocumentSelectionItem ToDocumentSelectionItem(BridgeDocumentSelectionCandidate candidate)
    {
        var fileInfo = new FileInfo(candidate.FullPath);
        return new BridgeDocumentSelectionItem
        {
            RelativePath = candidate.RelativePath,
            SourcePattern = candidate.SourcePattern,
            CategoryHint = candidate.CategoryHint,
            SizeBytes = fileInfo.Length,
            LineCount = TryCountLines(candidate.FullPath)
        };
    }

    private IEnumerable<string> EnumerateDocumentSelectionCandidates(DocumentSelectionPattern spec, CancellationToken ct)
    {
        if (!spec.HasWildcard)
        {
            var exactPath = Path.GetFullPath(Path.Combine(_repositoryRoot, spec.Pattern.Replace('/', Path.DirectorySeparatorChar)));
            if (IsUnderRepositoryRoot(exactPath) && File.Exists(exactPath))
            {
                yield return exactPath;
            }

            yield break;
        }

        var basePath = Path.GetFullPath(Path.Combine(_repositoryRoot, spec.BaseDirectory.Replace('/', Path.DirectorySeparatorChar)));
        if (!IsUnderRepositoryRoot(basePath) || !Directory.Exists(basePath))
        {
            yield break;
        }

        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            MatchCasing = MatchCasing.CaseInsensitive,
            AttributesToSkip = FileAttributes.Hidden | FileAttributes.System
        };

        foreach (var path in Directory.EnumerateFiles(basePath, "*", options))
        {
            ct.ThrowIfCancellationRequested();
            var relativePath = ToRepoRelativePath(path);
            if (!IsIgnoredDocumentSelectionPath(relativePath))
            {
                yield return path;
            }
        }
    }

    private string ToRepoRelativePath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (!IsUnderRepositoryRoot(fullPath))
        {
            return string.Empty;
        }

        return Path.GetRelativePath(_repositoryRoot, fullPath).Replace('\\', '/');
    }

    private bool IsUnderRepositoryRoot(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var root = _repositoryRoot.EndsWith(Path.DirectorySeparatorChar)
            ? _repositoryRoot
            : _repositoryRoot + Path.DirectorySeparatorChar;
        return fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase)
            || string.Equals(fullPath, _repositoryRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static DocumentSelectionPattern CreateDocumentSelectionPattern(string pattern, string? categoryHint, bool isInclude)
    {
        var normalizedPattern = NormalizeDocumentSelectionPattern(pattern, isInclude);
        var wildcardIndex = normalizedPattern.IndexOfAny(['*', '?']);
        var hasWildcard = wildcardIndex >= 0;
        var baseDirectory = hasWildcard
            ? GetWildcardBaseDirectory(normalizedPattern, wildcardIndex)
            : Path.GetDirectoryName(normalizedPattern)?.Replace('\\', '/') ?? string.Empty;

        return new DocumentSelectionPattern(
            normalizedPattern,
            baseDirectory,
            categoryHint,
            hasWildcard,
            GlobToRegex(normalizedPattern));
    }

    private static string NormalizeDocumentSelectionPattern(string pattern, bool isInclude)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new ArgumentException("Document selection patterns must not be empty.", nameof(pattern));
        }

        var normalized = pattern.Trim().Replace('\\', '/').TrimStart('/');
        if (Path.IsPathRooted(pattern) || Regex.IsMatch(normalized, "^[A-Za-z]:", RegexOptions.CultureInvariant))
        {
            throw new ArgumentException("Document selection patterns must be repo-root-relative.", nameof(pattern));
        }

        var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0 || segments.Any(segment => segment == "." || segment == ".."))
        {
            throw new ArgumentException("Document selection patterns must not contain current-directory or parent-directory segments.", nameof(pattern));
        }

        var broadPatterns = new HashSet<string>(StringComparer.Ordinal)
        {
            "*",
            "*.*",
            "**",
            "**/*",
            "**/*.*"
        };

        if (isInclude && broadPatterns.Contains(normalized))
        {
            throw new ArgumentException("Document selection include patterns must target an explicit repo subset, not the whole repository.", nameof(pattern));
        }

        if (isInclude && normalized.IndexOfAny(['*', '?']) >= 0 && !normalized.Contains('/', StringComparison.Ordinal))
        {
            throw new ArgumentException("Wildcard document selection include patterns must include an explicit directory segment.", nameof(pattern));
        }

        return normalized;
    }

    private static string GetWildcardBaseDirectory(string normalizedPattern, int wildcardIndex)
    {
        var slashIndex = normalizedPattern.LastIndexOf('/', wildcardIndex);
        if (slashIndex < 0)
        {
            return string.Empty;
        }

        return normalizedPattern[..slashIndex];
    }

    private static Regex GlobToRegex(string pattern)
    {
        var builder = new StringBuilder("^");
        for (var i = 0; i < pattern.Length; i++)
        {
            var current = pattern[i];
            if (current == '*')
            {
                if (i + 1 < pattern.Length && pattern[i + 1] == '*')
                {
                    if (i + 2 < pattern.Length && pattern[i + 2] == '/')
                    {
                        builder.Append("(?:.*/)?");
                        i += 2;
                    }
                    else
                    {
                        builder.Append(".*");
                        i++;
                    }
                }
                else
                {
                    builder.Append("[^/]*");
                }
            }
            else if (current == '?')
            {
                builder.Append("[^/]");
            }
            else
            {
                builder.Append(Regex.Escape(current.ToString()));
            }
        }

        builder.Append('$');
        return new Regex(builder.ToString(), RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    }

    private static bool IsIgnoredDocumentSelectionPath(string relativePath)
    {
        var segments = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Any(segment => IgnoredDocumentSelectionDirectoryNames.Contains(segment, StringComparer.OrdinalIgnoreCase));
    }

    private static int? TryCountLines(string path)
    {
        try
        {
            return File.ReadLines(path).Count();
        }
        catch
        {
            return null;
        }
    }

    private sealed record DocumentSelectionPattern(
        string Pattern,
        string BaseDirectory,
        string? CategoryHint,
        bool HasWildcard,
        Regex Regex)
    {
        public bool IsMatch(string relativePath) => Regex.IsMatch(relativePath);
    }

    private sealed record BridgeDocumentSelectionCandidate(
        string FullPath,
        string RelativePath,
        string SourcePattern,
        string? CategoryHint);

    private sealed class BridgeDocumentSelection
    {
        public bool Success { get; set; }

        public string RequestId { get; set; } = string.Empty;

        public string ToolName { get; set; } = string.Empty;

        public DateTimeOffset CapturedAtUtc { get; set; }

        public string RepositoryRoot { get; set; } = string.Empty;

        public IReadOnlyList<string> IncludePatterns { get; set; } = Array.Empty<string>();

        public IReadOnlyList<string> ExcludePatterns { get; set; } = Array.Empty<string>();

        public int? MaxFiles { get; set; }

        public int CandidateCount { get; set; }

        public int SelectedCount { get; set; }

        public bool Limited { get; set; }

        public IReadOnlyList<BridgeDocumentSelectionItem> Documents { get; set; } = Array.Empty<BridgeDocumentSelectionItem>();

        public string? ErrorCode { get; set; }

        public string? Message { get; set; }
    }

    private sealed class BridgeDocumentSelectionItem
    {
        public string RelativePath { get; set; } = string.Empty;

        public string SourcePattern { get; set; } = string.Empty;

        public string? CategoryHint { get; set; }

        public long SizeBytes { get; set; }

        public int? LineCount { get; set; }
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
