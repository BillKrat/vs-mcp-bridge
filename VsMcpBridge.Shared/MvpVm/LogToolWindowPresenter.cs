using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using VsMcpBridge.Shared.Constants;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.Loggers;
using VsMcpBridge.Shared.Models;

namespace VsMcpBridge.Shared.MvpVm
{
    public class LogToolWindowPresenter : ILogToolWindowPresenter
    {
        private const string InitialLogMessage = "VS MCP Bridge log will appear here.";
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private readonly IThreadHelper _threadHelper;
        private readonly IConfiguration? _configuration;
        private readonly IBridgeLogSink? _logSink;
        private readonly IChatRequestService? _chatRequestService;
        private readonly bool _logRawPromptResponse;
        private readonly IProposalManager _proposalManager;

        public LogToolWindowPresenter(IServiceProvider serviceProvider, ILogger logger, IThreadHelper threadHelper, ILogToolWindowViewModel logToolWindowViewModel)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _threadHelper = threadHelper;
            _configuration = _serviceProvider.GetService<IConfiguration>();
            _logSink = _serviceProvider.GetService<IBridgeLogSink>();
            _chatRequestService = _serviceProvider.GetService<IChatRequestService>();
            _logRawPromptResponse = TryGetBooleanConfiguration(_configuration, ConfigurationKeys.AuditLogRawPromptResponse);
            LogToolWindowViewModel = logToolWindowViewModel;
            _proposalManager = _serviceProvider.GetService<IProposalManager>()
                ?? new ProposalManager(_serviceProvider, _logger, _threadHelper, LogToolWindowViewModel);
            if (_proposalManager.HasProposalFilePicker)
                LogToolWindowViewModel.SetProposalBrowseHandler(_proposalManager.BrowseProposalFile);
            else
                LogToolWindowViewModel.SetProposalBrowseHandler(null);
            LogToolWindowViewModel.SetProposalRemoveHandler(_proposalManager.RemoveSelectedProposalFile);
            LogToolWindowViewModel.SetProposalResetHandler(_proposalManager.ResetCurrentRequestState);
            LogToolWindowViewModel.SetNewChatHandler(OnNewChatRequested);
            LogToolWindowViewModel.SetProposalSubmissionHandler(OnSubmitProposalRequested);
            LogToolWindowViewModel.SetOpenGitChangesHandler(OnOpenGitChangesRequested);
            LogToolWindowViewModel.SetApprovalRequestHandlers(_proposalManager.ApproveRequested, _proposalManager.RejectRequested);

            if (LogToolWindowViewModel is INotifyPropertyChanged notifyPropertyChanged)
                notifyPropertyChanged.PropertyChanged += OnViewModelPropertyChanged;

            if (_logSink != null)
                _logSink.EntryLogged += OnLogEntryLogged;
        }

        public ILogToolWindowControl LogToolWindowControl { get; set; } = null!;

        public ILogToolWindowViewModel LogToolWindowViewModel { get; set; }

        public void Initialize()
        {
            _logger.LogInformation("Initializing VS MCP Bridge tool window...");

            LogToolWindowControl.DataContext = LogToolWindowViewModel;
            LogToolWindowViewModel.LogText = string.Empty;
            _proposalManager.Initialize();

            _logger.LogInformation("VS MCP Bridge tool window Initialized.");
        }

        private void OnLogEntryLogged(BridgeLogEntry entry)
        {
            if (entry == null)
                return;

            AppendLog(BridgeLogFormatter.FormatLine(entry.TimestampUtc, entry.Level, entry.CategoryName, entry.Message));
            if (entry.Exception != null)
                AppendLog(entry.Exception.ToString());
        }

        public void AppendLog(string message)
        {
            RunOnUiThread(() =>
            {
                var existingLog = LogToolWindowViewModel.LogText;
                LogToolWindowViewModel.LogText =
                    string.IsNullOrWhiteSpace(existingLog) || string.Equals(existingLog, InitialLogMessage, StringComparison.Ordinal)
                        ? message
                        : $"{existingLog}{Environment.NewLine}{message}";
            });
        }

        public void ShowApprovalPrompt(string description, string? originalSegment, string? updatedSegment, IReadOnlyList<ProposalReviewedChange>? reviewedChanges, Action onApprove, Action onReject, IReadOnlyList<string>? includedFiles = null, string? requestId = null)
        {
            _proposalManager.ShowApprovalPrompt(description, originalSegment, updatedSegment, reviewedChanges, onApprove, onReject, includedFiles, requestId);
        }

