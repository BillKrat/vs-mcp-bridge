# Blog Publishing Scripts

`Publish-BlogPostDraft.ps1` publishes one canonical repo-backed post to BlogEngine as a draft and refreshes BlogEngine visibility through the BlogAI reload endpoint.

## Script

- [Publish-BlogPostDraft.ps1](Y:/vs-mcp-bridge/scripts/blog-publishing/Publish-BlogPostDraft.ps1)

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
