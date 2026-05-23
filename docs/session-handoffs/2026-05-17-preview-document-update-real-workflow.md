# Preview Document Update Real Workflow Handoff

## Checkpoint

- branch: `main`
- starting HEAD: `f3ad9a2 Implement preview-only document update tool`
- starting working tree: clean and aligned with `origin/main`
- slice: real preview-only document update workflow

## What Was Previewed

The direct MCP stdio workflow previewed a documentation update to `docs/preview-only-document-update-tool-design.md`.

The proposed change inserted a short `Real Workflow Evidence` section after the implementation-status boundary. This was a useful target because the design doc previously described the preview-only contract and implementation validation, but did not yet record a real documentation workflow using the MCP preview tool before normal repo editing.

## Boundary Result

- MCP tool used: `bridge_preview_document_update`
- MCP wrote any files: no
- MCP applied any patch: no
- Codex later edited the repo normally: yes
- Apply/write MCP tool implemented or invoked: no

The MCP run used `expectedContentHash` plus full `replacementContent`.
The target SHA-256 stayed unchanged after preview, repeated preview, no-op, and drift calls:

- before: `f607d6f798beb0a294ff5682cfaaa3525d8d747f111e60f9982ad0a27ab5e2ae`
- after all MCP calls: `f607d6f798beb0a294ff5682cfaaa3525d8d747f111e60f9982ad0a27ab5e2ae`

Only after reviewing the preview result did Codex apply the accepted documentation change through normal repository editing.

## MCP Observations

- direct MCP stdio with the official MCP .NET client succeeded
- server info: `VsMcpBridge.McpServer 1.0.0.0`
- `tools/list` returned 21 tools
- `tools/list` included `bridge_preview_document_update`
- first preview returned `success=true`, `status=PreviewGenerated`, `previewOnly=true`
- repeated identical preview returned the same diff
- no-op returned `success=true`, `status=NoOp`
- wrong expected hash returned `success=false`, `status=DriftDetected`, `errorCode=DriftDetected`

## What Changed

Changed:

- `docs/preview-only-document-update-tool-design.md`
  - added a short `Real Workflow Evidence` section recording the successful no-write preview boundary

Added evidence:

- `artifacts/logs/mcp-preview-document-update-real-workflow-20260517.log`
- `artifacts/logs/mcp-preview-document-update-real-workflow-20260517.metadata.json`
- `docs/diagrams/mcp-preview-document-update-real-workflow-20260517.mmd`
- `docs/session-handoffs/2026-05-17-preview-document-update-real-workflow.md`

## Was Preview Useful

Yes. The tool proved the exact safety shape needed for this slice:

- it accepted one explicit repo-root-relative target
- it verified the expected pre-edit state by hash
- it produced a human-reviewable proposed update
- it repeated deterministically
- it identified no-op and drift cases
- it left the file unchanged until Codex performed the normal repo edit

## Friction And Gaps

- A raw ad hoc JSON-RPC Content-Length helper timed out before initialize. The official MCP client path worked and should remain the repeatable path for future direct stdio validation.
- The current diff is deterministic but not minimal. For this small insertion into a full-document replacement, `changedLineCount` was 460 because the unified diff reports the whole document as removed and re-added.
- The MCP wrapper currently generates request id and operation id internally. That preserves correlation, but caller-supplied deterministic IDs are not available through this wrapper signature.

## Validation To Run

Run before closeout:

- `git diff --check`
- `dotnet test ./VsMcpBridge.Shared.Tests/VsMcpBridge.Shared.Tests.csproj`
- `dotnet build ./VsMcpBridge.McpServer/VsMcpBridge.McpServer.csproj`

## Next Step

Keep `bridge_preview_document_update` preview-only. A future useful refinement would be minimal/range-aware diff generation for readability, still without adding any apply/write path.
