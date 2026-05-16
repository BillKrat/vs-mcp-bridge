# GwnWikiExtension Settings Export

This folder is a read-only preservation export from BlogEngine database table `dbo.be_DataStoreSettings` for `ExtensionId = 'GwnWikiExtension'`.

Export contents:

- `manifest.json`: index of exported settings rows and best-effort parsed token/link mappings.
- `<row>/setting.database.json`: metadata for one settings row.
- `<row>/settings.raw.txt`: exact exported `Settings` field for one settings row.
- `<row>/settings.parsed.json`: best-effort parser output. Parsing is inspection-only; `settings.raw.txt` is the source of truth.

Re-run from the repo root:

```powershell
.\scripts\blog-publishing\Export-GwnWikiExtensionSettings.ps1 `
  -SqlConnectionString $env:AdventuresOnTheEdgeCS
```

Use `-OutputRoot` to write to a different dated folder.

Do not edit database records from this export path. It exists to preserve the hyperlink-token plugin settings before canonical blog cleanup.