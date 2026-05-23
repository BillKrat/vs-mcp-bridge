using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BlogAI.Web.Auth;

public static class BlogAiLocalAuthStatusServiceExtensions
{
    public static IServiceCollection AddBlogAiLocalAuthStatusServices(this IServiceCollection services)
    {
        services.TryAddScoped<IBlogAiLocalAuthStatusService, BlogAiLocalAuthStatusService>();

        return services;
    }
}
