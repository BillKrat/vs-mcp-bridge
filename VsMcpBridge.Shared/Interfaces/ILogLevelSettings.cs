using Microsoft.Extensions.Logging;

namespace VsMcpBridge.Shared.Interfaces;

public interface ILogLevelSettings
{
    LogLevel MinimumLevel { get; set; }
}
