# Pre-Publish Inspection - how-stdio-works-in-vs-mcp-bridge - 2026-05-16

## Scope

This report is a targeted read-only inspection for the previously blocked BlogEngine row:

- Slug: `how-stdio-works-in-vs-mcp-bridge`
- DB PostRowID: `155`
- DB BlogID: `27604f05-86ad-47ef-9e05-950bb762570c`
- DB PostID: `d0541943-0de1-4c25-a7af-9950c55f1591`

No database writes, reload calls, public site changes, or canonical post rewrites were performed.

## Inputs

- Fresh compare report: `SolutionFolder/docs/blogs/prepublish-compare-how-stdio-works-in-vs-mcp-bridge-20260516.md`
- Prior blocked-row report: `SolutionFolder/docs/blogs/prepublish-blocked-row-diff-20260516.md`
- Preserved export baseline: `SolutionFolder/docs/blogs/source-of-truth/db-export-20260516/how-stdio-works-in-vs-mcp-bridge/`
- Fresh current live DB export: `SolutionFolder/docs/blogs/source-of-truth/prepublish-inspections/20260516/how-stdio-works-in-vs-mcp-bridge-20260516-194637/current-live-db/`
- Canonical repo source: `SolutionFolder/docs/blogs/posts/how-stdio-works-in-vs-mcp-bridge/`

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
| Intentional canonical BlogEngine tokens | `[Page:Stdio]` |
| Current DB body safe to overwrite with canonical content | Yes, with current live taxonomy preserved |

## Content Hashes

| Source | SHA-256 |
| --- | --- |
| Preserved export body | `27874b495d02eb1a60865210b54837ac0f6359c33fe15430f4f99a130296ae81` |
| Current live DB body | `27874b495d02eb1a60865210b54837ac0f6359c33fe15430f4f99a130296ae81` |
| Canonical repo body | `29d6e0fb2554f601703e83becda8d577be5546dd1f97b142951853b696f84ea7` |

The current live DB body still matches the preserved `db-export-20260516` body exactly.
The canonical body intentionally differs because the repo cleanup rewrote this post around the current stdio transport boundary, clean stdout expectations, named-pipe activation handoff, and BridgeToolExecutor separation.

## Metadata Comparison

| Field | Preserved export | Current live DB | Canonical repo |
| --- | --- | --- | --- |
| Title | How stdio Works in VS MCP Bridge | How stdio Works in VS MCP Bridge | How stdio Works in VS MCP Bridge |
| Slug | how-stdio-works-in-vs-mcp-bridge | how-stdio-works-in-vs-mcp-bridge | how-stdio-works-in-vs-mcp-bridge |
| Status | published | published | published |
| DateCreated | 2026-04-19T23:24:00.000 | 2026-04-19T23:24:00.000 | 2026-04-19T23:24:00.000 |
| DateModified | 2026-04-27T10:21:07.833 | 2026-04-27T10:21:07.833 | N/A |
| Categories | AI Systems Author, MCP Bridge | AI Systems Author, MCP Bridge | AI Systems Author, MCP Bridge |
| Tags | AI Tooling, MCP, Named Pipes, stdio, Visual Studio, VS MCP Bridge, VSIX | AI Tooling, MCP, Named Pipes, stdio, Visual Studio, VS MCP Bridge, VSIX | VS MCP Bridge, MCP, stdio, Named Pipes, Visual Studio, VSIX, AI Tooling, Diagnostics |

The prior blocked-row report classified this slug as `mechanical-taxonomy-drift`.
The fresh targeted export does not find remaining category or tag drift: current live DB categories and tags match the preserved export values exactly.

The generic fresh compare still reports `categories = False` and `tags = False` in its metadata table while also displaying the same current/export taxonomy values.
Treat the current blocker as cleared by this targeted inspection rather than as body, identity, status, date, category, or tag drift.

## Token Review

Intentional canonical BlogEngine tokens:

- `[Page:Stdio]`

Current live DB body tokens:

- `[Page:Stdio]`

Preserved export body tokens:

- `[Page:Stdio]`

The `[Page:Stdio]` token is present in the preserved export, current live DB body, and canonical repo body.
The next publish-review update must preserve this token in the DB body unless a separate `GwnWikiExtension` mapping decision changes token policy.

## Stale Link Review

| Source | Finding |
| --- | --- |
| Preserved export body | None |
| Current live DB body | None |
| Canonical repo body | None |

No stale direct links were found in the preserved export, current live DB body, or canonical repo body.

## Safety Decision

The current live DB row is safe for the next guarded single-post publish-review update because:

- body content still matches the preserved export baseline;
- title, slug, status, created date, modified date, BlogID, PostID, and PostRowID are unchanged;
- current live taxonomy matches the preserved export taxonomy;
- canonical content intentionally differs and contains the expected cleaned article body;
- canonical stale-link count is zero;
- `[Page:Stdio]` is present in canonical content and should be preserved;
- no public site behavior was changed in this slice.

The next publish-review update should preserve the current live taxonomy, matching the proven workflow used for prior single-post updates.

## Recommended Publish Decision

Proceed with a single-post publish-review update for `how-stdio-works-in-vs-mcp-bridge` in the next slice.

Do not batch publish. Use the guarded script, export before and after, call the BlogAI reload endpoint once, verify rendered canonical markers, and confirm `[Page:Stdio]` remains present in the DB body before touching any other row.
