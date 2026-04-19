using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.Models;

namespace VsMcpBridge.Shared.MvpVm
{
    public class LogToolWindowPresenter : ILogToolWindowPresenter
    {
        private const string InitialLogMessage = "VS MCP Bridge log will appear here.";

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private readonly IThreadHelper _threadHelper;
        private readonly IProposalDraftState? _proposalDraftState;
        private readonly IProposalFilePicker? _proposalFilePicker;
        private readonly List<ProposalEditorFileDraft> _proposalFileDrafts = new();
        private readonly HashSet<string> _suppressedRequestIds = new(StringComparer.Ordinal);
        private Action? _pendingApproveAction;
        private Action? _pendingRejectAction;
        private bool _isUpdatingProposalEditor;
        private string? _activeManualRequestId;

        public LogToolWindowPresenter(IServiceProvider serviceProvider, ILogger logger, IThreadHelper threadHelper, ILogToolWindowViewModel logToolWindowViewModel)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _threadHelper = threadHelper;
            _proposalDraftState = _serviceProvider.GetService<IProposalDraftState>();
            _proposalFilePicker = _serviceProvider.GetService<IProposalFilePicker>();
            LogToolWindowViewModel = logToolWindowViewModel;
            if (_proposalFilePicker != null)
            LogToolWindowViewModel.SetProposalBrowseHandler(OnBrowseProposalFileRequested);
            else
                LogToolWindowViewModel.SetProposalBrowseHandler(null);
            LogToolWindowViewModel.SetProposalRemoveHandler(OnRemoveProposalFileRequested);
            LogToolWindowViewModel.SetProposalResetHandler(OnResetProposalRequested);
            LogToolWindowViewModel.SetNewChatHandler(OnNewChatRequested);
            LogToolWindowViewModel.SetProposalSubmissionHandler(OnSubmitProposalRequested);
            LogToolWindowViewModel.SetOpenGitChangesHandler(OnOpenGitChangesRequested);
            LogToolWindowViewModel.SetApprovalRequestHandlers(OnApproveRequested, OnRejectRequested);

            if (LogToolWindowViewModel is INotifyPropertyChanged notifyPropertyChanged)
                notifyPropertyChanged.PropertyChanged += OnViewModelPropertyChanged;
        }

        public ILogToolWindowControl LogToolWindowControl { get; set; } = null!;

        public ILogToolWindowViewModel LogToolWindowViewModel { get; set; }

        public void Initialize()
        {
            _logger.LogInformation("Initializing VS MCP Bridge tool window...");

            LogToolWindowControl.DataContext = LogToolWindowViewModel;
            LogToolWindowViewModel.LogText = string.Empty;
            SyncProposalDraftState();

            _logger.LogInformation("VS MCP Bridge tool window Initialized.");
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
            RunOnUiThread(() =>
            {
                if (ShouldIgnoreRequestUpdate(requestId))
                    return;

                _pendingApproveAction = onApprove;
                _pendingRejectAction = onReject;
                ClearCompletedProposalPreview();
                LogToolWindowViewModel.IsRequestInProgress = false;
                LogToolWindowViewModel.StatusMessage = string.Empty;
                LogToolWindowViewModel.PendingApprovalDescription = description;
                LogToolWindowViewModel.PendingApprovalOriginalSegment = originalSegment ?? string.Empty;
                LogToolWindowViewModel.PendingApprovalUpdatedSegment = updatedSegment ?? string.Empty;
                LogToolWindowViewModel.PendingApprovalReviewedChanges = CloneReviewedChanges(reviewedChanges);
                LogToolWindowViewModel.PendingApprovalIncludedFiles = CloneIncludedFiles(includedFiles ?? LogToolWindowViewModel.ProposalSelectedFiles);
                LogToolWindowViewModel.HasPendingApproval = true;
                CompleteActiveManualRequest(requestId);
            });
        }

        public void ShowStatusMessage(string message)
        {
            RunOnUiThread(() => LogToolWindowViewModel.StatusMessage = message);
        }

