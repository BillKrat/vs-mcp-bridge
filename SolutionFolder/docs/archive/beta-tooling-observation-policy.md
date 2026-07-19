# Beta Tooling Observation Policy

> **Superseded.** This describes an earlier "Beta 1/Beta 2 release" model that no longer reflects current project positioning. As of 2026-07-19 the project is in early design, prior to gap analysis, backlog prioritization, and sprints. See `SolutionFolder/docs/current-bridge-capabilities.md` for current status.


## Purpose

Prevent architecture, workflow, and tooling decisions from being driven by beta-environment anomalies.

Beta sessions are expected to expose local machine, shell, deployment, Visual Studio, Codex, and build-path differences. Treat those observations as evidence to classify before they become workarounds, process rules, or product architecture.

Use this policy with `SolutionFolder/docs/operational-observation-log-template.md` when a beta-only or beta-first anomaly appears.

## Classification Order

Classify suspected issues in this order:

1. Environment
2. Tooling/version
3. Workflow
4. Architecture

Start with the most local and reversible cause. Escalate only when evidence shows the issue is not explained by environment or tooling/version differences.

## Common Beta Anomalies

Examples that should be classified before changing process, tooling, or architecture:

- `Y:` drive remapping or UNC path drift
- environment variable visibility across shells
- WebDeploy credential scope
- Visual Studio shell versus Codex shell differences
- VSIX build path differences

These examples are valid observations. They are not automatically repo defects, tooling requirements, or architecture signals.

## Rules

- Do not automate around a single beta anomaly.
- Record observations before implementing workarounds.
- Prefer reproducible evidence over impressions, recollection, or one-off recovery notes.
- Require repeated occurrence before process or tooling changes.
- Distinguish repo defects from environment defects before assigning remediation.

## Evidence Expectations

Useful evidence includes:

- the exact workflow step where the anomaly appeared
- current branch, commit, and working tree state when relevant
- shell or host context, such as Visual Studio Developer PowerShell, Codex shell, or regular PowerShell
- relevant command names and redacted output
- whether the anomaly reproduces in a second shell, clean session, or documented path
- whether the same repo state works in another environment

Do not capture secrets, credential values, machine-specific private paths beyond what is needed for diagnosis, or transient output that cannot be safely shared.

## Decision Threshold

Use the smallest response that fits the evidence:

- `Environment`: document the observation, local precondition, or recovery path.
- `Tooling/version`: record the affected tool and version boundary before proposing a tooling change.
- `Workflow`: update process guidance only after repeated friction or one high-impact safety issue.
- `Architecture`: require reproducible evidence that the issue is inherent to the system boundary, not the beta environment.

Architecture changes should be the last classification, not the default response to beta friction.

## Non-Goals

This policy does not introduce runtime code, tooling automation, deployment changes, or new mutation paths. It is a decision guardrail for interpreting beta observations before changing the repository.
