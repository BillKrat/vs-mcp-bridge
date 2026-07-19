# Beta 1 Release Candidate Validation Bundle

> **Superseded.** This describes an earlier "Beta 1/Beta 2 release" model that no longer reflects current project positioning. As of 2026-07-19 the project is in early design, prior to gap analysis, backlog prioritization, and sprints. See `SolutionFolder/docs/current-bridge-capabilities.md` for current status.


## Purpose

Aggregate the evidence required to evaluate Beta 1 release readiness for `vs-mcp-bridge`.

This bundle aggregates evidence and now points to the completed release-candidate validation pass. It does not change runtime code, deploy, implement features, or declare Beta 1 complete.

## 1. Release Candidate Scope

The Beta 1 release candidate scope is defined by `SolutionFolder/docs/archive/beta-1-release-definition.md`.

Beta 1 is intended to prove that `vs-mcp-bridge` can support real AI-assisted development through conservative, observable, approval-gated boundaries:

- compiled bridge tools
- read-only MCP diagnostics
- explicit-input search
- preview-only proposals
- trace evidence
- deployment validation evidence
- recovery guidance
- contributor guidance

Beta 1 is not a production-auth, autonomous-execution, automatic-deployment, or BlogEngine.NET integration milestone.

This bundle uses the current repository evidence at checkpoint:

- branch: `main`
- expected sync: `main == origin/main`
- release-candidate validation HEAD: `e380382 Refresh Beta 1 deployment validation`
- working tree expectation: clean

The stabilization handoff date is resolved. Git history confirms `SolutionFolder/docs/session-handoffs/2026-05-17-operational-stabilization-checkpoint.md` is the canonical handoff. It was moved under `SolutionFolder/docs/session-handoffs/` during the 2026-05-24 repository structure cleanup, but no `2026-05-24-operational-stabilization-checkpoint.md` handoff exists.

## 2. Capabilities Included In Beta 1

The following capabilities are included in Beta 1 and have existing repository evidence:

| Capability | Evidence Status | Primary Evidence |
| --- | --- | --- |
| compiled bridge tools | covered by existing evidence | `SolutionFolder/docs/tool-execution-trace-workflow.md`, `SolutionFolder/docs/session-handoffs/2026-05-09-tool-execution-validation.md` |
| bridge tool inventory | covered by existing evidence | `SolutionFolder/docs/session-handoffs/2026-05-16-tool-inventory-validation.md`, `SolutionFolder/docs/session-handoffs/2026-05-16-mcp-tool-inventory-validation.md` |
| regex search | covered by existing evidence | `SolutionFolder/docs/session-handoffs/2026-05-16-mcp-regex-search-validation.md` |
| BM25 search | covered by existing evidence | `SolutionFolder/docs/session-handoffs/2026-05-16-mcp-bm25-search-validation.md` |
| document selection | covered by existing evidence | `SolutionFolder/docs/session-handoffs/2026-05-16-document-selection-validation.md` |
| preview-only document update | covered by existing evidence | `SolutionFolder/docs/session-handoffs/2026-05-17-preview-document-update-validation.md` |
| preview-only gated handoff tool | covered by existing evidence | `SolutionFolder/docs/session-handoffs/2026-05-17-gated-handoff-preview-tool-validation.md`, `SolutionFolder/docs/session-handoffs/2026-05-24-gated-handoff-preview-real-workflow.md` |
| trace artifact workflow | covered by existing evidence | `SolutionFolder/docs/tool-execution-trace-workflow.md`, `SolutionFolder/artifacts/logs/`, `SolutionFolder/docs/diagrams/` |
| approval-gated workflow | covered by existing evidence | `SolutionFolder/docs/future-contributor-operating-expectations.md`, `SolutionFolder/docs/session-slice-operating-template.md`, `SolutionFolder/docs/session-handoffs/2026-05-16-tool-approval-validation.md` |
| deployment validation | covered by current refresh evidence | `SolutionFolder/docs/session-handoffs/2026-05-17-blogai-webdeploy-validation.md`, `SolutionFolder/docs/session-handoffs/2026-05-17-blogai-deployed-guardrail-validation.md`, `SolutionFolder/docs/session-handoffs/2026-05-24-beta-1-deployment-validation-refresh.md` |
| release-candidate validation pass | covered by current validation evidence | `SolutionFolder/docs/session-handoffs/2026-05-24-beta-1-release-candidate-validation.md` |
| recovery guidance | covered by existing evidence | `SolutionFolder/docs/local-only-files.md`, `SolutionFolder/docs/session-handoffs/2026-05-24-local-only-file-recovery-validation.md` |
| contributor guidance | covered by existing evidence | `AI_START.md`, `AGENTS.md`, `SolutionFolder/docs/future-contributor-operating-expectations.md`, `SolutionFolder/docs/session-slice-operating-template.md` |

