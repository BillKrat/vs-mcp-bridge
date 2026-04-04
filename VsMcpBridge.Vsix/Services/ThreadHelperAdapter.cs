using Microsoft.VisualStudio.Shell;
using System;
using System.Threading.Tasks;
using VsMcpBridge.Shared.Interfaces;

namespace VsMcpBridge.Vsix.Services;

internal sealed class ThreadHelperAdapter : IThreadHelper
{
    public bool CheckAccess() => ThreadHelper.CheckAccess();

    public void Run(Func<Task> value) => ThreadHelper.JoinableTaskFactory.Run(value);

    public async Task SwitchToMainThreadAsync() => await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

    public void ThrowIfNotOnUIThread() => ThreadHelper.ThrowIfNotOnUIThread();
}
