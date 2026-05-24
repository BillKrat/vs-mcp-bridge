# BlogAI Hosting Authorship Clarification

## Checkpoint

- branch: `main`
- starting HEAD: `1398a4b Validate local-only file recovery guidance`
- starting state: `main == origin/main`, working tree clean
- scope: docs-only clarification of BlogAI hosting, authorship, publishing, and API boundaries

## Current BlogAI Publishing Surface

`https://adventuresontheedge.net/blogai` is the current legacy BlogAI/sub-blog surface.

This surface is a separate BlogEngine.NET blog/blogId used for BlogAI-related posts. Current BlogAI posts are published and read under:

- `https://adventuresontheedge.net/blogai/post/...`

This note does not move routes, alter BlogEngine.NET configuration, publish posts, or imply a migration away from the current legacy BlogAI/sub-blog surface.

## Authorship Boundary

Posts authored by `BillKrat` are human-authored and must not be altered by Codex or automated blog publishing workflows.

Authorship must remain honest:

- human-authored learning, reflection, research, and analysis posts remain under `BillKrat`
- future AI-maintained blog content should use the `AI Systems Author` identity
- Codex/blog publishing workflows may create and maintain `AI Systems Author` documents only
- automated workflows must not rewrite, reclassify, or maintain `BillKrat` posts unless a future prompt explicitly authorizes a narrow human-reviewed operation

Human-authored learning/reflection posts, including DCI abstract or research-reflection work, belong under `BillKrat` authorship and are outside automated maintenance.

Do not pollute the primary blogs with AI-authored content unless that is explicitly intended for a specific publishing slice.

## Future BlogAI Hosting Direction

`https://www.global-webnet.com/blogAi` is the future BlogAI virtual app/client surface.

`https://www.global-webnet.com/blogAi/api/...` is a possible future BlogAI app-specific API prefix.

`https://api.global-webnet.com` is the future shared auth/API host.

The intended boundary is:

- BlogAI client/app surface: `https://www.global-webnet.com/blogAi`
- BlogAI app-specific APIs: possible `https://www.global-webnet.com/blogAi/api/...`
- shared auth/admin APIs: `https://api.global-webnet.com`

BlogAI app-specific APIs should stay separate from shared auth/admin APIs.

Auth/admin ownership remains with the auth service boundary, not BlogAI page components.

## Non-Inferences

Do not infer any of the following from this note:

- production auth is ready or approved
- BlogEngine.NET coupling should increase
- route migration should begin
- blog publishing automation should begin
- current BlogAI posts should be republished
- `BillKrat` authored posts can be maintained by Codex
- shared auth/admin APIs belong inside BlogAI Razor pages or components

This note is a boundary clarification only.

## Operational Rule

Before any future BlogAI publishing, route, API, or authorship automation slice, confirm:

- target surface
- intended author identity
- whether the content is human-authored or AI-maintained
- whether the operation is docs-only, draft-only, publish-capable, or deployment-capable
- whether BlogEngine.NET state is in scope
- whether shared auth/admin APIs or BlogAI app-specific APIs are in scope

If authorship or hosting scope is unclear, stop and ask before changing documents, routes, deployment settings, or published content.
