# 2026-07-19 — Early-Design Repositioning And Repo Cleanup

## Purpose

Bring the repository's documented state in line with reality: the project is
in early design (basic infrastructure only, nothing committed or working),
not a "Beta 1 released" product. Bill's stated reason: work so far was built
via ChatGPT/Codex without a sound architecture/design/gap-analysis/backlog
process, so there's no confidence in current code — docs should not suggest
otherwise. Also: fix a CI failure Bill was getting emailed about, and clean
up branches/GitHub rulesets ahead of starting real sprint work.

## What Was Completed

**Repositioning (commits `7e8fcb7`, `a1ed57e`, `671e474`):**

- Rewrote `README.md`: added an early-design status banner, replaced the
  "What It Does Today" / "Current Status" / "Next Steps" sections and the
  "VSIX Review Experience" section — no more itemized "verified"/"works end
  to end" capability lists.
- Rewrote `SolutionFolder/docs/current-bridge-capabilities.md` as the single
  early-design status doc (design → gap analysis → prioritized backlog →
  sprints), replacing the old Beta 1/Implemented/Experimental/Not-Implemented
  structure.
- Trimmed `SolutionFolder/docs/ARCHITECTURE.md` ("Current Verified Runtime
  Slice" → "Current Stage") and
  `SolutionFolder/docs/VS_MCP_BRIDGE_TECHNICAL_ANALYSIS.md` (Executive
  Summary, Current Verified State, Current Technical Priorities, Known Risk
  Areas, Guidance For Future Changes) of the same "verified"/"validated
  baseline" framing.
- Archived the ~15-doc "Beta 1/Beta 2" release-milestone apparatus
  (`beta-1-release-definition/decision/summary/candidate-validation-bundle/
  gap-analysis`, `beta-2-observation-gate`, `beta-tooling-observation-policy`,
  `beta-2-candidates/`) to `SolutionFolder/docs/archive/` via `git mv`
  (history preserved), each with a superseded banner. Fixed their internal
  cross-links to the new path.
- Consolidated README's ~15-item trace-artifact link list (dated logs/
  diagrams presented as "live MCP stdio validation") into one pointer, and
  added a caveat banner to the four `*-trace-workflow.md` docs: methodology
  still valid for future sprint validation, but referenced artifacts predate
  the reset and aren't current evidence.
- Updated `backlog/README.md` and `backlog/shiny-object-policy.md`, which
  referenced an "active beta-release path" that no longer exists under this
  framing.
- Promoted `current-bridge-capabilities.md` into `AI_START.md`'s required
  Core Grounding Set (was buried in an optional ~24-item list).
- **Late catch (commit `671e474`):** `AI_START.md` had a "Current Known
  Resume Point" section instructing any new session to "start here first" at
  a 2026-05-16 handoff, plus a duplicate trace-artifact list and an
  undisclosed ~75-line BlogAI/Auth narrative in the same "verified/
  implemented" language. Relabeled that whole section as a historical
  archive and removed the default "start here first" instruction so a fresh
  session doesn't silently land back on pre-reset state.

**CI fix (commit `5163aec`):**

- `.github/workflows/mcp-validation.yml` invoked `.\scripts\validate-mcp.ps1`,
  a path that stopped existing after the 2026-05-24 `SolutionFolder`
  consolidation moved the script to `SolutionFolder/scripts/`. Every run had
  failed since (33 consecutive failures, ~8 weeks). Fixed the path; first
  green run since 2026-05-24 confirmed at
  `https://github.com/BillKrat/vs-mcp-bridge/actions/runs/29705467925`.

**Repo/branch cleanup (2026-07-19, via git + Claude-in-Chrome browser
automation):**

- Deleted 3 stale branches, all confirmed fully merged into `main` with zero
  unique commits: `copilot/create-minimal-csharp-solution`,
  `feature/approval-apply-ui-slice`, `feature/mef-tool-discovery-slice`.
- Deletion was initially blocked by a GitHub ruleset ("Basic," targeting all
  4 branches, GH013 error). Resolved by creating a new "Main protection"
  ruleset scoped to `main` only (same two rules: restrict deletions, block
  force pushes) and deleting "Basic." See memory
  `reference_github_ruleset_gotchas.md` for the `/rules/<id>` (read-only) vs
  `/settings/rules/<id>` (editable) URL gotcha that caused early confusion.
- Only `main` remains, locally and on `origin`.

## What Was Validated

- CI green on the latest push (`671e474`) —
  `https://github.com/BillKrat/vs-mcp-bridge/actions`.
- `git status` clean, `main` in sync with `origin/main`, no unpushed commits.
- Repo-wide greps for `Beta 1`/`Beta 2`/"works end to end"/"is verified" etc.
  across all non-archived, non-historical docs come back clean except for
  our own negation sentences ("nothing here is verified...").
- `git branch -r` shows only `origin/main`; ruleset settings confirmed via
  browser (`Main protection` targets `main` only, 2 rules active).

## Current Blockers / Known Issues

- **Not addressed today, flagged as a known follow-up:** the underlying
  ~40 dated session-handoff files referenced from `AI_START.md`'s archived
  section (lines ~95–184), and the BlogAI/Auth/Global-WebNet narrative they
  describe, still use "implemented"/"durable validation evidence" language.
  The section is now clearly labeled historical and no longer defaults new
  sessions there, but the individual files themselves weren't repositioned —
  that's a separate sub-project scope Bill hasn't reviewed against the new
  early-design framing yet.
- `AI_STOP.md`'s embedded "Latest Session Closeout" log (below) was stale
  (dated 2026-05-08, referencing a different branch) — updated as part of
  this handoff.

## Likely Relevant Files

- `README.md`, `AGENTS.md`, `AI_START.md`
- `SolutionFolder/docs/current-bridge-capabilities.md`
- `SolutionFolder/docs/ARCHITECTURE.md`
- `SolutionFolder/docs/VS_MCP_BRIDGE_TECHNICAL_ANALYSIS.md`
- `SolutionFolder/docs/archive/` (superseded Beta docs)
- `.github/workflows/mcp-validation.yml`
- `backlog/README.md`, `backlog/shiny-object-policy.md`

## Recommended Next Steps

1. Per Bill's stated process: complete architectural design, run a gap
   analysis, prioritize the resulting backlog, then start sprints —
   capability claims only get added back once a sprint delivers them.
2. When BlogAI/Auth work is revisited, apply the same early-design
   repositioning treatment to that narrative (currently just contained, not
   fixed) — see "Current Blockers" above.
3. Branches will be created per sprint going forward; `main` is the only
   branch that should exist otherwise.

## Suggested Resume Prompt

`Read AI_START.md and SolutionFolder/docs/current-bridge-capabilities.md, then resume from SolutionFolder/docs/session-handoffs/2026-07-19-early-design-repositioning-and-repo-cleanup.md.`
