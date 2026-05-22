# BlogAI Stale Chrome Search Findings

## Purpose

This handoff records a short pressure-test session that used direct MCP inventory validation and a real BlogAI stale shared chrome/cache search workload.

No BlogAI features were implemented.
No authentication, deployment, project, package extraction, production BlogEngine.NET, database, or runtime code changes were made.

## Checkpoint

- Branch: `main`
- Starting HEAD: `e5ed588 Capture first BlogAI pressure-test findings`
- Starting state: `main...origin/main`, clean working tree
- Recent commits reviewed with `git log --oneline -8`

## Inventory Validation Result

Direct MCP inventory validation succeeded through a temporary stdio helper outside the repository:

- server: `VsMcpBridge.McpServer 1.0.0.0`
- `tools/list` count: `17`
- `tools/list` included `bridge_get_tool_inventory`: yes
- `bridge_get_tool_inventory` call error: false
- inventory result success: true
- inventory request id: `9a77087a97d04782a4f79f5bf9bf40dc`
- inventory tool count: `2`
- deterministic compiled tool order:
  - `bridge.bm25TextSearch`
  - `bridge.regexTextSearch`

Observed evidence is summarized in `artifacts/logs/blogai-stale-chrome-search-pressure-test-20260516.log`.

## Bridge Search Tool Callability

The compiled search tools were visible through inventory metadata, but they were not exposed as standalone MCP tool names in `tools/list`.

The real BlogAI search workload therefore used deterministic repository search with `rg` as the fallback path. This preserves the pressure-test finding: inventory can describe compiled bridge search tools, but this session did not have a direct MCP call surface for invoking `bridge.regexTextSearch` or `bridge.bm25TextSearch` as executable search tools.

## Search Workload Performed

Target issue:

- stale shared chrome/cache marker related to `feature/approval-apply-ui-slice`

Searches performed:

- `rg -n "feature/approval-apply-ui-slice" docs/blogs/posts`
- `rg -n "feature/approval-apply-ui-slice" docs/blogs docs/session-handoffs scripts -g "*.md" -g "*.ps1"`
- `rg -n "feature/approval-apply-ui-slice" docs/blogs/source-of-truth/widget-settings docs/blogs/source-of-truth/db-export-20260516 docs/blogs/source-of-truth/publish-review-updates -g "*.txt" -g "*.json" -g "*.html" -g "*.md"`
- `rg -n "feature/approval-apply-ui-slice" Y:\BlogAI\BlogEngine -g "*.cs" -g "*.config" -g "*.xml" -g "*.cshtml" -g "*.aspx" -g "*.ascx" -g "*.html" -g "*.js" -g "*.css"`
- `rg -n "be_widget_|clearCache|AccessAdminSettingsPages|posts/reload|BlogEngineReloadKey" Y:\BlogAI\BlogEngine -g "*.cs" -g "*.config" -g "*.xml" -g "*.cshtml" -g "*.aspx" -g "*.ascx"`

## Files And Artifacts Inspected

- `docs/blogs/stale-shared-feature-branch-link-inspection-20260516.md`
- `docs/blogs/blogengine-cache-clear-failure-inspection-20260516.md`
- `docs/blogs/final-rendered-route-verification-after-cache-clear-20260516.md`
- `docs/blogs/final-rendered-route-verification-20260516.md`
- `docs/blogs/source-of-truth/widget-settings/datastore-row-26512-20260516/`
- `docs/blogs/source-of-truth/widget-settings/datastore-row-26512-20260516-after-update/`
- `docs/blogs/source-of-truth/db-export-20260516/`
- `docs/blogs/source-of-truth/publish-review-updates/20260516/`
- `Y:\BlogAI\BlogEngine\BlogEngine.NET\Custom\Widgets\Common.cs`
- `Y:\BlogAI\BlogEngine\BlogEngine.NET\AppCode\Api\SettingsController.cs`
- `Y:\BlogAI\BlogEngine\BlogEngine.NET\AppCode\WebUtils.cs`
- `Y:\BlogAI\BlogEngine\BlogEngine.NET\App_Data\rights.xml`

