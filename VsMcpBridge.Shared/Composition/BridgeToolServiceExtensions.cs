using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using VsMcpBridge.Shared.Tools;

namespace VsMcpBridge.Shared.Composition
{
    public static class BridgeToolServiceExtensions
    {
        public static IServiceCollection AddBridgeToolServices(
            this IServiceCollection services,
            Action<BridgeToolDiscoveryOptions>? configureDiscovery = null)
        {
            services.AddBridgeSecurityServices();
            var discoveryOptions = new BridgeToolDiscoveryOptions();
            configureDiscovery?.Invoke(discoveryOptions);
            services.TryAddSingleton(discoveryOptions);
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IBridgeTool, RegexTextSearchTool>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IBridgeTool, Bm25TextSearchTool>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IBridgeToolDiscovery, CompiledBridgeToolDiscovery>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IBridgeToolDiscovery, MefBridgeToolDiscovery>());
            services.TryAddSingleton<IBridgeToolCatalog>(provider =>
                new CompiledBridgeToolCatalog(provider.GetRequiredService<System.Collections.Generic.IEnumerable<IBridgeToolDiscovery>>()));
            services.TryAddSingleton<IBridgeToolExecutor, BridgeToolExecutor>();

            return services;
        }
    }
}
