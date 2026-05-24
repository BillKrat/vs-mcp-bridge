# ChatGPT Codex Gated Handoff Workflow

## Purpose

Design a future ChatGPT to Codex handoff workflow through `vs-mcp-bridge` that preserves the current human-gated operating style while removing manual copy and paste.

This is a future workflow design note only. It does not describe current bridge behavior and does not add runtime code, tool implementation, deployment behavior, autonomous continuation, or new mutation capability.

## Goal

The target workflow should let ChatGPT prepare a scoped Codex task, send it through `vs-mcp-bridge`, and receive a structured Codex result without bypassing the user's approval gates.

The workflow should preserve these current expectations:

- small, understandable slices
- explicit human approval before meaningful work
- no automatic continuation after a slice completes
- no destructive git actions without explicit approval
- secret redaction before prompts, logs, summaries, or audit metadata are persisted
- correlation and audit metadata for every handoff

## Proposed Flow

1. ChatGPT prepares a scoped Codex task.
2. `vs-mcp-bridge` submits the task to Codex.
3. Codex runs one approved repository slice.
4. Codex returns a structured result to `vs-mcp-bridge`.
5. ChatGPT summarizes the Codex result to the user.
6. The user chooses the next action:
   - ask questions
   - proceed
   - stop

There is no auto-continuation after Codex returns. A completed Codex run ends at a decision point for the user.

## Codex Task Shape

A future submitted task should be scoped enough that Codex can run one narrow slice without inferring broad authority.

Minimum task fields:

- objective
- repository or workspace target
- allowed files or allowed areas, when known
- explicit non-goals
- approval state for the slice
- expected validation commands
- commit/push expectation
- secret-handling constraints
- request id
- operation id
- originating conversation or handoff id

The default should be preview/propose, or one explicitly approved narrow slice. A broad task should be rejected or returned for clarification instead of being expanded silently.

## Codex Result Shape

Codex should return a structured result that ChatGPT can summarize without scraping terminal text.

Minimum result fields:

- summary
- changed files
- validation commands and results
- commit hash if pushed
- blockers or errors
- whether secrets were redacted
- request id
- operation id
- terminal status

Changed files should distinguish created, modified, deleted, and untracked files where possible. Validation results should preserve command names, exit codes, and non-secret warning or error summaries.

## User Gate

After ChatGPT receives the Codex result, the user chooses the next action:

- ask questions about the result
- proceed with another scoped slice
- stop

ChatGPT must not automatically submit a follow-up Codex task just because Codex completed successfully. A successful result is a checkpoint, not permission to continue.

## Safety Rules

Future implementation must preserve these rules:

- no destructive git actions without explicit approval
- no deploy, publish, reset, checkout, branch deletion, force push, or cleanup action as an implicit side effect
- no hidden broad repository mutation
- no mutation hidden inside search, inventory, or diagnostic tools
- no raw secrets in prompts, results, logs, summaries, diffs, or audit metadata
- no silent continuation after a returned Codex result
- no escalation from preview/propose into apply/write without a separate approval gate
- no assumption that a prior ChatGPT message grants indefinite permission

If a requested task crosses a mutation threshold, the future workflow should require explicit targets, previewable changes, approval metadata, validation expectations, and a terminal result.

## Bridge Boundary

Future implementation should fit the existing bridge tool security seams and approval boundaries.

The handoff path should preserve:

- `BridgeToolExecutor` as the execution/security boundary for executable bridge tools
- manifest metadata for any future handoff tool
- capability metadata that distinguishes preview, execution, git, deploy, and external-system actions
- policy evaluation before execution
- approval evaluation when required by the descriptor
- secret-reference handling instead of raw secret payloads
- redaction before logs and audit metadata
- structured success and failure results
- request and operation correlation
- audit classification and terminal outcome metadata

Tool-execution approval and proposal/apply approval should remain separate concepts. A ChatGPT to Codex handoff should not bypass either boundary.

## Audit And Correlation

Every handoff should preserve enough metadata to reconstruct what happened:

- originating conversation or user decision point
- request id
- operation id
- selected repo and branch
- task summary
- approval status
- Codex run status
- changed-file summary
- validation summary
- commit hash when pushed
- blocker or failure category
- redaction status

Audit metadata should be compact and structured. It should not store raw prompts containing secrets, full file bodies, unredacted terminal output, or credential values.

## Current Behavior Boundary

This document is not a claim that the workflow exists today.

Current behavior remains:

- human-managed ChatGPT/Codex coordination
- manual prompt transfer where needed
- normal Codex repository edits and validation
- no implemented ChatGPT to Codex bridge submission tool
- no autonomous continuation workflow
- no new MCP mutation or apply tool from this design note

## Deferred

Do not implement as part of this design note:

- ChatGPT submission tool
- Codex orchestration service
- background job runner
- automatic continuation
- deploy automation
- destructive git automation
- production secret broker
- persistent audit store
- UI approval prompts
- changes to MCP transport
- repository mutation tools

## Recommended First Implementation Slice

The safest future first slice is a preview-only handoff descriptor:

- accept an explicit scoped task payload
- validate required fields
- assign or preserve request and operation ids
- redact known secret-like values
- return the normalized task payload and approval requirement
- perform no Codex submission
- mutate no files
- run no shell commands

That would prove task shape, redaction, correlation, manifest metadata, capability metadata, and audit classification before any real Codex execution is wired through the bridge.
