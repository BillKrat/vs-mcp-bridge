# Blog Publishing Scripts

`Publish-BlogPostDraft.ps1` publishes one canonical repo-backed post to BlogEngine as a draft and refreshes BlogEngine visibility through the BlogAI reload endpoint.
`Compare-BlogPostBeforePublish.ps1` is the read-only safety check to run before publishing.
`Compare-ReadyBlogPostsBeforePublish.ps1` batches that safety check across the ready-post publishing list.
`Compare-BlockedBlogRowsBeforePublish.ps1` inspects blocked live-vs-export differences without publishing.

## Script

- [Publish-BlogPostDraft.ps1](Y:/vs-mcp-bridge/scripts/blog-publishing/Publish-BlogPostDraft.ps1)
- [Export-BlogPostsFromDatabase.ps1](Y:/vs-mcp-bridge/scripts/blog-publishing/Export-BlogPostsFromDatabase.ps1)
- [Export-GwnWikiExtensionSettings.ps1](Y:/vs-mcp-bridge/scripts/blog-publishing/Export-GwnWikiExtensionSettings.ps1)
- [Export-DataStoreSettingRow.ps1](Y:/vs-mcp-bridge/scripts/blog-publishing/Export-DataStoreSettingRow.ps1)
- [Compare-BlogPostBeforePublish.ps1](Y:/vs-mcp-bridge/scripts/blog-publishing/Compare-BlogPostBeforePublish.ps1)
- [Compare-ReadyBlogPostsBeforePublish.ps1](Y:/vs-mcp-bridge/scripts/blog-publishing/Compare-ReadyBlogPostsBeforePublish.ps1)
- [Compare-BlockedBlogRowsBeforePublish.ps1](Y:/vs-mcp-bridge/scripts/blog-publishing/Compare-BlockedBlogRowsBeforePublish.ps1)

## Read-Only Export

`Export-BlogPostsFromDatabase.ps1` preserves existing BlogEngine database rows before repo-side blog rewrites.

```powershell
.\scripts\blog-publishing\Export-BlogPostsFromDatabase.ps1 `
  -SqlConnectionString $env:AdventuresOnTheEdgeCS
```

Default output:

```text
docs/blogs/source-of-truth/db-export-20260516/
```

The export writes `manifest.json`, `README.md`, and one folder per `dbo.be_Posts` row containing `post.database.json` plus exact `content.html`.
It is read-only and does not publish, reload, update, or delete database records.

`Compare-BlogPostBeforePublish.ps1` compares one current live BlogEngine database row against the preserved DB export baseline and the canonical repo post before any overwrite workflow.

```powershell
.\scripts\blog-publishing\Compare-BlogPostBeforePublish.ps1 `
  -Slug 'vs-mcp-bridge-blog-series-part-1' `
  -SqlConnectionString $env:AdventuresOnTheEdgeCS
```

By default the script writes a dated markdown report under `docs/blogs/`.
It performs a parameterized `SELECT` only and does not publish, reload, update, or delete database records.

`Compare-ReadyBlogPostsBeforePublish.ps1` runs the same read-only compare for every slug listed in `docs/blogs/blog-publishing-review-plan-20260516.md` as ready for publishing review.

```powershell
.\scripts\blog-publishing\Compare-ReadyBlogPostsBeforePublish.ps1 `
  -SqlConnectionString $env:AdventuresOnTheEdgeCS
```

By default the script writes `docs/blogs/prepublish-compare-ready-posts-20260516.md`.
It performs parameterized `SELECT` calls only and does not publish, reload, update, or delete database records.

`Compare-BlockedBlogRowsBeforePublish.ps1` inspects rows blocked by the ready-post batch compare and classifies whether the drift is body content, identity metadata, taxonomy/description metadata, or timestamp-only.

```powershell
.\scripts\blog-publishing\Compare-BlockedBlogRowsBeforePublish.ps1 `
  -SqlConnectionString $env:AdventuresOnTheEdgeCS
```

By default the script writes `docs/blogs/prepublish-blocked-row-diff-20260516.md`.
It performs parameterized `SELECT` calls only and does not publish, reload, update, or delete database records.

`Export-GwnWikiExtensionSettings.ps1` preserves hyperlink-token plugin settings before canonical blog link cleanup.

```powershell
.\scripts\blog-publishing\Export-GwnWikiExtensionSettings.ps1 `
  -SqlConnectionString $env:AdventuresOnTheEdgeCS
```

