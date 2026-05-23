# BlogAI Local Auth UI Integration Design

## Purpose

Design how the `BlogAI.Web` Blazor shell should display and consume local/development `AdventuresAuth` state before any UI integration is implemented.

This design initially did not add UI components, implement auth UI, call the API from Blazor, add production auth, add OAuth/OpenID/RBAC, add persistence, change deployment, or couple to BlogEngine.NET.

The first local/dev implementation now adds a UI-facing service in `BlogAI.Web/Auth` and updates `/local-dev` to render display-safe decision metadata. It still does not add production auth, real login UX, OAuth/OpenID/RBAC, persistence, deployment changes, raw secret display, or BlogEngine.NET coupling.

## Current State

- `BlogAI.Web` serves `/` and `/local-dev`.
- `Adventures.Auth.LocalApi` exists as a local/dev Minimal API skeleton.
- The in-process BlogAI auth consumer boundary exists behind `IBlogAiAuthConsumerService`.
- Blazor components must remain thin and presentation-focused.
- Auth decisions belong in services/interfaces, not Razor components.

## Intended UI Behavior

The first local/dev auth-aware UI should show state, not become the identity system.

The UI may display:

- local/development authentication state, such as unauthenticated, allowed, denied, or unknown
- protected-route placeholder state
- allow/deny decision metadata only
- reason code or outcome when safe
- `CorrelationId`, `RequestId`, and `AuthDecisionId` where useful for diagnostics
- non-sensitive local principal placeholder values only if already redacted and intentionally exposed by the service boundary

The UI must not display:

- raw token values
- raw secrets
- passwords
- cookies
- authorization headers
- credential-bearing request or response bodies
- production identity provider details

## Service Boundary

Blazor components should call a UI-facing service/interface.

Conceptual flow:

1. A Razor component renders `/local-dev` or a future protected placeholder.
2. The component asks a UI-facing service for the local auth display model.
3. The UI-facing service calls either:
   - the in-process BlogAI auth consumer boundary, such as `IBlogAiAuthConsumerService`, or
   - a future local API client for `Adventures.Auth.LocalApi`.
4. The BlogAI auth consumer boundary or API client obtains an `AdventuresAuth` decision.
5. The UI-facing service maps that decision into a display-safe view model.
6. The component renders only the display-safe view model.

Boundary rules:

- Razor components do not evaluate credentials.
- Razor components do not decide authenticated versus unauthenticated.
- Razor components do not parse raw tokens, cookies, or authorization headers.
- Razor components do not call BlogEngine.NET auth.
- UI-facing services may map decision metadata into display models, but auth decisions remain in `AdventuresAuth` or the BlogAI auth consumer boundary.
- Logs and diagnostic UI must use correlation and decision metadata, not raw secret material.

## Conceptual Pages And Components

Initial conceptual surface:

- `/local-dev`
- local auth status panel
- protected content placeholder
- diagnostic correlation display

The local auth status panel may show:

- current local auth state
- outcome or reason code
- client application
- environment
- correlation/request/decision identifiers

The protected content placeholder may show:

- allowed placeholder content when the service boundary returns allowed
- denied placeholder state when unauthenticated or invalid
- no sensitive denial detail

The diagnostic correlation display may show:

- `CorrelationId`
- `RequestId`
- `AuthDecisionId`

## Local Validation Scenarios

Future implementation should validate:

- unauthenticated state is shown safely
- development-authenticated state is shown safely
- invalid local session is denied
- protected placeholder content is hidden or denied when unauthenticated
- protected placeholder content is allowed only when the service boundary returns allowed
- correlation id is visible when useful for diagnostics
- request id is preserved or generated
- auth decision id is visible without exposing secrets
- no raw token, secret, cookie, authorization header, password, or credential-bearing body appears in UI, logs, traces, or artifacts
- no BlogEngine.NET dependency is introduced
- no production auth behavior is introduced
- no localization resources are introduced unless a future localization slice approves them

## Non-Goals

Explicitly out of scope:

- production login UI
- password forms
- OAuth/OpenID flows
- RBAC or roles UI
- account management
- persistent session UX
- real cookie/session topology
- production identity provider integration
- deployment integration
- database-backed identity
- BlogEngine.NET auth bridge
- BlogEngine.NET runtime modification
- localization resources
- blog content translation

## Future Implementation Direction

The smallest future implementation slice should add a UI-facing service contract and a display-safe local auth state model before expanding Razor components.

Suggested conceptual names:

- `IBlogAiLocalAuthStatusService`
- `BlogAiLocalAuthStatus`
- `BlogAiLocalAuthDisplayModel`

The first implementation should remain local/dev only and should include build validation, focused tests where practical, and trace artifacts showing allow/deny/correlation/redaction behavior at the UI boundary.

## Initial Implementation

The first local/dev implementation uses:

- `IBlogAiLocalAuthStatusService`
- `BlogAiLocalAuthStatusService`
- `BlogAiLocalAuthStatus`
- `BlogAiLocalAuthDecisionDisplay`

The service calls `IBlogAiAuthConsumerService` and maps the result into display-safe state for `/local-dev`. The Razor page renders the model and does not evaluate credentials, parse tokens, or make auth decisions.

The route-level protected placeholder behavior is also service-driven: the display model marks the placeholder as `Shown` only when the BlogAI auth consumer decision is allowed, and `Hidden` when the decision is denied. The Razor page only renders that display-safe state and never sees the local development credential, raw tokens, headers, cookies, passwords, or secret-bearing request material.

Durable validation evidence for the initial local/dev UI display is captured in `docs/session-handoffs/2026-05-17-blogai-local-auth-ui-validation.md`.

Durable validation evidence for the route-level protected placeholder behavior is captured in `docs/session-handoffs/2026-05-17-blogai-route-protected-placeholder-validation.md`.

## Local API Client Boundary

`BlogAI.Web/Auth` now includes `IBlogAiLocalAuthApiClient` and `BlogAiLocalAuthApiClient` as a local/dev-only transport boundary for `Adventures.Auth.LocalApi`.

This client is injectable and can call the local `/auth/login`, `/auth/logout`, `/auth/me`, and `/auth/validate` endpoints. It uses display-safe response models and does not add production configuration, auth middleware, cookies/session topology, OAuth/OpenID/RBAC, persistence, real login UI, or BlogEngine.NET coupling.

`/local-dev` still uses the in-process `IBlogAiAuthConsumerService` path by default.

An explicit local/dev diagnostic selector can exercise the API client path with `/local-dev?authPath=api-client`. This mode is labeled as API-client diagnostic output, renders the same display-safe decision and correlation fields, and reports API-client failure as a diagnostic failure state without silently falling back to the in-process path.

Durable validation evidence for the explicit local/dev API-client parity mode is captured in `docs/session-handoffs/2026-05-17-blogai-auth-api-client-parity-mode-validation.md`.

No focused client tests were added in the first client slice because the repo does not yet have a `BlogAI.Web` test project, and adding a new test project/package would be larger than this boundary-only change. Current validation is compile-time build validation plus sentinel scanning; a future parity slice should add tests or durable trace evidence when the client is exercised.

Durable validation evidence for the local/dev API client boundary is captured in `docs/session-handoffs/2026-05-17-blogai-local-auth-api-client-boundary-validation.md`.
