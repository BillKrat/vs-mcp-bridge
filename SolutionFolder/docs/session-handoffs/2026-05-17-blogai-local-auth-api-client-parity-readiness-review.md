# BlogAI Local Auth API Client Parity Readiness Review

## Purpose

Review whether `/local-dev` should temporarily exercise the local/dev BlogAI auth API client path and define the parity gates before any runtime code is added.

## Checkpoint

- Branch: `main`
- Expected sync: `main == origin/main`
- Head before this review: `d5fef59 Add BlogAI local auth API client boundary trace artifacts`
- Working tree expectation before this review: clean

## Inputs Reviewed

- `AI_START.md`
- `SolutionFolder/docs/AI_STOP.md`
- `SolutionFolder/docs/ARCHITECTURE.md`
- `SolutionFolder/docs/session-handoffs/2026-05-17-blogai-local-auth-api-client-boundary-validation.md`
- `SolutionFolder/docs/session-handoffs/2026-05-17-blogai-local-auth-api-client-readiness-review.md`
- `SolutionFolder/docs/blogai-local-auth-ui-integration-design.md`
- `SolutionFolder/docs/adventures-auth-local-api-boundary-design.md`

## Review Decision

Ready to add an opt-in local/dev parity mode that exercises the API client path, but not ready to switch `/local-dev` to the API client by default.

The next implementation should compare the current in-process path against the API client path in a diagnostic-only mode. The current in-process path remains the baseline because it is already validated and does not require coordinating a second local process.

## Readiness Areas

| Area | Status | Rationale | Follow-up |
| --- | --- | --- | --- |
| Current in-process baseline | Pass | `/local-dev` already proves denied/hidden and allowed/shown protected placeholder behavior with correlation metadata and no rendered secrets. | Keep this as the default path and parity baseline. |
| API client boundary | Pass | `IBlogAiLocalAuthApiClient` exists, is injectable, and has durable boundary validation. | Exercise it only behind an explicit local/dev diagnostic mode. |
| Local API host | Pass | `Adventures.Auth.LocalApi` exists as a local/dev Minimal API skeleton with documented endpoints. | Future parity mode may require the local API host to be running. |
| Default-path safety | Pass | Current evidence shows `/local-dev` is not switched to the API client by default. | Preserve this until parity evidence exists. |
| UI scope | Pass | Razor should render display-safe models only and not own auth decisions. | Any selector must be diagnostic, local/dev, and non-secret-bearing. |
| Test/evidence readiness | Pass | Existing traces define the expected evidence pattern. | Capture durable parity trace artifacts in the implementation slice. |
| Production-auth risk | Pass | Stop conditions are clear and remain enforceable. | Stop if the slice needs production auth, middleware, cookies, persistence, or real login behavior. |

## Answers

### Should `/local-dev` temporarily exercise the API client path?

Yes, but only as an explicit local/dev diagnostic parity mode.

The value is proving that BlogAI can consume `Adventures.Auth.LocalApi` over the API-shaped boundary and still produce the same display-safe allow/deny outcomes as the in-process baseline.

### Should it be behind an explicit local/dev selector or diagnostic mode?

Yes.

The API-backed path should not silently replace the current default. It should be behind an explicit local/dev diagnostic selector or mode, such as:

- an internal display-mode option with default `InProcess`
- a local/dev-only query option such as `?authSource=api` if implemented safely
- a diagnostic comparison mode that renders in-process and API-backed results side by side

The selector must not accept raw credentials, tokens, headers, passwords, cookies, or secret-bearing values from the UI. It should only select the source of the already-defined local/dev diagnostic path.

## Required Parity Checks

A future implementation must compare the API client path against the current in-process path for:

- unauthenticated local request denied
- denied protected placeholder hidden
- development-authenticated local request allowed
- allowed protected placeholder shown
- invalid local session denied if included
- `CorrelationId` preserved or generated safely
- `RequestId` preserved or generated safely
- `AuthDecisionId` preserved or generated safely
- `ClientApplication=BlogAI`
- `Environment=LocalDevelopment`
- safe principal placeholder display only
- no raw development credential rendered or logged
- no raw token, secret, authorization header, cookie, password, API key, or credential-bearing body rendered or logged
- API-backed response maps into the same display-safe model shape as the in-process response
- failure to reach the local API is represented as a diagnostic local/dev failure, not a production auth failure or silent fallback
- `/local-dev` default still uses the in-process path unless the slice explicitly records and validates a different default

## Stop Conditions

Stop immediately if the implementation requires or introduces:

- production deployment or `api.global-webnet.com` runtime configuration
- OAuth/OpenID, external identity providers, social login, or RBAC
- persistence/database-backed users, credentials, sessions, or tokens
- real cookie/session topology
- production login UI, password forms, account management, or user self-service flows
- BlogEngine.NET auth integration or runtime coupling
- auth middleware in `BlogAI.Web`
- auth decision logic in Razor components
- auth decision logic directly in Minimal API endpoint lambdas
- UI input for raw credentials, tokens, headers, passwords, cookies, API keys, or secret-bearing request bodies
- hidden fallback from API client to in-process auth without explicit evidence
- switching `/local-dev` to the API client by default before parity evidence is captured

## Smallest Future Implementation Slice

The smallest future implementation should:

- add an explicit local/dev diagnostic mode for API-client parity
- keep the existing in-process path as the default
- keep Razor thin and presentation-only
- keep auth decisions in `IBlogAiAuthConsumerService`, `IBlogAiLocalAuthApiClient`, and `AdventuresAuth` services
- map both paths into display-safe local auth status models
- avoid raw credential/token/secret/header/password UI input
- require the local API host to be running only for the API-backed diagnostic mode
- show or record whether API-backed parity passed or failed
- capture durable trace artifacts proving parity and no-secret rendering

Suggested future slice name:

- `Add BlogAI local auth API client parity mode`

## Validation

This review is docs-only. Required validation:

- `git diff --check`

## Resume Guidance

Future sessions should resume from this handoff before wiring `/local-dev` to exercise the API client path.

Do not infer approval for production authentication, production deployment, real login UI, OAuth/OpenID/RBAC, persistence/database, BlogEngine.NET auth coupling, cookies/session topology, auth middleware, or a default-path switch from this readiness review.
