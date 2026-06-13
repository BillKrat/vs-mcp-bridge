# Beta 1 Release Definition

## Purpose

Define the first releasable Beta milestone for `vs-mcp-bridge`.

Beta 1 is a named operating milestone, not a deployment event by itself. It should be declared only when the repository, documented workflows, validation evidence, and recovery guidance are stable enough for repeated human-approved use.

## Mission

Beta 1 proves that `vs-mcp-bridge` can expose selected Visual Studio and repository context to AI-assisted workflows through conservative, observable, approval-gated boundaries.

The milestone should demonstrate that compiled bridge tools, read-only MCP diagnostics, preview-only proposals, trace evidence, deployment validation, and contributor guidance can support real work without introducing autonomous execution, hidden repository mutation, automatic deployment, or production auth scope.

## Included Capabilities

Beta 1 includes:

- compiled bridge tools
- bridge tool inventory
- regex search
- BM25 search
- document selection
- preview-only document update
- preview-only gated handoff tool
- trace artifact workflow
- approval-gated workflow
- deployment validation
- recovery guidance
- contributor guidance

## Required Validation

Before declaring Beta 1 complete, the following validation must have current passing evidence or an explicitly accepted exception:

- `VsMcpBridge.Shared.Tests` passing
- VSIX validation passing
- deployment validation passing
- preview tool validation passing
- recovery guidance validation passing

Validation evidence should be durable enough for a future session to reconstruct what was tested, which commit was tested, and which commands or manual checks were used.

## Explicitly Out Of Scope

Beta 1 does not include:

- autonomous execution
- Codex execution tooling
- repo mutation tooling
- automatic deployment
- production auth
- OAuth/OpenID/RBAC
- persistence/database
- admin APIs
- BlogEngine.NET integration

These items remain future work unless a later release definition explicitly brings them into scope.

## Known Limitations

Beta 1 is expected to retain these limitations:

- preview-only orchestration
- approval-gated workflow
- VSIX build path requirements
- local-only configuration requirements

These limitations are acceptable for Beta 1 when they are documented, validated, and recoverable by a human operator.

## Release Criteria

Beta 1 can be declared complete when all of the following are true:

- The repository is on the intended release commit with a clean working tree.
- `AI_START.md`, `AGENTS.md`, and the current architecture/workflow docs point to the active operating guidance.
- Compiled bridge tool behavior is documented and validated through current tests or handoff evidence.
- Bridge tool inventory diagnostics are validated and remain read-only.
- Regex search and BM25 search are validated as explicit-input, read-only MCP tools.
- Document selection is validated as metadata selection, not content search or mutation.
- Preview-only document update behavior is validated with no MCP write/apply path.
- Preview-only gated handoff behavior is validated against real workflow use.
- Trace artifact workflow guidance exists and has at least one current observed workflow artifact set.
- Approval-gated workflow guidance is documented and matches current tool behavior.
- Deployment validation has current evidence and does not depend on printing or storing secrets.
- Recovery guidance has current validation evidence for local-only configuration or setup recovery.
- Contributor guidance identifies source-of-truth docs, validation expectations, and deferred scope.
- Known limitations are documented without being treated as release blockers.
- Out-of-scope items are not partially implemented as hidden beta dependencies.

## Stretch Goals

The following are useful, but not required for Beta 1:

- additional trace artifact examples beyond the minimum current evidence
- additional contributor onboarding examples
- improved preview diff readability
- more ergonomic handoff formatting
- additional environment observation examples
- broader deployment smoke coverage
- more detailed release notes

Do not delay Beta 1 solely for stretch goals unless they expose a release-blocking safety, validation, or recovery gap.

## Success Criteria

The project is ready to carry the "Beta 1" label when a future contributor can:

- start from the documented entry points without relying on chat history
- understand which workflows are validated and which are deferred
- run the required validation gates with documented commands or manual steps
- use compiled bridge tools and MCP diagnostics without mutating the repository through MCP
- review preview-only proposals before any normal repository edit occurs
- recover local-only configuration from documented guidance
- distinguish beta-environment anomalies from repo, workflow, tooling, or architecture defects
- see clear evidence that deployment validation has been performed safely

Comfort with Beta 1 means the project is useful and repeatable inside its conservative boundaries. It does not mean the bridge is autonomous, production-authenticated, broadly deployed, or integrated with BlogEngine.NET.
