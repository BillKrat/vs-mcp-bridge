using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;
using VsMcpBridge.Shared.Interfaces;

namespace VsMcpBridge.Shared.Loggers
{
    public class AppDataFolderLogger : LoggerBase
    {
        private static readonly object LogSync = new();
        private static readonly ILogLevelSettings TraceSettings = new LogLevelSettings
        {
            MinimumLevel = LogLevel.Trace
        };

        public AppDataFolderLogger() : base(TraceSettings)
        {
        }

        protected override void LogMessage(LogLevel level, string source, string message, Exception? exception = null)
        {
            var line = $"[{DateTime.Now:HH:mm:ss}] [{level}] [{source}] {message}";

            // TODO: This following magic strings are tightly coupled to the VsMcpBridge.Server
            // implementation and should be made more robust if we add more components that need logging.

            try
            {
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var logDirectory = Path.Combine(localAppData, "VsMcpBridge", "Logs", "McpServer");
                var logPath = Path.Combine(logDirectory, "pipe-client.log");

                lock (LogSync)
                {
                    Directory.CreateDirectory(logDirectory);
                    File.AppendAllText(logPath, line + Environment.NewLine, Encoding.UTF8);
                    if (exception is not null)
                    {
                        File.AppendAllText(logPath, exception + Environment.NewLine, Encoding.UTF8);
                    }
                }
            }
            catch
            {
                // Never let diagnostics interfere with MCP stdio or request flow.
            }
        }
    }
}
