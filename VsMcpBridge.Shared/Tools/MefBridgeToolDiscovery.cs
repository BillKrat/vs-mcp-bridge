using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Composition.Hosting;

namespace VsMcpBridge.Shared.Tools
{
    public sealed class MefBridgeToolDiscovery : IBridgeToolDiscovery
    {
        private readonly BridgeToolDiscoveryOptions _options;
        private readonly ILogger _logger;

        public MefBridgeToolDiscovery(BridgeToolDiscoveryOptions options, ILogger logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IReadOnlyList<IBridgeTool> DiscoverTools()
        {
            if (!_options.EnableMefDirectoryDiscovery)
                return Array.Empty<IBridgeTool>();

            var directories = _options.MefDirectories.Where(directory => !string.IsNullOrWhiteSpace(directory)).ToArray();
            _logger.LogInformation(
                $"MEF bridge tool discovery started [Enabled={_options.EnableMefDirectoryDiscovery}] [DirectoryCount={directories.Length}] [SearchPattern={_options.MefSearchPattern}].");

            var assemblies = new List<Assembly>();
            foreach (var directory in directories)
            {
                if (!Directory.Exists(directory))
                {
                    _logger.LogWarning($"MEF bridge tool discovery directory missing [Directory={directory}].");
                    continue;
                }

                foreach (var assemblyPath in Directory.EnumerateFiles(directory, _options.MefSearchPattern))
                {
                    try
                    {
                        assemblies.Add(Assembly.LoadFrom(assemblyPath));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            new InvalidOperationException(ex.Message),
                            $"MEF bridge tool discovery failed to load assembly [AssemblyPath={assemblyPath}].");
                    }
                }
            }

            if (assemblies.Count == 0)
            {
                _logger.LogInformation("MEF bridge tool discovery completed [Enabled=True] [AssemblyCount=0] [ToolCount=0].");
                return Array.Empty<IBridgeTool>();
            }

            try
            {
                using (var container = new ContainerConfiguration()
                           .WithAssemblies(assemblies)
                           .CreateContainer())
                {
                    var tools = container.GetExports<IBridgeTool>().ToArray();
                    _logger.LogInformation(
                        $"MEF bridge tool discovery completed [Enabled=True] [AssemblyCount={assemblies.Count}] [ToolCount={tools.Length}].");
                    return tools;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    new InvalidOperationException(ex.Message),
                    $"MEF bridge tool discovery failed [AssemblyCount={assemblies.Count}].");
                _logger.LogInformation(
                    $"MEF bridge tool discovery completed [Enabled=True] [AssemblyCount={assemblies.Count}] [ToolCount=0].");
                return Array.Empty<IBridgeTool>();
            }
        }
    }
}
