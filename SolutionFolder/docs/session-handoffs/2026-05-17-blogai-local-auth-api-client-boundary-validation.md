# BlogAI Local Auth API Client Boundary Validation

## Purpose

Capture durable evidence that `BlogAI.Web` has a local/dev-only auth API client boundary for `Adventures.Auth.LocalApi` while preserving the existing `/local-dev` in-process default path.

## Inputs Inspected

- `AI_START.md`
- `SolutionFolder/docs/session-handoffs/2026-05-17-blogai-local-auth-api-client-readiness-review.md`
- `SolutionFolder/docs/adventures-auth-local-api-boundary-design.md`
- `SolutionFolder/docs/blogai-local-auth-ui-integration-design.md`
- `SolutionFolder/docs/blogai-minimal-auth-consumer-prototype-plan.md`
- `BlogAI.Web/Auth`
- `BlogAI.Web/Components/Pages/LocalDev.razor`

## Evidence Artifacts

- `SolutionFolder/artifacts/logs/blogai-local-auth-api-client-boundary-trace-20260517.log`
- `SolutionFolder/artifacts/logs/blogai-local-auth-api-client-boundary-trace-20260517.metadata.json`
- `SolutionFolder/docs/diagrams/blogai-local-auth-api-client-boundary-trace-20260517.mmd`

## Observed Results

| Check | Result | Evidence |
| --- | --- | --- |
| `git status --short --branch` | Pass | `main` matched `origin/main` before edits. |
| `dotnet build ./BlogAI.Web/BlogAI.Web.csproj` | Pass | Build succeeded. |
| `dotnet test ./VsMcpBridge.Shared.Tests/VsMcpBridge.Shared.Tests.csproj` | Pass | 304 tests passed. |
| `GET /` | Pass | Returned HTTP 200. |
| `GET /local-dev` | Pass | Returned HTTP 200. |
| API client boundary exists | Pass | `IBlogAiLocalAuthApiClient` and `BlogAiLocalAuthApiClient` exist under `BlogAI.Web/Auth`. |
| API client boundary is injectable | Pass | `AddHttpClient<IBlogAiLocalAuthApiClient, BlogAiLocalAuthApiClient>` is registered in `BlogAiLocalAuthStatusServiceExtensions`. |
| `/local-dev` uses in-process path by default | Pass | `LocalDev.razor` injects `IBlogAiLocalAuthStatusService`; `BlogAiLocalAuthStatusService` uses `IBlogAiAuthConsumerService`. |
| `/local-dev` not switched to API client | Pass | No default UI path dependency on `IBlogAiLocalAuthApiClient` was introduced. |
| Existing protected placeholder behavior preserved | Pass | Rendered hidden denied state and shown development-authenticated state. |
| Correlation metadata rendered | Pass | Rendered `CorrelationId`, `RequestId`, and `AuthDecisionId`. |
| Raw development credential not rendered | Pass | Rendered output did not contain `local-dev-credential`. |
| Raw token/secret/header/password sentinels not rendered | Pass | Rendered output did not contain checked raw token, secret, authorization header, bearer, or password sentinels. |
| No production auth scope | Pass | No production deployment, OAuth/OpenID/RBAC, persistence/database, BlogEngine.NET auth coupling, real login UI, cookies/session topology, auth middleware, or UI default-path switch was added. |

## Interpretation

The local/dev BlogAI auth API client boundary is present but intentionally dormant for the visible UI path:

- `IBlogAiLocalAuthApiClient` defines the local/dev client seam.
- `BlogAiLocalAuthApiClient` can call local `/auth/login`, `/auth/logout`, `/auth/me`, and `/auth/validate`.
- The client uses display-safe request/decision/principal models under `BlogAI.Web/Auth`.
- Dependency injection can resolve the client through typed `HttpClient` registration.
- `/local-dev` still uses the existing in-process display path by default.

This preserves the current stable `/local-dev` baseline while making the future API-backed parity slice possible.

## Resume Guidance

Future sessions can use this validation as the evidence checkpoint before wiring BlogAI to `Adventures.Auth.LocalApi`.

The next implementation slice, if approved, should be narrow and explicit:

- add an API-backed local/dev parity harness or opt-in mode
- keep `/local-dev` Razor presentation-only
- prove the API-backed path returns the same denied/hidden and allowed/shown display outcomes
- capture trace artifacts before making the API-backed path the default

Do not infer approval for production deployment, production login UI, OAuth/OpenID/RBAC, persistence/database, BlogEngine.NET auth coupling, cookies/session topology, auth middleware, or switching `/local-dev` to the API client by default from this validation.
