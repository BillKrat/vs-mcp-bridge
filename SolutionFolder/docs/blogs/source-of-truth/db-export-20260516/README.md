# Blog Database Export

This folder is a read-only preservation export from BlogEngine database table `dbo.be_Posts`.

Export contents:

- `manifest.json`: index of every exported row, source identifiers, status, timestamps, and links found in body content.
- `<slug-or-row>/post.database.json`: metadata for one database row.
- `<slug-or-row>/content.html`: exact exported `PostContent` body for one database row.

Re-run from the repo root:

```powershell
.\scripts\blog-publishing\Export-BlogPostsFromDatabase.ps1 `
  -SqlConnectionString $env:AdventuresOnTheEdgeCS
```

Use `-OutputRoot` to write to a different dated folder. Use `-ActiveOnly` only when you intentionally want to exclude rows where `IsDeleted = 1`.

Do not edit database records from this export path. It is intended to preserve the current database baseline before blog rewrites.