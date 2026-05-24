# BlogAI Local Auth UI Readiness Review

## Purpose

Review whether the repo is ready to implement the first local/dev BlogAI auth UI-facing service and display surface.

This is documentation only. It does not add UI components, implement auth UI, call the API from Blazor, add production auth, add OAuth/OpenID/RBAC, add persistence, change deployment, or couple to BlogEngine.NET.

## Inputs Reviewed

- `SolutionFolder/docs/blogai-local-auth-ui-integration-design.md`
- `SolutionFolder/docs/adr/0002-blogai-ui-framework.md`
- `SolutionFolder/docs/adr/0003-blogai-globalization-localization.md`
- `SolutionFolder/docs/blogai-adventures-auth-consumer-design.md`
- `SolutionFolder/docs/blogai-minimal-auth-consumer-prototype-plan.md`
- `SolutionFolder/docs/session-handoffs/2026-05-17-blogai-blazor-shell-validation.md`

## Readiness Review

| Area | Status | Rationale | Follow-Up |
| --- | --- | --- | --- |
| UI integration objective | Pass | The objective is narrow: display local/dev auth state and protected-route placeholder state, not implement production auth or identity UX. | Keep the first slice on `/local-dev`. |
| Local/dev-only scope | Pass | All reviewed docs constrain the work to local development and explicitly defer production auth, deployment, persistence, and BlogEngine.NET coupling. | Stop if production hosting, certificates, real cookies, or deployment settings become necessary. |
| Thin component rule | Pass | ADR 0002 and the UI integration design require Razor components to render display models only. | Components should not parse credentials, call auth APIs directly, or make allow/deny decisions. |
| UI-facing service/interface boundary | Pass | The design calls for a UI-facing service/interface between Razor components and the BlogAI auth consumer or future local API client. | Add the service contract before expanding component behavior. |
| Auth decision ownership | Pass | `AdventuresAuth` and the BlogAI auth consumer boundary remain responsible for auth decisions. BlogAI UI may only display safe state. | Keep `IBlogAiAuthConsumerService` or a local API client behind the UI-facing service. |
| Safe decision/correlation display | Pass | The design allows `Allowed`, outcome, reason code, `CorrelationId`, `RequestId`, and `AuthDecisionId` where useful. | Display only metadata needed for local diagnostics. |
| Sensitive-data rules | Pass | Raw tokens, secrets, passwords, cookies, authorization headers, and credential-bearing bodies are explicitly prohibited from UI, logs, traces, and artifacts. | Add tests or trace evidence proving redaction/no-secret display if any auth marker is introduced. |
| Validation scenarios | Pass | Required scenarios cover unauthenticated, dev-authenticated, invalid session, protected placeholder denied/allowed behavior, correlation, and no BlogEngine.NET dependency. | First implementation should include build and route smoke validation; add focused tests where practical. |
| Non-goals | Pass | Production login UI, password forms, OAuth/OpenID, RBAC, account management, persistent session UX, deployment, persistence, BlogEngine.NET, and localization resources are all out of scope. | Revisit design before adding any non-goal. |
| Stop conditions | Pass | Stop if scope expands into production auth, raw secret display, component-owned auth logic, API calls without a service boundary, persistence, deployment, or BlogEngine.NET coupling. | Treat any stop condition as a new design slice. |
| First implementation scope | Pass | The next slice can be small: add a UI-facing service/interface, map local auth decision state into a display-safe model, and update `/local-dev` display only. | Do not add a production login form or real identity flow. |

## Decision

Ready for minimal local auth UI slice.

The repo has enough design and validation evidence to implement a local/dev-only UI-facing service plus a small `/local-dev` display update, provided the implementation keeps auth decisions outside Razor components and displays only safe decision/correlation metadata.

## Next Implementation Slice

Implement the first local/dev BlogAI auth UI boundary:

- add a UI-facing service/interface in `BlogAI.Web`
- add a display-safe local auth state model
- update `/local-dev` display only
- show local auth decision state and correlation metadata
- do not add a production login form
- do not display raw token, secret, cookie, authorization header, password, or credential-bearing body values
- do not add OAuth/OpenID/RBAC
- do not add persistence
- do not change deployment
- do not couple to BlogEngine.NET
- keep Razor components presentation-focused
- validate with build and local route smoke tests

## Resume Guidance

Before implementing the next slice, read:

- `SolutionFolder/docs/blogai-local-auth-ui-integration-design.md`
- `SolutionFolder/docs/blogai-adventures-auth-consumer-design.md`
- `SolutionFolder/docs/session-handoffs/2026-05-17-blogai-blazor-shell-validation.md`
- this readiness review

The next implementation should stop immediately if it needs production auth, real login UX, persistent sessions, deployment changes, BlogEngine.NET integration, localization resources, or component-owned auth decisions.
