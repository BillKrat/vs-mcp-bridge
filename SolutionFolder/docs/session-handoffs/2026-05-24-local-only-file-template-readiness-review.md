# Local-Only File Template Readiness Review

## Checkpoint

- branch: `main`
- starting HEAD: `332a223 Validate gated handoff preview real workflow`
- starting state: `main == origin/main`, working tree clean
- scope: docs-only readiness review for local-only ignored files and future templates

## Inputs Reviewed

- `AI_START.md`
- `SolutionFolder/docs/session-handoffs/2026-05-24-gated-handoff-preview-real-workflow.md`
- `SolutionFolder/docs/session-handoffs/2026-05-24-post-solutionfolder-cleanup-backlog.md`
- `SolutionFolder/docs/session-slice-operating-template.md`
- `.gitignore`
- `README.md`
- current ignored filename inventory filtered for publish/config/secret-shaped paths

No credential-bearing file contents were opened or printed.

## Known Or Likely Required Local-Only Files

Known local-only deployment files:

- `BlogAI.Web/Properties/PublishProfiles/apiglobalwebnet.pubxml`
- `BlogAI.Web/Properties/PublishProfiles/apiglobalwebnet.pubxml.user`

Observed ignore rules:

- `*.pubxml` is ignored by `.gitignore`
- `*.user` is ignored by `.gitignore`

Known secret source:

- `$env:AdventuresOnTheEdgeDP`

This environment variable name may be documented. Its value must never be printed, copied into docs, committed, logged, traced, or included in examples.

Known local/user configuration path from current docs:

- `%LocalAppData%\VsMcpBridge\appsettings.user.json`

This is a machine-local configuration source and should remain outside the repository. It can be documented by path and schema shape, but real user values should not be tracked.

Build output copies observed during the ignored-file scan:

- `BlogAI.Web/bin/**/appsettings*.json`
- `BlogAI.Web/obj/**/appsettings*.json`
- `VsMcpBridge.App/bin/**/appsettings.json`
- `VsMcpBridge.Shared.Tests/bin/**/appsettings*.json`
- copied `Microsoft.Extensions.Configuration.UserSecrets.dll` under `bin/**`

These are generated build outputs, not source inventory candidates.

## Template Candidates

Safe future `.template` or `.example` candidates:

- `BlogAI.Web/Properties/PublishProfiles/apiglobalwebnet.pubxml.template`
  - useful because the publish profile path and profile name are operationally important
  - must omit passwords, tokens, publish-password material, and machine-specific secrets
  - should use placeholders for publish URL/server/site/profile values if those are not safe to publish
- `SolutionFolder/docs/local-only-file-inventory.md`
  - canonical human-readable inventory of required local-only files, secret sources, and setup notes
  - should document the required environment variable by name only: `$env:AdventuresOnTheEdgeDP`
- Optional documented example for `%LocalAppData%\VsMcpBridge\appsettings.user.json`
  - best represented as a redacted JSON shape in the canonical inventory doc, or as a `.template` under a docs/templates area if the schema becomes larger
  - should not be placed at the real LocalAppData path by repo tooling

Do not create templates in this slice.

## Document-Only Candidates

Keep these documented only unless a concrete onboarding gap proves a template is useful:

- `BlogAI.Web/Properties/PublishProfiles/apiglobalwebnet.pubxml.user`
  - user/machine-specific and likely credential-bearing
  - should remain ignored and undocumented beyond path, purpose, and secret rules
- `%LocalAppData%\VsMcpBridge\appsettings.user.json`
  - machine-local override file
  - real values stay local; docs may show redacted keys or placeholder shape
- .NET user secrets storage for `VsMcpBridge.App`
  - tool-owned/local secret store
  - document how it participates in config only if onboarding requires it
- `bin/**` and `obj/**` appsettings copies
  - generated build output
  - never template from these outputs
- copied `Microsoft.Extensions.Configuration.UserSecrets.dll`
  - generated package output
  - not a local config file

## Canonical Inventory Location

Future canonical inventory should live at:

- `SolutionFolder/docs/local-only-file-inventory.md`

Rationale:

- it is operational documentation, not a runtime config file
- it keeps the full inventory out of `README.md`
- it can link to specific template/example files later
- it fits the current `SolutionFolder/docs` structure without adding root clutter

## README Decision

`README.md` should eventually point to the canonical inventory doc.

It should not contain the full local-only file inventory. Duplicating the list in `README.md` would increase drift risk and make future credential-handling guidance harder to keep consistent.

## Secret And Redaction Rules

Rules for any future inventory or template slice:

- never track real credential-bearing files
- never commit `.pubxml.user`
- never commit real `.pubxml` files unless a specific profile is proven secret-free and approved; default remains ignored
- never print, store, or document the value of `$env:AdventuresOnTheEdgeDP`
- document secret source names only when operationally useful
- use placeholders such as `[REDACTED]`, `<set locally>`, or `<environment variable>` in templates
- do not include raw passwords, tokens, cookies, authorization headers, bearer values, API keys, or connection strings with credentials
- do not derive templates from generated `bin/**` or `obj/**` output
- if a file's purpose or secret content is uncertain, document it as `unknown-keep` and do not template or track it

## Readiness Decision

Ready for a future docs/template implementation slice, but not for broad inventory automation.

Expected approach:

- track safe `.template` or `.example` files only where a developer-required local-only file has a stable non-secret shape
- keep real credential-bearing files ignored
- create one canonical inventory doc under `SolutionFolder/docs`
- add a short `README.md` pointer to that canonical doc
- keep publish profiles ignored unless a later explicit review approves a safe template file

## Smallest Future Implementation Slice

Create the canonical inventory and one safe template candidate:

1. Create `SolutionFolder/docs/local-only-file-inventory.md`.
2. Add a short `README.md` pointer to that doc.
3. Add `BlogAI.Web/Properties/PublishProfiles/apiglobalwebnet.pubxml.template` only if the template can be written with placeholders and no secrets.
4. Do not add `.pubxml.user` templates unless there is a proven safe reason.
5. Validate with:
   - `git diff --check`
   - a staged secret-shaped scan before commit

Stop if any template would require copying real local file contents or exposing a secret-bearing value.

## Explicitly Deferred

- no runtime code
- no template files yet
- no publish profile changes
- no `.gitignore` changes
- no secret handling changes
- no deletion
- no deployment
