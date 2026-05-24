# Post-SolutionFolder Cleanup Backlog

## Checkpoint

- branch: `main`
- starting HEAD: `77dc377 Audit SolutionFolder reference paths`
- starting state: `main == origin/main`, working tree clean
- scope: docs-only backlog decision for post-`SolutionFolder` cleanup ideas

## Purpose

Record remaining cleanup ideas as intentional backlog, not active work.

The current `SolutionFolder` structure is stable enough for continued development. Further cleanup should wait for repeated friction, developer onboarding need, or an explicit polishing slice.

## Backlog Items

### 1. Visual Studio Solution Folder Mirroring

Potential future work:

- mirror `SolutionFolder/docs`, `SolutionFolder/artifacts`, and `SolutionFolder/scripts` where practical
- make tracked docs, artifacts, and scripts discoverable from the solution
- keep priority low until developer-facing polish matters

Decision: do not implement now. Avoid broad solution churn in cleanup follow-up slices.

### 2. Local-Only File Inventory And Templates

Potential future work:

- identify ignored local files required for development or deployment
- add tracked `.template` or `.example` files where appropriate
- document ignored local-only files in `README.md` or a dedicated doc
- never track secret-bearing real files

Decision: do not implement now. Only add templates after a concrete setup or onboarding gap repeats.

### 3. Deletion Candidate Review

Potential future work:

- review artifacts, docs, and scripts for obsolete generated noise
- classify files before deleting
- preserve historical evidence unless an explicit review says otherwise

Decision: do not delete anything in this backlog slice. Historical handoffs, trace logs, metadata, diagrams, and manual evidence remain preserved.

### 4. Root And Solution Developer Experience

Potential future work:

- improve root/solution discoverability if repeated friction appears
- keep projects at the repository root for now
- keep `README.md` and `LICENSE` at the repository root
- avoid broad project movement

Decision: current root shape is acceptable. Do not move projects or conventional root files as part of cleanup polish.

### 5. Validation Command Clarity

Potential future work:

- document validation gates more explicitly where developers expect them
- clarify that root `dotnet build` is not the VSIX validation gate while `VsMcpBridge.Vsix` remains in the solution
- prefer `dotnet test ./VsMcpBridge.Shared.Tests/VsMcpBridge.Shared.Tests.csproj` plus `SolutionFolder/scripts/build-vsix.ps1` for this cleanup area

Decision: keep the current documented interpretation from the cleanup handoff and audit handoff. Add broader validation docs only if confusion repeats.

## Current Decision

Do not implement these items now.

The post-move state is good enough for ongoing development:

- current references were audited
- broken artifact links in current workflow docs were fixed
- historical evidence was preserved
- VSIX validation has a working VS/MSBuild-specific script path

Future cleanup should remain narrow, evidence-preserving, and explicitly approved before deleting files or changing solution organization.
