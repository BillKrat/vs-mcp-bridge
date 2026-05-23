# Global WebNet Auth Boundary Direction

## Purpose

Clarify that the next authentication direction is a reusable Global WebNet auth/API boundary, not one-off BlogAI auth.

This is documentation only. It does not implement authentication, create `api.global-webnet.com` code, add projects or services, add database schema, add OAuth/OpenID, add RBAC or tenant modeling, or change deployment.

## Priority Hierarchy

Current priority order:

1. `vs-mcp-bridge` platform correctness remains primary.
2. A secure, reusable Global WebNet auth/API boundary is the next foundational security direction.
3. BlogAI is the first real workload and first consumer of that boundary.
4. Broader reuse remains later work: MCP tunnel, website authentication, mobile apps, and future applications.

BlogAI is useful because it pressure-tests the MCP/tool platform and the auth/security seams through a real application workflow. It should not cause auth to become a BlogAI-specific subsystem.

## Boundary Shape

Candidate future boundary:

- `https://api.global-webnet.com`: reusable Global WebNet API/auth boundary
- `https://www.global-webnet.com/blogAi`: first BlogAI consumer
- `https://www.global-webnet.com`: legacy BlogEngine.NET site remains stable

BlogAI should ask the API/auth boundary whether a request is authenticated. BlogAI should not own long-term identity decisions and should not depend on BlogEngine.NET auth as its authority.

The shared auth capability should be referred to as `AdventuresAuth`. If it later becomes reusable code, `Adventures.Auth` is the possible package or namespace direction. These names are planning labels until a later implementation or packaging slice explicitly creates code.

## First Functional Expectations

The first functional version should stay modest and prove the reusable boundary works for BlogAI as the first consumer.

Expected boundary behavior:

- login or authenticate a development-safe request
- validate authenticated vs unauthenticated requests
- deny unauthenticated access
- allow authenticated access
- keep logout or session invalidation as an explicit placeholder if not fully implemented
- keep current user or claims as an explicit placeholder if not fully implemented
- preserve a correlation id through the request and auth decision
- emit a redacted audit event for the decision
- log only redacted metadata
- avoid raw token, secret, password, cookie, authorization header, API key, or credential persistence in logs, prompts, traces, or artifacts
- allow BlogAI to call the boundary without owning the identity decision

This first version may be local-only or development-only. It does not require production `api.global-webnet.com` deployment.

## Alignment With MCP Platform Seams

The reusable auth boundary should use the same security vocabulary already being hardened in `vs-mcp-bridge`:

- capability-like claims can come later
- policy decisions should be explicit and observable
- audit events should be redacted and durable enough for diagnosis
- secret values should move by indirection, not through logs or prompts
- redaction should occur before durable evidence is written
- correlation should survive boundary crossings
- authorization seams should be testable before they become broad infrastructure

This does not mean BlogAI should implement the MCP security stack. It means both systems should preserve compatible boundaries and evidence standards.

## Deferred

Explicitly deferred:

- OAuth/OpenID provider behavior
- social login
- RBAC
- tenant or organization model
- mobile-specific auth flows
- refresh token lifecycle
- external identity provider federation
- production secret vault integration
- API gateway
- MCP tunnel integration
- website-wide auth migration
- production deployment or certificate decisions
- database-backed identity schema
- Auth0 replacement or identity platform work

## Implementation Gate

No auth code should begin until the reusable Global WebNet boundary direction is explicitly acknowledged in the implementation slice.

The first implementation must remain a local or development functional proof. BlogAI should consume the boundary first, but BlogAI must not own identity decisions. Broader consumers remain future work.

The local prototype design is captured in `docs/adventures-auth-local-prototype-design.md`.
The first local prototype validation trace is captured in `docs/session-handoffs/2026-05-17-adventures-auth-local-prototype-validation.md`.
The BlogAI consumer boundary is captured in `docs/blogai-adventures-auth-consumer-design.md`.
The local API boundary design and initial skeleton are captured in `docs/adventures-auth-local-api-boundary-design.md`; the skeleton uses ASP.NET Core Minimal APIs with thin endpoint handlers.
The local API skeleton validation trace is captured in `docs/session-handoffs/2026-05-17-adventures-auth-local-api-validation.md`.
The preferred initial BlogAI UI framework is Blazor Web App, recorded in `docs/adr/0002-blogai-ui-framework.md`.
