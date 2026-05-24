# Session Handoff: Invalid Active Document Placeholder Path Fix

Status: ACTIVE

Date: 2026-05-08
Repo: `Y:\vs-mcp-bridge`
Branch: `feature/approval-apply-ui-slice`
Starting HEAD for this handoff session: `294dde6`
Purpose: preserve the validated fix for Visual Studio active-document placeholder paths such as `<no document>` so future sessions can resume from a stable state after the corrupted session and reproduce the relevant validation.

## What Was Completed

This session completed a focused defensive fix in the VSIX service layer.

Completed code changes:

- updated `VsMcpBridge.Vsix/Services/VsService.cs` to validate `ActiveDocument.FullName` before treating it as a usable file path
- added `HasUsableDocumentPath` so blank values, placeholder values wrapped in `<...>`, and values containing invalid path characters are rejected
- changed active-document retrieval to treat invalid placeholder paths as no active document rather than returning the pseudo-path downstream
- added regression tests in `VsMcpBridge.Vsix.Tests/VsServiceTests.cs` covering null, blank, placeholder, invalid-character, and normal file paths

## Why This Slice Was Needed

The resumed session surfaced a Copilot/IDE context failure with the following key error detail:

- `File path contains illegal character '<' (0x3C): '<no document>'`

That failure is consistent with Visual Studio exposing a placeholder active-document path that was then consumed as if it were a real filesystem path.

## What Was Validated

Code validation completed successfully:

- `VsMcpBridge.Vsix.Tests`: 25/25 passed
- workspace build: successful

Validation notes:

- the new tests confirmed placeholder values such as `<no document>` and `<misc files>` are rejected
- the new tests confirmed normal file paths still pass validation
- the previously reported assembly-in-use build blocker from the Experimental Instance was not reproducible at the time of validation; the current workspace build succeeded

## Important Files

- `VsMcpBridge.Vsix/Services/VsService.cs`
- `VsMcpBridge.Vsix.Tests/VsServiceTests.cs`
- `AI_START.md`
- `SolutionFolder/docs/AI_STOP.md`

## Current Known State

- the fix is implemented and validated locally
- the repository also contains additional uncommitted documentation updates from the prior session in:
  - `SolutionFolder/docs/LOGGING_DIAGNOSTIC_RUNBOOK.md`
  - `SolutionFolder/docs/session-handoffs/2026-05-07-app-host-ping-trace-handoff.md`
- those existing documentation changes appear intentional and should be preserved during closeout

## Recommended Next Slice

Smallest next useful chunk:

- manually validate the `vs_get_active_document` path inside the VSIX/IDE context when no real editor document is active to confirm the bridge now returns the normal no-document response instead of surfacing a placeholder pseudo-path

After that, continue with the previously identified MCP-to-VSIX transport artifact slice using a pipe-backed tool such as `vs_get_active_document`.

## Constraints To Preserve

- preserve the decoupled host pattern with no App↔VSIX coupling
- keep MVP/VM boundaries intact
- keep MCP stdout clean and route diagnostics through approved channels
- prefer returning normal bridge responses over leaking Visual Studio UI placeholder values downstream

## Suggested Resume Prompt

- `Read AI_START.md, then resume from SolutionFolder/docs/session-handoffs/2026-05-08-invalid-active-document-placeholder-handoff.md.`

## Working Tree Expectation

At this point, the repository contains validated code changes plus intentional documentation edits, but the working tree has not yet been committed as part of this handoff closeout.
