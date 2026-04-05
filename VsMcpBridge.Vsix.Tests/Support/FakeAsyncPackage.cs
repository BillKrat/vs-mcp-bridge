using Microsoft.VisualStudio.Shell;
using System;
using System.Threading.Tasks;
using VsMcpBridge.Shared.Interfaces;

namespace VsMcpBridge.Vsix.Tests.Support;

internal sealed class FakeAsyncPackage : AsyncPackage, IAsyncPackage
{
    public Task<T> GetServiceAsync<T>(Type type)
    {
        throw new NotImplementedException();
    }
}
