# Preview-Only Document Update Tool Design

## Purpose

Design the first future MCP mutation-adjacent tool without introducing write capability.

Suggested tool name:

- `bridge_preview_document_update`

The tool would compute a deterministic proposed document update and return a human-reviewable preview.
It would not write files, apply patches, mutate repository state, change MCP transport, or change approval behavior.

## Scope

This is design guidance only.
It does not implement the tool.

The preview tool exists to prove the safety shape before any MCP apply/write tool exists:

- explicit target
- explicit expected state
- explicit proposed update
- deterministic diff
- structured no-op and drift handling
- redacted logs/audit metadata
- request and operation correlation

## Proposed Inputs

The tool should require explicit caller input:

| Input | Required | Purpose |
| --- | --- | --- |
| `targetPath` | yes | Repo-root-relative path for the file being previewed. |
| `expectedContent` | preferred | Exact content the caller expects before the update. |
| `expectedContentHash` | optional alternative | Hash of expected content when sending full original content is not practical. |
| `replacementContent` | one update mode | Complete proposed replacement content. |
| `rangeStart` / `rangeEnd` plus `replacementText` | optional future mode | Bounded range replacement once the full-file path is proven. |
| `description` | optional | Human-readable purpose for the preview. |
| `requestId` / `operationId` | optional caller supplied, otherwise generated | Correlation. |

Initial design should prefer full-document preview before range/patch preview.
Patch execution should remain deferred.

## Required Behavior

`bridge_preview_document_update` should:

- validate that `targetPath` is repo-root-relative
- reject absolute paths and parent traversal
- reject broad selection patterns or globs
- read only the explicit target path
- require `expectedContent` or `expectedContentHash` where practical
- compare expected state against current file state
- return drift/mismatch when current content does not match expected state
- compute before/after preview without writing
- produce a deterministic unified diff
- detect no-op changes
- return structured success/failure
- preserve request id and operation id
- log/audit as preview-only
- redact secret-like content from logs and audit metadata
- avoid storing raw document bodies in durable logs by default

The preview must be deterministic for the same input and repository state.

## Structured Result Shape

Recommended result fields:

```json
{
  "success": true,
  "toolId": "bridge.previewDocumentUpdate",
  "requestId": "...",
  "operationId": "...",
  "previewOnly": true,
  "targetPath": "docs/example.md",
  "targetExists": true,
  "expectedMatched": true,
  "noOp": false,
  "changedLineCount": 4,
  "diff": "...",
  "auditCategory": "DocumentPreview",
  "approvalRequiredForApply": true,
  "message": "Preview generated. No files were written."
}
```

Failure results should be structured, not exception text dumps.

Expected failure categories:

- `InvalidTargetPath`
- `TargetNotFound`
- `ExpectedContentRequired`
- `ExpectedContentMismatch`
- `NoOp`
- `DiffGenerationFailed`
- `PreviewFailed`

## Safety Boundaries

The preview tool must not:

- write files
- apply patches
- create files
- delete files
- rename or move files
- format documents
- execute shell commands
- call production endpoints
- crawl directories
- accept globs or broad file selection
- retry hidden alternate targets
- infer approval from prior search results
- include secrets in logs, audit metadata, or durable artifacts

The preview result must be human-reviewable and small enough for operator inspection.

## Existing Seams To Use

Future implementation should route through existing bridge tool seams:

- `BridgeToolManifest` for stable identity and descriptor metadata
- required capability metadata, for example `workspace.previewDocumentUpdate`
- `BridgeToolExecutor` for execution boundary logging, policy, redaction, audit, and correlation
- `ISecurityRedactor` before payload-oriented logs or audit metadata
- `IAuditSink` with preview-only audit classification
- `BridgeToolResult` or an equivalent structured result envelope

Approval requirement for preview-only behavior can default to not required, because no state changes occur.
The result must still state that any future apply tool requires approval.

A future apply tool must be separate and approval-required.
Preview generation must not be able to silently escalate into apply.

## Audit Classification

Recommended preview audit metadata:

- category: `DocumentPreview`
- severity: informational
- risk: low or medium depending on whether content snippets are included
- outcome: success, no-op, drift, invalid-target, failure
- target path, redacted as needed
- expected-state mode: full content or hash
- changed line count
- preview-only flag

Raw full document bodies should not be stored in audit metadata.
Diffs should be redacted before durable logs or audit sinks receive them.

## Test Strategy For Future Implementation

Future implementation should include tests for:

- deterministic unified diff output
- no-op result when replacement equals current content
- drift detection when current content differs from expected content/hash
- invalid path rejection for absolute paths, parent traversal, and globs
- missing file structured failure
- no file mutation on success or failure
- no apply path reachable from the preview tool
- request id and operation id preservation
- policy/audit/redaction boundary exercise through `BridgeToolExecutor`
- secret-like payload redaction in logs/audit metadata
- deterministic target metadata in the result

Tests should assert file contents before and after every preview call.

## Future Apply Boundary

`bridge_apply_document_update` remains deferred.

Before an apply tool exists, it must require:

- a prior preview result or equivalent explicit diff
- explicit target path
- exact expected current content or hash
- approval-required manifest metadata
- approval decision after preview
- all-or-nothing write behavior where practical
- rollback/recovery guidance
- durable evidence for meaningful mutations

Apply must not be hidden inside preview.

## Deferred Follow-Up

Deferred until after preview-only behavior is designed, implemented, and validated:

- `bridge_apply_document_update`
- approval-required writes
- rollback/recovery implementation
- patch bundles
- multi-file previews
- range-based previews
- VS editor integration
- evidence header auto-annotation
- production publishing automation

## Relationship To Current Tools

Current MCP tools remain analysis/search/diagnostic tools:

- `bridge_get_tool_inventory`
- `bridge_select_repo_documents`
- `bridge_regex_text_search`
- `bridge_bm25_text_search`

They must not grow hidden mutation behavior.

`bridge_preview_document_update` would be mutation-adjacent because it reads an explicit target and computes a proposed diff, but it remains below the mutation threshold because it cannot write.
The mutation threshold in `docs/mcp-controlled-mutation-threshold.md` still governs any future apply/write tool.
