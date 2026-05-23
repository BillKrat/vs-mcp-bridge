# BlogAI Production Exposure Boundary Review

## Purpose

Record the production exposure boundary for the currently deployed `BlogAI.Web` smoke-test shell before adding more deployed behavior.

## Checkpoint

- Branch: `main`
- Expected sync: `main == origin/main`
- Starting HEAD: `540972d Document BlogAI WebDeploy validation`
- Runtime code deployed from: `e46eaf5 Add BlogAI auth API parity harness`
- Working tree expectation: clean
- Redeploy performed by this review: no

## Inputs Reviewed

- `AI_START.md`
- `AI_STOP.md`
- `docs/ARCHITECTURE.md`
- `docs/session-handoffs/2026-05-17-blogai-webdeploy-validation.md`
- `docs/session-handoffs/2026-05-17-blogai-auth-api-client-next-step-decision.md`
- `docs/blogai-local-auth-ui-integration-design.md`

## Current Exposure

Currently exposed at `https://api.global-webnet.com`:

- `/`: minimal `BlogAI.Web` shell/home route. This is a smoke-test surface only.
- `/local-dev`: local/dev diagnostic page that renders display-safe auth decision state and correlation metadata.
- `/local-dev?authPath=api-client`: explicit local/dev API-client diagnostic parity mode when selected by query string.

The deployed app does not expose production authentication, a real login UI, account management, API gateway behavior, persistent identity, or a BlogEngine.NET auth bridge.

## Diagnostic Surfaces

`/local-dev` remains a local/dev diagnostic surface even though it is reachable on the deployed host. Its allowed/denied states are development placeholders, not production authorization decisions.

`/local-dev?authPath=api-client` is diagnostic-only. It must not become the default auth path without separate parity evidence and a separate implementation decision.

## Public Reachability Decision

`/local-dev` should not remain publicly reachable as a long-term state.

For the immediate smoke-test shell phase, it may remain reachable only as a temporary diagnostic surface. More deployed behavior should not be added while this route is publicly exposed without clearer guardrails. Public reachability of `/local-dev` does not approve production auth, real login behavior, or user-facing protected content.

## Immediate Guardrails

Before adding more deployed behavior:

- Add a visible deployed-environment banner or guardrail that identifies the app as a smoke-test shell.
- Clearly label `/local-dev` as a local/dev diagnostic surface if it remains reachable.
- Keep displayed decision and correlation data secret-free.
- Keep Razor components thin and service-driven.
- Keep `/local-dev` on the in-process baseline by default.
- Require a separate decision before adding production auth, protected user behavior, or persistent identity state.

## Explicitly Out Of Scope

- Production authentication
- OAuth/OpenID
- RBAC
- Persistence/database-backed identity
- BlogEngine.NET auth coupling
- Cookies/session topology
- Auth middleware
- Real login UI
- Account management
- Production identity provider integration

## Decision

Keep deployed `BlogAI.Web` as a smoke-test shell only for now.

Do not treat deployed `/local-dev` as production auth. Do not promote API-client usage or route-level placeholder behavior based only on public deployment of the diagnostic page.

## Smallest Next Implementation Slice

Add a simple deployed-environment banner or guardrail, not auth.

The smallest useful slice should:

- Display a smoke-test/local-dev diagnostic notice on `/` and `/local-dev`.
- Avoid production auth, login UI, middleware, cookies, persistence, and deployment redesign.
- Preserve the current in-process `/local-dev` baseline.
- Build and smoke test `/` and `/local-dev` after the change.

