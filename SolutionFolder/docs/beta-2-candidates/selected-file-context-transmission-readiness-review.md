# Selected File Context Transmission Readiness Review

## Purpose

Evaluate whether selected-file context transmission is ready to become a Beta 2 planning candidate.

This is a readiness review only. It does not approve implementation, change VSIX behavior, change MCP behavior, alter prompt construction, or create a Beta 2 backlog item.

## Source Observation

Primary observation:

- `SolutionFolder/docs/observations/2026-06-13-selected-file-model-transmission-check.md`

Current capability boundary:

- `SolutionFolder/docs/current-bridge-capabilities.md`

Planning gate:

- `SolutionFolder/docs/beta-2-observation-gate.md`

The observed behavior is that selected files participate in proposal/edit workflows but do not currently reach normal chat-model context. In normal VSIX chat requests, the model receives the typed prompt only as `messages[0].content`.

## Candidate Question

Should selected files be intentionally transmitted to model chat context in a future bridge workflow?

## Current Status

Status: not ready for implementation.

The observation is valid and useful, but it is currently a single concrete observation. The Beta 2 observation gate requires enough operational evidence to show recurring, costly, safety-relevant, or user-valuable friction before creating Beta 2 scope.

## Evidence Available

Available evidence:

- selected files are loaded into proposal/edit state
- selected file paths are tracked in proposal UI state
- selected file contents are not appended to normal chat requests
- selected file paths are not sent to the model in normal chat requests
- prompt length matched the logged `MessageLength`, indicating typed prompt only
- the model response asked the user to paste the file, consistent with no selected-file context being supplied

## Evidence Still Needed

Before implementation planning, gather evidence from repeated real workflows:

- how often users expect selected files to be model context
- whether missing selected-file context causes repeated workflow friction
- whether users need full-file context, selected ranges, summaries, or explicit attachments
- whether proposal/edit state and chat context should remain separate
- whether selected files should be sent automatically or only after explicit confirmation
- what size limits are acceptable
- what redaction or secret-screening behavior is required
- how request logs should record selected-file context without leaking contents
- whether external workspaces such as `Y:\BlogAI` need different safeguards than files inside the bridge repo

## Safety And Design Questions

Any future design must answer:

- What user action explicitly authorizes selected-file transmission?
- Are file paths, full contents, selected ranges, or summaries transmitted?
- How are large files bounded?
- How are secrets, credentials, tokens, generated files, binaries, and local-only files excluded or redacted?
- How does the UI show exactly what will be sent?
- How does the request log distinguish prompt text from selected-file context?
- Does the behavior apply to VSIX chat only, MCP tools only, or both?
- Does this interact with proposal/edit workflows, or remain a separate chat-context feature?
- How does cancellation, failure, and retry preserve user expectations?

## Not In Scope For This Review

This review does not propose:

- autonomous execution
- automatic Codex execution
- autonomous mutation
- automatic deployment
- hidden filesystem crawling
- implicit broad workspace context
- background agents
- automatic repository edits
- automatic BlogEngine.NET changes

## Preliminary Readiness Assessment

Selected-file context transmission is a plausible future candidate, but not yet implementation-ready.

The smallest responsible next step is more observation, not prompt construction changes. Use additional real BlogAI and bridge workflows to determine whether this is repeated friction or a one-time expectation mismatch.

## Required Gate Before Planning

Before this candidate can become planned Beta 2 work, require:

- at least two similar observations, or one high-impact safety/user-value issue
- durable evidence paths for the observed friction
- classification under the Beta 2 observation gate
- a design note defining explicit user consent, context shape, size limits, redaction, logging, and failure behavior
- confirmation that the change does not blur proposal/edit state with chat context unintentionally

## Recommended Next Slice

Continue recording selected-file context observations during real bridge usage.

If the friction repeats, create a design-readiness document that compares at least these options:

- no change; document current behavior more visibly
- explicit "attach selected files to chat" action
- selected ranges only
- file summaries only
- MCP explicit-document handoff only
- proposal-state-only behavior
