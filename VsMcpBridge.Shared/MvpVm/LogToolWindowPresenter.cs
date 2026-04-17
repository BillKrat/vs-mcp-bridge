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
        private Action? _pendingApproveAction;
        private Action? _pendingRejectAction;

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
            LogToolWindowViewModel.SetProposalSubmissionHandler(OnSubmitProposalRequested);
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

        public void ShowApprovalPrompt(string description, string? originalSegment, string? updatedSegment, IReadOnlyList<ProposalReviewedChange>? reviewedChanges, Action onApprove, Action onReject)
        {
            RunOnUiThread(() =>
            {
                _pendingApproveAction = onApprove;
                _pendingRejectAction = onReject;
                ClearCompletedProposalPreview();
                LogToolWindowViewModel.StatusMessage = string.Empty;
                LogToolWindowViewModel.PendingApprovalDescription = description;
                LogToolWindowViewModel.PendingApprovalOriginalSegment = originalSegment ?? string.Empty;
                LogToolWindowViewModel.PendingApprovalUpdatedSegment = updatedSegment ?? string.Empty;
                LogToolWindowViewModel.PendingApprovalReviewedChanges = CloneReviewedChanges(reviewedChanges);
                LogToolWindowViewModel.HasPendingApproval = true;
            });
        }

        public void ShowStatusMessage(string message)
        {
            RunOnUiThread(() => LogToolWindowViewModel.StatusMessage = message);
        }

        public void CompleteProposalCycle(string statusMessage)
        {
            RunOnUiThread(() =>
            {
                CaptureCompletedProposalPreview();
                ClearApproval();

                if (!string.IsNullOrWhiteSpace(LogToolWindowViewModel.ProposalFilePath))
                {
                    TryLoadProposalFile(LogToolWindowViewModel.ProposalFilePath, clearStatusMessage: false);
                }
                else
                {
                    LogToolWindowViewModel.IsProposalFileLoaded = false;
                    LogToolWindowViewModel.ProposalOriginalText = string.Empty;
                    LogToolWindowViewModel.ProposalProposedText = string.Empty;
                }

                LogToolWindowViewModel.StatusMessage = statusMessage;
            });
        }

        private void CaptureCompletedProposalPreview()
        {
            LogToolWindowViewModel.LastCompletedProposalOriginalText = LogToolWindowViewModel.ProposalOriginalText;
            LogToolWindowViewModel.LastCompletedProposalUpdatedText = LogToolWindowViewModel.ProposalProposedText;
            LogToolWindowViewModel.LastCompletedProposalOriginalSegment = LogToolWindowViewModel.PendingApprovalOriginalSegment;
            LogToolWindowViewModel.LastCompletedProposalUpdatedSegment = LogToolWindowViewModel.PendingApprovalUpdatedSegment;
            LogToolWindowViewModel.LastCompletedProposalReviewedChanges = CloneReviewedChanges(LogToolWindowViewModel.PendingApprovalReviewedChanges);
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

        private void OnSubmitProposalRequested(string filePath, string originalText, string proposedText)
        {
            _ = SubmitProposalAsync(filePath, originalText, proposedText);
        }

        private void OnBrowseProposalFileRequested()
        {
            var selectedPath = _proposalFilePicker?.PickFilePath();
            if (!string.IsNullOrWhiteSpace(selectedPath))
            {
                ClearCompletedProposalPreview();
                LogToolWindowViewModel.ProposalFilePath = selectedPath!;
            }
        }

        private async Task SubmitProposalAsync(string filePath, string originalText, string proposedText)
        {
            try
            {
                LogToolWindowViewModel.StatusMessage = string.Empty;
                _proposalDraftState?.SetActiveFilePath(filePath);
                _proposalDraftState?.SetSelectedText(originalText);
                var vsService = _serviceProvider.GetRequiredService<IVsService>();
                await vsService.ProposeTextEditAsync(Guid.NewGuid().ToString("N"), filePath, originalText, proposedText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Manual proposal submission failed for '{filePath}'.");
            }
        }

        private void ClearApproval()
        {
            _pendingApproveAction = null;
            _pendingRejectAction = null;
            LogToolWindowViewModel.PendingApprovalDescription = string.Empty;
            LogToolWindowViewModel.PendingApprovalOriginalSegment = string.Empty;
            LogToolWindowViewModel.PendingApprovalUpdatedSegment = string.Empty;
            LogToolWindowViewModel.PendingApprovalReviewedChanges = Array.Empty<ProposalReviewedChange>();
            LogToolWindowViewModel.HasPendingApproval = false;
        }

        private void ClearCompletedProposalPreview()
        {
            LogToolWindowViewModel.LastCompletedProposalOriginalText = string.Empty;
            LogToolWindowViewModel.LastCompletedProposalUpdatedText = string.Empty;
            LogToolWindowViewModel.LastCompletedProposalOriginalSegment = string.Empty;
            LogToolWindowViewModel.LastCompletedProposalUpdatedSegment = string.Empty;
            LogToolWindowViewModel.LastCompletedProposalReviewedChanges = Array.Empty<ProposalReviewedChange>();
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

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ILogToolWindowViewModel.ProposalFilePath))
            {
                ClearCompletedProposalPreview();
                _proposalDraftState?.SetActiveFilePath(LogToolWindowViewModel.ProposalFilePath);
                TryLoadProposalFile(LogToolWindowViewModel.ProposalFilePath);
            }
            else if (e.PropertyName == nameof(ILogToolWindowViewModel.ProposalOriginalText))
            {
                _proposalDraftState?.SetSelectedText(LogToolWindowViewModel.ProposalOriginalText);
            }
        }

        private void SyncProposalDraftState()
        {
            _proposalDraftState?.SetActiveFilePath(LogToolWindowViewModel.ProposalFilePath);
            _proposalDraftState?.SetSelectedText(LogToolWindowViewModel.ProposalOriginalText);
            if (!string.IsNullOrWhiteSpace(LogToolWindowViewModel.ProposalFilePath))
                TryLoadProposalFile(LogToolWindowViewModel.ProposalFilePath);
        }

        private void TryLoadProposalFile(string? filePath, bool clearStatusMessage = true)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                LogToolWindowViewModel.IsProposalFileLoaded = false;
                LogToolWindowViewModel.ProposalOriginalText = string.Empty;
                LogToolWindowViewModel.ProposalProposedText = string.Empty;
                if (clearStatusMessage)
                    LogToolWindowViewModel.StatusMessage = string.Empty;
                return;
            }

            try
            {
                var normalizedPath = filePath!;
                if (!File.Exists(normalizedPath))
                    throw new FileNotFoundException("File was not found.", normalizedPath);

                var content = File.ReadAllText(normalizedPath);
                LogToolWindowViewModel.IsProposalFileLoaded = true;
                LogToolWindowViewModel.ProposalOriginalText = content;
                LogToolWindowViewModel.ProposalProposedText = content;
                if (clearStatusMessage)
                    LogToolWindowViewModel.StatusMessage = string.Empty;
            }
            catch (Exception)
            {
                LogToolWindowViewModel.IsProposalFileLoaded = false;
                LogToolWindowViewModel.ProposalOriginalText = string.Empty;
                LogToolWindowViewModel.ProposalProposedText = string.Empty;
                LogToolWindowViewModel.StatusMessage = $"Unable to load file '{filePath}'.";
            }
        }

        public void RunOnUiThread(Action action)
        {
            if (_threadHelper.CheckAccess())
            {
                action();
                return;
            }

            _threadHelper.Run(async delegate
            {
                await _threadHelper.SwitchToMainThreadAsync();
                action();
            });
        }
    }
}