## 3. Validation Evidence

### Bridge Tool Validation

Bridge tool execution, manifest, inventory, approval, redaction, audit, and correlation evidence is spread across:

- `SolutionFolder/docs/tool-execution-trace-workflow.md`
- `SolutionFolder/docs/session-handoffs/2026-05-09-tool-execution-validation.md`
- `SolutionFolder/docs/session-handoffs/2026-05-09-tool-security-validation.md`
- `SolutionFolder/docs/session-handoffs/2026-05-16-tool-manifest-validation.md`
- `SolutionFolder/docs/session-handoffs/2026-05-16-tool-inventory-validation.md`
- `SolutionFolder/docs/session-handoffs/2026-05-16-tool-approval-validation.md`
- `SolutionFolder/docs/session-handoffs/2026-05-16-mcp-tool-inventory-validation.md`
- `SolutionFolder/docs/session-handoffs/2026-05-16-mcp-tool-inventory-live-validation.md`

Associated logs and diagrams include:

- `SolutionFolder/artifacts/logs/tool-regex-search-trace-20260509.*`
- `SolutionFolder/artifacts/logs/tool-security-trace-20260509.*`
- `SolutionFolder/artifacts/logs/tool-manifest-trace-20260516.*`
- `SolutionFolder/artifacts/logs/tool-inventory-trace-20260516.*`
- `SolutionFolder/artifacts/logs/tool-approval-trace-20260516.*`
- `SolutionFolder/artifacts/logs/mcp-tool-inventory-trace-20260516.log`
- `SolutionFolder/artifacts/logs/mcp-tool-inventory-live-validation-20260516.*`
- matching diagrams under `SolutionFolder/docs/diagrams/`

### Search And Document Selection Validation

Read-only MCP search and document selection evidence:

- `SolutionFolder/docs/session-handoffs/2026-05-16-mcp-regex-search-validation.md`
- `SolutionFolder/docs/session-handoffs/2026-05-16-mcp-bm25-search-validation.md`
- `SolutionFolder/docs/session-handoffs/2026-05-16-document-selection-validation.md`
- `SolutionFolder/artifacts/logs/mcp-regex-search-trace-20260516.*`
- `SolutionFolder/artifacts/logs/mcp-bm25-search-trace-20260516.*`
- `SolutionFolder/artifacts/logs/mcp-document-selection-trace-20260516.log`
- matching diagrams under `SolutionFolder/docs/diagrams/`

This evidence supports the Beta 1 boundary that regex and BM25 search operate over explicit caller-provided text and that document selection returns metadata rather than content search, ranking, indexing, or mutation.

### Preview-Only Document Update Validation

Preview-only document update evidence:

