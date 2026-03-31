namespace VsMcpBridge.Vsix.MvpVm
{
    internal interface ILogToolWindowPresenter
    {
        public ILogToolWindowControl LogToolWindowControl { get; set; }
        public ILogToolWindowViewModel LogToolWindowViewModel { get; set; }

        void Initialize();
    }
}
