# BlogAI Pressure-Test Findings

## Purpose

This handoff records the first short BlogAI pressure-test pass using current repository context and available MCP/tooling evidence.

No BlogAI features were implemented.
No authentication, deployment, project, package, BlogEngine.NET, database, or runtime code changes were made.

## Checkpoint

- Branch: `main`
- Starting HEAD: `5ffd255 Add BlogAI first pressure-test session plan`
- Starting state: `main...origin/main`, clean working tree
- Recent commits reviewed with `git log --oneline -8`

## What Was Inspected

- `AGENTS.md`
- `AI_START.md`
- `SolutionFolder/docs/AI_STOP.md`
- `SolutionFolder/docs/ARCHITECTURE.md`
- `SolutionFolder/docs/blogai-functional-pressure-test-plan.md`
- `SolutionFolder/docs/blogai-first-pressure-test-session.md`
- `SolutionFolder/docs/session-handoffs/2026-05-16-blogai-platform-direction.md`
- MCP inventory evidence:
  - `SolutionFolder/docs/session-handoffs/2026-05-16-mcp-tool-inventory-live-validation.md`
  - `SolutionFolder/artifacts/logs/mcp-tool-inventory-live-validation-20260516.log`
  - `SolutionFolder/docs/session-handoffs/2026-05-16-mcp-tool-inventory-validation.md`
  - `SolutionFolder/docs/session-handoffs/2026-05-16-tool-inventory-validation.md`
- BlogAI/blog source-of-truth and publishing artifacts:
  - `SolutionFolder/docs/blogs/README.md`
  - `SolutionFolder/docs/blogs/blog-cleanup-status-20260516.md`
  - `SolutionFolder/docs/blogs/blog-publishing-review-plan-20260516.md`
  - `SolutionFolder/docs/blogs/source-of-truth/db-export-20260516/manifest.json`
  - `SolutionFolder/scripts/blog-publishing/README.md`
  - targeted searches across `SolutionFolder/docs/blogs/`, `SolutionFolder/docs/session-handoffs/`, and `SolutionFolder/scripts/blog-publishing/`
- Local BlogAI source references from prior docs were checked read-only under `Y:\BlogAI\BlogEngine` for cache/auth markers.

## MCP Inventory Status

The current Codex tool surface did not expose a callable `bridge_get_tool_inventory` tool; deferred tool discovery also returned no matching tool.

Repository evidence still confirms the MCP diagnostic and compiled tools are present:

- prior live MCP validation listed 17 MCP tools including `bridge_get_tool_inventory`
- `bridge_get_tool_inventory` returned two compiled bridge tools in deterministic order:
  - `bridge.bm25TextSearch`
  - `bridge.regexTextSearch`
- the diagnostic response was metadata-only and did not execute bridge tools, trigger approval, call ChatEngine, call the VSIX pipe, or expose payloads/secrets

This is a pressure-test finding: the platform has the inventory diagnostic, but this Codex session did not have a direct bridge-tool invocation path available from the active tool surface.

## What Worked Well

- The startup docs now route cleanly from high-level agent guidance to the BlogAI pressure-test plan and first-session checklist.
- Existing MCP inventory artifacts provide enough evidence to confirm the expected compiled bridge search tools without relying on chat history.
- BlogAI repo-backed artifacts are discoverable and structured:
  - canonical posts live under `SolutionFolder/docs/blogs/posts/<slug>/`
  - preserved DB exports live under `SolutionFolder/docs/blogs/source-of-truth/db-export-20260516/`
  - widget and plugin settings exports are preserved under `SolutionFolder/docs/blogs/source-of-truth/`
  - publish-review and rendered-route reports preserve the prior operational trail
- Search quickly connected the current BlogAI blocker to durable evidence:
  - all 14 rendered routes previously showed expected post-body markers
  - all 14 still showed stale shared `feature/approval-apply-ui-slice` chrome
  - preserved widget row evidence points at cached TextBox widget content
  - local BlogAI source shows `be_widget_<id>` cache keys and `PUT /api/settings?action=clearCache` behind admin settings authorization

## Friction And Gaps Found

