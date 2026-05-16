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
