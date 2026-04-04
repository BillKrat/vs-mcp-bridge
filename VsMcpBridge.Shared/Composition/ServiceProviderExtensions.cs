using Microsoft.Extensions.DependencyInjection;
using System;

namespace VsMcpBridge.Shared.Composition
{
    public static class ServiceProviderExtensions
    {
        public static T Resolve<T>(this ServiceProvider serviceProvider)
        {
            var service = serviceProvider.GetService(typeof(T));
            if (service is null)
            {
                throw new InvalidOperationException($"Service of type {typeof(T).FullName} is not registered.");
            }
            return (T)service;
        }
    }
}
