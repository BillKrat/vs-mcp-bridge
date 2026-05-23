# ADR 0002: BlogAI UI Framework

## Status

Proposed

## Context

BlogAI is becoming the first real application workload for the `vs-mcp-bridge` platform and the reusable Global WebNet auth/API boundary.

Current direction:

- `vs-mcp-bridge` remains the primary platform priority.
- `AdventuresAuth` exists as the local/dev reusable auth decision capability.
- `Adventures.Auth.LocalApi` exposes that capability through local/dev ASP.NET Core Minimal APIs.
- BlogAI is the first consumer of the auth/API boundary.
- Future reusable package direction may become `Adventures.*`.

Before adding any BlogAI UI project, the repo needs a durable framework decision so implementation does not drift into a JavaScript-first stack by default or into UI-owned platform behavior.

## Decision

Use Blazor Web App as the preferred initial BlogAI UI framework.

This decision originally did not create a Blazor project, UI components, packages, deployment path, or BlogAI runtime behavior. The first implementation slice later added `BlogAI.Web` as a minimal Blazor Web App shell with no production auth, deployment configuration, persistence, OAuth/OpenID, RBAC, or BlogEngine.NET coupling.

## Rationale

Blazor Web App is the preferred initial direction because:

- BlogAI should stay aligned with the current .NET/C# platform direction.
- `AdventuresAuth`, future `Adventures.Auth`, Minimal API services, and `vs-mcp-bridge` are all .NET-aligned.
- Blazor Web App keeps UI, service boundaries, auth integration, testing, and deployment reasoning in one coherent stack.
- It supports incremental evolution without introducing a separate JavaScript-first build/runtime ecosystem too early.
- The initial BlogAI UI should stay close to application service boundaries while the auth/API boundary is still being pressure-tested.

## Architectural Boundary Rule

Blazor components must stay thin.

Rules:

- components render UI and handle interaction state
- components should avoid hard-coded culture-specific date, number, or currency assumptions
- auth, publishing, audit, workflow, and BlogAI business logic live in services/interfaces
- UI consumes application and API client boundaries
- UI does not own platform behavior
- UI does not make identity decisions
- UI does not bypass `AdventuresAuth` or the API boundary for protected behavior
- UI does not log raw secrets, tokens, cookies, authorization headers, credentials, or full credential-bearing request/response bodies

This mirrors the current API direction: Minimal API endpoint handlers stay thin, and auth decisions stay in shared services.

## Consequences

Positive consequences:

- BlogAI can begin UI work without introducing a separate JavaScript-first runtime.
- Auth/API integration can remain close to the existing .NET service boundary.
- Tests can share more .NET infrastructure and conventions.
- Future package extraction under `Adventures.*` remains easier to reason about.
- UI and API boundaries can use the same correlation, audit, redaction, and service-interface vocabulary.

Tradeoffs:

- Some JavaScript ecosystem UI libraries are not the default first choice.
- If BlogAI later needs complex client-side interactivity, the Blazor decision may need to be revisited.
- The team must keep components disciplined so UI code does not absorb service or workflow logic.

## Alternatives Considered

### React / Next.js

Viable later if BlogAI develops complex client-side UI needs, a strong JavaScript component ecosystem requirement, or deployment reasons that outweigh the .NET stack alignment.

Not the default first choice because it introduces a separate JavaScript-first build/runtime ecosystem before the service and auth boundaries are fully proven.

### SvelteKit

Viable later for a lightweight JavaScript-first app if the UI becomes the primary product surface and benefits from Svelte-specific ergonomics.

Not the default first choice because it adds another runtime and deployment model before BlogAI has validated its initial .NET service boundaries.

### Angular

Viable later for a large, highly structured frontend with strong framework conventions.

Not the default first choice because it is heavier than the current near-term BlogAI UI needs.

### MVC/Razor Pages

Viable for server-rendered pages with low interactivity.

Not the preferred initial choice because Blazor Web App better preserves incremental interactive UI options while staying in the .NET stack.

### Plain Static Frontend

Viable for static content or a very small unauthenticated surface.

Not sufficient as the preferred initial BlogAI UI direction because BlogAI is expected to integrate with auth/API, workflow, audit, and publishing/service boundaries.

## Non-Goals

This ADR does not approve:

- creating a Blazor project
- creating UI components
- adding packages
- changing deployment
- implementing BlogAI UI
- implementing production auth
- adding OAuth/OpenID
- adding RBAC
- adding database-backed identity
- coupling to BlogEngine.NET auth
- replacing the current Minimal API auth boundary direction
- introducing JavaScript-first tooling as the default UI path

## Deferred Decisions

Deferred until a future explicit implementation or design slice:

- Blazor render mode
- project name and placement
- route structure under `/blogAi`
- API client shape
- authentication state integration
- component library or CSS strategy
- validation and testing stack
- deployment model
- static asset pipeline
- production hosting
- accessibility and design system details

## Alignment

Current preferred stack direction:

- `AdventuresAuth` for reusable auth decision behavior
- ASP.NET Core Minimal APIs for auth/API boundaries
- Blazor Web App for the initial BlogAI UI
- future `Adventures.*` packages only when reuse pressure justifies extraction

This keeps BlogAI as the first consumer of the platform and auth/API boundary without making UI code the owner of platform behavior.

## Initial Shell

The initial shell project is `BlogAI.Web`.

It contains a basic Blazor Web App surface for local development only. Components are presentation-only placeholders. The project does not reference BlogEngine.NET, does not implement auth UI, and does not make identity decisions. Future protected behavior should consume service or API client boundaries rather than placing auth or business logic in Razor components.

Globalization and localization direction is recorded in `docs/adr/0003-blogai-globalization-localization.md`: BlogAI remains English-first for now, but UI/API presentation should be globalization-ready while logs, audit events, traces, and developer docs remain invariant.
