using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.Loggers;
using VsMcpBridge.Shared.Models;

namespace VsMcpBridge.Shared.MvpVm
{
    public class LogToolWindowViewModel : ObservableObject, ILogToolWindowViewModel
    {
        private const string InitialLogMessage = "VS MCP Bridge log will appear here.";

        private static readonly IReadOnlyList<LogLevel> _availableLogLevels = new[]
        {
            LogLevel.Trace, LogLevel.Debug, LogLevel.Information,
            LogLevel.Warning, LogLevel.Error, LogLevel.Critical, LogLevel.None
        };

        private readonly ILogLevelSettings _logLevelSettings;
        private string _logText = InitialLogMessage;
        private string _requestInputText = string.Empty;
        private string _lastSubmittedRequestText = string.Empty;
        private bool _isRequestInProgress;
        private string _proposalFilePath = string.Empty;
        private IReadOnlyList<string> _proposalSelectedFiles = Array.Empty<string>();
        private string _proposalOriginalText = string.Empty;
        private string _proposalProposedText = string.Empty;
        private string _pendingApprovalDescription = string.Empty;
        private string _pendingApprovalOriginalSegment = string.Empty;
        private string _pendingApprovalUpdatedSegment = string.Empty;
        private IReadOnlyList<ProposalReviewedChange> _pendingApprovalReviewedChanges = Array.Empty<ProposalReviewedChange>();
        private IReadOnlyList<string> _pendingApprovalIncludedFiles = Array.Empty<string>();
        private string _lastCompletedProposalOriginalText = string.Empty;
        private string _lastCompletedProposalUpdatedText = string.Empty;
        private string _lastCompletedProposalOriginalSegment = string.Empty;
        private string _lastCompletedProposalUpdatedSegment = string.Empty;
        private IReadOnlyList<ProposalReviewedChange> _lastCompletedProposalReviewedChanges = Array.Empty<ProposalReviewedChange>();
        private IReadOnlyList<string> _lastCompletedProposalIncludedFiles = Array.Empty<string>();
        private string _statusMessage = string.Empty;
        private bool _isProposalFileLoaded;
        private bool _hasProposalDrafts;
        private bool _hasSubmittableProposal;
        private bool _canBrowseProposalFile;
        private bool _hasPendingApproval;
        private LogLevel _selectedLogLevel;
        private Action? _onBrowseProposalFileRequested;
        private Action? _onRemoveProposalFileRequested;
        private Action? _onResetProposalRequested;
        private Action? _onNewChatRequested;
        private Action? _onSubmitProposalRequested;
        private Action? _onOpenGitChangesRequested;
        private Action? _onApproveRequested;
        private Action? _onRejectRequested;

        public LogToolWindowViewModel() : this(new LogLevelSettings()) { }

        public LogToolWindowViewModel(ILogLevelSettings logLevelSettings)
        {
            _logLevelSettings = logLevelSettings;
            _selectedLogLevel = logLevelSettings.MinimumLevel;

            BrowseProposalFileCommand = new RelayCommand(
                execute: () => _onBrowseProposalFileRequested?.Invoke(),
                canExecute: () => CanBrowseProposalFile && !HasPendingApproval);

            RemoveProposalFileCommand = new RelayCommand(
                execute: () => _onRemoveProposalFileRequested?.Invoke(),
                canExecute: () => ProposalSelectedFiles.Count > 0 && !HasPendingApproval && _onRemoveProposalFileRequested is not null);

            ResetProposalCommand = new RelayCommand(
                execute: () => _onResetProposalRequested?.Invoke(),
                canExecute: () => HasResettableProposalState && _onResetProposalRequested is not null);

            NewChatCommand = new RelayCommand(
                execute: () => _onNewChatRequested?.Invoke(),
                canExecute: () => HasSessionState && _onNewChatRequested is not null);

            SubmitProposalCommand = new RelayCommand(
                execute: () => _onSubmitProposalRequested?.Invoke(),
                canExecute: CanSubmitProposal);

            OpenGitChangesCommand = new RelayCommand(
                execute: () => _onOpenGitChangesRequested?.Invoke(),
                canExecute: () => CanOpenGitChanges && _onOpenGitChangesRequested is not null);

            ApproveCommand = new RelayCommand(
                execute: () => _onApproveRequested?.Invoke(),
                canExecute: () => HasPendingApproval);

            RejectCommand = new RelayCommand(
                execute: () => _onRejectRequested?.Invoke(),
                canExecute: () => HasPendingApproval);
        }

