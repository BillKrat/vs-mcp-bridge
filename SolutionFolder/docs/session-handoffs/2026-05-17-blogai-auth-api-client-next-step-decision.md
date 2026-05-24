# BlogAI Auth API Client Next Step Decision

## Purpose

Record the decision after explicit local/dev API-client parity mode validation and define the next safe slice without changing runtime behavior.

## Checkpoint

- Branch: `main`
- Expected sync: `main == origin/main`
- Head before this review: `8e63667 Add BlogAI auth API client parity trace artifacts`
- Working tree expectation before this review: clean

## Inputs Reviewed

- `AI_START.md`
- `SolutionFolder/docs/AI_STOP.md`
- `SolutionFolder/docs/ARCHITECTURE.md`
- `SolutionFolder/docs/session-handoffs/2026-05-17-blogai-auth-api-client-parity-mode-validation.md`
- `SolutionFolder/docs/session-handoffs/2026-05-17-blogai-local-auth-api-client-parity-readiness-review.md`
- `SolutionFolder/docs/blogai-local-auth-ui-integration-design.md`
- `SolutionFolder/docs/adventures-auth-local-api-boundary-design.md`

## Decision

Keep `/local-dev` on the in-process `IBlogAiAuthConsumerService` baseline by default.

Keep `/local-dev?authPath=api-client` as an explicit local/dev diagnostic-only mode for now.

The next useful slice is an API-host parity test harness, not a default-path switch.

## Answers

### Should parity mode remain diagnostic-only?

Yes.

The parity mode proves the API-shaped boundary can be exercised safely from BlogAI, but it still depends on coordinating a second local process. It is useful as validation and diagnostics, not as the default UI path yet.

### Is the next useful slice an API-host parity test harness?

Yes.

The current evidence is manual smoke plus durable trace artifacts. The next improvement should make API-host parity repeatable without relying on reconstructed manual steps. That harness should remain local/dev only and prove the API-backed path against the in-process baseline with the same allow/deny, placeholder, correlation, redaction, and no-fallback expectations.

### Should `/local-dev` default path remain in-process?

Yes.

The in-process path remains the stable baseline because it is already validated, deterministic, does not require a second host process, and preserves the current local/dev UI behavior. Do not switch the default unless later evidence makes the API-client path equally repeatable and operationally simpler.

### What evidence is required before promoting API-client usage?

Promotion from diagnostic-only to broader API-client usage requires all of the following:

- repeatable local/dev API-host parity harness
- automated or scripted evidence for `/`, `/local-dev`, and API-backed parity route behavior
- unauthenticated denied and protected placeholder hidden
- development-authenticated allowed and protected placeholder shown
- invalid session or invalid credential denied where included
- `CorrelationId`, `RequestId`, and `AuthDecisionId` preserved or generated safely
- safe `ClientApplication=BlogAI` and `Environment=LocalDevelopment` metadata
- no raw credential, token, secret, authorization header, bearer, cookie, password, API key, or credential-bearing body rendered or logged
- explicit API-client failure state with no silent fallback
- no production deployment, OAuth/OpenID/RBAC, persistence/database, BlogEngine.NET coupling, real login UI, cookies/session topology, or auth middleware
- durable trace artifacts that can be read without chat history

Even with that evidence, promotion should be a separate explicit decision slice.

## Stop Conditions

Stop immediately if the next slice requires or introduces:

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
- hidden fallback from API client to in-process auth
- switching `/local-dev` to the API client by default
- broad authentication platform scope beyond the local/dev parity proof

## Smallest Future Implementation Slice

Add a local/dev-only API-host parity test harness.

The slice should:

- keep `/local-dev` defaulting to the in-process baseline
- exercise the API-client path explicitly
- start or host the local API boundary only in a local/dev test context
- compare in-process and API-backed display-safe outcomes
- verify denied/hidden and allowed/shown behavior
- verify correlation metadata and no-secret rendering/logging
- verify API-client failure is explicit and does not silently fall back
- produce durable trace evidence

Do not make the API-client path the default in that slice.

## Resume Guidance

Future sessions should resume here before adding more BlogAI auth API-client wiring.

Recommended next slice name:

- `Add BlogAI auth API host parity test harness`

Preserve the current hierarchy:

- in-process `/local-dev` remains the baseline
- API-client mode remains diagnostic-only
- future API-host parity work stays local/dev and evidence-driven