        public void ShowStatusMessage(string message)
        {
            RunOnUiThread(() => LogToolWindowViewModel.StatusMessage = message);
        }

        public void CompleteProposalCycle(string statusMessage, string? requestId = null)
        {
            _proposalManager.CompleteProposalCycle(statusMessage, requestId);
        }

        private void OnSubmitProposalRequested()
        {
            _ = _proposalManager.SubmitProposalAsync(TryDispatchPromptRequestAsync, AppendPromptActivityEntry);
        }

        private void OnNewChatRequested()
        {
            _proposalManager.StartNewChat();
        }

        private void OnOpenGitChangesRequested()
        {
            _ = OpenGitChangesAsync();
        }

        private async Task OpenGitChangesAsync()
        {
            try
            {
                var vsService = _serviceProvider.GetRequiredService<IVsService>();
                await vsService.OpenGitChangesAsync();
            }
            catch (Exception ex)
            {
                LogToolWindowViewModel.StatusMessage = "Unable to open Git Changes from the bridge surface. Review the bridge log for details.";
                _logger.LogError(ex, "Open Git Changes failed.");
            }
        }

        private async Task<bool> TryDispatchPromptRequestAsync(string submittedRequestText)
        {
            var normalizedPrompt = submittedRequestText.Trim().ToLowerInvariant();
            var requestId = _proposalManager.ActiveManualRequestId;
            _logger.LogTrace("Prompt-box request dispatch evaluating route [RequestId={RequestId}] [NormalizedPrompt={NormalizedPrompt}] [HasChatRequestService={HasChatRequestService}] [SelectedFileCount={SelectedFileCount}].",
                requestId,
                normalizedPrompt,
                _chatRequestService != null,
                _proposalManager.SelectedFileCount);
            var vsService = _serviceProvider.GetRequiredService<IVsService>();

            switch (normalizedPrompt)
            {
                case "what is the active file":
                case "show active file":
                    {
                        _logger.LogTrace("Prompt-box request routed to built-in active file handler [RequestId={RequestId}].", requestId);
                        var response = await vsService.GetActiveDocumentAsync();
                        CompletePromptRequest(BuildActiveFileSummary(response), requestId);
                        return true;
                    }
                case "what is the selected text":
                case "show selected text":
                    {
                        _logger.LogTrace("Prompt-box request routed to built-in selected text handler [RequestId={RequestId}].", requestId);
                        var response = await vsService.GetSelectedTextAsync();
                        CompletePromptRequest(BuildSelectedTextSummary(response), requestId);
                        return true;
                    }
                case "list solution projects":
                    {
                        _logger.LogTrace("Prompt-box request routed to built-in project list handler [RequestId={RequestId}].", requestId);
                        var response = await vsService.ListSolutionProjectsAsync();
                        CompletePromptRequest(BuildProjectListSummary(response), requestId);
                        return true;
                    }
                case "show error list":
                    {
                        _logger.LogTrace("Prompt-box request routed to built-in error list handler [RequestId={RequestId}].", requestId);
                        var response = await vsService.GetErrorListAsync();
                        CompletePromptRequest(BuildErrorListSummary(response), requestId);
                        return true;
                    }
                default:
                    if (_chatRequestService != null)
                    {
                        _logger.LogTrace("Prompt-box request routed to chat request service [RequestId={RequestId}] [RequestLength={RequestLength}].", requestId, submittedRequestText.Length);
                        var response = await _chatRequestService.SendAsync(submittedRequestText, requestId);
                        _logger.LogTrace("Prompt-box chat request service returned response [RequestId={RequestId}] [ResponseLength={ResponseLength}].", requestId, response?.Length ?? 0);
                        CompletePromptRequest(response, requestId);
                        return true;
                    }

                    if (_proposalManager.SelectedFileCount == 0)
                    {
                        _logger.LogTrace("Prompt-box request had no matching built-in route or chat service and will return unsupported-request guidance [RequestId={RequestId}].", requestId);
                        CompletePromptRequest("Unsupported request. Try 'what is the active file', 'what is the selected text', 'list solution projects', or 'show error list'.", requestId);
                        return true;
                    }

                    _logger.LogTrace("Prompt-box request will fall through to proposal submission workflow [RequestId={RequestId}] [SelectedFileCount={SelectedFileCount}].", requestId, _proposalManager.SelectedFileCount);
                    return false;
            }
        }

