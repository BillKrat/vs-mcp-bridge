# Pre-Publish Inspection - vs-mcp-bridge-blog-series-part-5 - 2026-05-16

## Scope

This report is a targeted read-only inspection for the previously blocked BlogEngine row:

- Slug: `vs-mcp-bridge-blog-series-part-5`
- DB PostRowID: `147`
- DB BlogID: `27604f05-86ad-47ef-9e05-950bb762570c`
- DB PostID: `bd97e5de-4b4e-4660-98f1-465bd53eddec`

No database writes, reload calls, public site changes, or canonical post rewrites were performed.

## Inputs

- Fresh compare report: `SolutionFolder/docs/blogs/prepublish-compare-vs-mcp-bridge-blog-series-part-5-20260516.md`
- Prior blocked-row report: `SolutionFolder/docs/blogs/prepublish-blocked-row-diff-20260516.md`
- Preserved export baseline: `SolutionFolder/docs/blogs/source-of-truth/db-export-20260516/vs-mcp-bridge-blog-series-part-5/`
- Fresh current live DB export: `SolutionFolder/docs/blogs/source-of-truth/prepublish-inspections/20260516/vs-mcp-bridge-blog-series-part-5-20260516-192412/current-live-db/`
- Canonical repo source: `SolutionFolder/docs/blogs/posts/vs-mcp-bridge-blog-series-part-5/`

## Result Summary

| Check | Result |
| --- | --- |
| Fresh compare generated | True |
| Current live DB row exported | True |
| Body content changed since preserved export | False |
| Title changed since preserved export | False |
| Slug changed since preserved export | False |
| Status changed since preserved export | False |
| DateModified changed since preserved export | False |
| Category drift remains after targeted export | False |
| Tag drift remains after targeted export | False |
| Canonical content differs from current DB body | True |
| Stale direct-link findings in canonical content | 0 |
| Intentional canonical BlogEngine tokens | `[Page:Playbook]` |
| Current DB body safe to overwrite with canonical content | Yes, with current live taxonomy preserved |

## Content Hashes

| Source | SHA-256 |
| --- | --- |
| Preserved export body | `7a7b85913f888b3f21ee2a641ef1b836fcff8da3fb3137fd1ad640cd43bc1007` |
| Current live DB body | `7a7b85913f888b3f21ee2a641ef1b836fcff8da3fb3137fd1ad640cd43bc1007` |
| Canonical repo body | `d2f944be69eb510f626b02eba7f850b2abf8f22ac1bdbd727eb76a54ed283811` |

The current live DB body still matches the preserved `db-export-20260516` body exactly.
The canonical body intentionally differs because the repo cleanup rewrote this post around compiled discovery, opt-in MEF discovery, catalog metadata, executor-owned policy/audit boundaries, and durable Mermaid traces.

## Metadata Comparison

| Field | Preserved export | Current live DB | Canonical repo |
| --- | --- | --- | --- |
| Title | VS MCP Bridge Blog Series: Part 5 | VS MCP Bridge Blog Series: Part 5 | VS MCP Bridge Blog Series: Part 5 |
| Slug | vs-mcp-bridge-blog-series-part-5 | vs-mcp-bridge-blog-series-part-5 | vs-mcp-bridge-blog-series-part-5 |
| Status | published | published | published |
| DateCreated | 2026-04-11T18:10:00.000 | 2026-04-11T18:10:00.000 | 2026-04-11T18:10:00.000 |
| DateModified | 2026-04-23T06:18:01.040 | 2026-04-23T06:18:01.040 | N/A |
| Categories | AI Systems Author, MCP Bridge | AI Systems Author, MCP Bridge | AI Systems Author, MCP Bridge |
| Tags | AI Tooling, Failure Recovery, MCP, Runtime validation, testing, Visual Studio, VS MCP Bridge, VSIX | AI Tooling, Failure Recovery, MCP, Runtime validation, testing, Visual Studio, VS MCP Bridge, VSIX | VS MCP Bridge, MCP, Bridge Tools, MEF, Discovery, Extensibility, Diagnostics, Mermaid, Architecture |

The prior blocked-row report classified this slug as `mechanical-taxonomy-drift`.
The fresh targeted export does not find remaining category or tag drift: current live DB categories and tags match the preserved export values exactly.

The generic fresh compare still reports `categories = False` and `tags = False` in its metadata table while also displaying the same current/export taxonomy values.
Treat the current blocker as cleared by this targeted inspection rather than as body, identity, status, date, category, or tag drift.

## Token Review

Intentional canonical BlogEngine tokens:

- `[Page:Playbook]`

Current live DB body tokens:

- `[Page:Playbook]`

Preserved export body tokens:

- `[Page:Playbook]`

The `[Page:Playbook]` token is present in the preserved export, current live DB body, and canonical repo body.
The next publish-review update must preserve this token in the DB body unless a separate `GwnWikiExtension` mapping decision changes token policy.

## Stale Link Review

| Source | Finding |
| --- | --- |
| Preserved export body | `https://github.com/BillKrat/vs-mcp-bridge/blob/feature/approval-apply-ui-slice/SolutionFolder/docs/ARCHITECTURE.md` |
| Current live DB body | `https://github.com/BillKrat/vs-mcp-bridge/blob/feature/approval-apply-ui-slice/SolutionFolder/docs/ARCHITECTURE.md` |
| Canonical repo body | None |

The stale feature-branch link exists only in the preserved/runtime baseline.
The canonical post uses the main-branch architecture link, so publishing canonical content would remove this stale direct link while preserving `[Page:Playbook]`.

## Safety Decision

The current live DB row is safe for the next guarded single-post publish-review update because:

- body content still matches the preserved export baseline;
- title, slug, status, created date, modified date, BlogID, PostID, and PostRowID are unchanged;
- current live taxonomy matches the preserved export taxonomy;
- canonical content intentionally differs and contains the expected cleaned article body;
- canonical stale-link count is zero;
- `[Page:Playbook]` is present in canonical content and should be preserved;
- no public site behavior was changed in this slice.

The next publish-review update should preserve the current live taxonomy, matching the proven workflow used for prior single-post updates.

## Recommended Publish Decision

Proceed with a single-post publish-review update for `vs-mcp-bridge-blog-series-part-5` in the next slice.

Do not batch publish. Use the guarded script, export before and after, call the BlogAI reload endpoint once, verify rendered canonical markers, and confirm `[Page:Playbook]` remains present in the DB body before touching the next row.
