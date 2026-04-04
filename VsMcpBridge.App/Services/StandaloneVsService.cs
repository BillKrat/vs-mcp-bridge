using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.Models;

namespace VsMcpBridge.App.Services;

internal sealed class StandaloneVsService : IVsService
{
    private static readonly Regex DiagnosticPattern = new(
        @"^(?<file>.+?)\((?<line>\d+)(,(?<column>\d+))?\):\s(?<severity>error|warning)\s(?<code>[^:]+):\s(?<description>.+?)(\s\[(?<project>.+)\])?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly IBridgeLogger _logger;
    private readonly IThreadHelper _threadHelper;
    private readonly IApprovalWorkflowService _approvalWorkflowService;
    private readonly IEditApplier _editApplier;
    private readonly ILogToolWindowPresenter _logToolWindowPresenter;
    private readonly AppSessionState _sessionState;

    public StandaloneVsService(
        IBridgeLogger logger,
        IThreadHelper threadHelper,
        IApprovalWorkflowService approvalWorkflowService,
        IEditApplier editApplier,
        ILogToolWindowPresenter logToolWindowPresenter,
        AppSessionState sessionState)
    {
        _logger = logger;
        _threadHelper = threadHelper;
        _approvalWorkflowService = approvalWorkflowService;
        _editApplier = editApplier;
        _logToolWindowPresenter = logToolWindowPresenter;
        _sessionState = sessionState;
        _logger.LogVerbose("Bridge service startup complete.");
    }

    public Task<GetActiveDocumentResponse> GetActiveDocumentAsync()
    {
        _logger.LogInformation("Running app service operation 'GetActiveDocument'.");

        try
        {
            var filePath = GetActiveFilePath();
            if (string.IsNullOrWhiteSpace(filePath))
                return Task.FromResult(new GetActiveDocumentResponse { Success = false, ErrorMessage = "No active document." });

            if (!File.Exists(filePath))
                return Task.FromResult(new GetActiveDocumentResponse { Success = false, ErrorMessage = $"Active document '{filePath}' was not found." });

            var content = File.ReadAllText(filePath);
            return Task.FromResult(new GetActiveDocumentResponse
            {
                Success = true,
                FilePath = filePath,
                Language = DetectLanguage(filePath),
                Content = content
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("App service operation 'GetActiveDocument' failed.", ex);
            throw;
        }
    }

    public Task<GetSelectedTextResponse> GetSelectedTextAsync()
    {
        _logger.LogInformation("Running app service operation 'GetSelectedText'.");

        try
        {
            var filePath = GetActiveFilePath();
            if (string.IsNullOrWhiteSpace(filePath))
                return Task.FromResult(new GetSelectedTextResponse { Success = false, ErrorMessage = "No active document." });

            var selectedText = _sessionState.SelectedText;
            if (string.IsNullOrEmpty(selectedText))
                return Task.FromResult(new GetSelectedTextResponse { Success = false, FilePath = filePath, ErrorMessage = "No selected text." });

            return Task.FromResult(new GetSelectedTextResponse
            {
                Success = true,
                FilePath = filePath,
                SelectedText = selectedText
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("App service operation 'GetSelectedText' failed.", ex);
            throw;
        }
    }

    public Task<ListSolutionProjectsResponse> ListSolutionProjectsAsync()
    {
        _logger.LogInformation("Running app service operation 'ListSolutionProjects'.");

        try
        {
            var workspaceRoot = ResolveWorkspaceRoot();
            if (workspaceRoot == null)
                return Task.FromResult(new ListSolutionProjectsResponse { Success = false, ErrorMessage = "No workspace available." });

            var projectFiles = Directory.EnumerateFiles(workspaceRoot, "*.*proj", SearchOption.AllDirectories)
                .Where(path => !IsIgnoredPath(path))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var projects = projectFiles.Select(path => new ProjectInfo
            {
                Name = Path.GetFileNameWithoutExtension(path),
                FullPath = path,
                TargetFramework = ReadTargetFramework(path)
            }).ToList();

            return Task.FromResult(new ListSolutionProjectsResponse
            {
                Success = true,
                Projects = projects
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("App service operation 'ListSolutionProjects' failed.", ex);
            throw;
        }
    }

    public async Task<GetErrorListResponse> GetErrorListAsync()
    {
        _logger.LogInformation("Running app service operation 'GetErrorList'.");

        try
        {
            var buildTarget = ResolveBuildTarget();
            if (buildTarget == null)
            {
                return new GetErrorListResponse
                {
                    Success = false,
                    ErrorMessage = "No solution or project was found for diagnostics."
                };
            }

            var workingDirectory = File.Exists(buildTarget)
                ? Path.GetDirectoryName(buildTarget)!
                : buildTarget;

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build \"{buildTarget}\" --nologo --verbosity:minimal",
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return new GetErrorListResponse
                {
                    Success = false,
                    ErrorMessage = "Failed to start dotnet build."
                };
            }

            var stdout = await process.StandardOutput.ReadToEndAsync();
            var stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            var diagnostics = ParseDiagnostics(stdout)
                .Concat(ParseDiagnostics(stderr))
                .ToList();

            return new GetErrorListResponse
            {
                Success = process.ExitCode == 0 || diagnostics.Count > 0,
                ErrorMessage = process.ExitCode == 0 ? null : "Build reported errors.",
                Diagnostics = diagnostics
            };
        }
        catch (Exception ex)
        {
            _logger.LogError("App service operation 'GetErrorList' failed.", ex);
            throw;
        }
    }

    public Task<ProposeTextEditResponse> ProposeTextEditAsync(
        string requestId,
        string filePath,
        string originalText,
        string proposedText)
    {
        _logger.LogInformation($"Generating proposed diff for '{filePath}' [RequestId={requestId}].");

        _sessionState.SetActiveFilePath(filePath);
        _sessionState.SetSelectedText(originalText);

        var diff = GenerateUnifiedDiff(filePath, originalText, proposedText);
        if (!string.IsNullOrEmpty(diff))
        {
            var proposal = _approvalWorkflowService.CreateProposal(requestId, filePath, diff);
            _logger.LogInformation($"Created edit proposal [RequestId={proposal.RequestId}] [ProposalId={proposal.ProposalId}] for '{proposal.FilePath}'.");
            _logger.LogInformation($"Proposal pending approval [RequestId={proposal.RequestId}] [ProposalId={proposal.ProposalId}] for '{proposal.FilePath}'.");
            _logToolWindowPresenter.ShowApprovalPrompt(
                BuildProposalDescription(proposal),
                () => _threadHelper.Run(() => ApproveAndApplyAsync(proposal.ProposalId)),
                () => RejectProposal(proposal.ProposalId));
        }
        else
        {
            _logger.LogInformation($"No edit proposal created because the proposed text matches the original text [RequestId={requestId}] for '{filePath}'.");
        }

        return Task.FromResult(new ProposeTextEditResponse
        {
            RequestId = requestId,
            Success = true,
            FilePath = filePath,
            Diff = diff
        });
    }

    private string GetActiveFilePath()
    {
        if (!string.IsNullOrWhiteSpace(_sessionState.ActiveFilePath))
            return _sessionState.ActiveFilePath;

        var buildTarget = ResolveBuildTarget();
        if (buildTarget == null)
            return string.Empty;

        if (buildTarget.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)
            || buildTarget.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase))
        {
            var workspaceRoot = Path.GetDirectoryName(buildTarget);
            if (workspaceRoot == null)
                return string.Empty;

            return Directory.EnumerateFiles(workspaceRoot, "*.cs", SearchOption.AllDirectories)
                .FirstOrDefault(path => !IsIgnoredPath(path))
                ?? string.Empty;
        }

        return buildTarget;
    }

    private string? ResolveBuildTarget()
    {
        var workspaceRoot = ResolveWorkspaceRoot();
        if (workspaceRoot == null)
            return null;

        var solution = Directory.EnumerateFiles(workspaceRoot, "*.sln", SearchOption.TopDirectoryOnly)
            .Concat(Directory.EnumerateFiles(workspaceRoot, "*.slnx", SearchOption.TopDirectoryOnly))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (solution != null)
            return solution;

        return Directory.EnumerateFiles(workspaceRoot, "*.*proj", SearchOption.AllDirectories)
            .Where(path => !IsIgnoredPath(path))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private string? ResolveWorkspaceRoot()
    {
        var activeFilePath = _sessionState.ActiveFilePath;
        if (!string.IsNullOrWhiteSpace(activeFilePath))
        {
            var candidate = File.Exists(activeFilePath)
                ? Path.GetDirectoryName(activeFilePath)
                : activeFilePath;

            var rooted = FindWorkspaceRoot(candidate);
            if (rooted != null)
                return rooted;
        }

        return FindWorkspaceRoot(Environment.CurrentDirectory);
    }

    private static string? FindWorkspaceRoot(string? startDirectory)
    {
        if (string.IsNullOrWhiteSpace(startDirectory))
            return null;

        var current = Directory.Exists(startDirectory)
            ? new DirectoryInfo(startDirectory)
            : new DirectoryInfo(Path.GetDirectoryName(startDirectory)!);

        while (current != null)
        {
            if (current.EnumerateFiles("*.sln", SearchOption.TopDirectoryOnly).Any()
                || current.EnumerateFiles("*.slnx", SearchOption.TopDirectoryOnly).Any()
                || current.EnumerateFiles("*.*proj", SearchOption.TopDirectoryOnly).Any())
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }

    private static bool IsIgnoredPath(string path)
    {
        var normalized = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        return normalized.IndexOf($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) >= 0
            || normalized.IndexOf($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string ReadTargetFramework(string projectPath)
    {
        try
        {
            var document = XDocument.Load(projectPath);
            var propertyGroup = document.Root?
                .Elements()
                .FirstOrDefault(element => string.Equals(element.Name.LocalName, "PropertyGroup", StringComparison.OrdinalIgnoreCase));

            var targetFramework = propertyGroup?
                .Elements()
                .FirstOrDefault(element => string.Equals(element.Name.LocalName, "TargetFramework", StringComparison.OrdinalIgnoreCase))
                ?.Value;

            if (!string.IsNullOrWhiteSpace(targetFramework))
                return targetFramework;

            var targetFrameworks = propertyGroup?
                .Elements()
                .FirstOrDefault(element => string.Equals(element.Name.LocalName, "TargetFrameworks", StringComparison.OrdinalIgnoreCase))
                ?.Value;

            return targetFrameworks?.Split(';').FirstOrDefault()?.Trim() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string DetectLanguage(string filePath)
    {
        return Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".cs" => "C#",
            ".vb" => "Visual Basic",
            ".cpp" or ".cc" or ".cxx" => "C++",
            ".c" => "C",
            ".ts" => "TypeScript",
            ".js" => "JavaScript",
            ".json" => "JSON",
            ".xml" => "XML",
            ".xaml" => "XAML",
            ".md" => "Markdown",
            _ => string.Empty
        };
    }

    private static IEnumerable<DiagnosticItem> ParseDiagnostics(string output)
    {
        using var reader = new StringReader(output);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            var match = DiagnosticPattern.Match(line);
            if (!match.Success)
                continue;

            yield return new DiagnosticItem
            {
                Severity = NormalizeSeverity(match.Groups["severity"].Value),
                Code = match.Groups["code"].Value.Trim(),
                Description = match.Groups["description"].Value.Trim(),
                File = match.Groups["file"].Value.Trim(),
                Line = ParseInt(match.Groups["line"].Value),
                Column = ParseInt(match.Groups["column"].Value),
                Project = match.Groups["project"].Value.Trim()
            };
        }
    }

    private static int ParseInt(string value)
    {
        return int.TryParse(value, out var parsed) ? parsed : 0;
    }

    private static string NormalizeSeverity(string severity)
    {
        return severity.Equals("error", StringComparison.OrdinalIgnoreCase) ? "Error" : "Warning";
    }

    private static string BuildProposalDescription(EditProposal proposal)
    {
        return $"Pending proposal for '{proposal.FilePath}'{Environment.NewLine}{proposal.Diff}";
    }

    private async Task ApproveAndApplyAsync(string proposalId)
    {
        var proposal = _approvalWorkflowService.Approve(proposalId);
        _logger.LogInformation($"Proposal approved [RequestId={proposal.RequestId}] [ProposalId={proposal.ProposalId}] for '{proposal.FilePath}'.");

        try
        {
            await _editApplier.ApplyAsync(proposal);
            _approvalWorkflowService.MarkApplied(proposalId);
            _logger.LogInformation($"Apply succeeded [RequestId={proposal.RequestId}] [ProposalId={proposal.ProposalId}] for '{proposal.FilePath}'.");
        }
        catch (Exception ex)
        {
            _approvalWorkflowService.MarkFailed(proposalId);
            _logger.LogError($"Apply failed [RequestId={proposal.RequestId}] [ProposalId={proposal.ProposalId}] for '{proposal.FilePath}'.", ex);
        }
    }

    private void RejectProposal(string proposalId)
    {
        var proposal = _approvalWorkflowService.Reject(proposalId);
        _logger.LogInformation($"Proposal rejected [RequestId={proposal.RequestId}] [ProposalId={proposal.ProposalId}] for '{proposal.FilePath}'.");
    }

    private static string GenerateUnifiedDiff(string filePath, string original, string proposed)
    {
        var originalLines = original.Split('\n');
        var proposedLines = proposed.Split('\n');

        if (original == proposed)
            return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine($"--- a/{filePath}");
        sb.AppendLine($"+++ b/{filePath}");

        int i = 0;
        int j = 0;
        while (i < originalLines.Length || j < proposedLines.Length)
        {
            if (i < originalLines.Length && j < proposedLines.Length && originalLines[i] == proposedLines[j])
            {
                sb.AppendLine($" {originalLines[i]}");
                i++;
                j++;
            }
            else if (i < originalLines.Length)
            {
                sb.AppendLine($"-{originalLines[i]}");
                i++;
            }
            else
            {
                sb.AppendLine($"+{proposedLines[j]}");
                j++;
            }
        }

        return sb.ToString();
    }
}
