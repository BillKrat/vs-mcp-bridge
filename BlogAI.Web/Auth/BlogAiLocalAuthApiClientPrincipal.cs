namespace BlogAI.Web.Auth;

public sealed class BlogAiLocalAuthApiClientPrincipal
{
    public string Subject { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public IReadOnlyDictionary<string, string> Claims { get; set; } =
        new Dictionary<string, string>();
}