- No direct MCP bridge-tool invocation path was available from this Codex tool environment, even though repository evidence confirms the MCP inventory diagnostic exists.
- The current search tools are useful for code/content triage but are not yet surfaced as a convenient cross-repo BlogAI workflow. This matters because the immediate BlogAI question spans `Y:\vs-mcp-bridge` docs and `Y:\BlogAI` source.
- Existing docs identify the deployed `/api/posts/reload/{blogId}` endpoint, but local BlogAI source inspection did not find that endpoint. The deployed/local source mismatch remains a diagnostic gap.
- Cache-clear diagnosis still depends on manually correlating rendered-route reports, database/widget exports, local BlogAI source, and prior HTTP results.
- The prior `SolutionFolder/scripts/validate-mcp.ps1` helper has documented hang risk, so direct MCP validation is possible but not frictionless as a routine first step.

## Missing Tool Candidates

- A direct, low-friction MCP inventory validation command that can run from the repo root and emit a concise, committed-friendly transcript for `bridge_get_tool_inventory`.
- A bridge search workflow wrapper that can run regex/BM25 searches over a declared document set, including repo docs plus an explicitly allowed adjacent checkout such as `Y:\BlogAI`.
- A source-of-truth inventory summarizer for BlogAI docs that reports canonical posts, preserved exports, widget/plugin settings exports, publish-review reports, and rendered-route reports without manual `rg --files` filtering.
- A cache-diagnostic evidence collector that correlates:
  - rendered route markers
  - widget/settings exports
  - local source cache keys
  - relevant BlogEngine API/auth source
  - prior reload/cache-clear attempts
- A workflow trace candidate generator that turns observed command/search results into a Mermaid-ready trace outline before full trace capture.

## Candidate Trace Workflows

Capture these later as explicit trace/evidence slices:

1. `bridge_get_tool_inventory` direct MCP validation from the current repo checkpoint.
   - Prove tool listing and inventory response still include `bridge.regexTextSearch` and `bridge.bm25TextSearch`.

2. BlogAI stale shared chrome triage.
   - Trace route verification, stale marker search, widget settings export, local BlogAI cache source inspection, and recommended cache-remediation decision.

3. Cross-repo BlogAI source/docs search.
   - Trace a real question from BlogAI docs to `Y:\BlogAI` source using regex/BM25 search inputs and outputs.

4. One-slug read-only publish readiness check.
   - Re-run the existing compare-only path for one approved slug and capture the full command/report/evidence flow without database writes.

## Recommended Next MCP/Tooling Slice

Build a docs-and-validation slice around direct MCP inventory plus search pressure:

- do not add BlogAI features
- run or repair the shortest reliable repo-root validation path for `bridge_get_tool_inventory`
- capture a fresh inventory transcript showing `bridge.regexTextSearch` and `bridge.bm25TextSearch`
- use one real BlogAI question as the search workload
- document whether the current bridge search tools are enough or whether a cross-repo/search-wrapper tool is justified

The best real question is the stale shared chrome/cache path because it is operationally meaningful and already bounded by existing evidence.

## Recommended First BlogAI Implementation Slice

Do not start with auth, OAuth/OpenID, API deployment, package extraction, or BlogEngine migration.

The first BlogAI implementation slice should be a local-only, operator-safe cache diagnostics slice:

- inspect the deployed/local mismatch around `/api/posts/reload/{blogId}` and `/api/settings?action=clearCache`
- confirm the real admin/auth path for clearing BlogEngine cache without implementing new auth
- capture logs or source evidence for the prior HTTP 500
- define the narrowest safe remediation path for stale shared widget/page chrome
- keep production publishing automation deferred

Implementation should begin only after the next MCP/tooling slice proves the tool platform can inventory, search, and trace this workflow cleanly.

## Deferred Items

- BlogAI feature implementation
- authentication implementation
- OAuth/OpenID
- `api.global-webnet.com` services
- deployment changes
- new projects or packages
- BlogEngine.NET migration
- production publishing automation
- package extraction
- database writes
- cache-clear retries against production without explicit approval and known auth behavior

## Resume Guidance

Start the next slice by reading:

1. `AI_START.md`
2. `SolutionFolder/docs/blogai-functional-pressure-test-plan.md`
3. `SolutionFolder/docs/blogai-first-pressure-test-session.md`
4. this handoff
5. `SolutionFolder/docs/session-handoffs/2026-05-16-mcp-tool-inventory-live-validation.md`
6. `SolutionFolder/docs/blogs/blogengine-cache-clear-failure-inspection-20260516.md`

Then choose between:

- MCP/tooling slice: fresh inventory validation plus real BlogAI search workload
- BlogAI implementation slice: local-only cache/auth path verification, with no new auth or deployment work
