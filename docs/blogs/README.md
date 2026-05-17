# Blog Post Source Of Truth

The canonical repository-backed blog format now lives under `docs/blogs/posts/`.

Each post is stored as:

```text
docs/blogs/posts/<slug>/
  post.json
  content.html
```

Rules for this structure:

- `docs/blogs/posts` is the new source of truth for materialized posts in the repository.
- `post.json` holds the portable post metadata, including identifiers, publishing flags, and taxonomy arrays.
- `content.html` holds the article-body HTML fragment for the post.
- `post.json` and `content.html` together define one complete post.
- legacy files under `docs/blogs/posted` are preserved temporarily as migration input and reference material.

Current migration note:

- this baseline materialization preserves existing content and metadata as available from repo inputs; it does not rewrite post bodies, normalize wording, or deploy content.

## Current Alignment Documents

- `blog-alignment-inventory-20260516.md` records the initial comparison between the preserved database export, canonical repo posts, and rendered `https://www.global-webnet.com` pages.
- `blog-source-reconciliation-20260516.md` records the follow-up source-coverage reconciliation. It materializes the active BlogAI candidate posts that were missing from `docs/blogs/posts/`, separates the live Part 4 post from the repo trial draft, and documents that rendered `adventuresontheedge.net` links come from BlogEngine token expansion rather than explicit old-domain URLs in canonical article HTML.
- `blog-cleanup-status-20260516.md` records the current aligned/partially reviewed/untouched blog status, architecture narrative coverage, Mermaid references, intentional BlogEngine tokens, and remaining publishing gaps.
- `blog-publishing-review-plan-20260516.md` records the publishing-readiness plan for the cleaned canonical post set, including DB target identifiers, intentional tokens, Mermaid references, stale-link review, publish order, and post-publish verification checklist.
- `prepublish-compare-vs-mcp-bridge-blog-series-part-1-20260516.md` records the first read-only pre-publish compare for Part 1 against the live DB row, preserved DB export, and canonical repo source.
- `prepublish-compare-vs-mcp-bridge-blog-series-part-2-20260516.md` records the read-only pre-publish compare for Part 2 before the single-post review update.
- `prepublish-compare-vs-mcp-bridge-blog-series-part-3-20260516.md` records the fresh read-only pre-publish compare for Part 3 before targeted blocked-row inspection.
- `prepublish-compare-vs-mcp-bridge-blog-series-part-5-20260516.md` records the fresh read-only pre-publish compare for Part 5 before targeted blocked-row inspection.
- `prepublish-compare-vs-mcp-bridge-blog-series-part-7-20260516.md` records the read-only pre-publish compare for Part 7 before the single-post review update.
- `prepublish-compare-understanding-a-named-pipe-listener-20260516.md` records the read-only pre-publish compare for the named-pipe listener post before the single-post review update.
- `prepublish-compare-understanding-local-mcp-server-over-stdio-and-local-only-communication-over-a-named-pipe-20260516.md` records the read-only pre-publish compare for the stdio/named-pipe overview post before the single-post review update.
- `prepublish-compare-wpf-vsix-threading-understanding-ui-switching-async-behavior-and-pipe-safety-20260516.md` records the read-only pre-publish compare for the WPF/VSIX threading post before the single-post review update.
- `prepublish-compare-why-vsix-project-should-target-net-framework-4-7-2-20260516.md` records the read-only pre-publish compare for the VSIX .NET Framework 4.7.2 targeting post before the single-post review update.
- `prepublish-compare-inference-driven-software-design-with-copilot-pros-and-cons-20260516.md` records the read-only pre-publish compare for the inference-driven development post before the single-post review update.
- `prepublish-compare-understanding-ai-chat-sessions-models-and-agents-20260516.md` records the fresh read-only pre-publish compare for the AI chat sessions/models/agents post before targeted blocked-row inspection.
- `prepublish-compare-ready-posts-20260516.md` records the batch read-only pre-publish compare for all 14 posts marked ready for publishing review.
- `prepublish-blocked-row-diff-20260516.md` records the read-only inspection of the six rows whose current live DB state no longer matched the preserved export baseline.
- `prepublish-inspection-vs-mcp-bridge-blog-series-part-3-20260516.md` records the targeted read-only inspection that cleared Part 3 for the next guarded single-post review update.
- `prepublish-inspection-vs-mcp-bridge-blog-series-part-4-20260516.md` records the targeted read-only inspection that cleared the live Part 4 row for the next guarded single-post review update while leaving the repo-trial draft untouched.
- `prepublish-inspection-vs-mcp-bridge-blog-series-part-5-20260516.md` records the targeted read-only inspection that cleared Part 5 for the next guarded single-post review update and documents the intentional `[Page:Playbook]` token.
- `prepublish-inspection-understanding-ai-chat-sessions-models-and-agents-20260516.md` records the targeted read-only inspection that cleared the AI chat sessions/models/agents row for the next guarded single-post review update.
- `publish-review-update-vs-mcp-bridge-blog-series-part-1-20260516.md` records the first single-post review update, before/after exports, and rendered-cache behavior for Part 1.
- `publish-review-update-vs-mcp-bridge-blog-series-part-2-20260516.md` records the Part 2 single-post review update, before/after exports, BlogAI reload result, and rendered-route verification.
- `publish-review-update-vs-mcp-bridge-blog-series-part-3-20260516.md` records the Part 3 single-post review update, before/after exports, BlogAI reload result, and rendered-route verification.
- `publish-review-update-vs-mcp-bridge-blog-series-part-4-20260516.md` records the live Part 4 single-post review update, before/after exports, BlogAI reload result, rendered-route verification, and confirmation that the repo-trial draft remained untouched.
- `publish-review-update-vs-mcp-bridge-blog-series-part-7-20260516.md` records the Part 7 single-post review update, before/after exports, BlogAI reload result, and rendered-route verification.
- `publish-review-update-understanding-a-named-pipe-listener-20260516.md` records the named-pipe listener single-post review update, before/after exports, BlogAI reload result, token preservation, and rendered-route verification.
- `publish-review-update-understanding-local-mcp-server-over-stdio-and-local-only-communication-over-a-named-pipe-20260516.md` records the stdio/named-pipe overview single-post review update, before/after exports, BlogAI reload result, and rendered-route verification.
- `publish-review-update-wpf-vsix-threading-understanding-ui-switching-async-behavior-and-pipe-safety-20260516.md` records the WPF/VSIX threading single-post review update, before/after exports, BlogAI reload result, and rendered-route verification.
- `publish-review-update-why-vsix-project-should-target-net-framework-4-7-2-20260516.md` records the VSIX .NET Framework 4.7.2 targeting single-post review update, before/after exports, BlogAI reload result, and rendered-route verification.
- `publish-review-update-inference-driven-software-design-with-copilot-pros-and-cons-20260516.md` records the inference-driven development single-post review update, before/after exports, BlogAI reload result, token preservation, and rendered-route verification.
- `publish-review-update-understanding-ai-chat-sessions-models-and-agents-20260516.md` records the AI chat sessions/models/agents single-post review update, before/after exports, BlogAI reload result, token preservation, and rendered-route verification.
- `blogai-cache-reload-inspection-20260516.md` records the BlogAI/BlogEngine post-cache reload inspection after the first Part 1 DB body update and documents the safe development reload verification path.
- `gwn-wiki-extension-link-mapping-20260516.md` records the preserved `GwnWikiExtension` plugin settings export from `dbo.be_DataStoreSettings` and explains how bracket-style tokens resolve to production-domain links.

