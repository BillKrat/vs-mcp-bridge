# BlogAI Cache Reload Inspection - 2026-05-16

## Scope

This slice inspected BlogAI/BlogEngine cache behavior after the first single-post canonical body update for:

- `vs-mcp-bridge-blog-series-part-1`
- `dbo.be_Posts.PostRowID = 143`
- `BlogID = 27604f05-86ad-47ef-9e05-950bb762570c`
- `PostID = f0c7a958-f41a-4143-b601-82ce84fd4af0`

No additional posts were published in this slice.
No database content was changed in this slice.

## Inputs Inspected

VS MCP Bridge repo:

- `SolutionFolder/docs/blogs/publish-review-update-vs-mcp-bridge-blog-series-part-1-20260516.md`
- `SolutionFolder/scripts/blog-publishing/Publish-BlogPostDraft.ps1`
- `SolutionFolder/scripts/blog-publishing/Publish-BlogPostReviewUpdate.ps1`
- `SolutionFolder/scripts/blog-publishing/publish.cmd`
- `SolutionFolder/scripts/blog-publishing/publish-posts.cmd`
- `SolutionFolder/scripts/blog-publishing/README.md`

BlogAI checkout:

- `Y:\BlogAI\BlogEngine\BlogEngine.Core\Post.cs`
- `Y:\BlogAI\BlogEngine\BlogEngine.Core\Providers\BlogService.cs`
- `Y:\BlogAI\BlogEngine\BlogEngine.Core\Data\PostRepository.cs`
- `Y:\BlogAI\BlogEngine\BlogEngine.Core\Web\Extensions\ExtensionManager.cs`
- `Y:\BlogAI\BlogEngine\BlogEngine.NET\AppCode\Api\PostsController.cs`
- `Y:\BlogAI\BlogEngine\BlogEngine.NET\AppCode\App_Start\BlogEngineConfig.cs`

Environment/config observations:

- `$env:AdventuresOnTheEdgeCS` was present and used only for a read-only verification query.
- `$env:BlogEngineReloadKey` was present and was used for one development reload call.
- `$env:BLOG_RELOAD_KEY` was not present in the earlier publish slice.
- The bridge batch publish `.cmd` files use `BlogEngineReloadKey`, while some PowerShell examples mention `BLOG_RELOAD_KEY`.

## Cache Mechanism Found

BlogEngine keeps post lists in static dictionaries in `Post.cs`:

- `posts`
- `deletedposts`

`Post.Posts` fills the per-blog list lazily from `BlogService.FillPosts()` when the blog id is not already present in the static dictionary.
Direct database writes bypass the normal `Post.Save()` path, so the in-memory post list can continue serving the previous post body until the post list is reloaded or the application is recycled.

The low-level reload mechanism is present in `Post.Reload()`:

```text
Post.Reload() -> RefreshPostLists(Blog.CurrentInstance)
```

`RefreshPostLists` removes the current blog id from both `posts` and `deletedposts`.
The next request reloads posts from the provider/database.

The local `Y:\BlogAI` checkout did not contain a visible `PostsController.Reload` action for `/api/posts/reload/{blogId}`.
However, the deployed development BlogAI site did expose that endpoint and returned success with the reload key.
Treat the deployed endpoint as an environment-specific operational hook until the BlogAI source checkout is reconciled with the deployed code.

## Endpoint Behavior

Safe development reload call performed once:

```powershell
$blogId = '27604f05-86ad-47ef-9e05-950bb762570c'
Invoke-WebRequest `
  -Method Post `
  -Uri "https://www.global-webnet.com/api/posts/reload/$blogId" `
  -Headers @{ 'X-Blog-Reload-Key' = $env:BlogEngineReloadKey }
```

Result:

```text
HTTP 200
Body: true
```

The endpoint appears to reload post cache for the specified blog id.
This slice did not find evidence that it reloads widgets, `DataStoreSettings`, extension settings, or the entire application.
No app pool recycle was required for the Part 1 post body update.

## Part 1 Verification

Read-only database verification after reload:

| Check | Result |
| --- | --- |
| DB row | 143 |
| Slug | `vs-mcp-bridge-blog-series-part-1` |
| DateModified | `2026-05-16T16:07:49.903` |
| DB body SHA-256 | `03c196c998e3819b27bf1dc6c2b43dd4f40a04e9fe1bb2057fbc4bbd918d1ab8` |
| DB body contains `Source of Truth:` | True |
| DB body contains `BridgeToolExecutor` | True |
| DB body contains old intro phrase | False |

Rendered route verification after reload:

```text
https://www.global-webnet.com/post/2026/04/11/vs-mcp-bridge-blog-series-part-1
```

| Check | Result |
| --- | --- |
| HTTP status | 200 |
| Rendered body contains `Source of Truth:` | True |
| Rendered body contains `BridgeToolExecutor` | True |
| Rendered body contains `anti-black-box rule` | True |
| Rendered body contains old intro phrase | False |
| Rendered body contains new intro phrase | True |

## Why The Route Initially Showed Old Content

The Part 1 publish-review update changed the database directly and intentionally did not call the reload endpoint.
Because BlogEngine caches post lists in process, the public route continued to render the previously cached post object even though the DB row had already been updated.

After the development reload endpoint was called with `X-Blog-Reload-Key`, the route rendered the canonical Part 1 content.

## Safe Verification Path Before More Publishing

For each subsequent single-post review update:

1. Run the prepublish compare for the target slug.
2. Preserve the current live DB row immediately before writing.
3. Update only the intended target row.
4. Verify the DB body hash matches canonical content.
5. Call the development reload endpoint once:

   ```powershell
   Invoke-WebRequest `
     -Method Post `
     -Uri "https://www.global-webnet.com/api/posts/reload/<blogId>" `
     -Headers @{ 'X-Blog-Reload-Key' = $env:BlogEngineReloadKey }
   ```

6. Verify the rendered route contains a unique phrase from the canonical content.
7. Record the route result before moving to the next post.

Use `BlogEngineReloadKey` as the local environment variable name for this workflow unless the environment is intentionally changed.
Do not log or commit the key value.

## Remaining Cautions

- The deployed reload endpoint exists and worked, but the local BlogAI checkout inspected in this slice did not show the endpoint implementation. Reconcile the BlogAI source/deployed delta before treating this as fully source-documented.
- The endpoint appears sufficient for post body cache refresh; it should not be assumed to refresh widget or `DataStoreSettings` cache.
- App pool recycle was not required for Part 1 post verification.
- Production/original `AdventuresOnTheEdge.net` behavior was not tested or changed in this slice.

## Recommendation

Proceed to Part 2 only after treating the reload step as part of the single-post publishing checklist.
The next publishing slice should update one post, call the development reload endpoint once, and verify the rendered route before any additional post is touched.
