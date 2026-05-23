# Preview Document Update Validation

## Checkpoint

- branch: `main`
- starting HEAD: `2ff8d9e Design preview-only document update tool`
- working tree at start: clean and aligned with `origin/main`
- completed slice: implemented the preview-only document update MCP tool

## Implemented Behavior

- Added compiled bridge tool `bridge.previewDocumentUpdate`.
- Added MCP wrapper `bridge_preview_document_update`.
- Added manifest/capability/audit metadata:
  - capability: `workspace.previewDocumentUpdate`
  - approval requirement: `NotRequired`
  - audit category: `DocumentPreview`
- Supported explicit full-document preview inputs:
  - `targetPath`
  - `expectedContent` or `expectedContentHash`
  - `replacementContent`
  - optional wrapper `description`
- Returned structured statuses:
  - `PreviewGenerated`
  - `NoOp`
  - `DriftDetected`
  - `InvalidRequest`

## Safety Boundary

The tool is preview-only.
It reads only the explicit repo-root-relative target path, rejects absolute paths, parent traversal, and wildcards, detects drift before previewing replacement content, and returns deterministic metadata plus a unified diff.

It does not write files, apply patches, create proposals, call ChatEngine, call the VSIX named pipe, execute shell commands, scan the repository, call production endpoints, or add any apply/update capability.

Any future write-capable document update tool remains separate threshold work under `docs/mcp-controlled-mutation-threshold.md` and must require explicit approval after preview.

## Evidence

- observed trace log: `artifacts/logs/mcp-preview-document-update-trace-20260517.log`
- metadata: `artifacts/logs/mcp-preview-document-update-trace-20260517.metadata.json`
- sequence diagram: `docs/diagrams/mcp-preview-document-update-trace-20260517.mmd`
- source design: `docs/preview-only-document-update-tool-design.md`
- threshold doc: `docs/mcp-controlled-mutation-threshold.md`

## Validation

Completed:

- `dotnet test ./VsMcpBridge.Shared.Tests/VsMcpBridge.Shared.Tests.csproj`
- `dotnet build ./VsMcpBridge.McpServer/VsMcpBridge.McpServer.csproj`
- `dotnet build ./VsMcpBridge.App/VsMcpBridge.App.csproj`
- `git diff --check`

Covered by tests:

- preview generation
- deterministic unified diff
- no-op detection
- expected-content hash verification
- drift detection
- invalid path rejection
- no file mutation on success or failure
- audit classification metadata
- redaction of secret-like values in logs and audit metadata
- approval not required by default
- request and operation correlation preservation
- MCP wrapper execution through `BridgeToolExecutor`

## Resume Guidance

Start from `docs/ARCHITECTURE.md`, this handoff, and `docs/tool-execution-trace-workflow.md` before touching any mutation-adjacent MCP work.
Do not add apply/write behavior inside `bridge_preview_document_update`.
The next threshold step, if ever requested, should be a separate approval-required design and implementation slice.
