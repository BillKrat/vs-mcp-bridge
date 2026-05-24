# TextBox Widget Settings Row 26512 Update - 2026-05-16

## Scope

This slice preserved and updated BlogEngine.NET `dbo.be_DataStoreSettings` row `26512`.

The row backs a TextBox widget on the development BlogAI site:

- `https://www.global-webnet.com`

## Source Row

- `DataStoreSettingRowId`: `26512`
- `BlogId`: `27604f05-86ad-47ef-9e05-950bb762570c`
- `ExtensionType`: `1`
- `ExtensionId`: `a0bc6349-4f6c-4a87-addb-646a5c12565a`

## Preservation Artifacts

Before the database update, the current row was exported under:

```text
SolutionFolder/docs/blogs/source-of-truth/widget-settings/datastore-row-26512-20260516/
```

Important files:

- `settings.raw.txt`: exact pre-update `Settings` value.
- `settings.parsed.json`: best-effort XML/link inspection.
- `settings.proposed.txt`: exact proposed update applied to the database.
- `settings.before-after.diff`: before/after settings diff.
- `setting.database.json`: row metadata and pre-update hash.

After the database update, row `26512` was re-exported under:

```text
SolutionFolder/docs/blogs/source-of-truth/widget-settings/datastore-row-26512-20260516-after-update/
```

The post-update raw settings matched `settings.proposed.txt` exactly.

## Parseability

The settings payload is XML:

```text
SerializableStringDictionary
  SerializableStringDictionary
    DictionaryEntry Key="content"
```

The `content` value is stored as encoded HTML, matching the existing BlogEngine TextBox widget format.

## Updated Targets

The updated widget content points to stable `main` references:

- `https://github.com/BillKrat/vs-mcp-bridge/tree/main`
- `https://github.com/BillKrat/BlogAI`
- `https://github.com/BillKrat/vs-mcp-bridge/blob/main/SolutionFolder/docs/ARCHITECTURE.md`
- `https://github.com/BillKrat/vs-mcp-bridge/tree/main/SolutionFolder/docs/blogs`
- `https://github.com/BillKrat/vs-mcp-bridge/blob/main/SolutionFolder/docs/blogs/posts/vs-mcp-bridge-blog-series-part-1/content.html`
- `https://github.com/BillKrat/vs-mcp-bridge/blob/main/SolutionFolder/docs/blogs/posts/vs-mcp-bridge-blog-series-part-2/content.html`
- `https://github.com/BillKrat/vs-mcp-bridge/blob/main/SolutionFolder/docs/blogs/posts/vs-mcp-bridge-blog-series-part-3/content.html`
- `https://github.com/BillKrat/vs-mcp-bridge/blob/main/SolutionFolder/docs/blogs/posts/vs-mcp-bridge-blog-series-part-4/content.html`
- `https://github.com/BillKrat/vs-mcp-bridge/blob/main/SolutionFolder/docs/blogs/posts/how-stdio-works-in-vs-mcp-bridge/content.html`
- `https://github.com/BillKrat/vs-mcp-bridge/blob/main/SolutionFolder/docs/blogs/posts/understanding-a-named-pipe-listener/content.html`
- `https://github.com/BillKrat/vs-mcp-bridge/blob/main/SolutionFolder/docs/blogs/posts/understanding-local-mcp-server-over-stdio-and-local-only-communication-over-a-named-pipe/content.html`
- `https://github.com/BillKrat/vs-mcp-bridge/blob/main/SolutionFolder/docs/blogs/diagrams/vs-mcp-bridge-bootstrap-sequence.mmd`
- `https://github.com/BillKrat/vs-mcp-bridge/blob/main/SolutionFolder/docs/diagrams/tool-regex-search-trace-20260509.mmd`
- `https://github.com/BillKrat/vs-mcp-bridge/blob/main/SolutionFolder/docs/diagrams/tool-security-trace-20260509.mmd`
- `https://github.com/BillKrat/vs-mcp-bridge/blob/main/SolutionFolder/docs/diagrams/tool-approval-trace-20260516.mmd`
- `https://github.com/BillKrat/vs-mcp-bridge/blob/main/SolutionFolder/docs/diagrams/mef-discovery-trace-20260516.mmd`
- `https://github.com/BillKrat/vs-mcp-bridge/blob/main/SolutionFolder/docs/diagrams/vsix-activation-diagnostic-trace-20260516.mmd`

## Runtime Verification

Database update result:

- `RowsUpdated`: `1`
- post-update export matched `settings.proposed.txt`
- post-update settings parsed as XML
- post-update link count: `17`

Public site check:

- `https://www.global-webnet.com/` returned HTTP `200`
- the site rendered successfully
- the rendered page still showed the old widget content immediately after the SQL update

Reload endpoint check:

```text
POST https://www.global-webnet.com/api/posts/reload/27604f05-86ad-47ef-9e05-950bb762570c
```

Result:

- `401 Unauthorized`
- no `BLOG_RELOAD_KEY` was available in the current environment

Operational conclusion:

The SQL row is updated, but the public site appears to be serving cached BlogEngine widget settings until the BlogAI reload endpoint is called with the correct key or the application cache is otherwise refreshed.
