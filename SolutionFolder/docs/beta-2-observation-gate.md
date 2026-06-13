# Beta 2 Observation Gate

## Purpose

Prevent Beta 2 scope from being driven by imagination rather than operational evidence.

Beta 2 planning should start only after Beta 1 has produced enough real usage evidence to show which frictions are recurring, costly, safety-relevant, or valuable to users. Do not convert interesting possibilities into a Beta 2 backlog without observations.

## Rule

Beta 2 features must originate from one or more of:

- operational observation logs
- repeated workflow friction
- deployment friction
- contributor onboarding friction
- DCI-derived architectural insight validated against actual usage
- validated user demand

The source observation must be concrete enough to identify the workflow step, actors/tools involved, impact, frequency, workaround, and evidence path.

## Not Sufficient

The following are not enough to justify Beta 2 scope:

- interesting ideas
- speculative automation
- theoretical architecture improvements
- capability for capability's sake

These may be recorded as ideas or deferred notes, but they should not become Beta 2 backlog items until operational evidence supports them.

## Required Inputs Before Beta 2 Planning

Before creating a Beta 2 backlog, gather and review:

- accumulated operational observations
- preview tool usage observations
- deployment observations
- contributor observations
- local-only file recovery observations
- release feedback

Use `SolutionFolder/docs/operational-observation-log-template.md` for structured observation capture.

## Decision Process

Use this process before Beta 2 planning:

1. Observe
2. Record
3. Classify
4. Confirm recurrence
5. Prioritize
6. Plan
7. Implement

Do not skip from observation directly to implementation. The classification and recurrence checks are the gate.

## Explicit Warning

Do not create a Beta 2 backlog until enough operational observations exist.

Beta 1 release status does not automatically create Beta 2 scope. A clean Beta 1 release means the project has a stable observation surface, not permission to invent the next release.

## Beta 1 Examples

The following Beta 1 observations are useful examples of the kind of evidence that can inform future planning:

- `Y:` drive remapping and UNC path drift: valid environment friction. It supports documentation and recovery guidance unless recurrence proves path-discovery tooling is needed.
- Environment variable visibility for WebDeploy credentials: valid deployment friction. It supports explicit pre-check guidance and redaction rules, not automatic secret probing or deploy retries.
- Preview-tool risk over-flagging: valid preview workflow friction. It may justify classifier refinement only if repeated real handoff previews show the noise slows review.
- Local-only file survivability: valid onboarding and recovery friction. It supports inventory and safe templates only where the local-only file shape is stable and non-secret.
- VSIX build path requirements: valid tooling/version friction. It supports clear validation guidance around Visual Studio/MSBuild-specific paths, not broad build-system redesign by default.

## Valid Beta 2 Candidates

Valid Beta 2 candidates could include:

- improving preview risk classification after repeated handoff previews show specific false positives that slow human approval
- adding a deployment readiness preview if repeated deployment observations show missing environment-variable or publish-profile checks delay safe validation
- improving contributor onboarding examples if new contributors repeatedly fail to recreate local-only files from the current guidance
- improving VSIX validation ergonomics if repeated release-candidate runs fail because the required Visual Studio/MSBuild path is hard to locate
- improving trace artifact packaging if repeated validation sessions spend significant time manually assembling the same logs, metadata, diagrams, and handoff links
- adding targeted documentation links if repeated sessions lose time finding the current source-of-truth evidence

Each candidate still requires a narrow design slice, explicit non-goals, and validation expectations before implementation.

## Invalid Beta 2 Candidates

Invalid Beta 2 candidates include:

- automatic Codex execution because the preview-only handoff tool exists
- write-capable MCP repository mutation because preview-only document update works
- automatic deployment because manual deployment validation passed
- production auth because `/local-dev` diagnostics exist
- OAuth/OpenID/RBAC because auth is interesting future architecture
- persistence/database work without validated user demand
- admin APIs without a current owned boundary and repeated operational need
- BlogEngine.NET integration without validated workflow pressure
- broad orchestration loops because copy/paste between tools is mildly inconvenient once
- path-discovery automation after a single mapped-drive issue

These may remain deferred research or future ideas, but they are not Beta 2 scope without observation-backed need.

## Classification Guidance

Classify potential Beta 2 inputs in this order:

1. Environment
2. Tooling/version
3. Workflow
4. Architecture

This order keeps Beta 2 from treating beta-environment anomalies as architecture requirements. Use `SolutionFolder/docs/beta-tooling-observation-policy.md` when a proposed Beta 2 item appears to originate from beta-environment behavior.

## Planning Threshold

Before an item can enter a Beta 2 backlog, require:

- at least two similar observations, or one high-impact safety issue
- durable evidence for the pattern
- a clear owner boundary
- a narrow proposed change
- explicit non-goals
- validation plan
- stop conditions

If those are missing, continue observing.
