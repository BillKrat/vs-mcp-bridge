# Batch And Create Publish Validation

## Summary

The repo-driven BlogEngine publishing flow was validated across both update and first-time create scenarios.

Validated assets now include:

- single-post draft publish via `Publish-BlogPostDraft.ps1`
- batch publish wrapper via `Publish-BlogPosts.ps1`
- canonical create-path fixture post under `docs/blogs/posts/vs-mcp-bridge-publish-create-trial/`

## What Was Validated

Three slugs were published successfully:

- `vs-mcp-bridge-blog-series-part-1`
- `vs-mcp-bridge-blog-series-part-4`
- `vs-mcp-bridge-publish-create-trial`

Observed results:

- part 1 updated an existing BlogEngine post and forced it back to draft
- part 4 updated the repo-trial post successfully with populated categories/tags
- create-trial created a brand-new BlogEngine post as a draft
- rerunning create-trial updated the same BlogEngine `PostID` instead of creating a duplicate
- reload made the updated content visible immediately in BlogEngine admin

## Important Behavior Confirmed

- repo content is the source of truth
- script upsert resolves existing posts by `BlogID + Slug`
- script forces `IsPublished = false`
- reruns overwrite manual UI edits with repo content
- BlogEngine runtime visibility depends on the reload endpoint after SQL upsert

## Source Of Truth For Create Trial

Canonical repo post:

- `docs/blogs/posts/vs-mcp-bridge-publish-create-trial/post.json`
- `docs/blogs/posts/vs-mcp-bridge-publish-create-trial/content.html`

This fixture is now available for future first-time create-path regression checks.

## Convenience Scripts

These local wrappers exist only for operator convenience and remain intentionally untracked:

- `scripts/blog-publishing/publish.cmd`
- `scripts/blog-publishing/publish-posts.cmd`

The repo-backed scripts remain:

- `scripts/blog-publishing/Publish-BlogPostDraft.ps1`
- `scripts/blog-publishing/Publish-BlogPosts.ps1`

## Suggested Next Step

If ChatGPT picks this up next, the likely close-out step is to stage and commit:

- `scripts/blog-publishing/Publish-BlogPosts.ps1`
- `scripts/blog-publishing/README.md`
- `docs/blogs/posts/vs-mcp-bridge-publish-create-trial/**`
- this handoff file

while continuing to leave the `.cmd` wrappers untracked.
