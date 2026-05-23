# AdventuresAuth Local API Boundary Design

## Purpose

Design how the current local `AdventuresAuth` decision capability is hosted behind a minimal local/dev API boundary.

The first skeleton exists as `Adventures.Auth.LocalApi`. It is local/dev only. It does not add persistence/database, OAuth/OpenID/RBAC, deployment changes, or BlogEngine.NET coupling.

The future local API boundary should prove:

- the current `AdventuresAuthDecisionService` can be exposed through an API-shaped boundary
- BlogAI can remain the first consumer without owning identity decisions
- request, decision, audit, redaction, and correlation behavior survive a host boundary
- local/dev behavior stays deterministic before any production `api.global-webnet.com` work
- `AdventuresAuth` remains reusable beyond BlogAI

## Boundary Shape

Conceptual local host:

- local/dev-only API process or test host
- no production deployment
- no certificate, DNS, gateway, or cloud routing decision
- no BlogEngine.NET runtime dependency
- no persistence layer

The boundary should translate API requests into `AdventuresAuthRequest` values, invoke the existing auth decision service, and return structured allow/deny responses.

The boundary should not contain BlogAI-specific route authorization rules. BlogAI remains responsible for deciding which resources are public or protected and for mapping an auth decision to a BlogAI response.

## Host Style

The local/dev host uses ASP.NET Core Minimal APIs.

Rationale:

- the prototype is local/dev only
- the endpoint surface is small
- lower ceremony keeps the boundary visible
- `Program.cs` can show the complete `/auth` transport surface in one place
- the style matches the current minimal implementation philosophy

MVC controllers are deferred unless later endpoint complexity justifies that extra structure.

Minimal API shape:

- `Program.cs` maps an `/auth` endpoint group
- endpoint handlers remain thin transport adapters
- endpoint handlers delegate to shared `AdventuresAuth` services or interfaces
- auth decision logic stays out of endpoint lambdas
- request/response DTO mapping may live near the host boundary
- service registration should reuse the shared auth service layer where practical

## Conceptual Endpoints

The current local/dev skeleton maps:

- `POST /auth/login`: evaluate a local development credential and return an allow/deny decision plus local principal/session placeholders when allowed.
- `POST /auth/logout`: invalidate or clear a local session placeholder.
- `GET /auth/me`: return a current local principal placeholder for a valid local session.
- `POST /auth/validate`: validate a local session, token placeholder, or development auth signal and return an allow/deny decision.

Each endpoint preserves the same local/dev constraints as the in-process skeleton.

## Request Principles

API requests should include or allow generation of:

- `CorrelationId`
- `RequestId`
- `AuthDecisionId`
- `ClientApplication`
- `Environment`

Rules:

- If correlation fields are supplied, the API boundary preserves them.
- If correlation fields are absent, the API boundary generates non-secret identifiers.
- `ClientApplication` must be explicit, initially `BlogAI` for the first consumer.
- `Environment` must be explicit, initially `LocalDevelopment` for local validation.
- Raw passwords, tokens, cookies, authorization headers, API keys, or credential-bearing request bodies must not be logged or persisted.

## Response Principles

API responses should be structured and minimal.

Expected response shape:

- `Allowed`
- `Outcome`
- `ReasonCode`
- `CorrelationId`
- `RequestId`
- `AuthDecisionId`
- `ClientApplication`
- `Environment`
- optional local principal placeholder
- optional local session placeholder
- redaction or audit summary fields that do not contain raw secret values

The response should expose only what a consumer needs to make a local authorization decision. It should not expose raw credentials, raw tokens, raw cookies, or internal audit payloads containing sensitive material.

## API Boundary Responsibilities

The local API boundary does:

- expose the local/dev host through ASP.NET Core Minimal APIs unless a later design changes that
- translate HTTP request data into an `AdventuresAuthRequest`
- preserve or generate correlation metadata
- invoke `AdventuresAuthDecisionService`
- return a deterministic local/dev decision result
- return allow/deny and minimal local principal/session placeholders
- emit redacted audit metadata
- log decision metadata only
- preserve `AdventuresAuth` event names and reason codes where practical
- ensure redaction occurs before durable logs, traces, prompts, or artifacts
- remain replaceable by a future production host without changing the BlogAI identity ownership model

