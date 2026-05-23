namespace BlogAI.Web.Auth;

public interface IBlogAiLocalAuthApiClient
{
    Task<BlogAiLocalAuthApiClientDecision> LoginAsync(
        BlogAiLocalAuthApiClientRequest request,
        CancellationToken cancellationToken = default);

    Task<BlogAiLocalAuthApiClientDecision> LogoutAsync(
        BlogAiLocalAuthApiClientRequest request,
        CancellationToken cancellationToken = default);

    Task<BlogAiLocalAuthApiClientDecision> GetCurrentPrincipalAsync(
        BlogAiLocalAuthApiClientRequest request,
        CancellationToken cancellationToken = default);

    Task<BlogAiLocalAuthApiClientDecision> ValidateAsync(
        BlogAiLocalAuthApiClientRequest request,
        CancellationToken cancellationToken = default);
}
