# BlogEngine DataStore Settings Row Export

This folder is a read-only preservation export from BlogEngine database table `dbo.be_DataStoreSettings`.

Row exported:

- `DataStoreSettingRowId`: `26512`
- `BlogId`: `27604f05-86ad-47ef-9e05-950bb762570c`
- `ExtensionType`: `1`
- `ExtensionId`: `a0bc6349-4f6c-4a87-addb-646a5c12565a`
- exported at: `2026-05-16T21:59:45.764Z`

Export contents:

- `setting.database.json`: row metadata and raw settings hash.
- `settings.raw.txt`: exact exported `Settings` field and the source of truth for this preservation slice.
- `settings.parsed.json`: best-effort JSON/XML/link inspection output. Parsing is inspection-only.

Re-run from the repo root:

``powershell
.\scripts\blog-publishing\Export-DataStoreSettingRow.ps1 ``
  -SqlConnectionString $env:AdventuresOnTheEdgeCS ``
  -DataStoreSettingRowId 26512
``

Use `-OutputRoot` to write to a different dated folder.