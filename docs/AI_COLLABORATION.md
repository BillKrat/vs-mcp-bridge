# VS MCP Bridge - AI Collaboration Guide

## Purpose

This document defines how Bill, ChatGPT, Codex, and GitHub Copilot should collaborate in this repository without stepping on each other.

Use it for working agreements and routing. Use `docs/AI_HANDOFF.md` for current project state and dated AI notes.

## Role Split

- Bill: product direction, domain context, final decisions, and approval of changes to human-owned sections.
- ChatGPT: architecture review, risk analysis, decomposition, and design direction.
- Codex: repository implementation, documentation updates, code changes, verification, and keeping handoff state current in the Codex-owned section.
- GitHub Copilot: in-editor pair-programming help during active coding sessions with Bill.

## Source Of Truth Order

When these documents overlap, read them in this order:

1. `docs/CODING_STANDARDS.md`
2. `docs/AI_HANDOFF.md`
3. `docs/AI_COLLABORATION.md`
4. `docs/MVPVM_OVERVIEW.md`
5. `docs/VS_MCP_BRIDGE_TECHNICAL_ANALYSIS.md`
6. `README.md`

## What Goes Where

- `docs/CODING_STANDARDS.md`: coding rules, guardrails, and repo-wide implementation patterns.
- `docs/AI_HANDOFF.md`: current architecture summary, current risks, current test status, and append-only AI notes.
- `docs/AI_COLLABORATION.md`: who does what, how to hand off, and how to keep AI contributions from colliding.

## Non-Stomp Rules

- Each AI owns exactly one section in `docs/AI_HANDOFF.md`.
- AI sections are append-only. Add the newest dated entry at the top of your own section.
- Do not edit another AI's section, even to "fix" it.
- If another section looks stale or incorrect, record that in your own section.
- If Bill-owned top-level sections need to change, propose that in your own section under `### Proposed Update`.
- If coding standards should change, propose that in your own section under `### Proposed Standards Addition`.

## Standard Working Loop

1. Read `docs/CODING_STANDARDS.md` before proposing or writing code.
2. Read `docs/AI_HANDOFF.md` for the latest state and recent AI observations.
3. Read any architecture docs relevant to the task.
4. Make the smallest correct code and documentation changes needed.
5. If the change affects shared understanding, add a dated note to your own `docs/AI_HANDOFF.md` section.

## ChatGPT Ramp-Up

When ChatGPT joins a new session, the expected behavior is:

1. Read `docs/CODING_STANDARDS.md`.
2. Read `docs/AI_HANDOFF.md`.
3. Read this file.
4. Read the architecture docs and the specific code files named in `docs/AI_HANDOFF.md`.
5. Reply with:
   - confirmed understanding of current architecture
   - top risks or design smells
   - recommended next steps
   - any proposed updates for Bill-owned sections, without directly rewriting AI-owned sections

## Suggested Prompt For ChatGPT

Use this when you want ChatGPT aligned quickly:

> Read `docs/CODING_STANDARDS.md`, `docs/AI_HANDOFF.md`, and `docs/AI_COLLABORATION.md` first, in that order. Treat `docs/CODING_STANDARDS.md` as binding, `docs/AI_HANDOFF.md` as the latest shared state, and `docs/AI_COLLABORATION.md` as the operating agreement between Bill, ChatGPT, Codex, and GitHub Copilot. Then review the architecture and current implementation with emphasis on safe edit application, host-boundary design, observability, and keeping shared code host-agnostic. Call out concrete risks, recommend the next 3-5 engineering steps, and if top-level docs should change, propose those updates explicitly instead of silently rewriting AI-owned sections.
