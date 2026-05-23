# BlogAI AdventuresAuth Consumer Design

## Purpose

Define how BlogAI should consume the local `AdventuresAuth` boundary before adding any BlogAI app code.

This is design guidance only. It does not implement BlogAI integration, create BlogAI app code, add API hosting, add OAuth/OpenID, add persistence, change deployment, or modify BlogEngine.NET runtime.

## Consumer Boundary

BlogAI should consume `AdventuresAuth` as an auth decision boundary.

Rules:

- BlogAI asks the auth boundary for an authenticated or unauthenticated decision.
- BlogAI does not own identity decisions.
- BlogAI does not rely on BlogEngine.NET auth as authority.
- BlogAI receives only the minimum decision output needed to allow or deny the protected route.
- BlogAI may receive a minimal principal placeholder for local development.
- BlogAI may receive a local session placeholder for local development.
- BlogAI logs correlation and decision metadata only.
- BlogAI must not log raw secrets, raw tokens, raw cookies, raw authorization headers, raw credentials, or complete credential-bearing request/response bodies.

This keeps BlogAI as the first consumer of a reusable boundary, not the owner of an identity subsystem.

## Conceptual Consumer Flow

1. Browser requests a protected BlogAI route.
2. BlogAI creates or preserves request correlation metadata.
3. BlogAI checks local auth/session state available to the local prototype.
4. BlogAI calls or uses the `AdventuresAuth` boundary.
5. `AdventuresAuth` evaluates the local development auth signal.
6. `AdventuresAuth` returns allow/deny plus a minimal principal/session placeholder when allowed.
7. BlogAI allows protected content only when the boundary returns allowed.
8. BlogAI denies protected content when the boundary returns denied.
9. BlogAI records redacted correlation and decision metadata.

Conceptual mapping:

- protected route request -> `CorrelationId`, `RequestId`, `ClientApplication=BlogAI`, `Environment=LocalDevelopment`
- auth boundary decision -> `AuthDecisionId`, outcome, reason code, optional local principal placeholder
- BlogAI response -> allow protected content or deny with no sensitive detail leak

## Local Validation Scenarios

The first BlogAI consumer validation should prove:

- unauthenticated BlogAI request is denied
- authenticated development request is allowed
- invalid local session is denied
- correlation id flows from BlogAI into `AdventuresAuth`
- `RequestId` is preserved for the inbound BlogAI request
- `AuthDecisionId` is preserved for the boundary decision
- `ClientApplication` is `BlogAI`
- `Environment` identifies local or development context
- BlogAI records only redacted decision metadata
- no raw token, secret, password, credential, cookie, authorization header, or API key is logged
- BlogAI does not call BlogEngine.NET auth to make the decision

## Minimal Result Shape

BlogAI should need only:

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

The local principal placeholder should stay non-sensitive. It is not a production identity model, role model, or claims platform.

## Non-Goals

Explicitly out of scope:

- production auth
- production `api.global-webnet.com` deployment
- OAuth/OpenID
- refresh tokens
- RBAC
- tenant or organization model
- social login
- external identity provider integration
- BlogEngine.NET auth coupling
- deployment changes
- database-backed identity
- real password storage
- API gateway
- MCP tunnel integration
- mobile-specific auth flow

## Next Slice Guidance

The next implementation slice, if approved, should add the smallest local BlogAI-style consumer harness or adapter needed to call the existing in-process `AdventuresAuthDecisionService`.

It should not create production hosting, persistence, OAuth/OpenID, RBAC, deployment changes, BlogEngine.NET runtime changes, or a generalized identity platform.