## Stale Marker Status

Current canonical post source:

- `docs/blogs/posts` had no matches for `feature/approval-apply-ui-slice`.

Local BlogAI source:

- `Y:\BlogAI\BlogEngine` had no matches for `feature/approval-apply-ui-slice` in the searched source/config/web files.

Preserved repository evidence:

- The marker remains in preserved historical reports and source-of-truth artifacts.
- Pre-update widget row `26512` evidence contains the old TextBox widget content and feature-branch links.
- Preserved DB export and before-update publish-review artifacts still contain old feature-branch `docs/ARCHITECTURE.md` links for several rows.
- Rendered-route reports record that all 14 checked routes returned HTTP 200 and displayed expected post-body canonical markers while also showing the stale marker in shared chrome.

Current conclusion:

- The stale marker remains in repo evidence because the repo intentionally preserves historical baseline, before-update, and failure artifacts.
- The marker is not present in canonical post bodies or local BlogAI source.
- The likely current issue remains cached shared widget/page chrome, not stale canonical content.

## Likely Next Operational Action

Do not publish more posts and do not rewrite post bodies for this marker.

The next operational action should be:

1. Inspect deployed BlogAI logs or deployed source for the prior `PUT /api/settings?action=clearCache` HTTP 500.
2. Confirm whether deployed `/api/settings?action=clearCache` requires a real BlogEngine administrator session, anti-forgery/session state, a different blog context, or a deployed-only implementation.
3. Use one known-good cache remediation path:
   - authenticated BlogEngine admin cache clear, if verified and approved
   - admin UI cache/settings path, if safer
   - explicitly approved app pool recycle or app restart
4. Re-run `scripts/blog-publishing/Test-BlogRenderedRoutes.ps1` across the 14 routes.

Expected success criteria remain:

- all 14 routes return HTTP 200
- expected post-specific canonical markers remain visible
- `feature/approval-apply-ui-slice` is absent from all 14 routes
- shared widget text shows main-branch BlogAI/VS MCP Bridge links

## MCP And Tooling Friction

- Fresh direct MCP inventory validation worked.
- The validation required a temporary helper outside the repo rather than a short committed repo-root command.
- `scripts/validate-mcp.ps1` still validates a different path and is documented as having helper-build hang risk in earlier runs.
- The compiled search tools are visible in inventory metadata but were not callable as standalone MCP tools in this session.
- The real BlogAI search workload still required manual `rg` commands and manual synthesis across docs, source-of-truth exports, rendered-route reports, and adjacent BlogAI source.

## Recommended Next Slice

Create a narrow repo-owned validation helper or documented command path for the MCP inventory diagnostic:

- validate initialize
- validate `tools/list`
- validate `bridge_get_tool_inventory`
- assert `bridge.bm25TextSearch` and `bridge.regexTextSearch`
- write a concise log artifact suitable for handoffs

Keep this as tooling validation only. Do not add BlogAI auth, deployment, package extraction, or production BlogEngine.NET changes.

After that, the next BlogAI operational slice should inspect deployed cache-clear failure evidence and choose one approved cache remediation path.

## Follow-Up Note

The next tooling slice exposed the compiled regex search capability through MCP as `bridge_regex_text_search`.
That wrapper executes `bridge.regexTextSearch` through `BridgeToolExecutor` and accepts only explicit `inputText` or `entries` from the MCP request.
See `docs/session-handoffs/2026-05-16-mcp-regex-search-validation.md` and `artifacts/logs/mcp-regex-search-trace-20260516.log`.

## Resume Guidance

Start the next session with:

1. `AI_START.md`
2. this handoff
3. `artifacts/logs/blogai-stale-chrome-search-pressure-test-20260516.log`
4. `docs/blogs/blogengine-cache-clear-failure-inspection-20260516.md`
5. `docs/blogs/stale-shared-feature-branch-link-inspection-20260516.md`