Current Part 4 mapping:

- live DB/source post: `docs/blogs/posts/vs-mcp-bridge-blog-series-part-4/`
- repo publishing trial draft: `docs/blogs/posts/vs-mcp-bridge-blog-series-part-4-repo-trial/`

Do not publish the repo trial draft over the live Part 4 slug.

Current hyperlink-token rule:

- bracket-style tokens such as `[NamedPipeListener]`, `[Stdio]`, and `[Display:...]` are resolved by `GwnWikiExtension`
- `http://AdventuresOnTheEdge.net` is the production/original blog domain
- `https://www.global-webnet.com` is the development BlogAI site
- production-domain URLs in exported plugin settings are expected for now and should not be edited during canonical article cleanup

## Database Preservation Export

Before rewriting or aligning existing BlogEngine content, preserve the current database state under:

```text
docs/blogs/source-of-truth/db-export-20260516/
```

That export is read-only evidence from `dbo.be_Posts` and includes:

- `manifest.json` with every exported row, identifiers, status, timestamps, and links found in body content
- one folder per database post with `post.database.json` and exact exported `content.html`
- deleted rows as well as active rows so no database history is silently dropped

Re-run the export from the repository root:

```powershell
.\scripts\blog-publishing\Export-BlogPostsFromDatabase.ps1 `
  -SqlConnectionString $env:AdventuresOnTheEdgeCS
```

Do not use the export path to publish or mutate the database. Use it as the preserved baseline before editing canonical posts.

## GwnWikiExtension Settings Export

Before normalizing bracket-token links, preserve the current plugin mapping state under:

```text
docs/blogs/source-of-truth/gwn-wiki-extension-export-20260516/
```

Re-run the export from the repository root:

```powershell
.\scripts\blog-publishing\Export-GwnWikiExtensionSettings.ps1 `
  -SqlConnectionString $env:AdventuresOnTheEdgeCS
```

The export writes the exact raw `Settings` field from `dbo.be_DataStoreSettings` for `ExtensionId = 'GwnWikiExtension'` plus a best-effort parsed inspection artifact.
Do not mutate plugin settings from this repo path.

## Widget Settings Export

BlogEngine widget settings rows can be preserved with:

```powershell
.\scripts\blog-publishing\Export-DataStoreSettingRow.ps1 `
  -SqlConnectionString $env:AdventuresOnTheEdgeCS `
  -DataStoreSettingRowId 26512
```

The TextBox widget update for row `26512` is documented in `widget-settings-row-26512-update-20260516.md`.
The preserved before/after artifacts live under:

```text
docs/blogs/source-of-truth/widget-settings/
```

## Future Direction

Consider an `Adventures.Blog` project or library to own reusable blog synchronization tooling across projects that use the BlogAI blogging application.
That library could:

- export DB/runtime blog content into repo source-of-truth
- preserve onsite edits before overwrites
- compare DB/runtime/canonical repo content
- publish canonical content back to BlogAI safely
- preserve plugin/token/link mappings
- support multiple applications using BlogAI with minimal setup
- keep project blogs synchronized with source code and architecture docs

This should stay separate from content cleanup unless the cleanup work starts duplicating synchronization logic across repositories.
