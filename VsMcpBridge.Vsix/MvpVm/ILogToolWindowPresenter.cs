using System;

namespace VsMcpBridge.Vsix.MvpVm
{
    internal interface ILogToolWindowPresenter
    {
        ILogToolWindowControl LogToolWindowControl { get; set; }
        ILogToolWindowViewModel LogToolWindowViewModel { get; set; }

        void Initialize();
        void AppendLog(string message);
        void ShowApprovalPrompt(string description, Action onApprove, Action onReject);
    }
}