        public void CompleteProposalCycle(string statusMessage, string? requestId = null)
        {
            RunOnUiThread(() =>
            {
                if (ShouldIgnoreRequestUpdate(requestId))
                    return;

                CaptureCompletedProposalPreview();
                ClearApproval();
                LogToolWindowViewModel.IsRequestInProgress = false;

                if (!string.IsNullOrWhiteSpace(LogToolWindowViewModel.ProposalFilePath))
                {
                    ReloadActiveProposalFile(clearStatusMessage: false);
                }
                else
                {
                    LogToolWindowViewModel.IsProposalFileLoaded = false;
                    LogToolWindowViewModel.ProposalOriginalText = string.Empty;
                    LogToolWindowViewModel.ProposalProposedText = string.Empty;
                }

                LogToolWindowViewModel.StatusMessage = statusMessage;
                CompleteActiveManualRequest(requestId);
            });
        }

        private void CaptureCompletedProposalPreview()
        {
            LogToolWindowViewModel.LastCompletedProposalOriginalText = LogToolWindowViewModel.ProposalOriginalText;
            LogToolWindowViewModel.LastCompletedProposalUpdatedText = LogToolWindowViewModel.ProposalProposedText;
            LogToolWindowViewModel.LastCompletedProposalOriginalSegment = LogToolWindowViewModel.PendingApprovalOriginalSegment;
            LogToolWindowViewModel.LastCompletedProposalUpdatedSegment = LogToolWindowViewModel.PendingApprovalUpdatedSegment;
            LogToolWindowViewModel.LastCompletedProposalReviewedChanges = CloneReviewedChanges(LogToolWindowViewModel.PendingApprovalReviewedChanges);
            LogToolWindowViewModel.LastCompletedProposalIncludedFiles = CloneIncludedFiles(LogToolWindowViewModel.PendingApprovalIncludedFiles);
        }

        private void OnApproveRequested()
        {
            Action? approvalAction = null;

            RunOnUiThread(() =>
            {
                approvalAction = _pendingApproveAction;
                _pendingApproveAction = null;
                _pendingRejectAction = null;
            });

            approvalAction?.Invoke();
        }

        private void OnRejectRequested()
        {
            Action? rejectionAction = null;

            RunOnUiThread(() =>
            {
                rejectionAction = _pendingRejectAction;
                _pendingApproveAction = null;
                _pendingRejectAction = null;
            });

            rejectionAction?.Invoke();
        }

        private void OnSubmitProposalRequested()
        {
            _ = SubmitProposalAsync();
        }

        private void OnBrowseProposalFileRequested()
        {
            var selectedPath = _proposalFilePicker?.PickFilePath();
            if (!string.IsNullOrWhiteSpace(selectedPath))
            {
                ClearCompletedProposalPreview();
                AddOrSelectProposalFile(selectedPath!);
            }
        }

        private void OnRemoveProposalFileRequested()
        {
            RemoveSelectedProposalFile();
        }

        private void OnResetProposalRequested()
        {
            ResetCurrentRequestState();
        }

        private void OnNewChatRequested()
        {
            StartNewChat();
        }

        private void OnOpenGitChangesRequested()
        {
            _ = OpenGitChangesAsync();
        }

