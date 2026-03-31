using Microsoft.Extensions.DependencyInjection;
using VsMcpBridge.Vsix.MvpVm;
using VsMcpBridge.Vsix.ToolWindows;

namespace VsMcpBridge.Vsix.Composition
{
    internal static class MvpVmServiceExtensions
    {
        internal static IServiceCollection AddMvpVmServices(this IServiceCollection services)
        {
            services.AddSingleton<ILogToolWindowPresenter, LogToolWindowPresenter>();
            services.AddSingleton<ILogToolWindowViewModel, LogToolWindowViewModel>();
            services.AddSingleton<ILogToolWindowControl, LogToolWindowControl>();

            return services;
        }

    }
}
