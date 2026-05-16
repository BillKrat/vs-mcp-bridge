# GwnWikiExtension Link Mapping - 2026-05-16

## Scope

This document records the preserved hyperlink-token plugin settings that explain rendered bracket-token links in BlogEngine content.

No database records were changed.
No public site behavior was changed.
No blog article bodies were rewritten.

## Export

The read-only export lives under:

```text
docs/blogs/source-of-truth/gwn-wiki-extension-export-20260516/
```

Export command:

```powershell
.\scripts\blog-publishing\Export-GwnWikiExtensionSettings.ps1 `
  -SqlConnectionString $env:AdventuresOnTheEdgeCS
```

The export reads:

```sql
SELECT
    DataStoreSettingRowId,
    BlogId,
    ExtensionType,
    ExtensionId,
    Settings
FROM dbo.be_DataStoreSettings
WHERE ExtensionId = 'GwnWikiExtension';
```

Each exported row contains:

- `settings.raw.txt`: exact `Settings` field from SQL; this is the source of truth.
- `setting.database.json`: row metadata, raw length, and SHA-256 hash.
- `settings.parsed.json`: best-effort XML inspection output.
- `manifest.json`: row index and flattened token/link mappings.

## Export Results

| Count | Value |
| --- | ---: |
| Exported settings rows | 4 |
| XML-parseable rows | 4 |
| Parsed token/link mappings | 91 |
| BlogAI/development blog mappings | 32 |

The BlogAI/development blog row is:

- `DataStoreSettingRowId`: `26889`
- `BlogId`: `27604f05-86ad-47ef-9e05-950bb762570c`
- `ExtensionId`: `GwnWikiExtension`

## Domain Roles

- `http://AdventuresOnTheEdge.net` is the production/original blog domain.
- `https://www.global-webnet.com` is the development BlogAI site.
- The exported `GwnWikiExtension` settings intentionally still contain production-domain URLs.
- Those URLs should not be treated as accidental content defects during canonical blog cleanup.

## Bracket-Token Behavior

Blog content can contain bracket-style tokens such as:

- `[Page:...]`
- `[Display:...]`
- `[NamedPipeListener]`
- `[Stdio]`

`GwnWikiExtension` resolves those tokens using settings rows from `dbo.be_DataStoreSettings`.
The plugin stores token/link records as XML under the `Settings` field.
The parsed artifact reconstructs these records by aligning `Parameters` groups such as `CommandParameter`, `PermaLink`, `Command`, and `DisplayTemplate`.

## Relevant BlogAI Mappings

These mappings explain the previously observed old-domain rendered links on `https://www.global-webnet.com`.

| Token | URL |
| --- | --- |
| `NamedPipeListener` | `http://adventuresontheedge.net/post.aspx?id=6484fa94-5d8b-429a-99c6-779b300bc336` |
| `stdio` | `http://adventuresontheedge.net/post.aspx?id=d0541943-0de1-4c25-a7af-9950c55f1591` |
| `InferenceDriven` | `http://adventuresontheedge.net/post.aspx?id=b3da6b1c-a955-4ec2-afda-b281bd5d46fd` |
| `ChatSessionsModelsAndAgents` | `http://adventuresontheedge.net/post.aspx?id=5465cc54-65ab-4c4f-b6ac-4539de01c365` |
| `VsMcpBridge` | `http://adventuresontheedge.net/post.aspx?id=f0c7a958-f41a-4143-b601-82ce84fd4af0` |

## Cleanup Guidance

For future canonical blog cleanup:

- Preserve bracket tokens unless there is an explicit decision to replace them.
- If replacing a token, do it intentionally in canonical source and document the mapping change.
- Do not update `GwnWikiExtension` settings or database rows in a content-cleanup slice.
- Do not assume old-domain rendered links are article-body defects.
- Treat `settings.raw.txt` as the durable baseline before any later plugin-setting migration.

## Recommended Next Slice

Proceed with a single-post canonical cleanup using this plugin export as context.
For `vs-mcp-bridge-blog-series-part-1`, decide whether to preserve `[NamedPipeListener]` and `[Stdio]` tokens, replace them with canonical BlogAI URLs, or defer token normalization until plugin settings are migrated.
