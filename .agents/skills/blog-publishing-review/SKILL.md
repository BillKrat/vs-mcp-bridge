---
name: blog-publishing-review
description: Review and preserve repository-backed BlogAI/BlogEngine publishing source-of-truth material.
---

# Blog Publishing Review

## Use When

- Comparing canonical repo posts with database exports or rendered routes.
- Reviewing publish readiness for `docs/blogs/posts/`.
- Preserving BlogEngine database, widget, or plugin mapping evidence.
- Avoiding accidental overwrite of live or trial post variants.

## Workflow

1. Read `docs/blogs/README.md` first.
2. Treat `docs/blogs/posts/` as the canonical repository-backed post source.
3. Preserve database/runtime state under `docs/blogs/source-of-truth/` before publishing or rewriting.
4. Keep bracket-style `GwnWikiExtension` tokens intact unless an explicit task says otherwise.
5. Do not publish the Part 4 repo trial draft over the live Part 4 slug.
6. Record read-only compare, before/after export, reload, and rendered-route verification evidence in focused dated docs.

## References

- `docs/blogs/README.md`
- `docs/blogs/blog-publishing-review-plan-20260516.md`
- `docs/blogs/publish-review-status-20260516.md`
- `scripts/blog-publishing/README.md`
- `docs/blogs/source-of-truth/`
