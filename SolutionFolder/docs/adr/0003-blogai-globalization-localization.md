# ADR 0003: BlogAI Globalization And Localization

## Status

Proposed

## Context

`BlogAI.Web` now exists as the first minimal Blazor Web App shell. Before UI, auth, publishing, and workflow behavior grow, the repo needs a clear globalization and localization stance.

BlogAI should avoid accidental culture assumptions in user-facing presentation, but it should not localize operational evidence or create translation infrastructure before there is real demand.

This ADR is documentation only. It does not add resource files, translate UI, change runtime code, add localization packages, localize logs or traces, or change developer documentation.

## Decision

BlogAI should be globalization-ready and localization-capable, while remaining English-first for now.

Rules:

- Use UTC internally for timestamps.
- Keep machine-readable logs, audit records, event names, trace identifiers, reason codes, and correlation fields invariant.
- Apply culture-aware formatting only at UI or API presentation boundaries.
- Keep UI text English-first until a later explicit localization implementation slice.
- Prepare Blazor UI code for future `IStringLocalizer` usage when UI text begins to grow.
- Keep `AdventuresAuth` event names invariant English, such as `AdventuresAuth.RequestReceived` and `AdventuresAuth.AccessDenied`.
- Keep developer docs, runbooks, trace artifacts, metadata, and operational evidence English and invariant.
- Treat blog content translation as a separate content strategy, not part of UI shell localization.

## Rationale

This keeps the first UI shell simple while avoiding choices that would make later localization expensive.

UTC and invariant operational evidence preserve auditability, searchability, trace comparison, and durable workflow evidence. Culture-specific presentation belongs at the edges where humans read UI or API output, not in auth decisions, audit event names, logs, or cross-system identifiers.

English-first UI text lets BlogAI keep moving while leaving room for future .NET/Blazor localization patterns when the product surface justifies them.

## Consequences

Positive consequences:

- UI work can continue without premature resource files or translation process.
- Future localization remains possible without changing the core architecture.
- Logs, audit events, traces, and evidence stay deterministic and searchable.
- Auth/API and MCP-adjacent evidence remains compatible with current redaction, correlation, and audit conventions.

Tradeoffs:

- Initial UI remains English-only.
- Developers need to avoid embedding culture-specific date, time, number, and currency assumptions in components.
- A later implementation slice will still need to introduce resource files, culture selection, and localization testing if localization becomes a real requirement.

## Non-Goals

This ADR does not approve:

- adding `.resx` files or other resource files
- translating current UI text
- changing runtime localization configuration
- adding localization packages beyond framework defaults
- localizing logs, audit records, event names, traces, metadata, reason codes, or correlation fields
- localizing developer docs, runbooks, handoffs, or diagnostic artifacts
- translating blog content
- implementing language selection UI
- creating a content translation workflow
- changing deployment, routing, auth, API, or persistence behavior

## Deferred Decisions

Deferred until a later explicit implementation slice:

- supported cultures
- default request culture behavior
- culture selection UI
- resource file structure
- localization test strategy
- date, time, number, and currency formatting conventions for specific BlogAI screens
- whether API responses should include display-ready localized strings or invariant codes plus UI formatting
- blog content translation workflow and ownership
- SEO and route strategy for localized public content

## Future Implementation Direction

When localization becomes necessary:

- use standard .NET and Blazor localization patterns
- introduce `IStringLocalizer` at service or UI presentation boundaries
- keep Razor components thin and avoid embedding business or auth logic in localization decisions
- keep invariant event names and reason codes in `AdventuresAuth`, BlogAI audit evidence, and MCP trace artifacts
- format dates, numbers, and culture-sensitive values at presentation boundaries
- keep operational evidence English and invariant for repeatable diagnosis
