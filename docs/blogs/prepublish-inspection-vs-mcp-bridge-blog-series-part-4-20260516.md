# Pre-Publish Inspection - vs-mcp-bridge-blog-series-part-4 - 2026-05-16

## Scope

This report is a targeted read-only inspection for the previously blocked BlogEngine row:

- Slug: `vs-mcp-bridge-blog-series-part-4`
- DB PostRowID: `146`
- DB BlogID: `27604f05-86ad-47ef-9e05-950bb762570c`
- DB PostID: `f62f7756-269a-4d49-a87d-c0394c7627d9`

No database writes, reload calls, public site changes, or canonical post rewrites were performed.

## Live Vs Trial Mapping

Part 4 has two related repo folders with different meanings:

| Meaning | Folder | Slug | DB PostID | Status |
| --- | --- | --- | --- | --- |
| Live DB/canonical Part 4 post inspected in this slice | `docs/blogs/posts/vs-mcp-bridge-blog-series-part-4/` | `vs-mcp-bridge-blog-series-part-4` | `f62f7756-269a-4d49-a87d-c0394c7627d9` | Published |
| Trial draft context only | `docs/blogs/posts/vs-mcp-bridge-blog-series-part-4-repo-trial/` | `vs-mcp-bridge-blog-series-part-4-repo-trial` | `91a217f2-d9fc-4677-9e39-8f460d9e636d` | Draft |

This inspection used the live slug only: `vs-mcp-bridge-blog-series-part-4`.
The trial draft folder was read only for mapping context and was not modified or published.

## Inputs

- Fresh compare report: `docs/blogs/prepublish-compare-vs-mcp-bridge-blog-series-part-4-20260516.md`
- Prior blocked-row report: `docs/blogs/prepublish-blocked-row-diff-20260516.md`
- Preserved export baseline: `docs/blogs/source-of-truth/db-export-20260516/vs-mcp-bridge-blog-series-part-4/`
- Fresh current live DB export: `docs/blogs/source-of-truth/prepublish-inspections/20260516/vs-mcp-bridge-blog-series-part-4-20260516-185918/current-live-db/`
- Canonical live repo source: `docs/blogs/posts/vs-mcp-bridge-blog-series-part-4/`
- Trial draft context source: `docs/blogs/posts/vs-mcp-bridge-blog-series-part-4-repo-trial/`

## Result Summary

| Check | Result |
| --- | --- |
| Fresh compare generated | True |
| Current live DB row exported | True |
| Live slug used | `vs-mcp-bridge-blog-series-part-4` |
| Trial slug excluded from compare/export | True |
| Trial draft modified | False |
| Body content changed since preserved export | False |
| Title changed since preserved export | False |
| Slug changed since preserved export | False |
| Status changed since preserved export | False |
| DateModified changed since preserved export | False |
| Category drift remains after targeted export | False |
| Tag drift remains after targeted export | False |
| Canonical content differs from current DB body | True |
| Stale direct-link findings in canonical content | 0 |
| Intentional canonical BlogEngine tokens | None |
| Safe for next single-post review update | Yes, with taxonomy preserved from the current live row |

## Content Hashes

| Source | SHA-256 |
| --- | --- |
| Preserved export body | `7057316908f8d1b111d82eae0e82a2e96616e97113f8527ed1e0764e3eb55762` |
| Current live DB body | `7057316908f8d1b111d82eae0e82a2e96616e97113f8527ed1e0764e3eb55762` |
| Canonical live repo body | `ca34e445f2dbc801f3584549f0c85b04f228b25410d3f9a22af32733db895ba4` |

The current live DB body still matches the preserved `db-export-20260516` body exactly.
The canonical live body intentionally differs because the repo cleanup rewrote this post around compiled bridge tools, catalog/executor boundaries, approval-aware execution, audit metadata, and observable tool contracts.

## Metadata Comparison

| Field | Preserved export | Current live DB | Canonical live repo |
| --- | --- | --- | --- |
| Title | VS MCP Bridge Blog Series: Part 4 | VS MCP Bridge Blog Series: Part 4 | VS MCP Bridge Blog Series: Part 4 |
| Slug | vs-mcp-bridge-blog-series-part-4 | vs-mcp-bridge-blog-series-part-4 | vs-mcp-bridge-blog-series-part-4 |
| Status | published | published | published |
| DateCreated | 2026-04-11T18:01:00.000 | 2026-04-11T18:01:00.000 | 2026-04-11T18:01:00.000 |
| DateModified | 2026-04-12T10:30:50.303 | 2026-04-12T10:30:50.303 | N/A |
| Categories | AI Systems Author, MCP Bridge | AI Systems Author, MCP Bridge | AI Systems Author, MCP Bridge |
| Tags | AI Tooling, Failure Recovery, MCP, Named Pipes, Runtime validation, Visual Studio, VS MCP Bridge, VSIX | AI Tooling, Failure Recovery, MCP, Named Pipes, Runtime validation, Visual Studio, VS MCP Bridge, VSIX | VS MCP Bridge, MCP, Bridge Tools, Compiled Tools, Approval, Audit, Diagnostics, Regex Search, Architecture |

The prior blocked-row report classified this slug as `mechanical-taxonomy-drift`.
The fresh targeted export does not find remaining category or tag drift: current live DB categories and tags match the preserved export values exactly.

The generic fresh compare still reports `categories = False` and `tags = False` in its metadata table while also displaying the same current/export taxonomy values.
Treat the current blocker as cleared by this targeted inspection rather than as body, identity, status, date, category, or tag drift.

## Link And Token Review

Intentional canonical BlogEngine tokens:

- None

Current live DB body tokens:

- None

No token preservation requirement applies to this post.

Stale direct-link findings:

| Source | Finding |
| --- | --- |
| Preserved export body | `https://github.com/BillKrat/vs-mcp-bridge/blob/feature/approval-apply-ui-slice/docs/ARCHITECTURE.md` |
| Current live DB body | `https://github.com/BillKrat/vs-mcp-bridge/blob/feature/approval-apply-ui-slice/docs/ARCHITECTURE.md` |
| Canonical live repo body | None |

The stale feature-branch link exists only in the preserved/runtime baseline.
The canonical live post uses the main-branch architecture link, so publishing canonical content would remove this stale direct link.

## Safety Decision

The current live DB row is safe for the next guarded single-post publish-review update because:

- body content still matches the preserved export baseline;
- title, slug, status, created date, modified date, BlogID, PostID, and PostRowID are unchanged;
- current live taxonomy matches the preserved export taxonomy;
- canonical live content intentionally differs and contains the expected cleaned article body;
- canonical stale-link count is zero;
- no intentional BlogEngine tokens are present in canonical content;
- the live slug was inspected, not the trial draft slug;
- the trial draft was not modified.

The next publish-review update should preserve the current live taxonomy, matching the proven workflow used for prior single-post updates.

## Recommended Publish Decision

Proceed with a single-post publish-review update for `vs-mcp-bridge-blog-series-part-4` in the next slice.

Do not batch publish. Use the guarded script, export before and after, call the BlogAI reload endpoint once, and verify rendered canonical markers before touching the next row.
