# BlogAI Deployed Guardrail Validation

## Purpose

Capture durable validation evidence for the `BlogAI.Web` deployed-environment guardrail banner.

## Checkpoint

- Branch: `main`
- Expected sync at start: `main == origin/main`
- Starting HEAD: `6a6add2 Document Auth API admin boundary`
- Successful retry checkpoint: `1441d5e Add BlogAI deployed guardrail validation evidence`
- Runtime behavior changed by this slice: no
- Redeploy performed: one initial failed attempt, then one explicit retry succeeded after the deployment password environment variable became available

## Inputs Reviewed

- `AI_START.md`
- `SolutionFolder/docs/session-handoffs/2026-05-17-blogai-production-exposure-boundary-review.md`
- `SolutionFolder/docs/session-handoffs/2026-05-17-blogai-webdeploy-validation.md`

## Evidence Artifacts

- `SolutionFolder/artifacts/logs/blogai-deployed-guardrail-trace-20260517.log`
- `SolutionFolder/artifacts/logs/blogai-deployed-guardrail-trace-20260517.metadata.json`
- `SolutionFolder/docs/diagrams/blogai-deployed-guardrail-trace-20260517.mmd`
- `SolutionFolder/docs/session-handoffs/2026-05-17-blogai-deployed-guardrail-validation.md`

## Validation Summary

Build and tests passed:

- `dotnet build ./BlogAI.Web/BlogAI.Web.csproj`: passed
- `dotnet test ./VsMcpBridge.Shared.Tests/VsMcpBridge.Shared.Tests.csproj`: passed, 306 tests

Local smoke passed:

- `http://localhost:5255/`: `200`, guardrail rendered
- `http://localhost:5255/local-dev`: `200`, guardrail rendered
- `/local-dev` rendered diagnostic-only / not-production-auth text locally
- raw credential/token/secret/header/password sentinels did not render locally

Initial deployed smoke did not pass the guardrail requirement:

- `https://api.global-webnet.com/`: `200`, guardrail not rendered
- `https://api.global-webnet.com/local-dev`: `200`, guardrail not rendered
- raw credential/token/secret/header/password sentinels did not render on deployed responses

Deployed smoke now passes after the explicit WebDeploy retry:

- `https://api.global-webnet.com/`: `200`, guardrail rendered
- `https://api.global-webnet.com/local-dev`: `200`, guardrail rendered

## Deployment Attempt

The deployed site did not include the guardrail, so one WebDeploy attempt was allowed by the slice constraints.

- Publish profile: `apiglobalwebnet`
- Command shape: `dotnet publish ./BlogAI.Web/BlogAI.Web.csproj -c Release /p:PublishProfile=apiglobalwebnet`
- Secret values printed: no
- Publish profiles committed: no
- Exit code: `1`
- Non-secret error: `MSDEPLOY ERROR_USER_UNAUTHORIZED`; remote server returned `401 Unauthorized`
- Retries: none

## Deployment Retry

One explicit WebDeploy retry was performed using the UNC repo path and explicit credentials sourced from the environment variable.

- Repo path used: `\\Mac\Dev\vs-mcp-bridge`
- Publish profile: `apiglobalwebnet`
- Command shape: `dotnet publish ./BlogAI.Web/BlogAI.Web.csproj -c Release /p:PublishProfile=apiglobalwebnet /p:UserName="billkrat-001" /p:Password="[ENV_VAR_MASKED]"`
- Password source: `$env:AdventuresOnTheEdgeDP`
- Secret values printed: no
- Exit code: `0`
- Result: `Publish Succeeded`
- Non-secret warnings: 11 nullable warnings in `VsMcpBridge.Shared`
- Warning files: `LogToolWindowPresenter.cs`, `ProposalManager.cs`
- Warning codes: `CS8601`, `CS8602`, `CS8603`, `CS8604`
- Errors: `0`
- Retries: none after the successful retry

## Decision

Local guardrail behavior is validated.

Deployed guardrail behavior is now validated. The deployed site returned HTTP 200 for both smoke URLs and rendered the guardrail on both responses after the successful WebDeploy retry.

## Resume Guidance

For future deploy validation, keep using a secret-safe WebDeploy credential path and repeat deployed smoke checks after any publish:

- `https://api.global-webnet.com/`
- `https://api.global-webnet.com/local-dev`

Do not add production auth, OAuth/OpenID/RBAC, persistence/database, cookies/session topology, auth middleware, real login UI, or BlogEngine.NET coupling as part of the deployment retry.
