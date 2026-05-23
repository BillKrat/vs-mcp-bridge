using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VsMcpBridge.Shared.AdventuresAuth;
using VsMcpBridge.Shared.BlogAI.Auth;

namespace VsMcpBridge.Shared.Composition
{
    public static class BlogAiAuthConsumerServiceExtensions
    {
        public static IServiceCollection AddBlogAiAuthConsumerServices(this IServiceCollection services)
        {
            services.AddBridgeSecurityServices();
            services.TryAddSingleton<AdventuresAuthDecisionService>();
            services.TryAddSingleton<IBlogAiAuthConsumerService, BlogAiAuthConsumerService>();

            return services;
        }
    }
}
