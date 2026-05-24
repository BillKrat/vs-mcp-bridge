# BlogEngine Cache Clear Failure Inspection - 2026-05-16

This report inspects the failed BlogEngine cache-clear request after the 14 publish-review updated BlogAI post routes rendered the expected canonical post-body markers but still showed stale shared widget/page chrome content.

Scope constraints for this slice:

- No database writes were performed.
- No posts were published.
- No cache-clear retry was performed.
- No app recycle or restart was performed.
- Public site behavior was not changed.

## Prior Failed Request

The previous controlled cache-clear verification slice attempted one request:

| Check | Value |
| --- | --- |
| Method | `PUT` |
| Endpoint | `https://www.global-webnet.com/api/settings?action=clearCache` |
| Header used | `X-Blog-Reload-Key` from local `BlogEngineReloadKey` |
| Result | `500 Internal Server Error` |
| Response body | Empty |
| Follow-up route verification | 14 routes returned HTTP 200 and showed expected canonical post markers, but all 14 still contained `feature/approval-apply-ui-slice`. |

No second cache-clear attempt was made in this inspection slice.

## Source And Evidence Inspected

Repository and local BlogAI source inspection covered:

| Area | Path or evidence | Finding |
| --- | --- | --- |
| Prior cache-clear attempt report | `SolutionFolder/docs/blogs/final-rendered-route-verification-after-cache-clear-20260516.md` | Confirms the single HTTP 500 cache-clear attempt and the remaining stale shared marker on all 14 routes. |
| Stale shared widget inspection | `SolutionFolder/docs/blogs/stale-shared-feature-branch-link-inspection-20260516.md` | Confirms the stale marker matches cached TextBox widget content originally backed by `dbo.be_DataStoreSettings.DataStoreSettingRowId = 26512`; current live DB settings no longer contain the stale marker. |
| Settings API controller | `Y:\BlogAI\BlogEngine\BlogEngine.NET\AppCode\Api\SettingsController.cs` | Local source contains `PUT /api/settings?action=clearCache`; it calls `BlogEngine.Core.Blog.CurrentInstance.Cache.Reset()`. |
| Settings API authorization | `Y:\BlogAI\BlogEngine\BlogEngine.NET\AppCode\WebUtils.cs` and `Y:\BlogAI\BlogEngine\BlogEngine.NET\App_Data\rights.xml` | Local source requires `AccessAdminSettingsPages`, assigned to `Administrators`, before the settings controller can run. |
| Widget cache implementation | `Y:\BlogAI\BlogEngine\BlogEngine.NET\Custom\Widgets\Common.cs` | TextBox widget settings are cached under `be_widget_<id>` and direct SQL updates do not refresh that cache. |
| Blog cache reset implementation | `Y:\BlogAI\BlogEngine\BlogEngine.Core\Web\Controls\CacheProvider.cs` | `Blog.CurrentInstance.Cache.Reset()` removes cache entries scoped to the current blog id prefix. |
| Web API routing and error policy | `Y:\BlogAI\BlogEngine\BlogEngine.NET\AppCode\BlogEngineConfig.cs` | Local source maps `api/{controller}/{id}` and sets `IncludeErrorDetailPolicy.Never`, so production 500 bodies may be intentionally empty. |
| Deployed reload endpoint comparison | local `PostsController.cs` search | Local source does not contain the deployed `/api/posts/reload/{blogId}` endpoint used earlier for post reload verification, so a local/deployed source mismatch remains. |
| Logs | local `Y:\BlogAI` and `Y:\vs-mcp-bridge\artifacts` log searches | No relevant deployed HTTP 500 stack trace or settings API error log was available in the local checkouts/artifacts inspected. |

## Local Endpoint Behavior

The local `SettingsController` constructor performs an admin-rights check before any action-specific code runs:

```text
CheckRightsForAdminSettingsPage(true)
```

That check requires `AccessAdminSettingsPages`. In the local `rights.xml`, that right belongs to `Administrators`.

The local `PUT` action handles the clear-cache operation with:

```text
action == "clearCache" -> BlogEngine.Core.Blog.CurrentInstance.Cache.Reset()
```