Default output:

```text
docs/blogs/source-of-truth/gwn-wiki-extension-export-20260516/
```

The export reads `dbo.be_DataStoreSettings` where `ExtensionId = 'GwnWikiExtension'`.
It writes the exact raw `Settings` field plus best-effort XML inspection artifacts for bracket-token mappings such as `[NamedPipeListener]`, `[Stdio]`, and `[Display:...]`.
It is read-only and does not publish, reload, update, or delete plugin settings.

`Export-DataStoreSettingRow.ps1` preserves one `dbo.be_DataStoreSettings` row by row id. Use this for widget settings or other BlogEngine settings rows before any manual database edit.

```powershell
.\scripts\blog-publishing\Export-DataStoreSettingRow.ps1 `
  -SqlConnectionString $env:AdventuresOnTheEdgeCS `
  -DataStoreSettingRowId 26512
```

Default output for row `26512`:

```text
docs/blogs/source-of-truth/widget-settings/datastore-row-26512-20260516/
```

The export writes the exact raw `Settings` field plus best-effort JSON/XML/link inspection artifacts.
It is read-only and does not update database settings.

## Required Parameters

- `-SqlConnectionString`
- `-ReloadBaseUrl`
- one of:
  - `-Slug`
  - `-PostPath`

Optional:

- `-PostsRoot`
- `-ReloadKey`

## Example Invocation

```powershell
.\scripts\blog-publishing\Publish-BlogPostDraft.ps1 `
  -Slug 'vs-mcp-bridge-blog-series-part-1' `
  -SqlConnectionString 'Server=.;Database=db_a2cb58_adventure_1;Integrated Security=True;TrustServerCertificate=True' `
  -ReloadBaseUrl 'https://localhost:7147' `
  -ReloadKey $env:BLOG_RELOAD_KEY
```

You can also publish by explicit folder path:

```powershell
.\scripts\blog-publishing\Publish-BlogPostDraft.ps1 `
  -PostPath '.\docs\blogs\posts\vs-mcp-bridge-blog-series-part-1' `
  -SqlConnectionString 'Server=.;Database=db_a2cb58_adventure_1;Integrated Security=True;TrustServerCertificate=True' `
  -ReloadBaseUrl 'https://localhost:7147' `
  -ReloadKey $env:BLOG_RELOAD_KEY
```

## Behavior

- validates that `post.json` and `content.html` exist
- validates and trims required metadata fields
- validates that `categories` and `tags` are present as arrays
- normalizes `categories` and `tags` to trimmed string arrays, dropping blank entries and deduplicating case-insensitively while preserving first-seen casing
- reads the canonical repo post
- resolves the BlogEngine `PostID` by `BlogID + Slug`, or creates a new GUID when the post does not exist yet
- executes `dbo.UpsertBlogPostFromRepo`
- calls `POST /api/posts/reload/{blogId}`
- sends `X-Blog-Reload-Key` when `-ReloadKey` is provided
- reports success or throws on failure

## Local Setup Notes

- the target database must already have `dbo.StringList` and `dbo.UpsertBlogPostFromRepo` deployed
- the BlogAI application must be running and reachable at `-ReloadBaseUrl`
- connection details, endpoint values, and reload secrets are provided as parameters or local environment variables; no secrets are stored in source control
- this script is draft-only for this slice and forces `IsPublished = false`

## BlogEngine Integration (Repo-Driven Publishing)

Repo content is the source of truth. BlogEngine is the runtime and rendering target.

Required components:

- `vs-mcp-bridge` repo for canonical posts, the publish script, and SQL upsert scripts
- BlogAI repo for reload endpoint support

Canonical workflow:

1. Update the canonical post under `docs/blogs/posts/<slug>/`.
2. Run `Publish-BlogPostDraft.ps1` for that post.
3. The script upserts the post and triggers the reload endpoint.
4. Verify the draft in BlogEngine or through the post API.

Required local setup:

- SQL connection string
- BlogEngine running locally
- reload endpoint key

Known behavior:

- avoid browser refresh in the BlogEngine admin edit view immediately after publish

Boundaries:

- the script forces draft state
- repo content overwrites manual UI edits
- BlogEngine is not the source of truth
