# BlogAI Auth API Client Parity Mode Validation

## Purpose

Capture durable evidence that the explicit local/dev API-client parity mode works without changing the default `/local-dev` in-process baseline.

## Inputs Inspected

- `AI_START.md`
- `docs/session-handoffs/2026-05-17-blogai-local-auth-api-client-parity-readiness-review.md`
- `docs/blogai-local-auth-ui-integration-design.md`
- `docs/adventures-auth-local-api-boundary-design.md`
- `BlogAI.Web/Auth`
- `BlogAI.Web/Components/Pages/LocalDev.razor`
- `Adventures.Auth.LocalApi`

## Evidence Artifacts

- `artifacts/logs/blogai-auth-api-client-parity-mode-trace-20260517.log`
- `artifacts/logs/blogai-auth-api-client-parity-mode-trace-20260517.metadata.json`
- `docs/diagrams/blogai-auth-api-client-parity-mode-trace-20260517.mmd`

## Observed Results

| Check | Result | Evidence |
| --- | --- | --- |
| `GET /` | Pass | Returned HTTP 200. |
| `GET /local-dev` | Pass | Returned HTTP 200 and rendered the in-process baseline label. |
| Default auth path | Pass | `/local-dev` still uses the in-process baseline. |
| Baseline denied state | Pass | Denied local request rendered the protected placeholder as hidden. |
| Baseline allowed state | Pass | Development-authenticated local request rendered the protected placeholder as shown. |
| `GET /local-dev?authPath=api-client` | Pass | Returned HTTP 200 and rendered the API-client diagnostic label. |
| API-client denied state | Pass | API-client diagnostic path rendered unauthenticated denied/hidden output. |
| API-client allowed state | Pass | API-client diagnostic path rendered development-authenticated allowed/shown output. |
| No silent fallback | Pass | API-client diagnostic output included the no-fallback diagnostic note and did not render the default in-process note. |
| Correlation metadata | Pass | `CorrelationId`, `RequestId`, and `AuthDecisionId` rendered in both paths. |
| Sensitive rendering | Pass | Raw credential, token, secret, authorization header, bearer, and password sentinel categories did not render. |
| Scope guard | Pass | No production deployment, OAuth/OpenID/RBAC, persistence/database, BlogEngine.NET coupling, real login UI, cookies/session topology, auth middleware, or default-path switch was introduced. |

## Interpretation

The parity mode is now validated as an explicit local/dev diagnostic path:

- `/local-dev` remains the stable in-process baseline.
- `/local-dev?authPath=api-client` exercises `IBlogAiLocalAuthApiClient`.
- The API-client path reaches `Adventures.Auth.LocalApi` when the local API host is running.
- The API-client path maps responses into the same display-safe status model used by the baseline UI.
- Failure to reach the API client is designed to render a diagnostic failure state, not fall back silently to the in-process path.

This proves the API-shaped boundary can be exercised from BlogAI without turning it into production authentication.

## Validation

- `git diff --check`: passed with line-ending normalization warnings only.
- `dotnet build ./BlogAI.Web/BlogAI.Web.csproj`: passed with 0 warnings and 0 errors.
- `dotnet test ./VsMcpBridge.Shared.Tests/VsMcpBridge.Shared.Tests.csproj`: passed, 304 tests.
- Local smoke:
  - `/`: HTTP 200.
  - `/local-dev`: HTTP 200, in-process baseline.
  - `/local-dev?authPath=api-client`: HTTP 200, explicit API-client diagnostic path.

## Resume Guidance

Future sessions can use this validation as the checkpoint before deciding whether to add broader API-client trace coverage or keep the parity path as a diagnostic-only surface.

Do not infer approval for production deployment, OAuth/OpenID/RBAC, persistence/database, BlogEngine.NET auth coupling, real login UI, cookies/session topology, auth middleware, or switching `/local-dev` to the API client by default from this validation.
