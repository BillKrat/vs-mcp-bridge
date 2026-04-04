using Microsoft.VisualStudio.Shell;
using System;
using System.Threading.Tasks;
using VsMcpBridge.Shared.Interfaces;

namespace VsMcpBridge.Vsix.Tests
{
    internal class TestThreadHelper : IThreadHelper
    {
        public bool CheckAccess()
        {
            return ThreadHelper.CheckAccess();
        }

        public void Run(Func<Task> value)
        {
            ThreadHelper.JoinableTaskFactory.Run(value);
        }

        public Task SwitchToMainThreadAsync()
        {
            throw new NotImplementedException();
        }

        public void ThrowIfNotOnUIThread()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
        }
    }
}