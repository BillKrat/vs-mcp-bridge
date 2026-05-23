# BlogAI Functional Pressure-Test Plan

## Purpose

Use BlogAI as the first real operational workload for the MCP/tool platform.

The goal is to pressure-test the current bridge through practical BlogAI development and publishing-review work, not to expand infrastructure ahead of proven need. Real usage should validate tool execution, tool inventory, search, diagnostics, trace artifacts, approval/security seams, and durable handoff practices under workflows that matter.

This plan is guidance for the next development phase only. It adds no runtime behavior, authentication system, API service, deployment path, package, project, or BlogEngine.NET change.

## Initial Target Shape

The near-term shape is intentionally conservative:

- `https://www.global-webnet.com` remains the stable legacy BlogEngine.NET site.
- BlogAI may grow beside it under `/blogAi` as the practical application surface matures.
- `https://api.global-webnet.com` remains future auth/API boundary thinking only.
- Blazor Web App is the preferred initial BlogAI UI framework when a UI slice is explicitly approved.
- The first minimal Blazor shell is `BlogAI.Web`; it is local/dev only and contains no production auth, deployment configuration, persistence, or BlogEngine.NET runtime coupling.
- The first shell validation is captured in `docs/session-handoffs/2026-05-17-blogai-blazor-shell-validation.md`.
- BlogAI should remain English-first while being globalization-ready; operational logs, audit events, traces, and developer docs stay invariant.

Do not treat that target shape as approval to implement routing, hosting, production APIs, OAuth/OpenID, deployment automation, or BlogEngine.NET migration work. The platform should first prove that it can support real BlogAI workflows with observable, repeatable tooling.

## First Practical Workflows

Start with workflows that can use the current MCP/tool platform and produce useful evidence:

1. Inspect BlogAI source and docs.
   - Use the current repository and local BlogAI checkout as source material.
   - Identify the smallest real task that needs code, content, route, cache, or publishing-review understanding.
   - Keep conclusions in repo-backed docs or session handoffs, not chat-only notes.

2. Use bridge search tools for code and content triage.
   - Call the bridge tool inventory early when MCP is reachable so the available tool surface is explicit.
   - Use compiled search tools such as regex and BM25 text search for targeted code/content discovery.
   - Prefer concrete BlogAI questions over synthetic examples, such as where content is loaded, where cache behavior is controlled, or which docs define a publishing-review path.

3. Capture trace artifacts for meaningful workflows.
   - Preserve request and operation correlation when a workflow crosses the tool execution boundary.
   - Capture durable logs and diagrams for workflows that reveal platform behavior.
   - Keep workflows diagrammable from observed evidence, following the existing anti-black-box standard.

4. Identify missing tools from real usage.
   - Record gaps only after a real BlogAI task exposes them.
   - Distinguish missing platform tools from missing BlogAI application features.
   - Avoid turning tool gaps into immediate package, deployment, auth, or plugin-system work unless a later explicit design slice approves that scope.

5. Preserve findings in durable handoffs.
   - Add or update focused handoffs under `docs/session-handoffs/` when a future session needs a clean resume point.
   - Link trace artifacts, diagrams, commands, and validation status.
   - Keep `docs/ARCHITECTURE.md` as the system-behavior source of truth when platform behavior actually changes.

## Deferred Scope

The first pressure-test phase explicitly defers:

- authentication implementation
- OAuth/OpenID
- real API deployment
- `api.global-webnet.com` services
- BlogEngine.NET migration
- BlogEngine.NET code changes
- production publishing automation
- package extraction
- new projects or packages

These topics can remain architectural vocabulary and future boundary thinking, but they should not be implemented inside the pressure-test phase.

## Success Criteria

The phase is successful when:

- BlogAI work can be started using the current MCP/tool platform.
- Missing platform gaps are identified from real BlogAI workflows.
- Tool inventory, search, diagnostics, trace artifacts, and approval/security seams have been exercised with operationally meaningful tasks.
- No speculative infrastructure is added.
- No BlogAI auth, API deployment, BlogEngine.NET migration, production publishing automation, or package extraction work is introduced prematurely.
- Meaningful workflows remain observable, reproducible, and diagrammable from durable evidence.

## Next Development Guidance

Begin with one narrow BlogAI task that needs source or content understanding. Use current tools first, capture what happened, and let the observed workflow decide which platform gap is worth addressing next.
