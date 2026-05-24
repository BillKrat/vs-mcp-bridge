# Stale Shared Feature Branch Link Inspection - 2026-05-16

This report investigates why all 14 rendered BlogAI routes still contained `feature/approval-apply-ui-slice` after their post bodies were publish-review updated and verified.

Scope constraints for this slice:

- No database writes were performed.
- No BlogAI reload endpoint was called.
- No cache clear or app recycle was performed.
- Public site behavior was not changed.

## Finding

The stale rendered marker comes from the TextBox widget backed by `dbo.be_DataStoreSettings.DataStoreSettingRowId = 26512`, but the current live database row no longer contains the stale marker.

The stale marker is therefore most likely cached widget output/settings in the running BlogAI application, not current database content and not the updated post bodies.

## Evidence

Final rendered-route verification showed:

| Check | Result |
| --- | --- |
| Routes checked | 14 |
| HTTP 200 routes | 14 |
| Routes with expected post-specific canonical markers visible | 14 |
| Routes containing `feature/approval-apply-ui-slice` | 14 |
| Reload called in that slice | No |

The rendered stale text sample from Part 2 matched the old TextBox widget introduction:

- `The objective of this site is to blog`
- `VS 2026 MCP Bridge`
- `AppArchGuide2.0.pdf`
- `https://github.com/BillKrat/vs-mcp-bridge/tree/feature/approval-apply-ui-slice`

The same rendered response did not contain the post-update widget markers:

- `BlogAI development source:`
- `https://github.com/BillKrat/vs-mcp-bridge/tree/main`

## Preserved Widget Source

Pre-update widget export:

```text
SolutionFolder/docs/blogs/source-of-truth/widget-settings/datastore-row-26512-20260516/
```

Important preserved files:

| File | Evidence |
| --- | --- |
| `settings.raw.txt` | Contains the old TextBox widget content and two `feature/approval-apply-ui-slice` links. |
| `settings.parsed.json` | Extracted the old `https://github.com/BillKrat/vs-mcp-bridge/tree/feature/approval-apply-ui-slice` link. |
| `settings.before-after.diff` | Shows the stale widget content removed and replaced by main-branch links. |
| `setting.database.json` | Pre-update raw length `3124`, SHA-256 `108c246aa26aba0d875bacba57ca10a414452db2382605fca993c6c8577491a8`. |

Post-update widget export:

```text
SolutionFolder/docs/blogs/source-of-truth/widget-settings/datastore-row-26512-20260516-after-update/
```

Important preserved files:

| File | Evidence |
| --- | --- |
| `settings.raw.txt` | Contains main-branch widget links and does not contain `feature/approval-apply-ui-slice`. |
| `settings.parsed.json` | Extracted main-branch links such as `https://github.com/BillKrat/vs-mcp-bridge/tree/main`. |
| `setting.database.json` | Post-update raw length `4493`, SHA-256 `66b1b69fc8fe33142327f42a0079d7fc29c7c8eab5e3a633f471970ee654f33f`. |

The prior widget update report, `widget-settings-row-26512-update-20260516.md`, also recorded that the public site still showed old widget content immediately after the SQL update.

## Current Live DB Inspection

Read-only live DB checks were run against `dbo.be_DataStoreSettings`.

Row `26512` currently reports:

| Check | Result |
| --- | --- |
| `DataStoreSettingRowId` | `26512` |
| `BlogId` | `27604F05-86AD-47EF-9E05-950BB762570C` |
| `ExtensionType` | `1` |
| `ExtensionId` | `a0bc6349-4f6c-4a87-addb-646a5c12565a` |
| Settings length | `4493` |
| Contains `feature/approval-apply-ui-slice` | False |
| Contains `github.com/BillKrat/vs-mcp-bridge/tree/main` | True |
| Contains `github.com/BillKrat/vs-mcp-bridge/blob/main/SolutionFolder/docs/ARCHITECTURE.md` | True |
| Contains old widget text `The objective of this site is to blog` | False |
| Contains new widget text `BlogAI development source` | True |

A read-only search across `dbo.be_DataStoreSettings.Settings` found zero rows containing `feature/approval-apply-ui-slice`.

## GwnWikiExtension And Layout Checks

The preserved `GwnWikiExtension` export under:

```text
SolutionFolder/docs/blogs/source-of-truth/gwn-wiki-extension-export-20260516/
```

contains intentional production/original-domain `post.aspx?id=...` mappings, but no `feature/approval-apply-ui-slice` marker. This does not appear to be the source of the stale feature-branch link.

A read-only source search in the local BlogAI checkout did not find `feature/approval-apply-ui-slice`, `VS 2026 MCP Bridge`, `The objective of this site is to blog`, or `AppArchGuide2.0` in layout/theme source. This does not appear to be static layout source.

## Cache Mechanism

The local BlogAI checkout shows TextBox widget settings are cached by widget id:

```text
Y:\BlogAI\BlogEngine\BlogEngine.NET\Custom\Widgets\Common.cs
```

Relevant behavior:

- `Common.GetSettings(string id)` uses cache key `be_widget_<id>`.
- If that cache key is already populated, it returns cached settings instead of reloading `WidgetSettings(id)` from the data store.
- `Common.SaveSettings(...)` updates both the data store and the widget cache, but the earlier row `26512` update was a direct SQL update, not the normal widget save path.

The local BlogAI checkout also exposes an admin settings action:

```text
Y:\BlogAI\BlogEngine\BlogEngine.NET\AppCode\Api\SettingsController.cs
```

`Put(..., action = "clearCache")` calls:

```text
BlogEngine.Core.Blog.CurrentInstance.Cache.Reset()
```

This slice did not call that action because it would change public site behavior and likely requires admin authorization.

## Classification

| Candidate source | Classification |
| --- | --- |
| TextBox widget row `26512` current DB setting | Not stale; current DB row is updated. |
| TextBox widget row `26512` cached runtime setting | Likely source. Rendered stale text matches the preserved pre-update widget exactly. |
| GwnWikiExtension settings | Not source of feature-branch marker. Contains intentional `post.aspx?id=...` mappings only. |
| Layout/theme source | Not found in local BlogAI source search. |
| Post bodies | Not current source. All 14 routes display expected post-specific canonical markers, and publish-review reports recorded post-body stale-link cleanup. |
| Another `be_DataStoreSettings` row | Not found; live DB search returned zero rows containing the stale marker. |

## Recommended Safe Remediation

Do not publish or rewrite blog posts to fix this. The post bodies are already updated and rendered.

Recommended next remediation slice:

1. Preserve the current row `26512` again before any action.
2. Confirm the rendered site still shows the old widget content.
3. Use the safest available BlogAI cache-refresh mechanism for widget/settings cache, not the post-only reload path.
4. Prefer an authenticated BlogEngine cache clear if available and approved:
   - `PUT /api/settings?action=clearCache`
   - requires admin authorization
   - calls `Blog.CurrentInstance.Cache.Reset()`
5. If an authenticated cache clear is not available, use an app pool recycle or app restart only after explicit approval.
6. Re-run `SolutionFolder/scripts/blog-publishing/Test-BlogRenderedRoutes.ps1`.

Expected success criteria after remediation:

- all 14 routes still return HTTP 200
- all 14 routes still show expected post-specific canonical markers
- `feature/approval-apply-ui-slice` is absent from all 14 routes
- the rendered widget contains `BlogAI development source:` and `https://github.com/BillKrat/vs-mcp-bridge/tree/main`

## Current Decision

Stop after inspection. The source has been identified as stale cached TextBox widget content derived from row `26512`; the live database no longer contains the stale feature-branch link.

