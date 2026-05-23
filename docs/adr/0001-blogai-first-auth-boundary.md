# ADR 0001: BlogAI First Auth Boundary

## Status

Proposed

## Context

BlogAI is moving from platform-direction discussion toward real operational development.

Current directional shape:

- legacy BlogEngine.NET remains at `https://www.global-webnet.com`
- BlogAI may live under `https://www.global-webnet.com/blogAi`
- `https://api.global-webnet.com` may become a future API/auth boundary

Existing direction is captured in:

- `docs/global-webnet-auth-boundary-direction.md`
- `docs/blogai-auth-api-boundary-note.md`
- `docs/blogai-auth-trust-boundary-flow.md`
- `docs/session-handoffs/2026-05-17-platform-to-blogai-transition.md`

The first auth work must avoid premature infrastructure. BlogAI needs a clean owned boundary for authentication and API decisions, but that boundary should be reusable Global WebNet infrastructure with BlogAI as its first consumer, not a BlogAI-only subsystem. It does not yet need production OAuth/OpenID, a general identity provider, an Auth0 replacement, external provider integration, a public API gateway, or database-backed identity schema.

The boundary should align with MCP platform security principles already used in this repository:

- capabilities
- policy
- approval
- secret indirection
- audit
- redaction
- observability

## Decision

The first future authentication implementation should validate a reusable Global WebNet auth/API boundary concept that BlogAI consumes first.

It should:

- begin with the smallest viable trusted-session/API boundary needed for BlogAI development as the first consumer
- distinguish authenticated from unauthenticated access through an explicit boundary
- avoid tightly binding BlogAI to legacy BlogEngine.NET auth
- avoid making BlogAI the owner of identity decisions
- avoid becoming a general identity provider
- avoid replacing Auth0 or designing an Auth0 equivalent
- keep auth decisions observable and auditable
- keep sensitive values out of logs, prompts, durable artifacts, and diagnostics
- leave room to evolve later toward stronger identity providers or deployment shapes

This decision does not approve implementation. It defines the boundary any later implementation proposal must validate.

## Consequences

Positive consequences:

- BlogAI auth work can start small without collapsing into a full identity platform.
- The first auth work stays reusable instead of becoming a BlogAI one-off.
- Legacy BlogEngine.NET can remain stable while BlogAI develops beside it.
- Future `api.global-webnet.com` work has a clearer responsibility boundary.
- Security discussion can use the same vocabulary as the MCP platform: capabilities, policy, approval, secret indirection, audit, redaction, and observability.
- Future implementation can be validated by behavior instead of by speculative identity architecture.

Tradeoffs:

- Some legacy auth integration questions remain open.
- Stronger identity provider choices remain deferred.
- A minimal prototype may need to be replaced or adapted after trust-boundary validation.
- More design work is required before any production-ready auth implementation.

## First Validation Goal

The first implementation, when explicitly approved later, should validate only this:

- BlogAI can distinguish authenticated vs unauthenticated access through a clean boundary.
- BlogAI can call the boundary without owning the identity decision.
- Sensitive values are not logged, persisted in durable artifacts, or exposed to prompts.
- Auth decisions are observable and auditable with redacted operation metadata.
- The boundary can evolve later toward stronger identity providers and later consumers such as MCP tunnel, websites, mobile, and other applications without binding BlogAI tightly to legacy BlogEngine.NET auth.

This validation should be local-first, narrow, reversible, and documented before any production deployment or broader identity work.

## Non-Goals

This ADR explicitly does not pursue:

- production OAuth/OpenID
- social login
- multi-tenant organization model
- RBAC
- external provider integration
- refresh-token lifecycle
- public API gateway
- enterprise identity platform
- replacing Auth0
- tightly coupling to BlogEngine.NET auth
- BlogAI-owned identity decisions
- MCP tunnel integration
- mobile-specific auth flows
- website-wide auth migration
- `api.global-webnet.com` code
- API projects or new services
- database schema
- deployment changes
- BlogEngine.NET runtime changes

## Deferred Decisions

Deferred until later explicit design or implementation slices:

- token format
- cookie/session topology
- deployment and certificate strategy
- database schema
- organization or tenant model
- external identity providers
- secret storage provider
- legacy user migration
- API gateway shape
- production operational model

## Implementation Gate

Before any auth implementation begins, a future slice should document:

- auth flows
- trust boundaries
- callers and protected operations
- unauthenticated behavior
- authenticated behavior
- secret handling
- audit events
- redaction expectations
- failure modes
- local validation plan

The implementation gate checklist is `docs/blogai-auth-implementation-gate.md`.
The minimal local prototype objective is clarified in `docs/blogai-minimal-auth-prototype-clarification.md`.

No auth implementation is approved by this ADR alone.
