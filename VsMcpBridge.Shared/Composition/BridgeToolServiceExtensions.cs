using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VsMcpBridge.Shared.Tools;

namespace VsMcpBridge.Shared.Composition
{
    public static class BridgeToolServiceExtensions
    {
        public static IServiceCollection AddBridgeToolServices(this IServiceCollection services)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IBridgeTool, RegexTextSearchTool>());
            services.TryAddSingleton<IBridgeToolCatalog, CompiledBridgeToolCatalog>();
            services.TryAddSingleton<IBridgeToolExecutor, BridgeToolExecutor>();

            return services;
        }
    }
}
