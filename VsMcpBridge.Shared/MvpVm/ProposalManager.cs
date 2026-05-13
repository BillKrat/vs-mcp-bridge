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
    public sealed class ProposalManager : IProposalManager
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private readonly IThreadHelper _threadHelper;
        private readonly ILogToolWindowViewModel _viewModel;
        private readonly IProposalDraftState? _proposalDraftState;
        private readonly IProposalFilePicker? _proposalFilePicker;
        private readonly List<ProposalEditorFileDraft> _proposalFileDrafts = new();
        private readonly HashSet<string> _suppressedRequestIds = new(StringComparer.Ordinal);
        private Action? _pendingApproveAction;
        private Action? _pendingRejectAction;
        private bool _isUpdatingProposalEditor;

        public ProposalManager(IServiceProvider serviceProvider, ILogger logger, IThreadHelper threadHelper, ILogToolWindowViewModel viewModel)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _threadHelper = threadHelper;
            _viewModel = viewModel;
            _proposalDraftState = _serviceProvider.GetService<IProposalDraftState>();
            _proposalFilePicker = _serviceProvider.GetService<IProposalFilePicker>();
        }

        public int SelectedFileCount => _proposalFileDrafts.Count;

        public string? ActiveManualRequestId { get; private set; }

        public bool HasProposalFilePicker => _proposalFilePicker != null;

        public void Initialize()
        {
            SyncProposalDraftState();
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
                _viewModel.IsRequestInProgress = false;
                _viewModel.StatusMessage = string.Empty;
                _viewModel.PendingApprovalDescription = description;
                _viewModel.PendingProposalSourceType = ResolveProposalSourceType(requestId);
                _viewModel.PendingProposalErrorContextText = BuildProposalErrorContextText(
                    _viewModel.PendingProposalSourceType,
                    _viewModel.LastSubmittedRequestText);
                _viewModel.PendingProposalPreviewText = BuildProposalPreviewText(
                    updatedSegment,
                    reviewedChanges,
                    _viewModel.ProposalProposedText,
                    description);
                _viewModel.PendingApprovalOriginalSegment = originalSegment ?? string.Empty;
                _viewModel.PendingApprovalUpdatedSegment = updatedSegment ?? string.Empty;
                _viewModel.PendingApprovalReviewedChanges = CloneReviewedChanges(reviewedChanges);
                _viewModel.PendingApprovalIncludedFiles = CloneIncludedFiles(includedFiles ?? _viewModel.ProposalSelectedFiles);
                _viewModel.HasPendingApproval = true;
                CompleteActiveManualRequest(requestId);
            });
        }

        public void CompleteProposalCycle(string statusMessage, string? requestId = null)
        {
            RunOnUiThread(() =>
            {
                if (ShouldIgnoreRequestUpdate(requestId))
                    return;

                CaptureCompletedProposalPreview();
                ClearApproval();
                _viewModel.IsRequestInProgress = false;

                if (!string.IsNullOrWhiteSpace(_viewModel.ProposalFilePath))
                {
                    ReloadActiveProposalFile(clearStatusMessage: false);
                }
                else
                {
                    _viewModel.IsProposalFileLoaded = false;
                    _viewModel.ProposalOriginalText = string.Empty;
                    _viewModel.ProposalProposedText = string.Empty;
                }

                _viewModel.StatusMessage = statusMessage;
                CompleteActiveManualRequest(requestId);
            });
        }

        public async Task SubmitProposalAsync(Func<string, Task<bool>> tryDispatchPromptRequestAsync, Action<string> appendPromptActivityEntry)
        {
            try
            {
                var submittedRequestText = BuildSubmittedRequestText();
                var requestId = Guid.NewGuid().ToString("N");
                _logger.LogTrace("Prompt-box request submission started [RequestId={RequestId}] [RequestLength={RequestLength}] [SelectedFileCount={SelectedFileCount}].",
                    requestId,
                    submittedRequestText.Length,
                    _proposalFileDrafts.Count);
                ActiveManualRequestId = requestId;
                _suppressedRequestIds.Remove(requestId);
                _viewModel.LastSubmittedRequestText = submittedRequestText;
                _viewModel.IsRequestInProgress = true;
                _viewModel.StatusMessage = string.Empty;
                appendPromptActivityEntry(submittedRequestText);

                var toolDispatchHandled = await tryDispatchPromptRequestAsync(submittedRequestText);
                if (toolDispatchHandled)
                {
                    _logger.LogTrace("Prompt-box request handled through request routing path [RequestId={RequestId}].", requestId);
                    ActiveManualRequestId = null;
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
                    && string.Equals(ActiveManualRequestId, requestId, StringComparison.Ordinal)
                    && _viewModel.IsRequestInProgress
                    && !_viewModel.HasPendingApproval
                    && string.IsNullOrWhiteSpace(_viewModel.StatusMessage))
                {
                    _viewModel.IsRequestInProgress = false;
                    _viewModel.StatusMessage = fileEdits.Length == 0
                        ? "No proposal was created because no files were selected."
                        : $"No proposal was created because the proposed text already matches the original content for {proposalTarget}.";
                    ActiveManualRequestId = null;

                    _logger.LogWarning(
                        $"Manual proposal submission completed without a reviewable proposal [RequestId={requestId}] [Target={proposalTarget}] [FileCount={fileEdits.Length}] [HasMeaningfulChange={hasMeaningfulChange}].");
                }
            }
            catch (Exception ex)
            {
                _viewModel.IsRequestInProgress = false;
                _viewModel.StatusMessage = $"Request submission failed for {BuildActiveProposalTarget()}. Review the bridge log for details.";
                ActiveManualRequestId = null;
                _logger.LogError(ex, $"Manual proposal submission failed for '{BuildActiveProposalTarget()}'.");
            }
        }

        public void BrowseProposalFile()
        {
            var selectedPath = _proposalFilePicker?.PickFilePath();
            if (!string.IsNullOrWhiteSpace(selectedPath))
            {
                ClearCompletedProposalPreview();
                AddOrSelectProposalFile(selectedPath!);
            }
        }

        public void RemoveSelectedProposalFile()
        {
            var filePath = _viewModel.ProposalFilePath;
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

        public void ResetCurrentRequestState()
        {
            SuppressActiveManualRequestIfNeeded();

            ActiveManualRequestId = null;
            ClearApproval();
            ClearCompletedProposalPreview();
            _viewModel.RequestInputText = string.Empty;
            _viewModel.LastSubmittedRequestText = string.Empty;
            _viewModel.IsRequestInProgress = false;
            _viewModel.StatusMessage = string.Empty;

            var activeDraft = GetActiveProposalDraft();
            _proposalDraftState?.SetActiveFilePath(_viewModel.ProposalFilePath);
            _proposalDraftState?.SetSelectedText(activeDraft?.OriginalText ?? _viewModel.ProposalOriginalText);
        }

        public void StartNewChat()
        {
            SuppressActiveManualRequestIfNeeded();

            ActiveManualRequestId = null;
            ClearApproval();
            _proposalFileDrafts.Clear();
            ClearCompletedProposalPreview();
            SetProposalEditorState(string.Empty, string.Empty, string.Empty, isLoaded: false, clearStatusMessage: true);
            _viewModel.RequestInputText = string.Empty;
            _viewModel.LastSubmittedRequestText = string.Empty;
            _viewModel.IsRequestInProgress = false;
            _viewModel.StatusMessage = string.Empty;
            _proposalDraftState?.SetActiveFilePath(string.Empty);
            _proposalDraftState?.SetSelectedText(string.Empty);
        }

        public void ApproveRequested()
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

        public void RejectRequested()
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

        public void HandleViewModelPropertyChanged(PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ILogToolWindowViewModel.ProposalFilePath))
            {
                if (_isUpdatingProposalEditor)
                    return;

                ClearCompletedProposalPreview();
                _proposalDraftState?.SetActiveFilePath(_viewModel.ProposalFilePath);
                ActivateProposalFile(_viewModel.ProposalFilePath);
            }
            else if (e.PropertyName == nameof(ILogToolWindowViewModel.ProposalOriginalText))
            {
                if (_isUpdatingProposalEditor)
                    return;

                var activeDraft = GetActiveProposalDraft();
                if (activeDraft != null)
                {
                    activeDraft.OriginalText = _viewModel.ProposalOriginalText;
                    UpdateProposalSubmissionState();
                }

                _proposalDraftState?.SetSelectedText(_viewModel.ProposalOriginalText);
            }
            else if (e.PropertyName == nameof(ILogToolWindowViewModel.ProposalProposedText))
            {
                if (_isUpdatingProposalEditor)
                    return;

                var activeDraft = GetActiveProposalDraft();
                if (activeDraft != null)
                {
                    activeDraft.ProposedText = _viewModel.ProposalProposedText;
                    UpdateProposalSubmissionState();
                }
            }
        }

        private void CaptureCompletedProposalPreview()
        {
            _viewModel.LastCompletedProposalOriginalText = _viewModel.ProposalOriginalText;
            _viewModel.LastCompletedProposalUpdatedText = _viewModel.ProposalProposedText;
            _viewModel.LastCompletedProposalSourceType = _viewModel.PendingProposalSourceType;
            _viewModel.LastCompletedProposalPreviewText = _viewModel.PendingProposalPreviewText;
            _viewModel.LastCompletedProposalErrorContextText = _viewModel.PendingProposalErrorContextText;
            _viewModel.LastCompletedProposalOriginalSegment = _viewModel.PendingApprovalOriginalSegment;
            _viewModel.LastCompletedProposalUpdatedSegment = _viewModel.PendingApprovalUpdatedSegment;
            _viewModel.LastCompletedProposalReviewedChanges = CloneReviewedChanges(_viewModel.PendingApprovalReviewedChanges);
            _viewModel.LastCompletedProposalIncludedFiles = CloneIncludedFiles(_viewModel.PendingApprovalIncludedFiles);
        }

        private void ClearApproval()
        {
            _pendingApproveAction = null;
            _pendingRejectAction = null;
            _viewModel.PendingApprovalDescription = string.Empty;
            _viewModel.PendingProposalSourceType = string.Empty;
            _viewModel.PendingProposalPreviewText = string.Empty;
            _viewModel.PendingProposalErrorContextText = string.Empty;
            _viewModel.PendingApprovalOriginalSegment = string.Empty;
            _viewModel.PendingApprovalUpdatedSegment = string.Empty;
            _viewModel.PendingApprovalReviewedChanges = Array.Empty<ProposalReviewedChange>();
            _viewModel.PendingApprovalIncludedFiles = Array.Empty<string>();
            _viewModel.HasPendingApproval = false;
        }

        private void ClearCompletedProposalPreview()
        {
            _viewModel.LastCompletedProposalOriginalText = string.Empty;
            _viewModel.LastCompletedProposalUpdatedText = string.Empty;
            _viewModel.LastCompletedProposalSourceType = string.Empty;
            _viewModel.LastCompletedProposalPreviewText = string.Empty;
            _viewModel.LastCompletedProposalErrorContextText = string.Empty;
            _viewModel.LastCompletedProposalOriginalSegment = string.Empty;
            _viewModel.LastCompletedProposalUpdatedSegment = string.Empty;
            _viewModel.LastCompletedProposalReviewedChanges = Array.Empty<ProposalReviewedChange>();
            _viewModel.LastCompletedProposalIncludedFiles = Array.Empty<string>();
        }

        private static string BuildProposalPreviewText(
            string? updatedSegment,
            IReadOnlyList<ProposalReviewedChange>? reviewedChanges,
            string? proposedText,
            string? description)
        {
            var previewSource =
                FirstNonEmpty(updatedSegment)
                ?? FirstNonEmpty(reviewedChanges?.FirstOrDefault()?.UpdatedSegment)
                ?? FirstNonEmpty(proposedText)
                ?? FirstNonEmpty(ExtractPreviewFromDescription(description));

            return string.IsNullOrWhiteSpace(previewSource)
                ? string.Empty
                : TruncatePreview(previewSource);
        }

        private static string? ExtractPreviewFromDescription(string? description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return null;

            foreach (var line in description.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;

                if (trimmed.StartsWith("Pending proposal", StringComparison.OrdinalIgnoreCase)
                    || trimmed.StartsWith("--- ", StringComparison.Ordinal)
                    || trimmed.StartsWith("+++ ", StringComparison.Ordinal)
                    || trimmed.StartsWith("@@", StringComparison.Ordinal)
                    || trimmed.StartsWith("File: ", StringComparison.Ordinal))
                {
                    continue;
                }

                if (trimmed.StartsWith("+", StringComparison.Ordinal))
                    return trimmed.TrimStart('+').Trim();

                if (!trimmed.StartsWith("-", StringComparison.Ordinal))
                    return trimmed;
            }

            return null;
        }

        private static string? FirstNonEmpty(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string TruncatePreview(string value)
        {
            var firstLine = value
                .Replace("\r\n", "\n")
                .Split('\n')
                .FirstOrDefault(line => !string.IsNullOrWhiteSpace(line))
                ?.Trim()
                ?? string.Empty;

            const int maxLength = 120;
            if (firstLine.Length <= maxLength)
                return firstLine;

            return firstLine.Substring(0, maxLength - 1).TrimEnd() + "\u2026";
        }

        private string ResolveProposalSourceType(string? requestId)
        {
            if (!string.IsNullOrWhiteSpace(requestId)
                && string.Equals(ActiveManualRequestId, requestId, StringComparison.Ordinal))
            {
                return "Manual proposal";
            }

            var requestText = _viewModel.LastSubmittedRequestText;
            if (!string.IsNullOrWhiteSpace(requestText))
            {
                var normalizedRequestText = requestText.Trim().ToLowerInvariant();
                if (normalizedRequestText.IndexOf("compiler or diagnostic error", StringComparison.Ordinal) >= 0)
                {
                    if (normalizedRequestText.IndexOf("explain", StringComparison.Ordinal) >= 0)
                    {
                        return "AI explain error";
                    }

                    if (normalizedRequestText.IndexOf("suggest a likely fix", StringComparison.Ordinal) >= 0
                        || normalizedRequestText.IndexOf("suggest a fix", StringComparison.Ordinal) >= 0)
                    {
                        return "AI suggest error fix";
                    }
                }

                if (normalizedRequestText.IndexOf("suggest fixes", StringComparison.Ordinal) >= 0
                    || normalizedRequestText.IndexOf("suggest improvements", StringComparison.Ordinal) >= 0)
                {
                    return "AI suggest fixes";
                }

                if (normalizedRequestText.IndexOf("rewrite", StringComparison.Ordinal) >= 0)
                {
                    return "AI rewrite";
                }
            }

            return "Manual proposal";
        }

        private static string BuildProposalErrorContextText(string sourceType, string? requestText)
        {
            if (!string.Equals(sourceType, "AI explain error", StringComparison.Ordinal)
                && !string.Equals(sourceType, "AI suggest error fix", StringComparison.Ordinal))
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(requestText))
                return string.Empty;

            var normalizedRequestText = requestText.Replace("\r\n", "\n");
            const string explicitErrorMarker = "\n\nError:\n";
            var explicitErrorIndex = normalizedRequestText.IndexOf(explicitErrorMarker, StringComparison.Ordinal);
            if (explicitErrorIndex >= 0)
            {
                var errorStart = explicitErrorIndex + explicitErrorMarker.Length;
                var codeMarkerIndex = normalizedRequestText.IndexOf("\n\nCode:\n", errorStart, StringComparison.Ordinal);
                var errorBlock = codeMarkerIndex >= 0
                    ? normalizedRequestText.Substring(errorStart, codeMarkerIndex - errorStart)
                    : normalizedRequestText.Substring(errorStart);
                return TruncatePreview(errorBlock.Trim());
            }

            var promptSeparatorIndex = normalizedRequestText.IndexOf("\n\n", StringComparison.Ordinal);
            if (promptSeparatorIndex >= 0 && promptSeparatorIndex + 2 < normalizedRequestText.Length)
            {
                return TruncatePreview(normalizedRequestText.Substring(promptSeparatorIndex + 2).Trim());
            }

            return string.Empty;
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

        private void SyncProposalDraftState()
        {
            _proposalDraftState?.SetActiveFilePath(_viewModel.ProposalFilePath);
            _proposalDraftState?.SetSelectedText(_viewModel.ProposalOriginalText);
            if (!string.IsNullOrWhiteSpace(_viewModel.ProposalFilePath))
                ActivateProposalFile(_viewModel.ProposalFilePath);
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
                    _viewModel.StatusMessage = string.Empty;
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

        private ProposalEditorFileDraft? FindProposalDraft(string filePath)
        {
            return _proposalFileDrafts.FirstOrDefault(
                draft => string.Equals(draft.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        }

        private ProposalEditorFileDraft? GetActiveProposalDraft()
        {
            return FindProposalDraft(_viewModel.ProposalFilePath);
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
                    _viewModel.StatusMessage = string.Empty;
            }
            catch (Exception)
            {
                draft.IsLoaded = false;
                draft.OriginalText = string.Empty;
                draft.ProposedText = string.Empty;
                _viewModel.StatusMessage = $"Unable to load file '{draft.FilePath}'.";
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
                _viewModel.ProposalFilePath = filePath;
                _viewModel.ProposalOriginalText = originalText;
                _viewModel.ProposalProposedText = proposedText;
                _viewModel.IsProposalFileLoaded = isLoaded;
                if (clearStatusMessage)
                    _viewModel.StatusMessage = string.Empty;
            }
            finally
            {
                _isUpdatingProposalEditor = false;
            }

            UpdateProposalSubmissionState();
        }

        private void SuppressActiveManualRequestIfNeeded()
        {
            if (_viewModel.IsRequestInProgress && !string.IsNullOrWhiteSpace(ActiveManualRequestId))
            {
                _suppressedRequestIds.Add(ActiveManualRequestId!);
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
                && string.Equals(ActiveManualRequestId, requestId, StringComparison.Ordinal))
            {
                ActiveManualRequestId = null;
            }
        }

        private void UpdateProposalSubmissionState()
        {
            _viewModel.HasProposalDrafts = _proposalFileDrafts.Count > 0;
            _viewModel.ProposalSelectedFiles = _proposalFileDrafts
                .Select(draft => draft.FilePath)
                .ToArray();

            _viewModel.HasSubmittableProposal =
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
            return string.IsNullOrWhiteSpace(_viewModel.RequestInputText)
                ? "Manual proposal request"
                : _viewModel.RequestInputText.Trim();
        }

        private void RunOnUiThread(Action action)
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

        private sealed class ProposalEditorFileDraft
        {
            public string FilePath { get; set; } = string.Empty;
            public string OriginalText { get; set; } = string.Empty;
            public string ProposedText { get; set; } = string.Empty;
            public bool IsLoaded { get; set; }
        }
    }
}
