using Microsoft.Extensions.Logging;
using VsMcpBridge.Shared.Interfaces;

namespace VsMcpBridge.Shared.Loggers;

public sealed class LogLevelSettings : ILogLevelSettings
{
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;
}
