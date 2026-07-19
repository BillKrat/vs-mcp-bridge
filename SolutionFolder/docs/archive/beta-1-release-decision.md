# Beta 1 Release Decision

> **Superseded.** This describes an earlier "Beta 1/Beta 2 release" model that no longer reflects current project positioning. As of 2026-07-19 the project is in early design, prior to gap analysis, backlog prioritization, and sprints. See `SolutionFolder/docs/current-bridge-capabilities.md` for current status.


## Purpose

Record the final release decision for Beta 1.

This decision is based entirely on:

- `SolutionFolder/docs/archive/beta-1-release-definition.md`
- `SolutionFolder/docs/archive/beta-1-gap-analysis.md`
- `SolutionFolder/docs/archive/beta-1-release-candidate-validation-bundle.md`
- `SolutionFolder/docs/session-handoffs/2026-05-24-beta-1-release-candidate-validation.md`

No runtime code, deployment, auth behavior, tooling behavior, or feature scope changes are introduced by this document.

## Release Status

Released With Exceptions.

## Decision

Beta 1 is released with documented exceptions.

The release-candidate validation passed, all Beta 1 required evidence exists, and the remaining exceptions are the known Beta 1 limitations documented in the release definition and validation bundle. No failed validation gate is accepted as an exception.

## Release Recommendation

Recommendation: Released With Exceptions.

This matches the release-candidate recommendation of `Ready With Exceptions`.

Use `Released With Exceptions` instead of `Released` because Beta 1 intentionally retains documented operational limitations:

- preview-only orchestration
- approval-gated workflow
- VSIX build path requirements
- local-only configuration requirements

## Rationale

Beta 1 satisfies the release definition within its intended conservative scope:

- compiled bridge tools are documented and validated
- bridge tool inventory is documented and validated
- regex search and BM25 search are explicit-input, read-only MCP diagnostics
- document selection is metadata-only and read-only
- preview-only document update has no write/apply path
- preview-only gated handoff has no Codex execution, command execution, repo mutation, deployment, background workflow, or auto-continuation
- trace artifact workflow evidence exists
- approval-gated workflow guidance exists and matches current tool behavior
- deployment validation was refreshed without deployment and passed
- recovery guidance validation exists
- contributor guidance identifies source-of-truth docs, validation expectations, and deferred scope
- operational stabilization checkpoint path/date is corrected

The release-candidate validation pass recorded:

- `git status --short --branch`: pass, `main == origin/main`
- `git diff --check`: pass
- `dotnet test ./VsMcpBridge.Shared.Tests/VsMcpBridge.Shared.Tests.csproj`: pass, 313 tests
- `SolutionFolder/scripts/build-vsix.ps1`: pass, VSIX package produced

## Accepted Limitations

The following limitations are accepted for Beta 1:

- preview-only orchestration
- approval-gated workflow
- VSIX build path requirements
- local-only configuration requirements

These limitations are not failed release gates. They are the intended Beta 1 operating boundaries and are documented in the release definition, gap analysis, and release-candidate validation bundle.

## Deferred Features

The following features are deferred beyond Beta 1:

- autonomous execution
- Codex execution tooling
- repo mutation tooling
- automatic deployment
- production auth
- OAuth/OpenID/RBAC
- persistence/database
- admin APIs
- BlogEngine.NET integration
- bridge-side deployment automation
- write-capable MCP mutation tools
- background continuation loops
- production plugin loading or package publishing

Deferred features should not be treated as Beta 1 blockers or hidden Beta 1 dependencies.

## Evidence References

- `SolutionFolder/docs/archive/beta-1-release-definition.md`
- `SolutionFolder/docs/archive/beta-1-gap-analysis.md`
- `SolutionFolder/docs/archive/beta-1-release-candidate-validation-bundle.md`
- `SolutionFolder/docs/session-handoffs/2026-05-24-beta-1-release-candidate-validation.md`
- `SolutionFolder/docs/session-handoffs/2026-05-24-beta-1-deployment-validation-refresh.md`
- `SolutionFolder/docs/session-handoffs/2026-05-24-local-only-file-recovery-validation.md`
- `SolutionFolder/docs/session-handoffs/2026-05-17-gated-handoff-preview-tool-validation.md`
- `SolutionFolder/docs/session-handoffs/2026-05-17-preview-document-update-validation.md`
- `SolutionFolder/docs/session-handoffs/2026-05-16-mcp-regex-search-validation.md`
- `SolutionFolder/docs/session-handoffs/2026-05-16-mcp-bm25-search-validation.md`
- `SolutionFolder/docs/session-handoffs/2026-05-16-document-selection-validation.md`
- `SolutionFolder/docs/session-handoffs/2026-05-16-mcp-tool-inventory-validation.md`
- `SolutionFolder/docs/session-handoffs/2026-05-17-operational-stabilization-checkpoint.md`

## Validation References

Primary release-candidate validation:

- `SolutionFolder/docs/session-handoffs/2026-05-24-beta-1-release-candidate-validation.md`

Deployment refresh validation:

- `SolutionFolder/docs/session-handoffs/2026-05-24-beta-1-deployment-validation-refresh.md`

Recovery validation:

- `SolutionFolder/docs/session-handoffs/2026-05-24-local-only-file-recovery-validation.md`

Preview-tool validation:

- `SolutionFolder/docs/session-handoffs/2026-05-17-preview-document-update-validation.md`
- `SolutionFolder/docs/session-handoffs/2026-05-17-gated-handoff-preview-tool-validation.md`
- `SolutionFolder/docs/session-handoffs/2026-05-24-gated-handoff-preview-real-workflow.md`

## Future Beta 2 Considerations

Potential Beta 2 considerations should remain separate design slices:

- improve preview diff readability
- improve handoff formatting ergonomics
- add more contributor onboarding examples
- add more environment observation examples
- broaden deployment smoke coverage if repeated need appears
- improve release note ergonomics
- consider additional trace artifact examples

Any future movement toward Codex execution, repo mutation, automatic deployment, production auth, persistence, admin APIs, or BlogEngine.NET integration requires a separate design gate and explicit approval. None of those are implied by the Beta 1 release.

## Explicit Non-Release Scope

Beta 1 is not production auth.

Beta 1 is not autonomous execution.

Beta 1 is not a production orchestration platform.

Beta 1 is a conservative, human-approved, preview-first, read-only-diagnostic milestone for `vs-mcp-bridge`.
