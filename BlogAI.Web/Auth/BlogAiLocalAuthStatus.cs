using System.Collections.Generic;

namespace BlogAI.Web.Auth;

public sealed class BlogAiLocalAuthStatus
{
    public BlogAiLocalAuthStatus(
        string resourceName,
        string clientApplication,
        string environment,
        IReadOnlyList<BlogAiLocalAuthDecisionDisplay> decisions)
    {
        ResourceName = resourceName;
        ClientApplication = clientApplication;
        Environment = environment;
        Decisions = decisions;
    }

    public string ResourceName { get; }

    public string ClientApplication { get; }

    public string Environment { get; }

    public IReadOnlyList<BlogAiLocalAuthDecisionDisplay> Decisions { get; }
}
