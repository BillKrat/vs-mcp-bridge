# BlogAI Auth/API Boundary Note

## Purpose

Document the initial authentication and API boundary direction for BlogAI.

This is architecture direction only. It does not implement authentication, OAuth/OpenID, `api.global-webnet.com`, deployment, BlogEngine.NET runtime changes, new projects, or new services.

## Target Shape

The current directional shape remains:

- legacy site: `https://www.global-webnet.com`
- possible BlogAI application surface: `https://www.global-webnet.com/blogAi`
- possible future API/auth boundary: `https://api.global-webnet.com`

The legacy BlogEngine.NET site should remain stable while BlogAI grows beside it. The API/auth host is boundary thinking, not approval to create services, routing, deployment automation, or auth code.

## Why Auth/API Should Be A Boundary

BlogAI should treat authentication and API access as an explicit boundary because it will eventually need to separate:

- browser-facing BlogAI behavior
- API-facing BlogAI operations
- legacy BlogEngine.NET compatibility
- administrative actions
- secrets and credentials
- audit-relevant operations
- future user, role, or organization concepts

Keeping this boundary explicit now avoids mixing future BlogAI security decisions into legacy publishing code, cache diagnostics, MCP tooling, or ad hoc deployment scripts.

## Relationship To Legacy BlogEngine.NET Auth

Do not bind BlogAI tightly to legacy BlogEngine.NET authentication by default.

Reasons:

- legacy auth may remain useful for existing admin surfaces, but it should not define every future BlogAI trust boundary
- BlogAI may need different API, automation, or operational concepts than the legacy UI
- cache, publishing, and admin operations need clear auditability and explicit authority checks
- a tight coupling would make future migration or side-by-side operation harder

This does not mean replacing legacy auth immediately. It means documenting where BlogAI depends on it, where it should not, and where adapter boundaries may be needed later.

## Avoid Premature Identity Infrastructure

Do not build a full identity provider, Auth0 replacement, OAuth/OpenID system, RBAC model, token lifecycle, or social-login stack before the BlogAI boundary is clear.

The near-term work should answer simpler questions first:

- which BlogAI operations need authentication
- which operations need authorization beyond authentication
- which calls are browser-facing versus server-to-server
- which actions are administrative
- which actions require audit evidence
- which secrets are needed and where they should never appear
- which legacy BlogEngine.NET operations are being called or avoided

Architecture notes and trust-boundary diagrams should come before implementation.

The first trust-boundary sketch is `docs/blogai-auth-trust-boundary-flow.md`.
The first implementation boundary decision record is `docs/adr/0001-blogai-first-auth-boundary.md`.
The implementation gate checklist is `docs/blogai-auth-implementation-gate.md`.

## Possible Future Ownership For api.global-webnet.com

If introduced later, `https://api.global-webnet.com` may own:

- BlogAI API endpoints
- authentication handoff or token validation boundaries
- administrative BlogAI operations
- integration points that should not live inside legacy page rendering
- audit-friendly operation logging
- secret-reference resolution boundaries
- cache, publish, or content operations only after explicit design and approval

It should not be treated as a dumping ground for unclear responsibilities.
Each endpoint should have a documented caller, trust level, secret behavior, audit expectation, and failure mode before implementation.

## Alignment With MCP Security Seams

The BlogAI auth/API boundary should reuse the vocabulary already proven in the MCP/tool platform:

- approval: sensitive operations should have explicit human or policy approval where needed
- capabilities: operations should declare the authority they require
- secret indirection: callers should reference secrets without exposing raw values in normal request paths
- audit: meaningful operations should emit enough metadata to reconstruct what happened
- redaction: logs, traces, prompts, previews, and artifacts must remove secret-like values
- observability: workflows should preserve request/operation correlation and elapsed/failure data where useful
- policy: authorization decisions should be explicit and testable, not hidden in incidental code paths

These are design seams, not an instruction to implement MCP security infrastructure inside BlogAI auth. The value is shared language and boundary discipline.

## Never Log Or Persist

Do not log or persist:

- passwords
- API keys
- bearer tokens
- refresh tokens
- access tokens
- session cookies
- raw authorization headers
- raw secret values
- password reset tokens
- email verification tokens
- one-time codes
- private signing keys
- database connection strings
- unredacted production configuration
- complete request/response bodies that may contain credentials or tokens

Durable traces should prefer redacted metadata: operation name, target category, correlation id, outcome, elapsed time, redacted principal or capability labels, and secret reference identifiers when safe.

## Explicitly Deferred

Deferred until a later explicit implementation slice:

- OAuth/OpenID implementation
- user, role, or RBAC model
- tenant or organization model
- refresh token lifecycle
- social login
- external identity provider integration
- API gateway
- production secret storage
- deployment changes
- `api.global-webnet.com` project or service creation
- BlogEngine.NET runtime changes
- production cache-clear, publish, or deployment automation

## First Future Implementation Candidate

The first future implementation candidate is not auth code.

Recommended sequence:

1. document BlogAI auth flows and trust boundaries
2. identify callers, protected operations, secrets, audit events, and failure modes
3. decide what remains legacy-auth-backed and what needs a BlogAI-owned boundary
4. only then create a minimal authentication prototype, if the boundary document justifies it

That prototype should stay narrow, local-first, observable, and reversible.