        private async Task SubmitProposalAsync()
        {
            try
            {
                var submittedRequestText = BuildSubmittedRequestText();
                var requestId = Guid.NewGuid().ToString("N");
                _activeManualRequestId = requestId;
                _suppressedRequestIds.Remove(requestId);
                LogToolWindowViewModel.LastSubmittedRequestText = submittedRequestText;
                LogToolWindowViewModel.IsRequestInProgress = true;
                LogToolWindowViewModel.StatusMessage = string.Empty;
                AppendActivityEntry($"> {submittedRequestText}");

                var toolDispatchHandled = await TryDispatchPromptRequestAsync(submittedRequestText);
                if (toolDispatchHandled)
                {
                    _activeManualRequestId = null;
                    return;
                }

                var fileEdits = _proposalFileDrafts
                    .Select(draft => new ProposalFileEditRequest
                    {
                        FilePath = draft.FilePath,
                        OriginalText = draft.OriginalText,
                        ProposedText = draft.ProposedText
                    })
                    .ToArray();
                var hasMeaningfulChange = fileEdits.Any(
                    fileEdit => !string.Equals(fileEdit.OriginalText, fileEdit.ProposedText, StringComparison.Ordinal));
                var proposalTarget = BuildActiveProposalTarget();

                _logger.LogInformation(
                    $"Manual proposal submission started [RequestId={requestId}] [Target={proposalTarget}] [FileCount={fileEdits.Length}] [HasMeaningfulChange={hasMeaningfulChange}].");

                var activeDraft = GetActiveProposalDraft();
                if (activeDraft != null)
                {
                    _proposalDraftState?.SetActiveFilePath(activeDraft.FilePath);
                    _proposalDraftState?.SetSelectedText(activeDraft.OriginalText);
                }

                var vsService = _serviceProvider.GetRequiredService<IVsService>();
                if (fileEdits.Length == 1)
                    await vsService.ProposeTextEditAsync(requestId, fileEdits[0].FilePath, fileEdits[0].OriginalText, fileEdits[0].ProposedText);
                else
                    await vsService.ProposeTextEditsAsync(requestId, fileEdits);

                if (!hasMeaningfulChange
                    && string.Equals(_activeManualRequestId, requestId, StringComparison.Ordinal)
                    && LogToolWindowViewModel.IsRequestInProgress
                    && !LogToolWindowViewModel.HasPendingApproval
                    && string.IsNullOrWhiteSpace(LogToolWindowViewModel.StatusMessage))
                {
                    LogToolWindowViewModel.IsRequestInProgress = false;
                    LogToolWindowViewModel.StatusMessage = fileEdits.Length == 0
                        ? "No proposal was created because no files were selected."
                        : $"No proposal was created because the proposed text already matches the original content for {proposalTarget}.";
                    _activeManualRequestId = null;

                    _logger.LogWarning(
                        $"Manual proposal submission completed without a reviewable proposal [RequestId={requestId}] [Target={proposalTarget}] [FileCount={fileEdits.Length}] [HasMeaningfulChange={hasMeaningfulChange}].");
                }
            }
            catch (Exception ex)
            {
                LogToolWindowViewModel.IsRequestInProgress = false;
                LogToolWindowViewModel.StatusMessage = $"Request submission failed for {BuildActiveProposalTarget()}. Review the bridge log for details.";
                _activeManualRequestId = null;
                _logger.LogError(ex, $"Manual proposal submission failed for '{BuildActiveProposalTarget()}'.");
            }
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
            var vsService = _serviceProvider.GetRequiredService<IVsService>();

            switch (normalizedPrompt)
            {
                case "what is the active file":
                    {
                        var response = await vsService.GetActiveDocumentAsync();
                        CompletePromptRequest(BuildActiveFileSummary(response));
                        return true;
                    }
                case "list solution projects":
                    {
                        var response = await vsService.ListSolutionProjectsAsync();
                        CompletePromptRequest(BuildProjectListSummary(response));
                        return true;
                    }
                case "show error list":
                    {
                        var response = await vsService.GetErrorListAsync();
                        CompletePromptRequest(BuildErrorListSummary(response));
                        return true;
                    }
                default:
                    if (_proposalFileDrafts.Count == 0)
                    {
                        CompletePromptRequest("Unsupported request. Try 'what is the active file', 'list solution projects', or 'show error list'.");
                        return true;
                    }

                    return false;
            }
        }

