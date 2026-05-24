# Operational Stabilization Checkpoint

## Purpose

Capture what is now stable, intentionally deferred, and experimentally validated across `vs-mcp-bridge`, `BlogAI.Web`, `AdventuresAuth`, the deployment workflow, and the gated ChatGPT/Codex orchestration direction.

This is documentation only. It does not add runtime code, tooling implementation, deployment behavior, or mutation behavior.

## Checkpoint

- Branch: `main`
- Expected sync: `main == origin/main`
- Starting HEAD: `a91c78d Add gated handoff tool readiness review`
- Working tree expectation: clean
- Deployment performed by this checkpoint: no

## Stable

The following areas are stable enough to use as current operating assumptions:

- trace-artifact workflow for meaningful bridge and BlogAI validation
- preview-only mutation posture through `bridge_preview_document_update`
- approval-gated workflow for proposal/apply paths
- `BridgeToolExecutor` as the policy, approval, redaction, audit, and correlation boundary for executable bridge tools
- BlogAI smoke-test shell
- deployed guardrail/banner behavior locally and at `https://api.global-webnet.com`
- diagnostic-only `/local-dev`
- local/dev auth diagnostic boundaries
- API-client parity diagnostics through explicit mode only
- default `/local-dev` in-process auth diagnostic path
- WebDeploy validation for `BlogAI.Web`
- secret-safe deployment credential posture using an external environment variable
- UNC repo path requirement for the current Windows/Parallels workflow
- docs-first handoff pattern for preserving operational state

## Experimentally Validated

The following have been proven enough to inform future work, but should still be treated as bounded validation rather than production capability:

- direct MCP stdio validation of selected bridge diagnostics
- read-only inventory, regex, BM25, and document-selection diagnostics
- preview-only document update behavior with deterministic statuses
- local/dev `AdventuresAuth` decision and API-host shape
- BlogAI-side local/dev auth consumer and display-safe status model
- explicit API-client parity path for BlogAI diagnostics
- WebDeploy to `https://api.global-webnet.com`
- deployed guardrail smoke validation on `/` and `/local-dev`
- future ChatGPT to Codex gated handoff direction as design only
- readiness for a bridge-side handoff preview/proposal tool only

## Intentionally Deferred

These remain intentionally deferred until separate design gates and explicit implementation slices approve them:

- production auth
- OAuth/OpenID/RBAC
- persistence/database
- auth middleware
- cookies/session topology
- BlogEngine.NET coupling
- tenant/user/role implementation
- production login UI
- real password storage
- external identity providers
- auth-admin UI or APIs
- automatic Codex execution
- autonomous mutation workflows
- background Codex wait loops
- automatic continuation between slices
- bridge-side deployment automation
- destructive git automation
- write-capable MCP mutation tools

## Operational Principles

Current operating principles:

- the human remains the approval gate
- AI proposes, validates, and summarizes
- automation must preserve auditability
- narrow slices are preferred over large rewrites
- durable evidence is preferred over chat memory
- deployment validation must be reproducible
- secrets must remain externalized and redacted
- preview/proposal comes before mutation
- approval is not inferred from prior context, branch state, or clean validation
- meaningful workflows should preserve request/operation correlation
- bridge tool work should route through existing manifest, policy, approval, redaction, audit, and correlation seams

## Top Current Friction Points

Current friction points worth preserving as future improvement targets:

- manual copy/paste between ChatGPT and Codex for scoped tasks and results
- long `AI_START.md` routing list as BlogAI and bridge evidence grows
- `.gitignore` allow-list maintenance for durable docs and trace artifacts
- repeated need to restate no-runtime/no-deploy/no-mutation boundaries
- local path volatility between `Y:\vs-mcp-bridge` and `\\Mac\Dev\vs-mcp-bridge`
- WebDeploy depends on environment variable visibility in the current shell/process
- validation evidence is durable but still assembled manually across docs, logs, metadata, and diagrams
- API-client parity is explicit and safe, but not yet hardened as a broader harness

## High-Leverage Preview/Proposal Tooling Opportunities

The highest-leverage future tooling should stay preview/proposal-first:

- gated ChatGPT to Codex handoff preview contract
- structured task summary and constraint normalization
- validation-plan preview from explicit user input
- risk-flag preview for deploy, git, mutation, and secret-sensitive requests
- SolutionFolder/docs/handoff creation preview from explicit caller-provided content
- evidence-package preview that lists expected logs, metadata, diagrams, and handoffs before a run
- `.gitignore` allow-list preview for requested durable artifacts
- deployment readiness preview that checks path, branch, env-var presence, build command, and smoke plan without publishing
- API-host parity harness preview that states local services, expected endpoints, and validation checks before execution

These opportunities should not execute Codex, write files, deploy, mutate git state, or continue automatically in their first implementation form.

## Risks Of Premature Automation

Premature automation would create risks that the current workflow deliberately avoids:

- treating a prepared task as approval to execute it
- running Codex automatically after ChatGPT prepares a prompt
- continuing into a second slice without a user decision
- hiding repo mutation behind diagnostics or search
- publishing or deploying as a side effect of a helper tool
- committing, pushing, resetting, checking out, or deleting branches without explicit approval
- storing raw secrets in prompts, summaries, logs, or audit metadata
- building background wait loops that are hard to stop or audit
- turning local/dev auth diagnostics into production-auth assumptions
- coupling BlogAI auth work to BlogEngine.NET before the boundary is designed

## Conditions Before Bridge-Side Orchestration Implementation

Before any bridge-side orchestration implementation goes beyond preview/proposal, the repo needs:

- explicit tool manifest and capability metadata for the orchestration action
- structured task input contract
- structured result contract
- user-visible approval boundary before Codex execution
- stop conditions for broad scope, missing validation, destructive git, deploy, mutation, secrets, or auto-continuation requests
- redaction strategy for prompts, summaries, validation output, and errors
- audit classification and terminal status model
- request and operation correlation preserved across handoff, execution, and result
- deterministic validation evidence for approved runs
- no background loop without explicit lifecycle, cancellation, and timeout design
- explicit separation between preview, execution, result summarization, and next-slice approval

## Recommended Pause

Recommended pause/stabilization period: hold off on new runtime tooling or production-auth work until the current evidence set has been used in at least one normal resume cycle.

During the pause, prefer:

- using the existing docs to resume work
- validating that future agents can find the right handoff without chat history
- collecting friction from real use rather than adding infrastructure preemptively
- keeping WebDeploy and BlogAI checks manual and explicit unless a future preview-only readiness tool is approved

## Recommended Next Future Milestone

Recommended next future milestone: implement a preview-only gated handoff proposal tool.

That milestone should:

- accept explicit scoped task text, repo target, validation plan, and constraints
- normalize the handoff into a structured preview
- preserve request and operation correlation
- apply redaction before logs and audit metadata
- return risk flags and stop conditions
- submit nothing to Codex
- mutate no files
- run no shell commands
- require a separate user approval before any execution-capable workflow exists

Do not skip directly to automatic Codex execution, deployment automation, or write-capable MCP mutation tools.
