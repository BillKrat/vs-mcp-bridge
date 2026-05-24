# Local-Only File Recovery Validation

## Checkpoint

- branch: `main`
- starting HEAD: `5035ac8 Add local-only file inventory and templates`
- starting state: `main == origin/main`, working tree clean
- scope: docs-only recovery simulation for local-only file inventory and safe templates

## Inputs Reviewed

- `AI_START.md`
- `SolutionFolder/docs/local-only-files.md`
- `SolutionFolder/docs/session-handoffs/2026-05-24-local-only-file-template-readiness-review.md`
- `README.md`
- `.gitignore`
- `BlogAI.Web/Properties/PublishProfiles/apiglobalwebnet.pubxml.template`

No real credential-bearing publish profile contents were opened or printed.

## Recovery Simulation Result

Result: pass.

A developer rebuilding from a fresh clone should be able to identify the local-only files and secret sources needed for the current BlogAI/WebDeploy path:

- `BlogAI.Web/Properties/PublishProfiles/apiglobalwebnet.pubxml`
- `BlogAI.Web/Properties/PublishProfiles/apiglobalwebnet.pubxml.user`
- `$env:AdventuresOnTheEdgeDP`
- `%LocalAppData%\VsMcpBridge\appsettings.user.json`

The inventory explains:

- which ignored local files are required or likely required
- which tracked template exists
- which items may contain secrets
- how to recreate each item
- how to validate setup without printing secrets
- what must not be committed
- how to recover or regenerate local-only state

`README.md` points to `SolutionFolder/docs/local-only-files.md` instead of duplicating the full inventory.

`AI_START.md` includes the inventory in the optional grounding docs, which is appropriate because it is operational context, not mandatory startup reading for every session.

## Ignore And Template Check

Observed ignore behavior:

- `.gitignore` ignores real `.pubxml` files through `*.pubxml`
- `.gitignore` ignores real `.pubxml.user` files through `*.user`
- `.gitignore` ignores `.env`, `.pfx`, and `.publishsettings` files
- `BlogAI.Web/Properties/PublishProfiles/apiglobalwebnet.pubxml.template` is trackable

The tracked template is placeholder-only and contains no deployment credential values. It intentionally keeps `_SavePWD` false and directs developers to copy it to the ignored `.pubxml` path before filling local values.

No `.pubxml.user.template` was added. The recovery simulation confirmed the current document-only guidance is still the safer choice because `.pubxml.user` is Visual Studio/user-specific and may contain credential-bearing state.

## Gaps Found Or Fixed

No documentation defect was found that required changing the inventory or template in this slice.

The main operational caveat remains unchanged: WebDeploy retries must still be explicitly approved, and `$env:AdventuresOnTheEdgeDP` must be present in the same shell or process running the publish command without printing its value.

## Validation Commands

Commands used during the recovery validation:

```powershell
git status --short --branch
git check-ignore -v BlogAI.Web/Properties/PublishProfiles/apiglobalwebnet.pubxml BlogAI.Web/Properties/PublishProfiles/apiglobalwebnet.pubxml.user
git check-ignore -q BlogAI.Web/Properties/PublishProfiles/apiglobalwebnet.pubxml.template
Select-String -Path README.md,AI_START.md -Pattern "local-only-files"
git diff --check
dotnet test ./VsMcpBridge.Shared.Tests/VsMcpBridge.Shared.Tests.csproj
```

Observed validation result for this slice:

- `git diff --check`: pass
- `dotnet test ./VsMcpBridge.Shared.Tests/VsMcpBridge.Shared.Tests.csproj`: pass, 313 tests

Non-secret warnings observed during test build:

- existing nullable warnings in `VsMcpBridge.Shared`
- existing nullable warnings in `VsMcpBridge.McpServer`
- existing xUnit analyzer warning in `VsMcpBridge.Shared.Tests`

## Remaining Backlog

- Add more `.template` or `.example` files only after a repeated onboarding gap proves they are useful.
- Keep `.pubxml.user` document-only unless a later review proves a stable non-secret template shape.
- Consider a redacted `%LocalAppData%\VsMcpBridge\appsettings.user.json` example only if local configuration onboarding becomes confusing.
- Continue staged secret-shaped scans before committing docs or templates that mention credentials.
