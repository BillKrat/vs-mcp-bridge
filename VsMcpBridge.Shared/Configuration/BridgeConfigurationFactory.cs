using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using VsMcpBridge.Shared.Constants;

namespace VsMcpBridge.Shared.Configuration;

public static class BridgeConfigurationFactory
{
    public static IConfigurationRoot Create(string applicationBasePath)
    {
        if (string.IsNullOrWhiteSpace(applicationBasePath))
            throw new ArgumentException("Application base path is required.", nameof(applicationBasePath));

        var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(applicationBasePath);

        AddDefaultSources(configurationBuilder, GetDefaultUserSettingsFilePath());
        return configurationBuilder.Build();
    }

    public static void AddDefaultSources(IConfigurationBuilder configurationBuilder, string userSettingsFilePath)
    {
        if (configurationBuilder == null)
            throw new ArgumentNullException(nameof(configurationBuilder));

        configurationBuilder
            .AddEnvironmentVariables()
            .AddEnvironmentVariables(prefix: ConfigurationKeys.VsMcpBridgeEnvironmentPrefix)
            .AddJsonFile(ConfigurationKeys.AppSettingsFileName, optional: true, reloadOnChange: true)
            .AddJsonFile(userSettingsFilePath, optional: true, reloadOnChange: true);
    }

    public static string GetDefaultUserSettingsFilePath()
    {
        var localApplicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localApplicationDataPath, BridgeRuntimeConstants.PipeName, ConfigurationKeys.UserSettingsFileName);
    }
}
