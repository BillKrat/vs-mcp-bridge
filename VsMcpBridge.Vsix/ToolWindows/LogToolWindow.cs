using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using VsMcpBridge.Vsix.Logging;

namespace VsMcpBridge.Vsix.ToolWindows;

/// <summary>
/// Tool window that displays MCP request logs and pending approval prompts.
/// </summary>
[Guid("b2c3d4e5-f6a7-8901-bcde-f12345678901")]
public sealed class LogToolWindow : ToolWindowPane
{
    public LogToolWindow() : base(null)
    {
        var logger = new ActivityLogBridgeLogger();
        logger.LogVerbose("Creating VS MCP Bridge tool window.");

        Caption = "VS MCP Bridge";
        Content = new LogToolWindowControl();

        logger.LogInformation("VS MCP Bridge tool window created.");
    }
}
