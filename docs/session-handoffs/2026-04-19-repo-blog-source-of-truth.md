# VS MCP Bridge — Repo Blog Source of Truth Established

## Summary

The system now uses the repository as the canonical source of truth for blog posts.

Posts are now defined by `post.json` plus `content.html`.

The BlogEngine database is now a deployment target, not the authoritative source.

## What Changed

New structure:

```text
docs/blogs/posts/<slug>/
  post.json
  content.html
```

`post.json` now contains:

- `blogId`
- `postId`
- `title`
- `description`
- `author`
- `slug`
- `isPublished`
- `allowComments`
- `categories`
- `tags`

`content.html` contains:

- sanitized HTML body content only

`docs/blogs/posted` remains as legacy migration input.

## Why This Change Was Made

- enable deterministic publishing
- separate content from deployment
- support review-before-publish workflow
- align with future Blog AI system requirements
- eliminate DB as source of truth

## Data Model Alignment

Repo -> Database

- `title` -> `be_Posts.Title`
- `description` -> `be_Posts.Description`
- `content.html` -> `be_Posts.PostContent`
- `author` -> `be_Posts.Author`
- `slug` -> `be_Posts.Slug`
- `isPublished` -> `be_Posts.IsPublished`
- `allowComments` -> `be_Posts.IsCommentEnabled`
- `categories` -> `be_PostCategory` via `be_Categories` lookup
- `tags` -> `be_PostTag.Tag`

## Materialization Results

- total posts materialized: 9
- no posts skipped
- folders created under `docs/blogs/posts`

Notes:

- categories and tags are currently empty arrays
- no guessing was performed due to missing mapping input

## Data Quality Observations

- inconsistent HTML structure across legacy posts
- some posts were full HTML documents, others fragments
- missing embedded metadata in some posts (`title`/`h1`)
- discrepancies between `Results.csv` and archived HTML content
- legacy descriptions may contain raw or unrefined metadata

## Known Gaps

- category validation not yet enforced
- tag normalization not yet enforced
- deploy script not yet wired to repo model
- legacy `posted` folder still present intentionally

## Next Steps

1. Add category validation to deployment SQL and fail if missing.
2. Add tag normalization with trimming and deduplication.
3. Build a repo-driven deploy/update script.
4. Perform a controlled deployment test on one post.
5. Later, evaluate cleanup or removal of legacy posted content.

## Stability Statement

The repo-backed model is now stable for authoring and iteration.

Future work will build on this without altering the established structure.
