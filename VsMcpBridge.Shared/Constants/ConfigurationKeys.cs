namespace VsMcpBridge.Shared.Constants;

public static class ConfigurationKeys
{
    public const string LoggingProvider = "VsMcpBridge:Logging:Provider";
    public const string LoggingMinimumLevel = "VsMcpBridge:Logging:MinimumLevel";

    public const string ChatEngineProvider = "Adventures:ChatEngine:Provider";

    public const string VsMcpBridgeEnvironmentPrefix = "VSMCPBRIDGE_";
    public const string VsMcpBridgeLoggingProviderEnvironmentVariable = "VSMCPBRIDGE_VsMcpBridge__Logging__Provider";
    public const string VsMcpBridgeLoggingMinimumLevelEnvironmentVariable = "VSMCPBRIDGE_VsMcpBridge__Logging__MinimumLevel";
}