- `SolutionFolder/docs/preview-only-document-update-tool-design.md`
- `SolutionFolder/docs/session-handoffs/2026-05-17-preview-document-update-validation.md`
- `SolutionFolder/docs/session-handoffs/2026-05-17-preview-document-update-real-workflow.md`
- `SolutionFolder/artifacts/logs/mcp-preview-document-update-trace-20260517.*`
- `SolutionFolder/artifacts/logs/mcp-preview-document-update-real-workflow-20260517.*`
- `SolutionFolder/docs/diagrams/mcp-preview-document-update-trace-20260517.mmd`
- `SolutionFolder/docs/diagrams/mcp-preview-document-update-real-workflow-20260517.mmd`

The evidence records no write/apply path and preserves the rule that future mutation-capable tools remain separate threshold work.

### Gated Handoff Preview Tool Validation

Gated handoff preview evidence:

- `SolutionFolder/docs/chatgpt-codex-gated-handoff-workflow.md`
- `SolutionFolder/docs/first-gated-handoff-preview-tool-candidate.md`
- `SolutionFolder/docs/gated-handoff-preview-tool-usage-guide.md`
- `SolutionFolder/docs/session-handoffs/2026-05-17-gated-handoff-preview-tool-validation.md`
- `SolutionFolder/docs/session-handoffs/2026-05-24-gated-handoff-preview-real-workflow.md`
- `SolutionFolder/artifacts/logs/gated-handoff-preview-tool-trace-20260517.*`
- `SolutionFolder/docs/diagrams/gated-handoff-preview-tool-trace-20260517.mmd`

The evidence records preview-only behavior with no Codex execution, command execution, repo mutation, deployment, background workflow, or auto-continuation.

### Deployment Validation

Deployment validation evidence:

- `SolutionFolder/docs/session-handoffs/2026-05-17-blogai-webdeploy-validation.md`
- `SolutionFolder/docs/session-handoffs/2026-05-17-blogai-deployed-guardrail-validation.md`
- `SolutionFolder/docs/session-handoffs/2026-05-24-beta-1-deployment-validation-refresh.md`

The evidence records:

- secret-safe WebDeploy command shapes
- password sourced from `$env:AdventuresOnTheEdgeDP`
- no printed secret values
- successful WebDeploy validation to `https://api.global-webnet.com`
- smoke checks for `/` and `/local-dev`
- current Beta 1 deployed smoke refresh passing without deployment

This evidence now includes the current Beta 1 deployed smoke refresh. No deployment was needed because the deployed smoke passed.

### Deployed Guardrail Validation

Deployed guardrail evidence:

- `SolutionFolder/docs/session-handoffs/2026-05-17-blogai-deployed-guardrail-validation.md`
- `SolutionFolder/artifacts/logs/blogai-deployed-guardrail-trace-20260517.*`
- `SolutionFolder/docs/diagrams/blogai-deployed-guardrail-trace-20260517.mmd`

The evidence records local and deployed smoke validation for the display-only guardrail on `/` and `/local-dev` after an explicit successful WebDeploy retry.

### Recovery Guidance Validation

Recovery guidance evidence:

- `SolutionFolder/docs/local-only-files.md`
- `SolutionFolder/docs/session-handoffs/2026-05-24-local-only-file-template-readiness-review.md`
- `SolutionFolder/docs/session-handoffs/2026-05-24-local-only-file-recovery-validation.md`

The recovery validation records a pass for local-only file recovery, ignored publish-profile behavior, the tracked publish-profile template, and secret-source guidance.

### Local-Only File Validation

Local-only file evidence:

- `SolutionFolder/docs/local-only-files.md`
- `BlogAI.Web/Properties/PublishProfiles/apiglobalwebnet.pubxml.template`
- `.gitignore`
- `SolutionFolder/docs/session-handoffs/2026-05-24-local-only-file-recovery-validation.md`

The evidence covers required ignored local files, safe template usage, secret handling, validation commands, and what must not be committed.

### VSIX Validation

VSIX validation evidence:

- `SolutionFolder/docs/session-handoffs/2026-05-24-beta-1-release-candidate-validation.md`
- `SolutionFolder/docs/session-handoffs/2026-05-16-full-validation-checkpoint.md`
- `SolutionFolder/docs/session-handoffs/2026-05-24-repo-structure-cleanup.md`
- `README.md`

