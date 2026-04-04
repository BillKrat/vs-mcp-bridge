using System;
using System.Threading.Tasks;

namespace VsMcpBridge.Shared.Interfaces
{
    public interface IThreadHelper
    {
        bool CheckAccess();
        void Run(Func<Task> value);
        Task SwitchToMainThreadAsync();
        void ThrowIfNotOnUIThread();
    }
}
