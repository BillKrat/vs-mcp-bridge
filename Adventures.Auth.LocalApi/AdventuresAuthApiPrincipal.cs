using VsMcpBridge.Shared.AdventuresAuth;

namespace Adventures.Auth.LocalApi;

public sealed class AdventuresAuthApiPrincipal
{
    public string Subject { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public IReadOnlyDictionary<string, string> Claims { get; set; } =
        new Dictionary<string, string>();

    public static AdventuresAuthApiPrincipal? FromPrincipal(AdventuresAuthPrincipal? principal)
    {
        if (principal == null)
            return null;

        return new AdventuresAuthApiPrincipal
        {
            Subject = principal.Subject,
            DisplayName = principal.DisplayName,
            Claims = principal.Claims
        };
    }
}
