using Microsoft.Extensions.DependencyInjection;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Vsix.MvpVm;

namespace VsMcpBridge.Vsix.Composition
{
    public static class MvpVmServiceExtensions
    {
        public static IServiceCollection AddMvpVmServices(this IServiceCollection services)
        {
            services.AddSingleton<ILogToolWindowPresenter, LogToolWindowPresenter>();
            services.AddSingleton<ILogToolWindowViewModel, LogToolWindowViewModel>();

            return services;
        }
    }
}
