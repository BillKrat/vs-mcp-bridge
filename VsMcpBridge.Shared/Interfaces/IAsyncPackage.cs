using System;

namespace VsMcpBridge.Shared.Interfaces
{
    public interface IAsyncPackage
    {
        System.Threading.Tasks.Task<T> GetServiceAsync<T>(Type type);
    }
}