        public string LogText
        {
            get => _logText;
            set => SetProperty(ref _logText, value);
        }

        public string RequestInputText
        {
            get => _requestInputText;
            set
            {
                if (SetProperty(ref _requestInputText, value))
                {
                    OnPropertyChanged(nameof(HasRequestInputText));
                    OnPropertyChanged(nameof(RequestPhaseSummary));
                    OnPropertyChanged(nameof(ActivitySummary));
                    NotifyResetStateChanged();
                    NotifySessionStateChanged();
                    SubmitProposalCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public bool HasRequestInputText => !string.IsNullOrWhiteSpace(RequestInputText);

        public string LastSubmittedRequestText
        {
            get => _lastSubmittedRequestText;
            set
            {
                if (SetProperty(ref _lastSubmittedRequestText, value))
                {
                    OnPropertyChanged(nameof(HasLastSubmittedRequest));
                    NotifyResetStateChanged();
                    NotifySessionStateChanged();
                }
            }
        }

        public bool HasLastSubmittedRequest => !string.IsNullOrWhiteSpace(LastSubmittedRequestText);

        public bool IsRequestInProgress
        {
            get => _isRequestInProgress;
            set
            {
                if (SetProperty(ref _isRequestInProgress, value))
                {
                    OnPropertyChanged(nameof(ActivityTitle));
                    OnPropertyChanged(nameof(ActivitySummary));
                    OnPropertyChanged(nameof(ActivityMetricsSummary));
                    OnPropertyChanged(nameof(HasActivityMetricsSummary));
                    OnPropertyChanged(nameof(RequestPhaseSummary));
                    OnPropertyChanged(nameof(HasRequestPhaseSummary));
                    OnPropertyChanged(nameof(CanOpenGitChanges));
                    NotifyResetStateChanged();
                    NotifySessionStateChanged();
                    OpenGitChangesCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public string ProposalFilePath
        {
            get => _proposalFilePath;
            set
            {
                if (SetProperty(ref _proposalFilePath, value))
                {
                    OnPropertyChanged(nameof(ProposalActiveFileSummary));
                    OnPropertyChanged(nameof(ActivityMetricsSummary));
                    OnPropertyChanged(nameof(HasActivityMetricsSummary));
                    RemoveProposalFileCommand.NotifyCanExecuteChanged();
                    NotifySessionStateChanged();
                    SubmitProposalCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public IReadOnlyList<string> ProposalSelectedFiles
        {
            get => _proposalSelectedFiles;
            set
            {
                var normalizedValue = NormalizeProposalSelectedFiles(value);
                if (ProposalSelectedFilesEqual(_proposalSelectedFiles, normalizedValue))
                    return;

                if (SetProperty(ref _proposalSelectedFiles, normalizedValue))
                {
                    OnPropertyChanged(nameof(ProposalSelectionSummary));
                    OnPropertyChanged(nameof(RequestPhaseSummary));
                    OnPropertyChanged(nameof(ActivityMetricsSummary));
                    OnPropertyChanged(nameof(HasActivityMetricsSummary));
                    RemoveProposalFileCommand.NotifyCanExecuteChanged();
                    NotifySessionStateChanged();
                    SubmitProposalCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public string ProposalOriginalText
        {
            get => _proposalOriginalText;
            set
            {
                if (SetProperty(ref _proposalOriginalText, value))
                {
                    OnPropertyChanged(nameof(HasResettableProposalState));
                    ResetProposalCommand.NotifyCanExecuteChanged();
                    SubmitProposalCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public string ProposalProposedText
        {
            get => _proposalProposedText;
            set
            {
                if (SetProperty(ref _proposalProposedText, value))
                {
                    OnPropertyChanged(nameof(HasResettableProposalState));
                    ResetProposalCommand.NotifyCanExecuteChanged();
                    SubmitProposalCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public string PendingApprovalDescription
        {
            get => _pendingApprovalDescription;
            set
            {
                if (SetProperty(ref _pendingApprovalDescription, value))
                {
                    OnPropertyChanged(nameof(RequestPhaseSummary));
                    OnPropertyChanged(nameof(HasRequestPhaseSummary));
                }
            }
        }

        public string PendingApprovalOriginalSegment
        {
            get => _pendingApprovalOriginalSegment;
            set
            {
                if (SetProperty(ref _pendingApprovalOriginalSegment, value))
                    OnPropertyChanged(nameof(HasPendingApprovalRangePreview));
            }
        }

        public string PendingApprovalUpdatedSegment
        {
            get => _pendingApprovalUpdatedSegment;
            set
            {
                if (SetProperty(ref _pendingApprovalUpdatedSegment, value))
                    OnPropertyChanged(nameof(HasPendingApprovalRangePreview));
            }
        }

        public IReadOnlyList<ProposalReviewedChange> PendingApprovalReviewedChanges
        {
            get => _pendingApprovalReviewedChanges;
            set
            {
                if (SetProperty(ref _pendingApprovalReviewedChanges, value))
                {
                    OnPropertyChanged(nameof(HasPendingApprovalReviewedChanges));
                    OnPropertyChanged(nameof(ActivityMetricsSummary));
                    OnPropertyChanged(nameof(HasActivityMetricsSummary));
                }
            }
        }

        public IReadOnlyList<string> PendingApprovalIncludedFiles
        {
            get => _pendingApprovalIncludedFiles;
            set
            {
                var normalizedValue = NormalizeProposalSelectedFiles(value);
                if (ProposalSelectedFilesEqual(_pendingApprovalIncludedFiles, normalizedValue))
                    return;

                if (SetProperty(ref _pendingApprovalIncludedFiles, normalizedValue))
                {
                    OnPropertyChanged(nameof(HasPendingApprovalIncludedFiles));
                    OnPropertyChanged(nameof(PendingApprovalIncludedFilesHeader));
                    OnPropertyChanged(nameof(ActivityMetricsSummary));
                    OnPropertyChanged(nameof(HasActivityMetricsSummary));
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (SetProperty(ref _statusMessage, value))
                {
                    OnPropertyChanged(nameof(RequestPhaseSummary));
                    OnPropertyChanged(nameof(HasRequestPhaseSummary));
                    OnPropertyChanged(nameof(ActivitySummary));
                    OnPropertyChanged(nameof(ActivityMetricsSummary));
                    OnPropertyChanged(nameof(HasActivityMetricsSummary));
                    OnPropertyChanged(nameof(CanOpenGitChanges));
                    NotifyResetStateChanged();
                    NotifySessionStateChanged();
                    OpenGitChangesCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public string LastCompletedProposalOriginalText
        {
            get => _lastCompletedProposalOriginalText;
            set
            {
                if (SetProperty(ref _lastCompletedProposalOriginalText, value))
                {
                    OnPropertyChanged(nameof(RequestPhaseSummary));
                    OnPropertyChanged(nameof(HasRequestPhaseSummary));
                    OnPropertyChanged(nameof(HasLastCompletedProposalPreview));
                    OnPropertyChanged(nameof(ActivityTitle));
                    OnPropertyChanged(nameof(ActivitySummary));
                    OnPropertyChanged(nameof(ActivityMetricsSummary));
                    OnPropertyChanged(nameof(HasActivityMetricsSummary));
                    OnPropertyChanged(nameof(CanOpenGitChanges));
                    OnPropertyChanged(nameof(IsReviewFocusedLayoutActive));
                    OnPropertyChanged(nameof(ShowProposalEditor));
                    NotifyResetStateChanged();
                    NotifySessionStateChanged();
                    OpenGitChangesCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public bool HasSubmittableProposal
        {
            get => _hasSubmittableProposal;
            set
            {
                if (SetProperty(ref _hasSubmittableProposal, value))
                    SubmitProposalCommand.NotifyCanExecuteChanged();
            }
        }

        public bool HasProposalDrafts
        {
            get => _hasProposalDrafts;
            set
            {
                if (SetProperty(ref _hasProposalDrafts, value))
                {
                    NotifySessionStateChanged();
                }
            }
        }

        public string LastCompletedProposalUpdatedText
        {
            get => _lastCompletedProposalUpdatedText;
            set
            {
                if (SetProperty(ref _lastCompletedProposalUpdatedText, value))
                {
                    OnPropertyChanged(nameof(RequestPhaseSummary));
                    OnPropertyChanged(nameof(HasRequestPhaseSummary));
                    OnPropertyChanged(nameof(HasLastCompletedProposalPreview));
                    OnPropertyChanged(nameof(ActivityTitle));
                    OnPropertyChanged(nameof(ActivitySummary));
                    OnPropertyChanged(nameof(ActivityMetricsSummary));
                    OnPropertyChanged(nameof(HasActivityMetricsSummary));
                    OnPropertyChanged(nameof(HasResettableProposalState));
                    OnPropertyChanged(nameof(CanOpenGitChanges));
                    OnPropertyChanged(nameof(IsReviewFocusedLayoutActive));
                    OnPropertyChanged(nameof(ShowProposalEditor));
                    NotifyResetStateChanged();
                    NotifySessionStateChanged();
                    OpenGitChangesCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public string LastCompletedProposalOriginalSegment
        {
            get => _lastCompletedProposalOriginalSegment;
            set
            {
                if (SetProperty(ref _lastCompletedProposalOriginalSegment, value))
                {
                    OnPropertyChanged(nameof(HasLastCompletedProposalRangePreview));
                    NotifySessionStateChanged();
                }
            }
        }

        public string LastCompletedProposalUpdatedSegment
        {
            get => _lastCompletedProposalUpdatedSegment;
            set
            {
                if (SetProperty(ref _lastCompletedProposalUpdatedSegment, value))
                {
                    OnPropertyChanged(nameof(HasLastCompletedProposalRangePreview));
                    NotifySessionStateChanged();
                }
            }
        }

        public IReadOnlyList<ProposalReviewedChange> LastCompletedProposalReviewedChanges
        {
            get => _lastCompletedProposalReviewedChanges;
            set
            {
                if (SetProperty(ref _lastCompletedProposalReviewedChanges, value))
                {
                    OnPropertyChanged(nameof(HasLastCompletedProposalReviewedChanges));
                    OnPropertyChanged(nameof(ActivityMetricsSummary));
                    OnPropertyChanged(nameof(HasActivityMetricsSummary));
                    NotifySessionStateChanged();
                }
            }
        }

        public IReadOnlyList<string> LastCompletedProposalIncludedFiles
        {
            get => _lastCompletedProposalIncludedFiles;
            set
            {
                var normalizedValue = NormalizeProposalSelectedFiles(value);
                if (ProposalSelectedFilesEqual(_lastCompletedProposalIncludedFiles, normalizedValue))
                    return;

                if (SetProperty(ref _lastCompletedProposalIncludedFiles, normalizedValue))
                {
                    OnPropertyChanged(nameof(HasLastCompletedProposalIncludedFiles));
                    OnPropertyChanged(nameof(LastCompletedProposalIncludedFilesHeader));
                    OnPropertyChanged(nameof(ActivityMetricsSummary));
                    OnPropertyChanged(nameof(HasActivityMetricsSummary));
                }
            }
        }

        public bool IsProposalFileLoaded
        {
            get => _isProposalFileLoaded;
            set
            {
                if (SetProperty(ref _isProposalFileLoaded, value))
                {
                    SubmitProposalCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public bool HasPendingApproval
        {
            get => _hasPendingApproval;
            set
            {
                if (SetProperty(ref _hasPendingApproval, value))
                {
                    OnPropertyChanged(nameof(RequestPhaseSummary));
                    OnPropertyChanged(nameof(HasRequestPhaseSummary));
                    OnPropertyChanged(nameof(ActivityTitle));
                    OnPropertyChanged(nameof(ActivitySummary));
                    OnPropertyChanged(nameof(ActivityMetricsSummary));
                    OnPropertyChanged(nameof(HasActivityMetricsSummary));
                    OnPropertyChanged(nameof(IsReviewFocusedLayoutActive));
                    OnPropertyChanged(nameof(IsProposalOriginalTextReadOnly));
                    OnPropertyChanged(nameof(IsProposalProposedTextReadOnly));
                    OnPropertyChanged(nameof(ShowProposalEditor));
                    OnPropertyChanged(nameof(CanOpenGitChanges));
                    NotifyResetStateChanged();
                    NotifySessionStateChanged();
                    BrowseProposalFileCommand.NotifyCanExecuteChanged();
                    RemoveProposalFileCommand.NotifyCanExecuteChanged();
                    SubmitProposalCommand.NotifyCanExecuteChanged();
                    OpenGitChangesCommand.NotifyCanExecuteChanged();
                    ApproveCommand.NotifyCanExecuteChanged();
                    RejectCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public bool CanBrowseProposalFile
        {
            get => _canBrowseProposalFile;
            set
            {
                if (SetProperty(ref _canBrowseProposalFile, value))
                    BrowseProposalFileCommand.NotifyCanExecuteChanged();
            }
        }

        public bool IsProposalOriginalTextReadOnly => true;

        public bool IsProposalProposedTextReadOnly => HasPendingApproval;

        public string ProposalSelectionSummary =>
            ProposalSelectedFiles.Count switch
            {
                0 => "No files selected yet.",
                1 => "1 file selected for this request.",
                _ => $"{ProposalSelectedFiles.Count} files selected for this request."
            };

        public string ProposalActiveFileSummary =>
            string.IsNullOrWhiteSpace(ProposalFilePath)
                ? "Current file: none selected."
                : $"Current file: {ProposalFilePath}";

        public string ActivityTitle
        {
            get
            {
                if (IsRequestInProgress)
                    return "Working";

                if (HasPendingApproval)
                    return "Review Ready";

                if (HasLastCompletedProposalPreview || !string.IsNullOrWhiteSpace(StatusMessage))
                    return "Result";

                return "Ready";
            }
        }

        public string ActivitySummary
        {
            get
            {
                if (IsRequestInProgress)
                    return "The bridge is preparing the proposal. You can continue reviewing context while work stays in flight.";

                if (HasPendingApproval)
                    return "A proposal is ready. Review the affected files and reviewed changes, then choose Keep or Reject.";

                if (HasLastCompletedProposalPreview && !string.IsNullOrWhiteSpace(StatusMessage))
                    return StatusMessage;

                if (!string.IsNullOrWhiteSpace(StatusMessage))
                    return StatusMessage;

                if (!string.IsNullOrWhiteSpace(RequestInputText))
                    return "Send the current request when the selected files and draft are ready.";

                return "Enter a request, select the relevant files, and send it when the draft is ready for review.";
            }
        }

        public string ActivityMetricsSummary
        {
            get
            {
                var affectedFileCount = ResolveAffectedFileCount();
                var reviewedChangeCount = ResolveReviewedChangeCount();

                if (IsRequestInProgress)
                    return affectedFileCount > 0
                        ? $"{FormatFileCount(affectedFileCount)} queued for proposal generation."
                        : string.Empty;

                if (HasPendingApproval)
                {
                    var metrics = new List<string>();
                    if (affectedFileCount > 0)
                        metrics.Add($"{FormatFileCount(affectedFileCount)} affected");
                    if (reviewedChangeCount > 0)
                        metrics.Add($"{FormatChangeCount(reviewedChangeCount)} reviewed");

                    return string.Join(" | ", metrics);
                }

                if (HasLastCompletedProposalPreview)
                {
                    var outcome = ClassifyOutcome(StatusMessage);
                    return outcome switch
                    {
                        ProposalOutcomeCategory.Success => $"{FormatFileCount(affectedFileCount)} applied.",
                        ProposalOutcomeCategory.Skip => $"{FormatFileCount(affectedFileCount)} already matched the approved content.",
                        ProposalOutcomeCategory.Rejected => $"{FormatFileCount(affectedFileCount)} left unchanged after rejection.",
                        ProposalOutcomeCategory.Failure => $"{FormatFileCount(affectedFileCount)} protected by all-or-nothing apply.",
                        _ => affectedFileCount > 0 ? $"{FormatFileCount(affectedFileCount)} in the last completed proposal." : string.Empty
                    };
                }

                return affectedFileCount > 0
                    ? $"{FormatFileCount(affectedFileCount)} selected."
                    : string.Empty;
            }
        }

        public bool HasActivityMetricsSummary => !string.IsNullOrWhiteSpace(ActivityMetricsSummary);

        public bool CanOpenGitChanges =>
            !HasPendingApproval
            && !IsRequestInProgress
            && HasLastCompletedProposalPreview;

        public string RequestPhaseSummary
        {
            get
            {
                if (IsRequestInProgress)
                {
                    return "The bridge is preparing the proposal for review.";
                }

                if (HasPendingApproval)
                {
                    return "Approval review is ready. Inspect the concise review below, then choose Keep or Reject.";
                }

                if (HasLastCompletedProposalPreview)
                {
                    return "Last proposal result is available below. Review the summary or reset to start the next request.";
                }

                if (!string.IsNullOrWhiteSpace(StatusMessage))
                {
                    return StatusMessage;
                }

                if (ProposalSelectedFiles.Count > 0)
                {
                    return "Adjust the proposed content if needed, then submit when at least one selected file has a meaningful change.";
                }

                return "Select one or more files, prepare the proposed content, and submit when the request is ready for review.";
            }
        }

        public bool HasRequestPhaseSummary => !string.IsNullOrWhiteSpace(RequestPhaseSummary);

        public bool HasPendingApprovalRangePreview =>
            !string.IsNullOrEmpty(PendingApprovalOriginalSegment)
            || !string.IsNullOrEmpty(PendingApprovalUpdatedSegment);

        public bool HasPendingApprovalReviewedChanges => PendingApprovalReviewedChanges.Count > 0;

        public bool HasPendingApprovalIncludedFiles => PendingApprovalIncludedFiles.Count > 0;

        public string PendingApprovalIncludedFilesHeader => $"Included Files ({PendingApprovalIncludedFiles.Count})";

        public bool HasLastCompletedProposalPreview =>
            !string.IsNullOrEmpty(LastCompletedProposalOriginalText)
            || !string.IsNullOrEmpty(LastCompletedProposalUpdatedText);

        public bool HasLastCompletedProposalRangePreview =>
            !string.IsNullOrEmpty(LastCompletedProposalOriginalSegment)
            || !string.IsNullOrEmpty(LastCompletedProposalUpdatedSegment);

        public bool HasLastCompletedProposalReviewedChanges => LastCompletedProposalReviewedChanges.Count > 0;

        public bool HasLastCompletedProposalIncludedFiles => LastCompletedProposalIncludedFiles.Count > 0;

        public string LastCompletedProposalIncludedFilesHeader => $"Included Files ({LastCompletedProposalIncludedFiles.Count})";

        public bool IsReviewFocusedLayoutActive => HasPendingApproval || HasLastCompletedProposalPreview;

        public bool HasResettableProposalState =>
            HasRequestInputText
            || HasLastSubmittedRequest
            || IsRequestInProgress
            || HasPendingApproval
            || HasLastCompletedProposalPreview
            || !string.IsNullOrWhiteSpace(StatusMessage);

        public bool HasSessionState =>
            ProposalSelectedFiles.Count > 0
            || !string.IsNullOrWhiteSpace(ProposalFilePath)
            || HasProposalDrafts
            || HasResettableProposalState;

        public bool ShowProposalEditor => true;

        public bool ShowSubmitProposalButton => !IsReviewFocusedLayoutActive;

        public IRelayCommand BrowseProposalFileCommand { get; }

        public IRelayCommand RemoveProposalFileCommand { get; }

        public IRelayCommand ResetProposalCommand { get; }

        public IRelayCommand NewChatCommand { get; }

        public IRelayCommand SubmitProposalCommand { get; }

        public IRelayCommand OpenGitChangesCommand { get; }

        public IRelayCommand ApproveCommand { get; }

        public IRelayCommand RejectCommand { get; }

        public LogLevel SelectedLogLevel
        {
            get => _selectedLogLevel;
            set
            {
                if (SetProperty(ref _selectedLogLevel, value))
                    _logLevelSettings.MinimumLevel = value;
            }
        }

        public IReadOnlyList<LogLevel> AvailableLogLevels => _availableLogLevels;

        public void SetProposalBrowseHandler(Action? onBrowseProposalFileRequested)
        {
            _onBrowseProposalFileRequested = onBrowseProposalFileRequested;
            CanBrowseProposalFile = onBrowseProposalFileRequested is not null;
            BrowseProposalFileCommand.NotifyCanExecuteChanged();
        }

        public void SetProposalRemoveHandler(Action? onRemoveProposalFileRequested)
        {
            _onRemoveProposalFileRequested = onRemoveProposalFileRequested;
            RemoveProposalFileCommand.NotifyCanExecuteChanged();
        }

        public void SetProposalResetHandler(Action? onResetProposalRequested)
        {
            _onResetProposalRequested = onResetProposalRequested;
            ResetProposalCommand.NotifyCanExecuteChanged();
        }

        public void SetNewChatHandler(Action? onNewChatRequested)
        {
            _onNewChatRequested = onNewChatRequested;
            NewChatCommand.NotifyCanExecuteChanged();
        }

        public void SetProposalSubmissionHandler(Action? onSubmitProposalRequested)
        {
            _onSubmitProposalRequested = onSubmitProposalRequested;
            SubmitProposalCommand.NotifyCanExecuteChanged();
        }

        public void SetOpenGitChangesHandler(Action? onOpenGitChangesRequested)
        {
            _onOpenGitChangesRequested = onOpenGitChangesRequested;
            OpenGitChangesCommand.NotifyCanExecuteChanged();
        }

        public void SetApprovalRequestHandlers(Action? onApproveRequested, Action? onRejectRequested)
        {
            _onApproveRequested = onApproveRequested;
            _onRejectRequested = onRejectRequested;
            ApproveCommand.NotifyCanExecuteChanged();
            RejectCommand.NotifyCanExecuteChanged();
        }

        private bool CanSubmitProposal()
        {
            return !HasPendingApproval
                && HasRequestInputText
                && _onSubmitProposalRequested is not null;
        }

        private void NotifyResetStateChanged()
        {
            OnPropertyChanged(nameof(HasResettableProposalState));
            ResetProposalCommand.NotifyCanExecuteChanged();
        }

        private void NotifySessionStateChanged()
        {
            OnPropertyChanged(nameof(HasSessionState));
            NewChatCommand.NotifyCanExecuteChanged();
        }

        private static IReadOnlyList<string> NormalizeProposalSelectedFiles(IReadOnlyList<string>? value)
        {
            if (value == null || value.Count == 0)
                return Array.Empty<string>();

            return value is string[] stringArray ? stringArray : value.ToArray();
        }

        private static bool ProposalSelectedFilesEqual(IReadOnlyList<string> left, IReadOnlyList<string> right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (left.Count != right.Count)
                return false;

            for (var index = 0; index < left.Count; index++)
            {
                if (!string.Equals(left[index], right[index], StringComparison.Ordinal))
                    return false;
            }

            return true;
        }

        private int ResolveAffectedFileCount()
        {
            if (HasPendingApprovalIncludedFiles)
                return PendingApprovalIncludedFiles.Count;

            if (HasLastCompletedProposalIncludedFiles)
                return LastCompletedProposalIncludedFiles.Count;

            return ProposalSelectedFiles.Count;
        }

        private int ResolveReviewedChangeCount()
        {
            if (HasPendingApprovalReviewedChanges)
                return PendingApprovalReviewedChanges.Count;

            if (HasLastCompletedProposalReviewedChanges)
                return LastCompletedProposalReviewedChanges.Count;

            return 0;
        }

        private static string FormatFileCount(int count) => count == 1 ? "1 file" : $"{count} files";

        private static string FormatChangeCount(int count) => count == 1 ? "1 change" : $"{count} changes";

        private static ProposalOutcomeCategory ClassifyOutcome(string? statusMessage)
        {
            if (string.IsNullOrWhiteSpace(statusMessage))
                return ProposalOutcomeCategory.None;

            var normalizedStatus = statusMessage!;

            if (normalizedStatus.StartsWith("Apply succeeded", StringComparison.Ordinal))
                return ProposalOutcomeCategory.Success;

            if (normalizedStatus.StartsWith("Skipped apply", StringComparison.Ordinal))
                return ProposalOutcomeCategory.Skip;

            if (normalizedStatus.StartsWith("Proposal rejected", StringComparison.Ordinal))
                return ProposalOutcomeCategory.Rejected;

            if (normalizedStatus.StartsWith("Apply failed", StringComparison.Ordinal))
                return ProposalOutcomeCategory.Failure;

            return ProposalOutcomeCategory.None;
        }

        private enum ProposalOutcomeCategory
        {
            None,
            Success,
            Skip,
            Rejected,
            Failure
        }
    }
}
