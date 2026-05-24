# Auth API Admin Boundary Decision

## Purpose

Record where future authentication administration capabilities should live before any admin UI, admin API, production auth, or persistence work begins.

This is documentation only. It does not add runtime code, Razor pages, middleware, deployment changes, production auth, OAuth/OpenID, RBAC implementation, persistence, cookies/session topology, BlogEngine.NET coupling, or admin APIs.

## Checkpoint

- Branch: `main`
- Expected sync: `main == origin/main`
- Starting HEAD: `99f2cae Add BlogAI deployed environment guardrail`
- Working tree expectation: clean
- Deployment performed by this review: no

## Inputs Reviewed

- `AI_START.md`
- `SolutionFolder/docs/AI_STOP.md`
- `SolutionFolder/docs/ARCHITECTURE.md`
- `SolutionFolder/docs/session-handoffs/2026-05-17-blogai-production-exposure-boundary-review.md`
- `SolutionFolder/docs/session-handoffs/2026-05-17-blogai-webdeploy-validation.md`
- `SolutionFolder/docs/global-webnet-auth-boundary-direction.md`
- `SolutionFolder/docs/adventures-auth-local-prototype-design.md`

## Boundary Shape

Future target shape:

- `https://api.global-webnet.com`: future shared Global WebNet auth/API host.
- `https://www.global-webnet.com/blogAi`: future BlogAI client/app surface.
- `/blogAi/api/...`: possible future BlogAI app-specific API area for BlogAI workflow behavior.

The auth service boundary owns identity and authorization decisions. BlogAI may consume those decisions, but BlogAI page components should not own auth-admin behavior or identity-management rules.

## Admin Capability Ownership

Auth-admin capabilities belong to the auth service boundary, not BlogAI page components.

Future auth-admin capabilities may include:

- users
- tenants
- roles
- permissions
- audit views or audit query APIs
- application registrations
- policy metadata

These capabilities should not be inferred from the current local/dev diagnostic UI. The deployed `BlogAI.Web` surface remains a smoke-test shell, and `/local-dev` remains diagnostic-only.

## Decisions

Should the auth API eventually expose admin management features?

Yes, but only after real production auth and persistence boundaries are designed. Admin features need an explicit authority model, durable storage model, audit model, redaction rules, and authorization policy before implementation.

Should BlogAI own user, tenant, role, or permission management?

No. BlogAI should not own shared identity administration. BlogAI can eventually call auth-admin APIs through a thin service/UI boundary, but the auth service must own authorization decisions and admin policy.

Should admin UI or APIs be implemented now?

No. This slice records ownership boundaries only.

## Explicitly Deferred

- Auth-admin UI
- Auth-admin APIs
- Razor admin pages
- Production authentication
- OAuth/OpenID
- RBAC implementation
- Tenant or organization model implementation
- Persistence/database-backed identity
- Cookies/session topology
- Auth middleware
- BlogEngine.NET auth coupling
- API gateway behavior
- Production deployment changes

## Guardrails

Before any admin work begins:

- Keep `BlogAI.Web` as a smoke-test shell unless a later slice explicitly changes that.
- Keep `/local-dev` diagnostic-only and not production auth.
- Keep auth-admin ownership in the auth service boundary.
- Keep UI components thin if UI is later added.
- Do not add admin behavior without production auth and persistence design.
- Do not store or display raw passwords, tokens, cookies, authorization headers, API keys, or secret values.
- Require explicit audit, redaction, and policy expectations before any admin surface is exposed.

## Smallest Next Implementation-Safe Slice

The smallest next implementation-safe slice is still not auth-admin.

Recommended next options:

1. Capture durable validation evidence for the deployed guardrail.
2. Create a docs-only production auth/admin roadmap gate that defines the required design checkpoints before production auth, persistence, or admin APIs.

Do not implement admin UI, admin API, RBAC, persistence, middleware, or real login behavior as the next slice.

