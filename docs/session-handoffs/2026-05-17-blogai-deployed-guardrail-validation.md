# BlogAI Deployed Guardrail Validation

## Purpose

Capture durable validation evidence for the `BlogAI.Web` deployed-environment guardrail banner.

## Checkpoint

- Branch: `main`
- Expected sync at start: `main == origin/main`
- Starting HEAD: `6a6add2 Document Auth API admin boundary`
- Runtime behavior changed by this slice: no
- Redeploy performed: attempted once, failed with authorization error

## Inputs Reviewed

- `AI_START.md`
- `docs/session-handoffs/2026-05-17-blogai-production-exposure-boundary-review.md`
- `docs/session-handoffs/2026-05-17-blogai-webdeploy-validation.md`

## Evidence Artifacts

- `artifacts/logs/blogai-deployed-guardrail-trace-20260517.log`
- `artifacts/logs/blogai-deployed-guardrail-trace-20260517.metadata.json`
- `docs/diagrams/blogai-deployed-guardrail-trace-20260517.mmd`
- `docs/session-handoffs/2026-05-17-blogai-deployed-guardrail-validation.md`

## Validation Summary

Build and tests passed:

- `dotnet build ./BlogAI.Web/BlogAI.Web.csproj`: passed
- `dotnet test ./VsMcpBridge.Shared.Tests/VsMcpBridge.Shared.Tests.csproj`: passed, 306 tests

Local smoke passed:

- `http://localhost:5255/`: `200`, guardrail rendered
- `http://localhost:5255/local-dev`: `200`, guardrail rendered
- `/local-dev` rendered diagnostic-only / not-production-auth text locally
- raw credential/token/secret/header/password sentinels did not render locally

Deployed smoke did not pass the guardrail requirement:

- `https://api.global-webnet.com/`: `200`, guardrail not rendered
- `https://api.global-webnet.com/local-dev`: `200`, guardrail not rendered
- raw credential/token/secret/header/password sentinels did not render on deployed responses

## Deployment Attempt

The deployed site did not include the guardrail, so one WebDeploy attempt was allowed by the slice constraints.

- Publish profile: `apiglobalwebnet`
- Command shape: `dotnet publish ./BlogAI.Web/BlogAI.Web.csproj -c Release /p:PublishProfile=apiglobalwebnet`
- Secret values printed: no
- Publish profiles committed: no
- Exit code: `1`
- Non-secret error: `MSDEPLOY ERROR_USER_UNAUTHORIZED`; remote server returned `401 Unauthorized`
- Retries: none

## Decision

Local guardrail behavior is validated.

Deployed guardrail behavior is not validated yet. The deployed site still appears to be serving the previous build because WebDeploy authorization failed.

## Resume Guidance

Before claiming deployed guardrail validation, restore a secret-safe WebDeploy credential path and rerun one deploy attempt. After a successful deploy, repeat the deployed smoke checks:

- `https://api.global-webnet.com/`
- `https://api.global-webnet.com/local-dev`

Do not add production auth, OAuth/OpenID/RBAC, persistence/database, cookies/session topology, auth middleware, real login UI, or BlogEngine.NET coupling as part of the deployment retry.
