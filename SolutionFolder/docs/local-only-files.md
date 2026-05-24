# Local-Only Files

## Purpose

This document is the canonical inventory for local-only files and secret sources that may be required for development or deployment but must not be committed with real values.

Use tracked `.template` files when a file has a stable non-secret shape. Keep real credential-bearing files ignored.

## Inventory

| Local-only item | Tracked template | Purpose | May contain secrets | How to recreate | Validation command | Backup/recovery note |
| --- | --- | --- | --- | --- | --- | --- |
| `BlogAI.Web/Properties/PublishProfiles/apiglobalwebnet.pubxml` | `BlogAI.Web/Properties/PublishProfiles/apiglobalwebnet.pubxml.template` | Local WebDeploy publish profile for the `BlogAI.Web` smoke-test shell. | Yes. Real publish profiles can contain deployment endpoints, usernames, publish settings, or other sensitive metadata. | Copy the template to the `.pubxml` path and fill values locally. Do not copy values from chat, logs, or committed docs. Supply deployment credentials through explicit command parameters or local secret sources, not the tracked template. | `dotnet build ./BlogAI.Web/BlogAI.Web.csproj` before any approved deploy attempt. | Back up the real file only in a private credential-safe location. If lost, recreate from the template and local hosting-provider settings. |
| `BlogAI.Web/Properties/PublishProfiles/apiglobalwebnet.pubxml.user` | None. | Visual Studio/user-specific publish profile companion file. | Yes. It may contain user-specific or credential-bearing publish data. | Let Visual Studio or WebDeploy tooling regenerate it locally when needed. Do not create it from a tracked template unless a later review proves a safe non-secret shape. | Confirm it remains ignored: `git check-ignore -v BlogAI.Web/Properties/PublishProfiles/apiglobalwebnet.pubxml.user`. | Treat as disposable local tool state. If lost, regenerate through Visual Studio publish tooling or local deployment setup. |
| `$env:AdventuresOnTheEdgeDP` | None. | Password source for explicitly approved BlogAI.Web WebDeploy attempts. | Yes. The value is a secret. | Set the environment variable in the shell or process that will run WebDeploy. Document the name only, never the value. | Confirm presence without printing it: `if ([string]::IsNullOrWhiteSpace($env:AdventuresOnTheEdgeDP)) { "missing" } else { "present" }`. | Recover from the external credential manager or hosting-provider credential source, not from this repository. |
| `%LocalAppData%\VsMcpBridge\appsettings.user.json` | None currently. | Machine-local override configuration for bridge host settings. | Yes, depending on configured keys. | Create locally only when an override is needed. Prefer environment variables or user-secret stores for sensitive values. | Start the relevant host and verify the expected non-secret behavior. Avoid copying this file into repo artifacts. | Back up only through private machine/user configuration backup. Do not commit real values. |

## Template Usage

To create a local publish profile from the tracked template:

```powershell
Copy-Item `
  .\BlogAI.Web\Properties\PublishProfiles\apiglobalwebnet.pubxml.template `
  .\BlogAI.Web\Properties\PublishProfiles\apiglobalwebnet.pubxml
```

Then edit only the ignored `.pubxml` file with local publish settings.

The tracked template intentionally omits deployment credentials. For the known WebDeploy path, the deployment password source is `$env:AdventuresOnTheEdgeDP`; the value must be available to the same shell or process that runs the deployment command.

## Secret Rules

- Do not commit real `.pubxml` files.
- Do not commit `.pubxml.user` files.
- Do not commit `.env` files, publish settings exports, private keys, tokens, cookies, bearer values, raw passwords, API keys, or credential-bearing connection strings.
- Do not copy secrets from local files into docs, trace artifacts, commit messages, issue text, or prompts.
- Document secret source names only when operationally useful.
- Use placeholders such as `<set locally>`, `<environment variable>`, or `[REDACTED]` in tracked examples.
- Treat generated `bin/**` and `obj/**` copies as build output, not as sources for templates.

## Current Decision

Tracked now:

- `BlogAI.Web/Properties/PublishProfiles/apiglobalwebnet.pubxml.template`

Documented only:

- `BlogAI.Web/Properties/PublishProfiles/apiglobalwebnet.pubxml.user`
- `$env:AdventuresOnTheEdgeDP`
- `%LocalAppData%\VsMcpBridge\appsettings.user.json`

No real credential-bearing local files should be staged or committed.