The local API boundary does not:

- put auth decision logic directly in endpoint lambdas
- own BlogAI-specific route protection rules
- decide whether a BlogAI resource is public or protected
- create production identity semantics
- silently retry, infer, or repair credentials
- persist secrets, tokens, credentials, or raw auth headers

## BlogAI Consumer Responsibilities

The BlogAI consumer remains responsible for:

- deciding whether a route or resource requires authentication
- building or preserving request correlation
- calling the auth boundary through an interface or future API client
- mapping allow/deny to BlogAI response behavior
- logging only BlogAI resource, correlation, and decision metadata
- avoiding identity ownership
- avoiding BlogEngine.NET auth coupling

BlogAI should not become the identity provider or the long-term authority for authentication decisions.

## Audit And Redaction Expectations

The API boundary should follow the current local skeleton evidence standard:

- decisions are observable
- audit events are redacted before durable storage
- raw secret-like values are not written to logs, prompts, traces, or artifacts
- correlation fields appear in decision and audit evidence
- redaction evidence can be proven without storing raw sentinel values

The existing trace artifacts provide the baseline behavior:

- `docs/session-handoffs/2026-05-17-adventures-auth-local-prototype-validation.md`
- `docs/session-handoffs/2026-05-17-blogai-auth-consumer-validation.md`

## Non-Goals

Explicitly out of scope:

- production deployment
- production `api.global-webnet.com` implementation
- ASP.NET host creation in this slice
- middleware
- real endpoint implementation
- OAuth/OpenID
- refresh tokens
- real password storage
- database-backed users
- RBAC
- tenant or organization model
- social login
- external identity provider integration
- API gateway
- production secret vault integration
- BlogEngine.NET auth bridge
- MCP tunnel integration

## Initial Skeleton

The first implementation slice added:

- `Adventures.Auth.LocalApi/Program.cs`
- `Adventures.Auth.LocalApi/AdventuresAuthApiEndpointExtensions.cs`
- thin endpoint handlers in `Adventures.Auth.LocalApi/AdventuresAuthEndpointHandlers.cs`
- API DTOs and translation service under `Adventures.Auth.LocalApi`
- shared `IAdventuresAuthDecisionService` abstraction in `VsMcpBridge.Shared/AdventuresAuth`
- tests in `VsMcpBridge.Shared.Tests/AdventuresAuthLocalApiTests.cs`

The skeleton remains local/dev only. It validates unauthenticated denied, valid development auth allowed, invalid credential/session denied, current principal placeholder, logout invalidation, correlation preservation/generation, redaction from response evidence, thin handler delegation, no persistence, and no BlogEngine.NET dependency.

## Future Trace Slice

Durable trace artifacts for the local API host skeleton are captured in `docs/session-handoffs/2026-05-17-adventures-auth-local-api-validation.md`.

## BlogAI Client Boundary

`BlogAI.Web/Auth` now includes a local/dev-only `IBlogAiLocalAuthApiClient` boundary that can call the `Adventures.Auth.LocalApi` `/auth` endpoints.

The client does not switch BlogAI UI behavior by default. `/local-dev` still uses the in-process `IBlogAiAuthConsumerService` path. Explicit local/dev parity diagnostics can exercise the API-backed path with `/local-dev?authPath=api-client`, which must render display-safe output or an explicit diagnostic failure without silent fallback.

Durable validation evidence for the client boundary is captured in `docs/session-handoffs/2026-05-17-blogai-local-auth-api-client-boundary-validation.md`.
Durable validation evidence for the explicit API-client parity mode is captured in `docs/session-handoffs/2026-05-17-blogai-auth-api-client-parity-mode-validation.md`.

## Future Slice

The implementation should still stop if it needs production deployment, persistent identity storage, external identity providers, or BlogAI-specific authorization logic inside the API boundary.
