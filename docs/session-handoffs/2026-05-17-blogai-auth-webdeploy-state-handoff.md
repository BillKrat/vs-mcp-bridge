# BlogAI Auth WebDeploy State Handoff

## Purpose

Record the current BlogAI, AdventuresAuth, auth-boundary, and WebDeploy state so the next session can resume without reconstructing the recent deployment and local/dev auth work from chat history.

This is documentation only. No runtime code or deployment changes were made in this slice.

## Checkpoint

- Branch: `main`
- Expected sync: `main == origin/main`
- Starting HEAD: `b47d235 Record deployed guardrail retry success`
- Working tree expectation: clean
- Deployment performed by this handoff: no

## BlogAI.Web State

`BlogAI.Web` is currently a deployed smoke-test shell, not a production-auth application.

Current behavior:

- `/` returns the shell and deployed-environment guardrail.
- `/local-dev` remains a diagnostic-only local/dev surface.
- The deployed guardrail/banner is present locally and on the deployed site.
- The guardrail does not add auth, routing, middleware, persistence, cookies, or BlogEngine.NET coupling.
- `/local-dev` is explicitly not production auth.

Current deployed URLs:

- `https://api.global-webnet.com/`
- `https://api.global-webnet.com/local-dev`

## AdventuresAuth State

`AdventuresAuth` is the current local/dev auth decision boundary.

Current purpose:

- provide deterministic local/dev allow and deny decisions
- preserve correlation metadata
- keep response evidence display-safe and redacted
- prove the auth decision shape before production auth exists

The API-host direction is established through the local/dev `Adventures.Auth.LocalApi` Minimal API skeleton. Endpoint handlers remain thin and delegate to service-layer auth decisions.

## Deployment State

WebDeploy is validated for `BlogAI.Web`.

Current deployment facts:

- Target: `https://api.global-webnet.com`
- Publish profile: `apiglobalwebnet`
- Credential username used for validation: `billkrat-001`
- Password source: `$env:AdventuresOnTheEdgeDP`
- Password value: not recorded and must not be printed
- Publish profiles and user credential files remain uncommitted
- UNC repo path used for the successful retry: `\\Mac\Dev\vs-mcp-bridge`

Successful deploy command shape:

```powershell
dotnet publish ./BlogAI.Web/BlogAI.Web.csproj -c Release /p:PublishProfile=apiglobalwebnet /p:UserName="billkrat-001" /p:Password="[ENV_VAR_MASKED]"
```

Current deployed smoke validation:

- `https://api.global-webnet.com/`: `200`, guardrail rendered
- `https://api.global-webnet.com/local-dev`: `200`, guardrail rendered

For future deploys, confirm the UNC repo path is available, confirm `$env:AdventuresOnTheEdgeDP` is present without printing it, build `BlogAI.Web`, perform one explicit publish attempt when requested, and rerun deployed smoke checks.

## API-Client Parity State

The BlogAI API-client parity mode exists and is validated as a diagnostic-only path.

Current behavior:

- default `/local-dev` remains the in-process baseline
- `/local-dev?authPath=api-client` exercises the API-client diagnostic path
- API-client failures render diagnostic failure state rather than silently falling back
- parity mode does not imply production auth
- parity mode does not switch the default path away from in-process behavior

## Auth-Admin Boundary

Future auth-admin capability belongs to the auth service/API host boundary, not BlogAI Razor pages.

Expected future ownership:

- users
- tenants
- roles
- permissions
- audit views or audit query APIs
- application registrations
- policy metadata

BlogAI may eventually consume auth-admin APIs through a thin UI/service boundary, but it should not own shared identity administration or authorization policy.

## Explicitly Deferred

These remain out of scope until separate design gates approve them:

- production auth
- OAuth/OpenID/RBAC
- persistence/database
- cookies/session topology
- auth middleware
- BlogEngine.NET coupling
- tenant/user/role implementation
- production login UI
- real password storage
- external identity providers
- auth-admin UI or APIs

## Recommended Next Work

Recommended next work should remain design-gated and narrow:

- production-auth roadmap/design gates
- API-host parity harness hardening
- deployed-environment hardening
- eventual admin/auth API architecture

Do not make BlogAI Razor pages responsible for auth-admin behavior. Do not convert `/local-dev` or parity diagnostics into production auth without explicit production-auth, persistence, session, audit, and authorization-policy designs.

## Resume Guidance

Start from this handoff for the current BlogAI/Auth/WebDeploy architecture state. Then read the more specific evidence files only as needed:

- `docs/session-handoffs/2026-05-17-blogai-deployed-guardrail-validation.md`
- `docs/session-handoffs/2026-05-17-blogai-auth-api-client-parity-mode-validation.md`
- `docs/session-handoffs/2026-05-17-auth-api-admin-boundary-decision.md`
- `docs/session-handoffs/2026-05-17-adventures-auth-local-api-validation.md`
- `docs/session-handoffs/2026-05-17-blogai-webdeploy-validation.md`