Existing evidence records:

- release-candidate `SolutionFolder/scripts/build-vsix.ps1` passing and producing `VsMcpBridge.Vsix.vsix`
- `SolutionFolder/scripts/build-vsix.ps1` passing with Visual Studio Insiders MSBuild
- `VsMcpBridge.Vsix.Tests` passing in the full validation checkpoint
- root `dotnet build` not being the correct validation gate for the VSIX project because the VSIX SDK requires Visual Studio/MSBuild-specific tasks

The VSIX build path requirement remains a documented Beta 1 limitation.

### Shared.Tests Validation

Shared test evidence:

- `SolutionFolder/docs/session-handoffs/2026-05-24-beta-1-release-candidate-validation.md`
- `SolutionFolder/docs/session-handoffs/2026-05-16-full-validation-checkpoint.md`
- `SolutionFolder/docs/session-handoffs/2026-05-17-gated-handoff-preview-tool-validation.md`
- `SolutionFolder/docs/session-handoffs/2026-05-24-gated-handoff-preview-real-workflow.md`
- `SolutionFolder/docs/session-handoffs/2026-05-24-local-only-file-recovery-validation.md`
- `SolutionFolder/docs/session-handoffs/2026-05-24-repo-structure-cleanup.md`

Existing evidence records `VsMcpBridge.Shared.Tests` passing in multiple slices. The release-candidate validation pass recorded 313 passed tests, 0 failed, and 0 skipped.

## 4. Known Limitations

The following limitations are accepted Beta 1 boundaries, not release blockers by themselves:

- preview-only orchestration
- approval-gated workflow
- VSIX build path requirements
- local-only configuration requirements

Additional operational limitations recorded by current evidence:

- live VS-backed MCP validation depends on the Visual Studio Experimental Instance and `VS MCP Bridge` tool window activation
- deployment validation depends on local WebDeploy setup and `$env:AdventuresOnTheEdgeDP` visibility in the active shell/process
- validation evidence is durable but manually assembled across docs, logs, metadata, and diagrams
- the canonical operational stabilization checkpoint is dated 2026-05-17 even though it was later moved under `SolutionFolder/` during the 2026-05-24 repository structure cleanup

## 5. Accepted Exceptions

No failed validation gates are accepted as exceptions.

Beta 1 remains ready only with the documented limitations listed above: preview-only orchestration, approval-gated workflow, VSIX build path requirements, and local-only configuration requirements.

## 6. Deferred Features

The following features are explicitly deferred from Beta 1:

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

These items should not be treated as Beta 1 blockers or hidden Beta 1 dependencies.

## 7. Beta 1 Readiness Recommendation

Recommendation: **Ready With Exceptions**

Reason:

Existing evidence shows that Beta 1 capabilities are implemented, bounded, and validated for release-candidate readiness:

- `git status --short --branch` passed at `main == origin/main`
- `git diff --check` passed
- `dotnet test ./VsMcpBridge.Shared.Tests/VsMcpBridge.Shared.Tests.csproj` passed with 313 tests
- `SolutionFolder/scripts/build-vsix.ps1` passed and produced the VSIX package
- required Beta 1 evidence files are present
- deployment validation refresh is complete
- operational stabilization checkpoint path/date is corrected

Use `Ready With Exceptions` because Beta 1 intentionally retains documented limitations: preview-only orchestration, approval-gated workflow, VSIX build path requirements, and local-only configuration requirements.

## 8. Remaining Required Work

Only the remaining required items identified in `SolutionFolder/docs/archive/beta-1-gap-analysis.md` are included here.

1. Create a final Beta 1 declaration/checkpoint document.

   Required contents:

   - release commit
   - validation evidence links
   - accepted limitations
   - accepted exceptions, if any
   - deferred scope confirmation