The local settings controller does not reference:

- `X-Blog-Reload-Key`
- `BlogEngineReloadKey`
- a reload-key based authorization path

Therefore, the reload key that works for the deployed post reload workflow should not be assumed to authorize the settings cache-clear endpoint.

## Likely Cause Of HTTP 500

The most likely functional cause is that the request used the wrong authorization mechanism for this endpoint.

The attempted request sent `X-Blog-Reload-Key`, but local source shows `/api/settings?action=clearCache` is guarded by BlogEngine admin settings authorization, not by the reload-key header. A request without an authenticated admin context should not be expected to clear cache through this controller.

The exact reason the deployed site returned HTTP 500 instead of a clearer 401/403 could not be proven from local artifacts. Plausible explanations are:

- deployed source or configuration differs from the local BlogAI checkout;
- deployed Web API error handling converts the unauthorized/settings-controller failure into HTTP 500;
- the deployed endpoint implementation differs from local source;
- dependency injection, current-blog resolution, or authorization context failed before the controller could return an explicit unauthorized response.

The local source does not make `Cache.Reset()` itself look like the likely failure point. It is a scoped cache-key removal path. The higher-risk area is reaching that path with the correct authenticated/admin context on the deployed site.

## Local/Deployed Source Mismatch

A local/deployed source mismatch remains.

Earlier publish-review updates used a deployed post reload path that local source inspection did not find:

```text
/api/posts/reload/{blogId}
```

The local BlogAI checkout does contain the settings `clearCache` action, but does not contain the deployed post reload endpoint. That means local source can explain the intended BlogEngine settings cache-clear behavior, but it cannot fully prove the deployed implementation that returned HTTP 500.

## Widget Cache Impact

The stale rendered marker remains consistent with cached TextBox widget content, not current database content:

- the stale text matches the preserved pre-update widget content from row `26512`;
- the current live row `26512` no longer contains `feature/approval-apply-ui-slice`;
- a read-only search found no live `be_DataStoreSettings` rows containing the stale marker;
- post body markers render correctly on all 14 checked routes.

If the settings cache-clear action succeeds under the correct blog/admin context, `Blog.CurrentInstance.Cache.Reset()` should clear the current-blog cache entries, including cached widget settings such as the `be_widget_<id>` entry used by `Custom\Widgets\Common.cs`.

## Safest Remediation Options

Recommended options, from least invasive to most invasive:

1. Inspect deployed application logs for the exact `PUT /api/settings?action=clearCache` failure and capture the stack trace or authorization failure.
2. Confirm the deployed settings cache-clear endpoint source and authentication mechanism. Do not rely on `X-Blog-Reload-Key` for this endpoint unless deployed source proves it is supported.
3. Use a real authenticated BlogEngine administrator session/API path to clear cache through `/api/settings?action=clearCache`, if available and approved.
4. If an operator endpoint is preferred, implement or verify a dedicated cache-clear endpoint guarded by the existing reload-key mechanism, with explicit `401`, `403`, `200`, and `500` behavior and server-side logging. Keep it narrow to the current BlogAI cache/widget cache use case.
5. Clear the widget/settings cache through the BlogEngine admin UI if that path exists and is safer than a raw API call.
6. Use an app pool recycle or app restart only after explicit approval.

## Recommended Next Action

Do not publish more posts to address this. The post bodies already render correctly.

Before another cache-clear attempt, inspect the deployed BlogAI logs or deployed source for the HTTP 500 and confirm whether `/api/settings?action=clearCache` requires an authenticated administrator context, a different anti-forgery/session requirement, or a deployed-only implementation. Once that is documented, perform one known-good cache remediation path and rerun `SolutionFolder/scripts/blog-publishing/Test-BlogRenderedRoutes.ps1` across the 14 routes.

Current publishing status remains:

- 14 post bodies render their expected canonical markers.
- Final rendered-site signoff remains blocked only by stale shared widget/page chrome containing `feature/approval-apply-ui-slice`.
- No database or publishing action is recommended until the cache-clear failure path is understood or an approved recycle is selected.
