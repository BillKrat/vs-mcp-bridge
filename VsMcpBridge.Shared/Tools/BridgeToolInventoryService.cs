using System;
using System.Linq;
using VsMcpBridge.Shared.Security;

namespace VsMcpBridge.Shared.Tools
{
    public sealed class BridgeToolInventoryService : IBridgeToolInventoryService
    {
        private readonly IBridgeToolCatalog _catalog;

        public BridgeToolInventoryService(IBridgeToolCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public BridgeToolCatalogSnapshot GetSnapshot()
        {
            var tools = _catalog.GetTools()
                .Select(ToInventoryItem)
                .OrderBy(tool => tool.Id, StringComparer.Ordinal)
                .ToArray();

            return new BridgeToolCatalogSnapshot
            {
                Tools = tools
            };
        }

        private static BridgeToolInventoryItem ToInventoryItem(BridgeToolDescriptor descriptor)
        {
            var manifest = descriptor.Manifest;
            var hostAffinity = manifest.HostAffinity ?? new BridgeToolHostAffinity();
            var execution = manifest.Execution ?? new BridgeToolExecutionCharacteristics();
            var riskProfile = manifest.RiskProfile ?? new BridgeToolRiskProfile();
            var capabilities = (manifest.RequiredCapabilities ?? Array.Empty<BridgeCapability>())
                .Where(capability => capability != null && !string.IsNullOrWhiteSpace(capability.Name))
                .Select(capability => capability.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            return new BridgeToolInventoryItem
            {
                Id = manifest.Identity.Id,
                Name = manifest.Identity.Name,
                Version = manifest.Identity.Version,
                Description = manifest.Description,
                Category = manifest.Category,
                DiscoveryKind = execution.DiscoveryKind,
                Source = execution.Source,
                HostAffinity = hostAffinity.Host,
                IsHostSpecific = hostAffinity.IsHostSpecific,
                RequiredCapabilities = capabilities,
                ApprovalRequirement = manifest.ApprovalRequirement,
                AuditCategoryHint = riskProfile.AuditCategoryHint,
                SeverityHint = riskProfile.SeverityHint,
                RiskLevelHint = riskProfile.RiskLevelHint,
                ExecutesThroughBridgeToolExecutor = execution.ExecutesThroughBridgeToolExecutor
            };
        }
    }
}
