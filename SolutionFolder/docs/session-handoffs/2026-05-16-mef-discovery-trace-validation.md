# MEF Discovery Trace Validation - 2026-05-16

## Summary

The minimal MEF bridge tool discovery seam now has durable trace evidence without adding runtime behavior, tools, transport changes, proposal changes, BM25, sandboxing, hot reload, or unload support.

The trace confirms:

- compiled discovery remains the default catalog contributor
- MEF directory discovery is opt-in through `BridgeToolDiscoveryOptions`
- missing configured MEF directories are logged and do not fail startup
- invalid assembly loads are logged and discovery still completes
- compiled and MEF-discovered tools compose into `CompiledBridgeToolCatalog`
- MEF discovery does not execute the exported tool
- MEF-discovered tools still execute through `BridgeToolExecutor`
- policy, audit, and request/operation correlation remain executor-owned boundaries

## Durable Artifacts

- observed transcript: `SolutionFolder/artifacts/logs/mef-discovery-trace-20260516.log`
- run metadata: `SolutionFolder/artifacts/logs/mef-discovery-trace-20260516.metadata.json`
- Mermaid reconstruction: `SolutionFolder/docs/diagrams/mef-discovery-trace-20260516.mmd`
- workflow update: `SolutionFolder/docs/tool-execution-trace-workflow.md`

## Code Evidence

Current test coverage in `VsMcpBridge.Shared.Tests/BridgeToolInfrastructureTests.cs` includes:

- `AddBridgeToolServices_registers_catalog_and_executor`
- `Missing_mef_directory_does_not_fail_discovery_or_startup`
- `Mef_discovery_can_discover_exported_bridge_tool_when_enabled`
- `Mef_discovery_load_failures_are_logged_and_not_silent`
- `Mef_discovered_tool_still_runs_through_executor_security_and_audit_boundary`

The trace harness used the existing `MefFakeBridgeTool` export from the shared tests rather than introducing a production tool.

## Next Guidance

Keep MEF as discovery only. Any future plugin work should preserve these invariants before expanding capability:

- callers resolve tools through `IBridgeToolCatalog`
- callers execute tools through `IBridgeToolExecutor`
- policy, redaction, audit, and correlation stay inside `BridgeToolExecutor`
- directory-loaded tools are optional and are not production sandboxing
- BM25, MCP transport, proposal behavior, hot reload, and unload remain separate slices
