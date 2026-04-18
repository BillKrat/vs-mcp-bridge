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