        private void CompletePromptRequest(string responseText, string? requestId = null)
        {
            _logger.LogTrace("Prompt-box response is being applied to the visible UI state [RequestId={RequestId}] [ResponseLength={ResponseLength}] [HasContent={HasContent}].",
                requestId,
                responseText?.Length ?? 0,
                !string.IsNullOrWhiteSpace(responseText));
            LogToolWindowViewModel.IsRequestInProgress = false;
            LogToolWindowViewModel.StatusMessage = responseText;
            AppendResponseActivityEntry(responseText);
            _logger.LogTrace("Prompt-box response application completed [RequestId={RequestId}] [StatusMessageLength={StatusMessageLength}] [IsRequestInProgress={IsRequestInProgress}].",
                requestId,
                LogToolWindowViewModel.StatusMessage?.Length ?? 0,
                LogToolWindowViewModel.IsRequestInProgress);
        }

        private void AppendActivityEntry(string message)
        {
            RunOnUiThread(() =>
            {
                var existingLog = LogToolWindowViewModel.LogText;
                LogToolWindowViewModel.LogText =
                    string.IsNullOrWhiteSpace(existingLog)
                        ? message
                        : $"{existingLog}{Environment.NewLine}{Environment.NewLine}{message}";
            });
        }

        private void AppendPromptActivityEntry(string submittedRequestText)
        {
            if (_logRawPromptResponse)
                AppendActivityEntry($"[audit] > {submittedRequestText}");
        }

        private void AppendResponseActivityEntry(string responseText)
        {
            if (_logRawPromptResponse)
                AppendActivityEntry($"[audit] {responseText}");
        }

        private static bool TryGetBooleanConfiguration(IConfiguration? configuration, string key)
        {
            if (configuration == null)
                return false;

            var rawValue = configuration[key];
            return bool.TryParse(rawValue, out var parsed) && parsed;
        }

        private static string BuildActiveFileSummary(GetActiveDocumentResponse response)
        {
            if (!response.Success)
                return string.IsNullOrWhiteSpace(response.ErrorMessage) ? "Unable to determine the active file." : response.ErrorMessage;

            return string.IsNullOrWhiteSpace(response.FilePath)
                ? "No active file."
                : $"Active file: {response.FilePath}";
        }

        private static string BuildSelectedTextSummary(GetSelectedTextResponse response)
        {
            if (!response.Success)
                return string.IsNullOrWhiteSpace(response.ErrorMessage) ? "Unable to determine the selected text." : response.ErrorMessage;

            if (string.IsNullOrWhiteSpace(response.SelectedText))
                return string.IsNullOrWhiteSpace(response.FilePath)
                    ? "No selected text."
                    : $"No selected text in {response.FilePath}.";

            return string.IsNullOrWhiteSpace(response.FilePath)
                ? $"Selected text:{Environment.NewLine}{response.SelectedText}"
                : $"Selected text from {response.FilePath}:{Environment.NewLine}{response.SelectedText}";
        }

        private static string BuildProjectListSummary(ListSolutionProjectsResponse response)
        {
            if (!response.Success)
                return string.IsNullOrWhiteSpace(response.ErrorMessage) ? "Unable to list solution projects." : response.ErrorMessage;

            if (response.Projects == null || response.Projects.Count == 0)
                return "No solution projects found.";

            return "Solution projects:" + Environment.NewLine + string.Join(
                Environment.NewLine,
                response.Projects.Select(project => $"- {project.Name}"));
        }

        private static string BuildErrorListSummary(GetErrorListResponse response)
        {
            if (!response.Success)
                return string.IsNullOrWhiteSpace(response.ErrorMessage) ? "Unable to read the error list." : response.ErrorMessage;

            if (response.Diagnostics == null || response.Diagnostics.Count == 0)
                return "Error list is empty.";

            return "Error list:" + Environment.NewLine + string.Join(
                Environment.NewLine,
                response.Diagnostics.Select(diagnostic =>
                {
                    var filePrefix = string.IsNullOrWhiteSpace(diagnostic.File) ? string.Empty : $"{diagnostic.File}: ";
                    return $"- {diagnostic.Severity}: {filePrefix}{diagnostic.Description}";
                }));
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            _proposalManager.HandleViewModelPropertyChanged(e);
        }

        public void RunOnUiThread(Action action)
        {
            if (_threadHelper.CheckAccess())
            {
                action();
                return;
            }

            _threadHelper.Run(async () =>
            {
                await _threadHelper.SwitchToMainThreadAsync();
                action();
            });
        }
    }
}
