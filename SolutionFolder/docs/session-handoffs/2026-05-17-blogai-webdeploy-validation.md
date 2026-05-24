# BlogAI WebDeploy Validation

## Purpose

Capture the first successful WebDeploy validation for `BlogAI.Web` to `https://api.global-webnet.com` without recording secret values.

## Checkpoint

- Branch: `main`
- Expected sync after validation: `main == origin/main`
- Last code commit before deployment: `e46eaf5 Add BlogAI auth API parity harness`
- Working tree expectation before handoff: clean

## Deployment Summary

- Target app: `BlogAI.Web`
- Target URL: `https://api.global-webnet.com`
- WebDeploy profile name: `apiglobalwebnet`
- WebDeploy username: `billkrat-001`
- Password source: environment variable `AdventuresOnTheEdgeDP`
- Password value: not recorded
- Publish profiles committed: no

## Validation Before Deploy

- `git status --short --branch`: clean at `main...origin/main`
- `dotnet build ./BlogAI.Web/BlogAI.Web.csproj`: passed with 0 warnings and 0 errors

## Deploy Command Shape

Secret values were supplied through environment variable expansion and must not be printed or committed.

```powershell
dotnet publish ./BlogAI.Web/BlogAI.Web.csproj -c Release /p:PublishProfile=apiglobalwebnet /p:UserName="billkrat-001" /p:Password="[MASKED_ENV_VAR]"
```

## Deploy Result

- Deploy exit code: `0`
- Result: `Publish Succeeded`
- WebDeploy changes:
  - 5 files updated
  - 0 files added
  - 0 files deleted

Updated files reported by WebDeploy:

- `BlogAI.Web.deps.json`
- `BlogAI.Web.dll`
- `BlogAI.Web.exe`
- `BlogAI.Web.pdb`
- `web.config`

## Smoke Test

- `https://api.global-webnet.com/`: `200 OK`
- `https://api.global-webnet.com/local-dev`: `200 OK`

## Secret Safety

- The publish profile and `.pubxml.user` credential file remained local/untracked.
- The password value was not printed in the handoff.
- The deploy command should continue to use the environment variable or another secure local secret source.
- Do not commit publish profiles or `.pubxml.user` files.

## Final State

- `git status --short --branch`: clean at `main...origin/main`
- No runtime auth behavior was changed during deployment validation.
- No production auth, OAuth/OpenID/RBAC, persistence/database, BlogEngine.NET auth coupling, real login UI, cookies/session topology, or auth middleware was added.

## Resume Guidance

Future sessions can treat `https://api.global-webnet.com` as having the current `BlogAI.Web` shell and local/dev `/local-dev` diagnostic surface deployed as of this validation.

Before any future deployment, rerun build validation, verify publish-profile secret safety, and avoid printing credentials.
