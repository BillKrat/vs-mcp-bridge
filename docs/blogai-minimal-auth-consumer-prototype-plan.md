# BlogAI Minimal Auth Consumer Prototype Plan

## Purpose

Define the first minimal BlogAI-side prototype that consumes `AdventuresAuth`.

This plan guided the first local shared-layer adapter. It does not create BlogAI app code, create projects, implement auth middleware, change deployment, modify BlogEngine.NET runtime, add persistence/database, or add OAuth/OpenID/RBAC.

## First Prototype Objective

The first BlogAI-side prototype should prove:

- a BlogAI request can consume an `AdventuresAuth` decision result
- unauthenticated protected request is denied
- development-authenticated protected request is allowed
- invalid local session is denied
- correlation flows from BlogAI to `AdventuresAuth`
- raw secrets, tokens, cookies, authorization headers, credentials, and API keys are not logged

The prototype remains local/dev only. It is not production auth and it is not a BlogEngine.NET auth bridge.

## Initial Prototype Location

The first implementation slice placed the local/dev-only consumer adapter under `VsMcpBridge.Shared/BlogAI/Auth` with tests in `VsMcpBridge.Shared.Tests/BlogAiAuthConsumerTests.cs`.

That location keeps the prototype in-process, small, and testable. It is not a web host, middleware layer, production `api.global-webnet.com` deployment, BlogEngine.NET auth bridge, or identity platform.

The consumer boundary is exposed through `IBlogAiAuthConsumerService` so future BlogAI/web/API integration can depend on a stable abstraction instead of the concrete local prototype service. Shared DI registration is available through `AddBlogAiAuthConsumerServices`.

Durable trace evidence for the implemented interface-driven consumer flow is captured in `docs/session-handoffs/2026-05-17-blogai-auth-consumer-validation.md`.

The Blazor-side local/dev display design for consuming this boundary later is captured in `docs/blogai-local-auth-ui-integration-design.md`.

The first Blazor-side local/dev display uses `BlogAI.Web/Auth` as a UI-facing service boundary over `IBlogAiAuthConsumerService`; `/local-dev` renders only display-safe decision and correlation metadata.

The `/local-dev` route-level protected placeholder now follows the same boundary: unauthenticated local decisions hide the placeholder, while development-authenticated local decisions show it. The show/hide state is produced by the UI-facing service from the BlogAI auth consumer decision, not by Razor credential logic or middleware.

Durable validation evidence for this Blazor-side display is captured in `docs/session-handoffs/2026-05-17-blogai-local-auth-ui-validation.md`.

Durable validation evidence for the route-level protected placeholder behavior is captured in `docs/session-handoffs/2026-05-17-blogai-route-protected-placeholder-validation.md`.

The first local/dev BlogAI API client boundary now exists in `BlogAI.Web/Auth` behind `IBlogAiLocalAuthApiClient`. It is registered for dependency injection and targets the local `Adventures.Auth.LocalApi` endpoint shape. `/local-dev` continues to use the in-process `IBlogAiAuthConsumerService` display path by default, while `/local-dev?authPath=api-client` explicitly exercises the API-client diagnostic path for parity checks without silent fallback.

Durable validation evidence for this boundary is captured in `docs/session-handoffs/2026-05-17-blogai-local-auth-api-client-boundary-validation.md`.

## Future Code Location Guidance

Future code may move closer to a BlogAI app-layer or consumer-boundary location only when that can be done without production hosting, deployment changes, BlogEngine.NET runtime changes, or persistence.

Boundary expectations:

- BlogAI owns request handling and route allow/deny mapping.
- `AdventuresAuth` owns auth decisions.
- `AdventuresAuth` remains the reusable shared capability.
- BlogAI must not own identity decisions.
- BlogAI must not couple to BlogEngine.NET auth.
- BlogAI must not require production `api.global-webnet.com` hosting for the local prototype.

If an implementation slice cannot avoid new projects, production hosting, BlogEngine.NET runtime changes, or persistence, stop and return to design.

## Minimal Conceptual Flow

1. Request enters a local BlogAI route.
2. BlogAI determines whether the route is public or protected.
3. Public route returns normally if the prototype includes a public route.
4. Protected route extracts a development/local auth marker or local session placeholder.
5. BlogAI creates or preserves `CorrelationId`, `RequestId`, `ClientApplication=BlogAI`, and `Environment=LocalDevelopment`.
6. BlogAI builds an `AdventuresAuthRequest`.
7. `AdventuresAuth` returns an `AdventuresAuthDecision`.
8. BlogAI maps `Allowed=true` to protected content allowed.
9. BlogAI maps `Allowed=false` to protected content denied.
10. BlogAI logs only correlation, route category, outcome, and reason code metadata.

BlogAI should not log raw marker/session values. If a request contains secret-like material, durable evidence must show redaction without storing the raw values.

## Validation Scenarios

The first implementation slice should include tests for:

- public route allowed without auth if a public route is included
- protected route denied without auth
- protected route allowed with valid development auth
- protected route denied with invalid development auth
- protected route denied with invalid local session
- `CorrelationId` appears in both BlogAI and `AdventuresAuth` evidence
- `RequestId` remains tied to the BlogAI request
- `AuthDecisionId` remains tied to the auth decision
- `ClientApplication=BlogAI` is preserved
- `Environment=LocalDevelopment` is preserved
- secret-like values are redacted from BlogAI-side logs/evidence
- BlogAI does not call BlogEngine.NET auth to make the decision

The implementation slice should also produce durable trace artifacts after the tests pass.

## Non-Goals

Explicitly out of scope:

- production auth
- production `api.global-webnet.com` deployment
- real cookie/session topology
- OAuth/OpenID
- refresh tokens
- RBAC or roles
- tenant or organization model
- database-backed users
- real password storage
- BlogEngine.NET auth bridge
- BlogEngine.NET runtime modification
- external identity provider integration
- API gateway
- MCP tunnel integration
- mobile-specific auth flow

## Implementation Gate

The initial gate was reviewed in `docs/session-handoffs/2026-05-17-blogai-auth-consumer-readiness-review.md`, then the first local shared-layer adapter was added.

Any next implementation slice must still:

- remain local/dev only
- use the existing `AdventuresAuth` decision shape
- include tests for allow/deny/correlation/redaction behavior
- include durable trace artifacts
- preserve the separation between BlogAI request handling and `AdventuresAuth` identity decisions
- stop if production auth, persistence, OAuth/OpenID, RBAC, BlogEngine.NET auth coupling, or deployment changes become necessary

## Expected Next Slice

The next slice, if approved, should capture durable trace artifacts for the implemented local adapter or add only the smallest BlogAI-side harness needed to exercise it.

It should not create a production service, deploy anything, or introduce identity-platform scope.
