# VS MCP Bridge - AI Collaboration Guide

Last updated: 2026-04-10

## Purpose

This document defines how Bill, ChatGPT, Codex, and GitHub Copilot collaborate in this repository during the current phase of work.

Current phase:

- stabilize the existing codebase
- get the MCP bridge connected end to end
- prove the basic VSIX plus MCP workflow before expanding features

Use this file for role split, operating rules, and handoff expectations.
Use `docs/AI_HANDOFF.md` for current project state, current risks, and the ordered next steps.

## Current Working Model

- Bill sets product direction, chooses priorities, and makes final tradeoff decisions.
- ChatGPT provides architectural direction, sequencing advice, and risk review.
- Codex is the in-repo implementation partner responsible for code changes, verification, and keeping docs aligned with the current state of the repository.
- GitHub Copilot is optional in-editor assistance during active coding, but it is not the source of truth for architecture or repo state.

## Decision Rules

- Code and documentation should follow Bill's direction first.
- When architectural guidance and repository reality differ, Codex should surface the mismatch explicitly instead of silently forcing the code to fit the older plan.
- Prefer the smallest change that gets the bridge working over introducing new abstractions.
- Do not broaden scope during the MCP connection phase unless a change is required for correctness.

## Source Of Truth Order

When documents overlap, use them in this order:

1. `docs/CODING_STANDARDS.md`
2. `docs/AI_HANDOFF.md`
3. `docs/AI_COLLABORATION.md`
4. `docs/VS_MCP_BRIDGE_TECHNICAL_ANALYSIS.md`
5. `docs/MVPVM_OVERVIEW.md`
6. `README.md`

## What Goes Where

- `docs/CODING_STANDARDS.md`: implementation rules and repo-wide coding guardrails.
- `docs/AI_HANDOFF.md`: current state of the repo, verified status, immediate priorities, and short AI notes.
- `docs/AI_COLLABORATION.md`: collaboration rules and division of responsibility.
- `docs/VS_MCP_BRIDGE_TECHNICAL_ANALYSIS.md`: living technical reference and phase-level technical priorities.
- `docs/MVPVM_OVERVIEW.md`: UI pattern guidance for the tool window and shared WPF pieces.

## Standard Working Loop

1. Bill sets the immediate objective.
2. ChatGPT may refine the architecture, constraints, or sequencing for that objective.
3. Codex inspects the repository as it exists now.
4. Codex makes the smallest correct code and doc changes needed.
5. Codex verifies with builds, tests, or runtime checks that are feasible in the current environment.
6. If the result changes shared understanding, Codex updates `docs/AI_HANDOFF.md`.

## Current Near-Term Priority

The primary objective is not broad feature growth.

The primary objective is:

1. get the VSIX host loading reliably
2. get the named-pipe bridge working reliably
3. get the MCP server talking to the host end to end
4. validate the basic tool flow before adding more capabilities

## ChatGPT Guidance

ChatGPT is most useful when asked to do one of these:

- review the current architecture for MCP connection risks
- evaluate sequencing for the next 3-5 tasks
- identify the minimum safe protocol or approval-model changes needed
- review whether a proposed change preserves the VSIX/MCP separation

ChatGPT should avoid assuming that older docs describe the code perfectly. `docs/AI_HANDOFF.md` is the preferred current-state briefing.

## Codex Responsibilities

Codex should:

- inspect the code before making assumptions
- keep changes small and phase-appropriate
- verify what can be verified locally
- call out environmental limits explicitly
- update handoff docs when current state materially changes

Codex should not:

- redesign the system during a stabilization task
- silently preserve stale docs that conflict with the repo
- add new frameworks or large abstractions unless required

## Documentation Update Rule

When the current state changes in a way that affects future work, update `docs/AI_HANDOFF.md` in the same task when practical.

Examples:

- build status changed
- VSIX load status changed
- MCP connectivity status changed
- known blockers changed
- the near-term execution order changed

## Suggested Prompt For ChatGPT

Use something close to this:

> Read `docs/CODING_STANDARDS.md`, `docs/AI_HANDOFF.md`, and `docs/AI_COLLABORATION.md` first, in that order. Treat `docs/CODING_STANDARDS.md` as binding, `docs/AI_HANDOFF.md` as the current repo-state briefing, and `docs/AI_COLLABORATION.md` as the operating agreement between Bill, ChatGPT, Codex, and GitHub Copilot. We are in the MCP connection phase: prioritize getting the VSIX host, named-pipe bridge, and MCP server working end to end before expanding features. Based on the current implementation, identify the most important architectural risks, the next 3-5 engineering steps, and any minimum changes needed to keep the bridge reliable without broadening scope.
