# AdventuresAuth Local API Boundary Design

## Purpose

Design how the current local `AdventuresAuth` decision capability could later be hosted behind a minimal local/dev API boundary.

This is documentation only. It does not create an API project, add ASP.NET hosting code, add middleware, add endpoints, add persistence/database, add OAuth/OpenID/RBAC, change deployment, or couple to BlogEngine.NET.

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

## Conceptual Endpoints

These endpoints are conceptual only. They are not implementation-approved by this document.

- `POST /auth/login`: evaluate a local development credential and return an allow/deny decision plus local principal/session placeholders when allowed.
- `POST /auth/logout`: invalidate or clear a local session placeholder.
- `GET /auth/me`: return a current local principal placeholder for a valid local session.
- `POST /auth/validate`: validate a local session, token placeholder, or development auth signal and return an allow/deny decision.

Each endpoint should preserve the same local/dev constraints as the in-process skeleton.

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

The local API boundary should:

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

The local API boundary should not:

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

## First Future Implementation Slice

If approved later, the first implementation slice should be narrow:

- create a local minimal API host skeleton
- use the existing `AdventuresAuthDecisionService`
- keep in-memory/dev credential behavior only
- expose only local/dev endpoint behavior needed for validation
- include tests for unauthenticated denied, valid development auth allowed, and invalid auth denied
- include redaction and correlation tests
- capture durable trace evidence
- avoid production deployment, OAuth/OpenID, RBAC, persistence, BlogEngine.NET coupling, and real cookie/session topology

The implementation should stop if it needs production deployment, persistent identity storage, external identity providers, or BlogAI-specific authorization logic inside the API boundary.
