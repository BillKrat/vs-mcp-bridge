using System.Collections.Generic;

namespace BlogAI.Web.Auth;

public sealed class BlogAiLocalAuthStatus
{
    public BlogAiLocalAuthStatus(
        string resourceName,
        string clientApplication,
        string environment,
        string authPath,
        string authPathLabel,
        bool isDiagnosticMode,
        string? diagnosticMessage,
        IReadOnlyList<BlogAiLocalAuthDecisionDisplay> decisions)
    {
        ResourceName = resourceName;
        ClientApplication = clientApplication;
        Environment = environment;
        AuthPath = authPath;
        AuthPathLabel = authPathLabel;
        IsDiagnosticMode = isDiagnosticMode;
        DiagnosticMessage = diagnosticMessage;
        Decisions = decisions;
    }

    public string ResourceName { get; }

    public string ClientApplication { get; }

    public string Environment { get; }

    public string AuthPath { get; }

    public string AuthPathLabel { get; }

    public bool IsDiagnosticMode { get; }

    public string? DiagnosticMessage { get; }

    public IReadOnlyList<BlogAiLocalAuthDecisionDisplay> Decisions { get; }
}
