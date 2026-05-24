# Repo Structure Cleanup Handoff

## Checkpoint

- branch: `main`
- starting state: `main == origin/main`
- goal: safe developer-experience cleanup for root folders, docs, artifacts, and scripts
- cleanup mode: classify, move, and update references; no deletion of historical or diagnostic evidence

## What Moved

Moved durable documentation into:

- `SolutionFolder/docs/`

Moved durable evidence artifacts into:

- `SolutionFolder/artifacts/`

Moved operational scripts into:

- `SolutionFolder/scripts/`

Moved root documentation except entrypoint/conventional root files:

- `AI_STOP.md` -> `SolutionFolder/docs/AI_STOP.md`
- `Minor-refactor.md` -> `SolutionFolder/docs/Minor-refactor.md`
- duplicate root `Codex-crash-handoff.md` -> `SolutionFolder/docs/root-Codex-crash-handoff.md`

Moved root helper scripts:

- `Remove-BinObjFolders.ps1` -> `SolutionFolder/scripts/Remove-BinObjFolders.ps1`
- `launch-exp.cmd` -> `SolutionFolder/scripts/launch-exp.cmd`

Added existing non-secret development launch settings:

- `Adventures.Auth.LocalApi/Properties/launchSettings.json`

## What Stayed At Root

Kept at root because they are conventional repo entrypoints or root configuration:

- `README.md`
- `LICENSE`
- `AGENTS.md`
- `AI_START.md`
- `VsMcpBridge.slnx`
- `.gitignore`
- `.gitattributes`
- `.mcp.json`
- project folders
- build/config files that conventionally belong next to projects

`AI_START.md` remains the root AI entrypoint map and now routes to `SolutionFolder/docs/`.
`AGENTS.md` remains the root repository instruction file.

## Artifacts Placement Decision

Artifacts remain separate under:

- `SolutionFolder/artifacts/`

They were not moved under `SolutionFolder/docs/artifacts/` because artifacts are evidence outputs, logs, metadata, and diagnostic captures. They support docs, but they are not narrative documentation.

## File Classification

Directory-level classification used for this pass:

- `SolutionFolder/docs/`: mixed `canonical-current`, `historical-evidence`, `unknown-keep`, and documentation source material
- `SolutionFolder/docs/session-handoffs/`: `historical-evidence` and operational resume context
- `SolutionFolder/docs/blogs/posts/`: `canonical-current` repo-backed blog source content
- `SolutionFolder/docs/blogs/source-of-truth/`: `historical-evidence`
- `SolutionFolder/docs/diagrams/`: `diagnostic-trace` diagrams and explanatory trace diagrams
- `SolutionFolder/docs/manual-test-fixtures/`: `historical-evidence` and manual validation fixtures
- `SolutionFolder/artifacts/`: `diagnostic-trace`
- `SolutionFolder/scripts/`: `operational-script`
- root `README.md`, `AGENTS.md`, `AI_START.md`, `LICENSE`, `VsMcpBridge.slnx`: `canonical-current`

## What Was Not Deleted

No historical handoffs, trace logs, metadata files, diagrams, manual fixtures, source-of-truth exports, or root duplicate handoff content were deleted.

Previously ignored evidence files that surfaced after the move were preserved under `SolutionFolder/` rather than discarded.

## Deletion Candidates

No files were deleted in this slice.

Deletion candidates for a later explicit cleanup review:

- `SolutionFolder/docs/root-Codex-crash-handoff.md`: duplicate of `SolutionFolder/docs/Codex-crash-handoff.md` at the time of this cleanup
- `SolutionFolder/docs/Minor-refactor.md`: very small historical note with no current operational role found beyond the prior solution folder entry
- `SolutionFolder/artifacts/vs-exp-activitylog*.xml`: Visual Studio activity logs preserved as diagnostic traces; delete only if explicitly deemed obsolete
- `.DS_Store` files: local OS metadata, safe to ignore/delete separately if present and untracked

## Reference Updates

Updated current repo references from root paths to the new structure:

- `docs/` -> `SolutionFolder/docs/`
- `artifacts/` -> `SolutionFolder/artifacts/`
- `scripts/` -> `SolutionFolder/scripts/`
- `AI_STOP.md` -> `SolutionFolder/docs/AI_STOP.md`
- root helper script names -> `SolutionFolder/scripts/...`

Preserved source-of-truth exports were not rewritten as canonical current docs. Some old path strings may remain inside preserved DB exports or historical evidence because those files intentionally record prior rendered/runtime content.

## Solution And Git Updates

- Simplified `VsMcpBridge.slnx` documentation entries to stable solution folders:
  - `/SolutionFolder/docs/`
  - `/SolutionFolder/artifacts/`
  - `/SolutionFolder/scripts/`
- Added existing `Adventures.Auth.LocalApi` project to the solution.
- Kept existing BlogAI and test project entries.
- Updated `.gitignore` with broad allow-list entries for `SolutionFolder/docs`, `SolutionFolder/artifacts`, and `SolutionFolder/scripts`.

## Validation

Required validation for the cleanup slice:

- `git diff --check`
- `dotnet build`
- `dotnet test ./VsMcpBridge.Shared.Tests/VsMcpBridge.Shared.Tests.csproj`
- ripgrep checks for old root paths:
  - `docs/`
  - `artifacts/`
  - `scripts/`
  - old root md/script filenames
- `git status --short --branch`

Known interpretation for path checks:

- Current docs/code/config should reference `SolutionFolder/...`.
- Preserved historical/source-of-truth evidence may still contain old GitHub or rendered links involving `docs/`; those are not treated as broken current references in this slice.

Observed validation:

- `git diff --check`: passed
- `dotnet test ./VsMcpBridge.Shared.Tests/VsMcpBridge.Shared.Tests.csproj`: passed with `313` tests
- `dotnet build -m:1`: non-VSIX projects built; root build failed when reaching `VsMcpBridge.Vsix` because `Microsoft.VSSDK.BuildTools` uses `CodeTaskFactory` / `SetVsSDKEnvironmentVariables`, which is not supported by .NET Core MSBuild
- `SolutionFolder/scripts/build-vsix.ps1`: passed using Visual Studio Insiders MSBuild and produced `VsMcpBridge.Vsix.vsix`

The serial build confirmed `Adventures.Auth.LocalApi`, `BlogAI.Web`, `VsMcpBridge.Shared`, `VsMcpBridge.Shared.Wpf`, `VsMcpBridge.App`, `Adventures.ChatEngine`, `Adventures.ChatEngine.OpenAI`, `VsMcpBridge.McpServer`, and the test assemblies build before the VSIX SDK failure.

Root `dotnet build` is not the correct validation gate while `VsMcpBridge.Vsix` remains in the solution and requires Visual Studio/MSBuild-specific build tasks. Use the VSIX-aware script for that project.

## Deferred Backlog

Not implemented in this slice:

- Visual Studio solution folder mirroring for `SolutionFolder/docs`, `SolutionFolder/artifacts`, and `SolutionFolder/scripts`
- local-only ignored file inventory/templates
- deletion candidate review
- broader developer-facing solution polish
