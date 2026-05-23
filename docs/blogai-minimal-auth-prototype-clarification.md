# BlogAI Minimal Auth Prototype Clarification

## Purpose

Resolve the BlogAI auth gate review blockers before any authentication implementation begins, while aligning the first prototype with a reusable Global WebNet auth/API boundary.

This is documentation only. It does not implement auth, add services or projects, create database schema, change deployment, modify BlogEngine.NET runtime, or create `api.global-webnet.com` code.

## Exact Minimal Prototype Objective

The first acceptable prototype objective is:

Prove that a reusable Global WebNet auth/API boundary can make an authenticated vs unauthenticated access decision for BlogAI as the first consumer while preserving redacted, observable decision evidence.

The prototype must be:

- local-only or development-only
- limited to a single protected BlogAI request path or equivalent local BlogAI consumer call
- independent of production identity providers
- independent of production `api.global-webnet.com`
- independent of legacy BlogEngine.NET auth ownership
- independent of BlogAI-owned identity decisions
- free of OAuth/OpenID scope
- free of RBAC, tenant, organization, or role modeling
- free of MCP tunnel, mobile, or website-wide auth integration
- free of persistent credential storage unless a later docs slice explicitly designs and approves it

The prototype may simulate authenticated state with a development-only test principal or test header if that simulation is documented, deterministic, and never treated as production security.

## Local Validation Procedure

### Setup

Before coding, define the exact local request path to protect.

The path should be a development-only BlogAI route, handler, or consumer call that exercises the reusable boundary locally without production deployment, certificate changes, database schema, `api.global-webnet.com` production code, or BlogEngine.NET runtime modification.

### Unauthenticated Access

Representation:

- no development auth marker
- no test principal
- no accepted local-only auth signal

Expected result:

- access is denied
- outcome is recorded as unauthenticated denied
- no raw token, secret, cookie, or credential is logged
- response does not leak implementation details

### Authenticated Access

Representation:

- deterministic development-only auth marker, test principal, or equivalent local-only simulation
- no production credential
- no production identity provider
- no legacy BlogEngine.NET auth dependency

Expected result:

- access is allowed
- outcome is recorded as authenticated allowed
- BlogAI receives the boundary decision instead of making the identity decision itself
- principal metadata is redacted or non-sensitive
- no raw token, secret, cookie, or credential is logged

### Denied Or Error Outcome

Representation:

- malformed local-only auth marker
- missing required local-only claim
- prototype policy denial

Expected result:

- access is denied or error is returned in a structured way
- outcome category distinguishes denial from system error
- logs and evidence include correlation metadata
- logs and evidence exclude raw sensitive values

## Expected Logs And Audit Events

Use stable event names for local validation evidence:

- `BlogAiAuth.RequestReceived`
- `BlogAiAuth.DecisionEvaluated`
- `BlogAiAuth.AccessAllowed`
- `BlogAiAuth.AccessDenied`
- `BlogAiAuth.SecretRedacted`
- `GlobalWebNetAuth.BoundaryCalled`

Optional event names if needed:

- `BlogAiAuth.InvalidAuthSignal`
- `BlogAiAuth.PolicyDenied`
- `BlogAiAuth.ValidationFailed`

Each event should include only redacted metadata.

Suggested fields:

- `CorrelationId`
- `RequestId`
- `AuthDecisionId`
- `EventName`
- `Outcome`
- `ReasonCode`
- `RouteCategory`
- `AuthMode`
- `ElapsedMs`

Do not include raw tokens, raw cookies, raw authorization headers, raw secrets, passwords, API keys, or complete credential-bearing request/response bodies.

## Correlation Shape

The validation evidence should preserve these identifiers:

- `CorrelationId`: stable across the local request flow
- `RequestId`: identifies one inbound request
- `AuthDecisionId`: identifies one auth decision inside the request

Minimum requirement:

- every allow/deny decision includes all three identifiers
- the identifiers are visible in local validation evidence
- the identifiers contain no secret material

## Pass Evidence

The prototype clarification is satisfied when a future local validation run can show:

- unauthenticated request denied
- authenticated request allowed
- BlogAI can call the boundary without owning the identity decision
- denied/error request produces a clear denial or error outcome
- no raw token, secret, cookie, authorization header, API key, or credential appears in logs, prompts, traces, or artifacts
- `CorrelationId` is preserved across request and auth decision evidence
- `RequestId` and `AuthDecisionId` are present on decision events
- auth decision is observable through named events
- local validation is repeatable without production deployment changes
- no BlogEngine.NET runtime modification is required
- no production identity provider is required

## Fail Conditions

The prototype fails the gate if:

- raw secrets or tokens appear in logs, prompts, traces, or artifacts
- decision path is opaque or cannot be reconstructed from local evidence
- legacy BlogEngine.NET auth coupling is introduced as a requirement
- BlogAI-owned identity decisions are introduced as a requirement
- scope expands into OAuth/OpenID
- scope expands into RBAC, tenant, organization, or role modeling
- scope expands into a general identity platform
- scope expands into MCP tunnel, mobile, or website-wide auth integration
- production deployment or certificate changes are needed
- database schema is needed
- `api.global-webnet.com` code or project creation is needed
- local validation cannot be repeated

## Ready State

With this clarification in place, the previous gate-review blockers are resolved in documentation and aligned with the reusable Global WebNet boundary direction in `docs/global-webnet-auth-boundary-direction.md`.

The project is ready to consider a later minimal local auth prototype slice, provided that slice keeps the exact objective above and does not add deferred identity, deployment, database, broader-consumer, or legacy runtime scope.
