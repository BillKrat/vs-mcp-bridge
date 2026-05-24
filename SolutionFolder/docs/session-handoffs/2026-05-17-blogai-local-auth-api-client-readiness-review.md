# BlogAI Local Auth API Client Readiness Review

## Purpose

Review whether the repo is ready to add a local/dev-only BlogAI client boundary for `Adventures.Auth.LocalApi`, without adding runtime code in this slice.

## Checkpoint

- Branch: `main`
- Expected sync: `main == origin/main`
- Head before this review: `a5696af Add BlogAI route protected placeholder trace artifacts`
- Working tree expectation before this review: clean

## Inputs Reviewed

- `AI_START.md`
- `SolutionFolder/docs/AI_STOP.md`
- `SolutionFolder/docs/ARCHITECTURE.md`
- `SolutionFolder/docs/session-handoffs/2026-05-17-blogai-route-protected-placeholder-validation.md`
- `SolutionFolder/docs/blogai-local-auth-ui-integration-design.md`
- `SolutionFolder/docs/blogai-minimal-auth-consumer-prototype-plan.md`
- `SolutionFolder/docs/adventures-auth-local-api-boundary-design.md`

## Review Decision

Ready for a narrow local/dev BlogAI auth API client boundary, but not as a replacement for the current `/local-dev` in-process path by default.

The repo has enough evidence to justify a small future client-wiring slice if the goal is to validate the API-shaped host boundary. The current `/local-dev` page should continue using in-process `IBlogAiAuthConsumerService` until an explicit implementation slice adds a local API client seam and proves equivalent allow/deny, correlation, and redaction behavior.

## Readiness Areas

| Area | Status | Rationale | Follow-up |
| --- | --- | --- | --- |
| Current UI behavior | Pass | `/local-dev` already proves denied/hidden and allowed/shown protected placeholder behavior through a display-safe service model. | Preserve this as the baseline when adding client wiring. |
| Local API host boundary | Pass | `Adventures.Auth.LocalApi` exists as a local/dev Minimal API skeleton with documented endpoints and validation evidence. | Future wiring should target only local/dev endpoints. |
| Client boundary need | Pass | A client seam would prove BlogAI can consume the API-shaped boundary instead of only the in-process service. | Add only if the next goal is host-boundary validation. |
| Default `/local-dev` path | Pass | It is not yet necessary to switch the visible page to API-backed behavior by default. | Keep in-process behavior unless the implementation slice defines an explicit local API client mode. |
| Correlation expectations | Pass | Existing docs require `CorrelationId`, `RequestId`, and `AuthDecisionId` preservation or generation across the boundary. | Client wiring must prove these values flow through the API response. |
| Redaction expectations | Pass | Existing traces prove raw credentials/tokens/secrets are not rendered. | Client wiring must add equivalent trace evidence and avoid logging request secrets. |
| Auth decision ownership | Pass | Auth decisions remain in `AdventuresAuth` and BlogAI consumer services; Razor renders display-safe models only. | API client must not move decision logic into Razor or endpoint/client glue. |
| Non-goals | Pass | Production deployment, OAuth/OpenID/RBAC, persistence/database, BlogEngine.NET auth coupling, real login UI, cookies/session topology, and auth middleware remain out of scope. | Treat any need for these as a stop condition. |

## Answers

### Is it time to add a BlogAI local API client boundary?

Yes, if the next implementation goal is specifically to validate BlogAI consuming `Adventures.Auth.LocalApi` through a local/dev client seam.

No, if the goal is only to improve `/local-dev` display behavior. The current in-process path is simpler, validated, and still appropriate for the UI placeholder baseline.

### Should `/local-dev` continue using in-process `IBlogAiAuthConsumerService` for now?

Yes.

`/local-dev` should continue using the in-process service as the default path until a future slice adds an explicit local/dev API client boundary. The future client should be introduced behind a small interface or service seam so the page still renders only a display-safe model.

### What exact stop conditions prevent this from becoming production auth?

Stop immediately if the slice requires or introduces:

- production deployment or `api.global-webnet.com` runtime configuration
- OAuth/OpenID, social login, external identity providers, or RBAC
- persistence/database-backed users, sessions, credentials, or tokens
- real cookie/session topology
- production login UI, password forms, account management, or user self-service flows
- BlogEngine.NET auth integration or runtime coupling
- auth middleware in `BlogAI.Web`
- auth decision logic in Razor components
- auth decision logic directly in Minimal API endpoint lambdas
- logging or rendering raw credentials, tokens, cookies, authorization headers, passwords, API keys, or secret-bearing request bodies
- hidden fallback from API client to in-process auth without explicit evidence

## Smallest Future Implementation Slice

If approved, the smallest implementation slice should:

- add a local/dev-only BlogAI API client service/interface for `Adventures.Auth.LocalApi`
- keep `/local-dev` Razor presentation-only
- keep `IBlogAiLocalAuthStatusService` as the UI-facing display boundary or add an equivalent narrow abstraction
- call only local/dev endpoints:
  - `POST /auth/login`
  - `POST /auth/validate`
  - optionally `GET /auth/me` and `POST /auth/logout` if needed for parity evidence
- preserve or generate `CorrelationId`, `RequestId`, `AuthDecisionId`, `ClientApplication=BlogAI`, and `Environment=LocalDevelopment`
- map API responses into the existing display-safe local auth status model
- keep the in-process path available as the current baseline unless the slice explicitly replaces it
- add tests or trace evidence proving:
  - unauthenticated local request denied/hidden
  - development-authenticated local request allowed/shown
  - invalid local session denied
  - correlation metadata preserved
  - raw credential/token/secret/header/password values absent from rendered output, logs, and artifacts
  - no production auth, middleware auth, persistence, deployment, OAuth/OpenID/RBAC, or BlogEngine.NET coupling

## Validation

This review is docs-only. Required validation:

- `git diff --check`

## Resume Guidance

Future sessions should resume from this handoff before adding BlogAI-to-`Adventures.Auth.LocalApi` client wiring.

The next implementation slice should remain local/dev only and should be named narrowly, for example:

- `Add BlogAI local AdventuresAuth API client boundary`

Do not infer approval for production authentication or deployment from this readiness review.
