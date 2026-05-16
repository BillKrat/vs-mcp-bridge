namespace VsMcpBridge.McpServer.Pipe;

public static class PipeActivationDiagnostics
{
    public const string ActivationRequiredMessage =
        "VS MCP Bridge is not active. Launch the Visual Studio Experimental Instance, open View -> Other Windows -> VS MCP Bridge, ensure the tool window initializes the VSIX named-pipe side, then retry the VS-backed MCP tool.";
}
