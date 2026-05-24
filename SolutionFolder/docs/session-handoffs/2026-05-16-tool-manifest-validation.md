# Bridge Tool Manifest Validation Handoff

## Summary

Bridge tool manifest metadata now has durable trace evidence.

The trace proves that `RegexTextSearchTool` descriptor metadata derives a lightweight `BridgeToolManifest`, flows into catalog-visible descriptor data, enters `ToolExecutionSecurityContext`, appears in executor trace logging, and is copied into `BridgeAuditEnvelope.Metadata`.

Observed run:

- run name: `tool-manifest-trace-20260516`
- branch: `main`
- baseline commit: `5e9b71f Add bridge tool manifest metadata model`
- capture date: `2026-05-20`
- primary compiled tool: `bridge.regexTextSearch`
- request id: `tool-manifest-trace-20260516-req-001`
- operation id: `tool-manifest-trace-20260516-op-001`
- result: success, 2 returned matches, 2 total matches

Durable artifacts:

- log transcript: `SolutionFolder/artifacts/logs/tool-manifest-trace-20260516.log`
- metadata: `SolutionFolder/artifacts/logs/tool-manifest-trace-20260516.metadata.json`
- diagram: `SolutionFolder/docs/diagrams/tool-manifest-trace-20260516.mmd`

## Evidence Covered

Compiled tool manifest evidence:

- `BridgeToolDescriptor.Manifest` derives identity, version, description, category, source, discovery kind, host affinity, execution characteristics, required capabilities, approval requirement, and risk hints.
- `RegexTextSearchTool` observed manifest values were `bridge.regexTextSearch`, `Regex Text Search`, `1.0.0`, `Search`, `Compiled`, `Compiled`, `Shared`, no required capabilities, `NotRequired`, and `ToolExecution/Informational/Low`.
- `ToolExecutionSecurityContext.Manifest` was observed by a recording policy with the same manifest identity, version, category, discovery kind, host affinity, required capabilities, and approval requirement.
- Executor trace logging included redacted request payload metadata and manifest metadata. The synthetic raw secret used to test redaction did not appear in durable logs.
- `BridgeAuditEnvelope.Metadata` included manifest identity/version/category/source/discovery/host, approval requirement, audit category hint, severity hint, risk hint, required capabilities, and approval decision.

Approval-context evidence:

- `RegexTextSearchTool` does not require approval, so the default compiled trace correctly skips `IToolExecutionApprovalService`.
- A temporary harness-only approval probe observed `ToolExecutionApprovalContext.Manifest` for an approval-required descriptor.
- That probe was not added to repository production tools and did not change runtime behavior.

MEF evidence:

- The trace does not change MEF behavior.
- The MEF path is documented from existing shared-test evidence: `MefFakeBridgeTool` remains a test export, and `BridgeToolInfrastructureTests.Mef_discovery_can_discover_exported_bridge_tool_when_enabled` asserts its manifest metadata when MEF directory discovery is explicitly enabled.

## Scope Guard

This validation did not add remote tools, OAuth/RBAC/user identity, persistent manifests, package publishing, signed plugin infrastructure, MEF redesign, MCP transport changes, VS command movement, or new production tools.

Future tool-manifest work should keep `BridgeToolExecutor` as the execution, policy, approval, redaction, audit, and trace boundary.
