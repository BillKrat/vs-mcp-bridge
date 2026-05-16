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
