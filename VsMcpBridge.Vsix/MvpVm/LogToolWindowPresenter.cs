using VsMcpBridge.Vsix.Logging;

namespace VsMcpBridge.Vsix.MvpVm
{
    internal class LogToolWindowPresenter(IBridgeLogger logger) : ILogToolWindowPresenter
    {
        public ILogToolWindowControl LogToolWindowControl { get; set; }
        public ILogToolWindowViewModel LogToolWindowViewModel { get; set; }

        public void Initialize()
        {
            logger.LogInformation("Initializing VS MCP Bridge tool window...");

            LogToolWindowControl.DataContext = LogToolWindowViewModel;


            logger.LogInformation("VS MCP Bridge tool window Initialized.");
        }


    }
}
