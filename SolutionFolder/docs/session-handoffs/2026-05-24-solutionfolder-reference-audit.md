# SolutionFolder Reference Audit

## Checkpoint

- branch: `main`
- starting HEAD: `4741e2f Consolidate docs artifacts and scripts under SolutionFolder`
- starting state: `main == origin/main`, working tree clean
- scope: post-move reference audit for docs, artifacts, scripts, root entrypoint docs, skills, selected tests, and solution metadata

## Audit Scope

Searched for stale references involving:

- `docs/`
- `artifacts/`
- `scripts/`
- `docs/session-handoffs/`
- `docs/diagrams/`
- `artifacts/logs/`
- moved root docs such as `AI_STOP.md`, `Minor-refactor.md`, and `Codex-crash-handoff.md`
- moved root scripts such as `Remove-BinObjFolders.ps1` and `launch-exp.cmd`

Reviewed current entrypoint and operational surfaces first:

- `AI_START.md`
- `README.md`
- `AGENTS.md`
- `.agents/skills/**`
- `SolutionFolder/docs/*.md`
- `SolutionFolder/scripts/**/*.md`
- `VsMcpBridge.slnx`
- selected tests that intentionally use synthetic `docs/*.md` fixture paths

## Findings

### Valid New References

Current entrypoint docs and skills mostly already point to the new structure:

- `SolutionFolder/docs/...`
- `SolutionFolder/artifacts/logs/...`
- `SolutionFolder/docs/diagrams/...`
- `SolutionFolder/scripts/...`

These references were kept.

### Broken References Fixed

Four current workflow docs contained relative Markdown links from `SolutionFolder/docs/*.md` to `../SolutionFolder/artifacts/...`.

From files already inside `SolutionFolder/docs`, those targets resolve to a non-existent nested path. The link labels were correct, but the relative link targets were broken.

Fixed link targets to `../artifacts/...` in:

- `SolutionFolder/docs/app-host-ping-trace-workflow.md`
- `SolutionFolder/docs/tool-execution-trace-workflow.md`
- `SolutionFolder/docs/vsix-host-ping-trace-workflow.md`
- `SolutionFolder/docs/vsix-host-selected-text-trace-workflow.md`

### Valid Historical References

Old path examples in `SolutionFolder/docs/session-handoffs/2026-05-24-repo-structure-cleanup.md` intentionally document the move mapping:

- `docs/` -> `SolutionFolder/docs/`
- `artifacts/` -> `SolutionFolder/artifacts/`
- `scripts/` -> `SolutionFolder/scripts/`
- moved root doc/script filenames

These are valid historical references and were kept.

Historical/source-of-truth evidence may still contain old rendered or exported path strings. Those files were not rewritten in this audit.

### Valid Test Fixture References

`VsMcpBridge.Shared.Tests` still contains synthetic `docs/*.md` paths. These are test fixture paths for preview/document-selection behavior, not references to the repository's physical documentation folder.

They were kept.

## Fixes Made

- Repaired broken relative artifact links in the four current workflow docs listed above.
- Added this audit handoff.

No runtime code, file moves, deletions, deployment, or solution-folder mirroring were performed.

## Link Check

A current-doc Markdown link check over root entrypoint docs, top-level `SolutionFolder/docs/*.md`, `SolutionFolder/scripts/**/*.md`, and `.agents/skills/**/*.md` found no missing local file links after the fixes.

## Validation

- `git diff --check`: passed
- `dotnet test ./VsMcpBridge.Shared.Tests/VsMcpBridge.Shared.Tests.csproj`: passed with `313` tests
- `SolutionFolder/scripts/build-vsix.ps1`: passed using Visual Studio Insiders MSBuild and produced `VsMcpBridge.Vsix.vsix`

Observed warnings were existing nullable/analyzer warnings in `VsMcpBridge.Shared`, `VsMcpBridge.McpServer`, `VsMcpBridge.Vsix`, and `VsMcpBridge.Shared.Tests`; no new runtime code was changed in this slice.

## Remaining Backlog

Deferred intentionally:

- Visual Studio solution folder mirroring for `SolutionFolder/docs`, `SolutionFolder/artifacts`, and `SolutionFolder/scripts`
- local-only ignored file inventory/templates
- deletion candidate review
- broader developer-facing solution polish
- optional deeper audit of historical/source-of-truth exports if a future slice explicitly wants rendered historical links normalized
