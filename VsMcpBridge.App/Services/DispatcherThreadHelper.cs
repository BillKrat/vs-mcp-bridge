using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using VsMcpBridge.Shared.Interfaces;

namespace VsMcpBridge.App.Services;

internal sealed class DispatcherThreadHelper : IThreadHelper
{
    private Dispatcher Dispatcher => Application.Current.Dispatcher;

    public bool CheckAccess() => Dispatcher.CheckAccess();

    public void ThrowIfNotOnUIThread()
    {
        if (!Dispatcher.CheckAccess())
            throw new InvalidOperationException("Must be called on the UI thread.");
    }

    /// <summary>
    /// Runs <paramref name="value"/> on the UI thread and blocks the calling thread
    /// until it completes. If already on the UI thread, runs inline.
    /// Mirrors <c>ThreadHelper.JoinableTaskFactory.Run</c>.
    /// </summary>
    public void Run(Func<Task> value)
    {
        if (Dispatcher.CheckAccess())
        {
            value().GetAwaiter().GetResult();
            return;
        }

        Dispatcher.Invoke(() => value().GetAwaiter().GetResult());
    }

    /// <summary>
    /// Returns a completed task when already on the UI thread; otherwise yields
    /// back to the UI thread via the Dispatcher.
    /// Mirrors <c>ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync</c>.
    /// </summary>
    public Task SwitchToMainThreadAsync()
    {
        if (Dispatcher.CheckAccess())
            return Task.CompletedTask;

        return Dispatcher.InvokeAsync(() => { }).Task;
    }
}
