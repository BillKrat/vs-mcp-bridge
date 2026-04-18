# Repo Publishing SQL Contract

## Summary

`docs/blogs/posts/<slug>/post.json` plus `content.html` is now the repository source of truth for blog posts in `vs-mcp-bridge`.

`UpsertBlogPostFromRepo` is the publishing-side SQL contract that performs deterministic BlogEngine database upsert from that repo model.

## Important Runtime Constraint

Direct SQL upserts do not refresh the BlogEngine in-memory post cache.

That means a successful database write is necessary but not sufficient for the live application to reflect the new post state immediately.

## Reload Endpoint Ownership

The BlogAI repo provides the cache-refresh endpoint:

- `POST /api/posts/reload/{blogId}`

That endpoint must be called after the SQL upsert when publishing or updating posts through the repo-driven flow.

## Required Publishing Sequence

1. Read the repo post from `docs/blogs/posts/<slug>/post.json` and `content.html`.
2. Execute `UpsertBlogPostFromRepo`.
3. Call `POST /api/posts/reload/{blogId}`.
4. Verify the result through `/api/posts` or the BlogEngine admin UI.

## Stability Note

This contract is now the expected publishing sequence for repo-driven blog deployment:

- repo content defines the post
- SQL performs the deterministic persistence step
- reload refreshes BlogEngine runtime state
- verification confirms the publish completed end to end