        private void CompletePromptRequest(string responseText)
        {
            LogToolWindowViewModel.IsRequestInProgress = false;
            LogToolWindowViewModel.StatusMessage = responseText;
            AppendActivityEntry(responseText);
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

        private static string BuildActiveFileSummary(GetActiveDocumentResponse response)
        {
            if (!response.Success)
                return string.IsNullOrWhiteSpace(response.ErrorMessage) ? "Unable to determine the active file." : response.ErrorMessage;

            return string.IsNullOrWhiteSpace(response.FilePath)
                ? "No active file."
                : $"Active file: {response.FilePath}";
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

        private void ClearApproval()
        {
            _pendingApproveAction = null;
            _pendingRejectAction = null;
            LogToolWindowViewModel.PendingApprovalDescription = string.Empty;
            LogToolWindowViewModel.PendingApprovalOriginalSegment = string.Empty;
            LogToolWindowViewModel.PendingApprovalUpdatedSegment = string.Empty;
            LogToolWindowViewModel.PendingApprovalReviewedChanges = Array.Empty<ProposalReviewedChange>();
            LogToolWindowViewModel.PendingApprovalIncludedFiles = Array.Empty<string>();
            LogToolWindowViewModel.HasPendingApproval = false;
        }

        private void ClearCompletedProposalPreview()
        {
            LogToolWindowViewModel.LastCompletedProposalOriginalText = string.Empty;
            LogToolWindowViewModel.LastCompletedProposalUpdatedText = string.Empty;
            LogToolWindowViewModel.LastCompletedProposalOriginalSegment = string.Empty;
            LogToolWindowViewModel.LastCompletedProposalUpdatedSegment = string.Empty;
            LogToolWindowViewModel.LastCompletedProposalReviewedChanges = Array.Empty<ProposalReviewedChange>();
            LogToolWindowViewModel.LastCompletedProposalIncludedFiles = Array.Empty<string>();
        }

        private static IReadOnlyList<ProposalReviewedChange> CloneReviewedChanges(IReadOnlyList<ProposalReviewedChange>? reviewedChanges)
        {
            return reviewedChanges == null || reviewedChanges.Count == 0
                ? Array.Empty<ProposalReviewedChange>()
                : reviewedChanges.Select(change => new ProposalReviewedChange
                {
                    SequenceNumber = change.SequenceNumber,
                    OriginalSegment = change.OriginalSegment,
                    UpdatedSegment = change.UpdatedSegment
                }).ToArray();
        }

        private static IReadOnlyList<string> CloneIncludedFiles(IReadOnlyList<string>? includedFiles)
        {
            return includedFiles == null || includedFiles.Count == 0
                ? Array.Empty<string>()
                : includedFiles.ToArray();
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ILogToolWindowViewModel.ProposalFilePath))
            {
                if (_isUpdatingProposalEditor)
                    return;

                ClearCompletedProposalPreview();
                _proposalDraftState?.SetActiveFilePath(LogToolWindowViewModel.ProposalFilePath);
                ActivateProposalFile(LogToolWindowViewModel.ProposalFilePath);
            }
            else if (e.PropertyName == nameof(ILogToolWindowViewModel.ProposalOriginalText))
            {
                if (_isUpdatingProposalEditor)
                    return;

                var activeDraft = GetActiveProposalDraft();
                if (activeDraft != null)
                {
                    activeDraft.OriginalText = LogToolWindowViewModel.ProposalOriginalText;
                    UpdateProposalSubmissionState();
                }

                _proposalDraftState?.SetSelectedText(LogToolWindowViewModel.ProposalOriginalText);
            }
            else if (e.PropertyName == nameof(ILogToolWindowViewModel.ProposalProposedText))
            {
                if (_isUpdatingProposalEditor)
                    return;

                var activeDraft = GetActiveProposalDraft();
                if (activeDraft != null)
                {
                    activeDraft.ProposedText = LogToolWindowViewModel.ProposalProposedText;
                    UpdateProposalSubmissionState();
                }
            }
        }

        private void SyncProposalDraftState()
        {
            _proposalDraftState?.SetActiveFilePath(LogToolWindowViewModel.ProposalFilePath);
            _proposalDraftState?.SetSelectedText(LogToolWindowViewModel.ProposalOriginalText);
            if (!string.IsNullOrWhiteSpace(LogToolWindowViewModel.ProposalFilePath))
                ActivateProposalFile(LogToolWindowViewModel.ProposalFilePath);
            else
                UpdateProposalSubmissionState();
        }

        private void ActivateProposalFile(string? filePath, bool clearStatusMessage = true)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                if (_proposalFileDrafts.Count == 0)
                    SetProposalEditorState(string.Empty, string.Empty, string.Empty, isLoaded: false, clearStatusMessage: clearStatusMessage);

