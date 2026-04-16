using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.Loggers;

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
        private string _proposalFilePath = string.Empty;
        private string _proposalOriginalText = string.Empty;
        private string _proposalProposedText = string.Empty;
        private string _pendingApprovalDescription = string.Empty;
        private string _pendingApprovalOriginalSegment = string.Empty;
        private string _pendingApprovalUpdatedSegment = string.Empty;
        private string _lastCompletedProposalOriginalText = string.Empty;
        private string _lastCompletedProposalUpdatedText = string.Empty;
        private string _lastCompletedProposalOriginalSegment = string.Empty;
        private string _lastCompletedProposalUpdatedSegment = string.Empty;
        private string _statusMessage = string.Empty;
        private bool _isProposalFileLoaded;
        private bool _canBrowseProposalFile;
        private bool _hasPendingApproval;
        private LogLevel _selectedLogLevel;
        private Action? _onBrowseProposalFileRequested;
        private Action<string, string, string>? _onSubmitProposalRequested;
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

            SubmitProposalCommand = new RelayCommand(
                execute: () => _onSubmitProposalRequested?.Invoke(ProposalFilePath, ProposalOriginalText, ProposalProposedText),
                canExecute: CanSubmitProposal);

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

        public string ProposalFilePath
        {
            get => _proposalFilePath;
            set
            {
                if (SetProperty(ref _proposalFilePath, value))
                {
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
                    SubmitProposalCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public string PendingApprovalDescription
        {
            get => _pendingApprovalDescription;
            set => SetProperty(ref _pendingApprovalDescription, value);
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

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string LastCompletedProposalOriginalText
        {
            get => _lastCompletedProposalOriginalText;
            set
            {
                if (SetProperty(ref _lastCompletedProposalOriginalText, value))
                {
                    OnPropertyChanged(nameof(HasLastCompletedProposalPreview));
                    OnPropertyChanged(nameof(ShowProposalEditor));
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
                    OnPropertyChanged(nameof(HasLastCompletedProposalPreview));
                    OnPropertyChanged(nameof(ShowProposalEditor));
                }
            }
        }

        public string LastCompletedProposalOriginalSegment
        {
            get => _lastCompletedProposalOriginalSegment;
            set
            {
                if (SetProperty(ref _lastCompletedProposalOriginalSegment, value))
                    OnPropertyChanged(nameof(HasLastCompletedProposalRangePreview));
            }
        }

        public string LastCompletedProposalUpdatedSegment
        {
            get => _lastCompletedProposalUpdatedSegment;
            set
            {
                if (SetProperty(ref _lastCompletedProposalUpdatedSegment, value))
                    OnPropertyChanged(nameof(HasLastCompletedProposalRangePreview));
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
                    OnPropertyChanged(nameof(IsProposalOriginalTextReadOnly));
                    OnPropertyChanged(nameof(IsProposalProposedTextReadOnly));
                    BrowseProposalFileCommand.NotifyCanExecuteChanged();
                    SubmitProposalCommand.NotifyCanExecuteChanged();
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

        public bool HasPendingApprovalRangePreview =>
            !string.IsNullOrEmpty(PendingApprovalOriginalSegment)
            || !string.IsNullOrEmpty(PendingApprovalUpdatedSegment);

        public bool HasLastCompletedProposalPreview =>
            !string.IsNullOrEmpty(LastCompletedProposalOriginalText)
            || !string.IsNullOrEmpty(LastCompletedProposalUpdatedText);

        public bool HasLastCompletedProposalRangePreview =>
            !string.IsNullOrEmpty(LastCompletedProposalOriginalSegment)
            || !string.IsNullOrEmpty(LastCompletedProposalUpdatedSegment);

        public bool ShowProposalEditor => !HasLastCompletedProposalPreview;

        public IRelayCommand BrowseProposalFileCommand { get; }

        public IRelayCommand SubmitProposalCommand { get; }

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

        public void SetProposalSubmissionHandler(Action<string, string, string>? onSubmitProposalRequested)
        {
            _onSubmitProposalRequested = onSubmitProposalRequested;
            SubmitProposalCommand.NotifyCanExecuteChanged();
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
                && IsProposalFileLoaded
                && _onSubmitProposalRequested is not null
                && !string.IsNullOrWhiteSpace(ProposalFilePath)
                && !string.Equals(ProposalOriginalText, ProposalProposedText, StringComparison.Ordinal);
        }
    }
}
