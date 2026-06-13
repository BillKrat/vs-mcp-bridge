# Current Bridge Capabilities

## Purpose

Set expectations for what `vs-mcp-bridge` currently does, what is experimental, and what is not implemented.

This document describes current repository state. It does not create new scope, roadmap commitments, runtime behavior, tooling changes, or deployment behavior.

## Beta 1 Status

Beta 1 is released with documented exceptions.

The release proves that selected Visual Studio and repository context can support AI-assisted workflows through conservative, observable, human-approved boundaries.

Use these documents as the release baseline:

- `SolutionFolder/docs/beta-1-release-summary.md`
- `SolutionFolder/docs/beta-1-release-decision.md`
- `SolutionFolder/docs/beta-2-observation-gate.md`

Beta 1 is not production auth, not autonomous execution, and not a production orchestration platform.

## Implemented Capabilities

Current implemented capabilities include:

- VSIX-hosted Visual Studio bridge surface
- standalone WPF app host for shared presenter and viewmodel reuse
- shared bridge contracts, diagnostics, pipe dispatch, and tool-window orchestration
- local named-pipe communication between the MCP server and VSIX host
- active Visual Studio document inspection
- selected text inspection
- solution project listing
- Visual Studio Error List inspection
- compiled bridge tool inventory diagnostics
- regex search over explicit caller-provided text
- minimal BM25-style ranked search over explicit caller-provided documents
- deterministic repo-root-relative document metadata selection
- preview-only document update validation
- proposal-only text edit workflows
- multi-file proposal review through one approval-gated proposal
- approval-gated apply inside Visual Studio
- trace artifact workflow guidance
- deployment validation evidence
- recovery guidance
- contributor guidance

## Experimental Capabilities

Current experimental capabilities are intentionally constrained:

- prompt-box chat through host-registered `IChatRequestService` implementations
- preview-only document update tooling
- preview-only gated handoff proposal tooling
- BlogAI pressure-test workflows that use the bridge as an observation surface
- MCP search diagnostics over explicit caller-supplied text or documents
- document selection helpers that return metadata for caller-side review

Experimental capabilities should remain preview-first, observable, and approval-gated unless a separate design slice changes that boundary.

## Not Implemented

The following are not current bridge capabilities:

- autonomous execution
- automatic Codex execution
- selected-file model transmission
- autonomous deployment
- background agents
- autonomous mutation
- direct MCP repository mutation
- automatic blog publishing
- production auth
- OAuth, OpenID, or RBAC
- persistence or database-backed workflow state
- admin APIs
- BlogEngine.NET integration as a managed production feature
- automatic deployment retries
- hidden filesystem crawling for MCP search tools

Deferred items are not hidden Beta 1 dependencies. They require separate observation, design, and approval before implementation.

## Known Constraints

Known current constraints include:

- workflow remains approval-gated
- orchestration remains preview-only
- MCP search tools require explicit caller-provided text or documents
- document selection returns metadata, not model-ready context
- VSIX build and validation have path requirements
- local-only configuration files must be recreated from documented templates
- deployment validation is manual smoke validation, not automatic deployment
- proposal UI tracks current and last-completed proposal state rather than durable proposal history
- the bridge is a local integration, not a hosted multi-user service

## Usage Expectations

Use the bridge for observable, human-controlled AI-assisted workflows:

- inspect active Visual Studio state
- gather deterministic diagnostic evidence
- run explicit-input MCP search diagnostics
- prepare preview-only proposals
- review proposed edits before applying
- record operational observations before proposing new automation

Do not treat the bridge as an autonomous operator, deployment system, background worker, or production authorization boundary.

### Selected Files

Selected files participate in proposal/edit workflows:

- selected paths are tracked in proposal state
- selected file contents are loaded into proposal drafts
- selected files support preview, approval, and apply workflows

Selected files do not currently reach normal model chat context:

- selected file content is not appended to normal chat model requests
- selected file paths are not sent to the model in normal chat requests
- the chat request payload sends the typed prompt as `messages[0].content`

For chat-model context, selected-file behavior is currently effectively cosmetic. For proposal/edit workflows, it is functional state.

## Common Misconceptions

Misconception: Selecting a file in the VSIX tool window sends that file to the model.

Reality: Selection supports proposal/edit state. Normal chat requests send only the typed prompt.

Misconception: MCP search tools can read file paths or crawl the repository.

Reality: MCP search diagnostics operate over explicit text, `entries`, or `documents` provided by the caller.

Misconception: Beta 1 release means autonomous execution is available.

Reality: Beta 1 explicitly excludes autonomous execution, automatic Codex execution, autonomous mutation, and automatic deployment.

Misconception: Preview-only gated handoff tooling can execute Codex work.

Reality: It supports preview and review. It does not execute commands, mutate repositories, deploy, or create background continuations.

Misconception: Deployment validation means deployment is automated.

Reality: Deployment validation is evidence that deployed smoke checks passed. It is not bridge-side deployment automation.

## Future Direction

Future work should be chosen through the Beta 2 observation gate:

1. observe
2. record
3. classify
4. confirm recurrence
5. prioritize
6. plan
7. implement

Valid future work must originate from operational observations, repeated workflow friction, deployment friction, contributor onboarding friction, DCI-derived architectural insight validated against usage, or validated user demand.

Do not create a Beta 2 backlog from interesting ideas, speculative automation, theoretical architecture improvements, or capability for capability's sake.