                if (clearStatusMessage)
                    LogToolWindowViewModel.StatusMessage = string.Empty;
                return;
            }

            var existingDraft = FindProposalDraft(filePath!);
            if (existingDraft != null)
            {
                ShowProposalDraft(existingDraft, clearStatusMessage);
                return;
            }

            if (_proposalFileDrafts.Count == 1)
            {
                ReplaceSingleProposalFile(filePath!, clearStatusMessage);
                return;
            }

            AddOrSelectProposalFile(filePath!, clearStatusMessage);
        }

        private void AddOrSelectProposalFile(string filePath, bool clearStatusMessage = true)
        {
            var existingDraft = FindProposalDraft(filePath);
            if (existingDraft != null)
            {
                ShowProposalDraft(existingDraft, clearStatusMessage);
                return;
            }

            var draft = new ProposalEditorFileDraft { FilePath = filePath };
            _proposalFileDrafts.Add(draft);
            LoadProposalDraft(draft, clearStatusMessage);
            ShowProposalDraft(draft, clearStatusMessage: false);
        }

        private void ReplaceSingleProposalFile(string filePath, bool clearStatusMessage)
        {
            ProposalEditorFileDraft draft;
            if (_proposalFileDrafts.Count == 0)
            {
                draft = new ProposalEditorFileDraft();
                _proposalFileDrafts.Add(draft);
            }
            else
            {
                draft = _proposalFileDrafts[0];
            }

            draft.FilePath = filePath;
            draft.IsLoaded = false;
            draft.OriginalText = string.Empty;
            draft.ProposedText = string.Empty;

            LoadProposalDraft(draft, clearStatusMessage);
            ShowProposalDraft(draft, clearStatusMessage: false);
        }

        private void RemoveSelectedProposalFile()
        {
            var filePath = LogToolWindowViewModel.ProposalFilePath;
            if (string.IsNullOrWhiteSpace(filePath))
                return;

            var existingDraft = FindProposalDraft(filePath);
            if (existingDraft == null)
                return;

            _proposalFileDrafts.Remove(existingDraft);

            if (_proposalFileDrafts.Count == 0)
            {
                SetProposalEditorState(string.Empty, string.Empty, string.Empty, isLoaded: false, clearStatusMessage: true);
                UpdateProposalSubmissionState();
                return;
            }

            ShowProposalDraft(_proposalFileDrafts[0], clearStatusMessage: true);
        }

        private ProposalEditorFileDraft? FindProposalDraft(string filePath)
        {
            return _proposalFileDrafts.FirstOrDefault(
                draft => string.Equals(draft.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        }

        private ProposalEditorFileDraft? GetActiveProposalDraft()
        {
            return FindProposalDraft(LogToolWindowViewModel.ProposalFilePath);
        }

        private void LoadProposalDraft(ProposalEditorFileDraft draft, bool clearStatusMessage)
        {
            try
            {
                if (!File.Exists(draft.FilePath))
                    throw new FileNotFoundException("File was not found.", draft.FilePath);

                var content = File.ReadAllText(draft.FilePath);
                draft.IsLoaded = true;
                draft.OriginalText = content;
                draft.ProposedText = content;
                if (clearStatusMessage)
                    LogToolWindowViewModel.StatusMessage = string.Empty;
            }
            catch (Exception)
            {
                draft.IsLoaded = false;
                draft.OriginalText = string.Empty;
                draft.ProposedText = string.Empty;
                LogToolWindowViewModel.StatusMessage = $"Unable to load file '{draft.FilePath}'.";
            }
        }

        private void ShowProposalDraft(ProposalEditorFileDraft draft, bool clearStatusMessage)
        {
            SetProposalEditorState(
                draft.FilePath,
                draft.OriginalText,
                draft.ProposedText,
                draft.IsLoaded,
                clearStatusMessage);
        }

        private void ReloadActiveProposalFile(bool clearStatusMessage)
        {
            var activeDraft = GetActiveProposalDraft();
            if (activeDraft == null)
                return;

            LoadProposalDraft(activeDraft, clearStatusMessage);
            ShowProposalDraft(activeDraft, clearStatusMessage: false);
        }

        private void SetProposalEditorState(string filePath, string originalText, string proposedText, bool isLoaded, bool clearStatusMessage)
        {
            _isUpdatingProposalEditor = true;
            try
            {
                LogToolWindowViewModel.ProposalFilePath = filePath;
                LogToolWindowViewModel.ProposalOriginalText = originalText;
                LogToolWindowViewModel.ProposalProposedText = proposedText;
                LogToolWindowViewModel.IsProposalFileLoaded = isLoaded;
                if (clearStatusMessage)
                    LogToolWindowViewModel.StatusMessage = string.Empty;
            }
            finally
            {
                _isUpdatingProposalEditor = false;
            }

            UpdateProposalSubmissionState();
        }

        private void ResetCurrentRequestState()
        {
            SuppressActiveManualRequestIfNeeded();

            _activeManualRequestId = null;
            ClearApproval();
            ClearCompletedProposalPreview();
            LogToolWindowViewModel.RequestInputText = string.Empty;
            LogToolWindowViewModel.LastSubmittedRequestText = string.Empty;
            LogToolWindowViewModel.IsRequestInProgress = false;
            LogToolWindowViewModel.StatusMessage = string.Empty;

            var activeDraft = GetActiveProposalDraft();
            _proposalDraftState?.SetActiveFilePath(LogToolWindowViewModel.ProposalFilePath);
            _proposalDraftState?.SetSelectedText(activeDraft?.OriginalText ?? LogToolWindowViewModel.ProposalOriginalText);
        }

        private void StartNewChat()
        {
            SuppressActiveManualRequestIfNeeded();

            _activeManualRequestId = null;
            ClearApproval();
            _proposalFileDrafts.Clear();
            ClearCompletedProposalPreview();
            SetProposalEditorState(string.Empty, string.Empty, string.Empty, isLoaded: false, clearStatusMessage: true);
            LogToolWindowViewModel.RequestInputText = string.Empty;
            LogToolWindowViewModel.LastSubmittedRequestText = string.Empty;
            LogToolWindowViewModel.IsRequestInProgress = false;
            LogToolWindowViewModel.StatusMessage = string.Empty;
            _proposalDraftState?.SetActiveFilePath(string.Empty);
            _proposalDraftState?.SetSelectedText(string.Empty);
        }

        private void SuppressActiveManualRequestIfNeeded()
        {
            if (LogToolWindowViewModel.IsRequestInProgress && !string.IsNullOrWhiteSpace(_activeManualRequestId))
            {
                _suppressedRequestIds.Add(_activeManualRequestId!);
            }
        }

        private bool ShouldIgnoreRequestUpdate(string? requestId)
        {
            if (string.IsNullOrWhiteSpace(requestId))
                return false;

            var normalizedRequestId = requestId!;

            if (_suppressedRequestIds.Contains(normalizedRequestId))
            {
                _suppressedRequestIds.Remove(normalizedRequestId);
                return true;
            }

            return false;
        }

        private void CompleteActiveManualRequest(string? requestId)
        {
            if (!string.IsNullOrWhiteSpace(requestId)
                && string.Equals(_activeManualRequestId, requestId, StringComparison.Ordinal))
            {
                _activeManualRequestId = null;
            }
        }

        private void UpdateProposalSubmissionState()
        {
            LogToolWindowViewModel.HasProposalDrafts = _proposalFileDrafts.Count > 0;
            LogToolWindowViewModel.ProposalSelectedFiles = _proposalFileDrafts
                .Select(draft => draft.FilePath)
                .ToArray();

            LogToolWindowViewModel.HasSubmittableProposal =
                _proposalFileDrafts.Count > 0
                && _proposalFileDrafts.All(draft => draft.IsLoaded)
                && _proposalFileDrafts.Any(draft => !string.Equals(draft.OriginalText, draft.ProposedText, StringComparison.Ordinal));
        }

        private string BuildActiveProposalTarget()
        {
            return _proposalFileDrafts.Count switch
            {
                0 => "the selected proposal files",
                1 => _proposalFileDrafts[0].FilePath,
                _ => $"{_proposalFileDrafts.Count} files"
            };
        }

        private string BuildSubmittedRequestText()
        {
            return string.IsNullOrWhiteSpace(LogToolWindowViewModel.RequestInputText)
                ? "Manual proposal request"
                : LogToolWindowViewModel.RequestInputText.Trim();
        }

        private sealed class ProposalEditorFileDraft
        {
            public string FilePath { get; set; } = string.Empty;
            public string OriginalText { get; set; } = string.Empty;
            public string ProposedText { get; set; } = string.Empty;
            public bool IsLoaded { get; set; }
        }

        public void RunOnUiThread(Action action)
        {
            if (_threadHelper.CheckAccess())
            {
                action();
                return;
            }

            _threadHelper.Run(() =>
            {
                action();
                return Task.CompletedTask;
            });
        }
    }
}
