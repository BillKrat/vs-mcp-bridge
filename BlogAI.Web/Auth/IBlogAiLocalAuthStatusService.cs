namespace BlogAI.Web.Auth;

public interface IBlogAiLocalAuthStatusService
{
    Task<BlogAiLocalAuthStatus> GetStatusAsync(
        string? authPath = null,
        CancellationToken cancellationToken = default);
}
