# AdventuresAuth Local Prototype Design

## Purpose

Design the first local-only authentication prototype for the reusable Global WebNet auth/API boundary.

This document is design guidance. It does not approve production authentication, create projects or services, add database schema, change deployment, modify BlogEngine.NET, or add OAuth/OpenID, RBAC, tenant, or organization modeling.

The prototype should validate:

- an owned reusable auth/API boundary locally
- authenticated vs unauthenticated decisions
- BlogAI as the first consumer of the boundary
- observable audit and correlation evidence
- redaction before logs, prompts, traces, or artifacts
- functional behavior without production identity complexity

## Naming

- `AdventuresAuth`: shared auth capability and product shorthand.
- `Adventures.Auth`: possible future package or namespace if the capability later becomes reusable code.
- `https://api.global-webnet.com`: possible first deployment host boundary, not created by this prototype design.

The first implementation should be a local or development proof only. It should not create production `api.global-webnet.com` code.

## Conceptual Components

### Local AdventuresAuth API Host

Local development host for the conceptual auth/API boundary.

It represents the future `api.global-webnet.com` boundary without requiring deployment, certificates, database schema, or production routing.

### BlogAI Consumer/Client

First caller of the boundary.

BlogAI asks AdventuresAuth for an authentication decision. BlogAI does not own identity decisions and does not depend on BlogEngine.NET auth as authority.

### Auth Decision Service

Evaluates whether a request is authenticated for the local prototype.

For the first proof, this may use deterministic development-only credentials, a test principal, or a test session signal. The decision path must be observable and repeatable.

### Session/Token Validator Placeholder

Placeholder for future session or token validation.

The local prototype may simulate validation, but it must not introduce production token format, refresh token lifecycle, OAuth/OpenID, or persistent credential storage.

### Audit/Logging Boundary

Captures redacted auth decision events with correlation metadata.

Audit evidence should be enough to reconstruct the decision path without storing secrets.

### Redaction Boundary

Removes or avoids raw passwords, tokens, cookies, authorization headers, API keys, and secret values before durable evidence is written.

## Conceptual Endpoints

These endpoints are conceptual only. They are not implementation-approved by this document.

- `POST /auth/login`: evaluate a local development credential or test principal and produce a local authenticated state.
- `POST /auth/logout`: invalidate or clear a local session placeholder.
- `GET /auth/me`: return a redacted current principal or claims placeholder.
- `POST /auth/validate`: validate a local session, token, or test signal and return an allow/deny decision.

Each endpoint should have a documented caller, request category, decision outcome, redaction expectation, and audit event before coding begins.

## Expected Events

Use stable event names for local validation evidence:

- `AdventuresAuth.RequestReceived`
- `AdventuresAuth.LoginEvaluated`
- `AdventuresAuth.SessionValidated`
- `AdventuresAuth.AccessAllowed`
- `AdventuresAuth.AccessDenied`
- `AdventuresAuth.SecretRedacted`

Events must contain redacted metadata only.

## Correlation Shape

Minimum event fields:

- `CorrelationId`: stable across the BlogAI consumer call and AdventuresAuth decision path
- `RequestId`: identifies one inbound request to the local boundary
- `AuthDecisionId`: identifies one auth decision
- `ClientApplication`: identifies the caller, initially `BlogAI`
- `Environment`: identifies local or development validation context

No identifier may contain secret material.

## Local Validation Scenarios

The first prototype should be considered useful only if a local validation run can show:

- unauthenticated request is denied
- valid development credential or local session signal is allowed
- invalid credential or malformed local session signal is denied
- logout invalidates the local session placeholder or records a clearly deferred invalidation result
- `/auth/me` returns a redacted current principal placeholder for an authenticated local request
- no raw secret, password, token, cookie, authorization header, API key, or credential appears in logs, prompts, traces, or artifacts
- `CorrelationId`, `RequestId`, `AuthDecisionId`, `ClientApplication`, and `Environment` appear in the decision evidence

## Non-Goals

Explicitly out of scope:

- production deployment
- production `api.global-webnet.com` service creation
- real password storage
- database schema
- OAuth/OpenID
- refresh tokens
- RBAC
- tenant or organization model
- social login
- mobile-specific auth flow
- BlogEngine.NET auth integration
- MCP tunnel integration
- API gateway
- external identity provider federation
- production secret vault integration

## Implementation Gate

No code is approved by this design.

A later implementation slice must restate the local-only objective, name the exact local host and BlogAI consumer path, define the development credential or session simulation, and preserve the no-secret logging rule before any prototype code is added.

## Initial Skeleton Location

The first implementation slice placed the local-only skeleton under `VsMcpBridge.Shared/AdventuresAuth` with tests in `VsMcpBridge.Shared.Tests/AdventuresAuthTests.cs`.

That location was chosen because the slice explicitly avoided creating new projects or services. The code remains an in-process local prototype surface, not a deployed API host, package extraction, production `api.global-webnet.com` service, or BlogEngine.NET integration.

## Durable Validation Evidence

The first durable validation trace is captured in:

- `artifacts/logs/adventures-auth-local-prototype-trace-20260517.log`
- `artifacts/logs/adventures-auth-local-prototype-trace-20260517.metadata.json`
- `docs/diagrams/adventures-auth-local-prototype-trace-20260517.mmd`
- `docs/session-handoffs/2026-05-17-adventures-auth-local-prototype-validation.md`

## BlogAI Consumer Design

The BlogAI consumer boundary is designed in `docs/blogai-adventures-auth-consumer-design.md`.

That design keeps BlogAI as the first local consumer of `AdventuresAuth` while preserving the rule that BlogAI does not own identity decisions or rely on BlogEngine.NET auth.
