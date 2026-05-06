using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using VsMcpBridge.Shared.Configuration;
using VsMcpBridge.Shared.Constants;
using Xunit;

namespace VsMcpBridge.Shared.Tests;

public sealed class BridgeConfigurationFactoryTests
{
    [Fact]
    public void Create_loads_environment_appsettings_and_user_settings_in_requested_order()
    {
        var basePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(basePath);

        var userSettingsPath = BridgeConfigurationFactory.GetDefaultUserSettingsFilePath();
        var userSettingsDirectory = Path.GetDirectoryName(userSettingsPath)!;
        Directory.CreateDirectory(userSettingsDirectory);

        var environmentVariableName = $"{ConfigurationKeys.VsMcpBridgeEnvironmentPrefix}VsMcpBridge__Logging__Provider";
        var originalEnvironmentValue = Environment.GetEnvironmentVariable(environmentVariableName);

        try
        {
            File.WriteAllText(Path.Combine(basePath, ConfigurationKeys.AppSettingsFileName), "{\"VsMcpBridge\":{\"Logging\":{\"Provider\":\"FromAppSettings\",\"MinimumLevel\":\"Warning\"}}}");
            File.WriteAllText(userSettingsPath, "{\"VsMcpBridge\":{\"Logging\":{\"Provider\":\"FromUserSettings\"}}}");
            Environment.SetEnvironmentVariable(environmentVariableName, "FromEnvironment");

            var configuration = BridgeConfigurationFactory.Create(basePath);

            Assert.Equal("FromUserSettings", configuration[ConfigurationKeys.LoggingProvider]);
            Assert.Equal("Warning", configuration[ConfigurationKeys.LoggingMinimumLevel]);
        }
        finally
        {
            Environment.SetEnvironmentVariable(environmentVariableName, originalEnvironmentValue);
            if (Directory.Exists(basePath))
                Directory.Delete(basePath, recursive: true);
            if (File.Exists(userSettingsPath))
                File.Delete(userSettingsPath);
        }
    }

    [Fact]
    public void AddDefaultSources_throws_for_null_builder()
    {
        Assert.Throws<ArgumentNullException>(() => BridgeConfigurationFactory.AddDefaultSources(null!, "settings.json"));
    }

    [Fact]
    public void Create_throws_for_blank_base_path()
    {
        Assert.Throws<ArgumentException>(() => BridgeConfigurationFactory.Create(" "));
    }
}
