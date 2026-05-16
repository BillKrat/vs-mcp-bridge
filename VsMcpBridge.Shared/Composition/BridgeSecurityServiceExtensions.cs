using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VsMcpBridge.Shared.Security;

namespace VsMcpBridge.Shared.Composition
{
    public static class BridgeSecurityServiceExtensions
    {
        public static IServiceCollection AddBridgeSecurityServices(this IServiceCollection services)
        {
            services.TryAddSingleton<ISecurityRedactor, BridgeSecurityRedactor>();
            services.TryAddSingleton<IAuditSink, NoOpAuditSink>();
            services.TryAddSingleton<IToolExecutionPolicy, AllowToolExecutionPolicy>();

            return services;
        }
    }
}
