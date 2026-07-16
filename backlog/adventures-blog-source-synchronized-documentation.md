# Adventures.Blog — Source-Synchronized, Searchable Blog Documentation

## Title

Adventures.Blog — Source-Synchronized, Searchable Blog Documentation for Complex Codebases

## Date Captured

2026-07-16

## Captured By

Bill / Claude (source review 2026-07-16)

## Summary

`vs-mcp-bridge` already has a working, repo-driven BlogEngine.NET publishing pipeline, not just an idea for one:

- Canonical posts live at `SolutionFolder/docs/blogs/posts/<slug>/` as `post.json` + `content.html`, with the repo treated as source of truth and BlogEngine as the runtime/rendering target (see `SolutionFolder/scripts/blog-publishing/README.md`, "BlogEngine Integration (Repo-Driven Publishing)").
- A read-only compare stage (`Compare-BlogPostBeforePublish.ps1`, `Compare-ReadyBlogPostsBeforePublish.ps1`, `Compare-BlockedBlogRowsBeforePublish.ps1`) diffs live DB rows against a preserved export and the canonical repo post before any write.
- A guarded single-post update stage (`Publish-BlogPostReviewUpdate.ps1`, `Publish-BlogPostDraft.ps1`) writes only `Description`, `PostContent`, and `DateModified`, preserves row identity/taxonomy, and triggers a BlogAI reload endpoint.
- `Test-BlogRenderedRoutes.ps1` verifies the published result against the live rendered site.
- This has already been run end-to-end across a 7-part `vs-mcp-bridge-blog-series` plus several architecture-topic posts (named pipes, stdio, VSIX threading, inference-driven development, and — as of this backlog entry — a new "What Is AI?" post).

The pipeline's own README already names the next step: "Consider an `Adventures.Blog` project or library to own reusable blog synchronization tooling across projects that use the BlogAI blogging application" (`SolutionFolder/docs/blogs/README.md`, "Future Direction").

This backlog item captures the larger version of that idea: generalize the existing sync pipeline from a per-project PowerShell script set into a reusable capability that keeps an organization's blog/documentation content **consistent with its actual source code** — and, because content stays keyed to the repo it documents, makes a complex codebase searchable and explainable through its own blog history, not just its comments and commit log.

## Correction — 2026-07-16: The Repo Is Not Currently Aligned With The Deployed Site

The original capture of this item described the repo as "source of truth" without checking the live site against it. Bill flagged that the deployed content and the repo have already diverged, and asked that this be verified, not assumed, before closing the book on this topic. Checked directly:

- `https://adventuresontheedge.net/post/2026/06/13/models-vs-agents-vs-tools` is live today with content Bill updated directly on the deployed site — an essay titled "Models and Agents and Tools, oh my!" (MVPVM/Martin Fowler/MVC terminology-overloading history). No corresponding entry exists under `SolutionFolder/docs/blogs/posts/`.
- The live site's current front-page post is "Direct Corpus Interaction - Introduction" (a DCI/retrieval-architecture piece, dated 2026-06-13), also with no corresponding entry under `SolutionFolder/docs/blogs/posts/`.
- Per Bill: additional live "Post List" content exists beyond these two (DCI series entries and other experiences) that likewise has no repo counterpart.

So the accurate framing is not "the repo is source of truth" but "the repo is *intended* to be source of truth, and right now it is not — the deployed site has newer and additional content the repo doesn't know about." The existing pipeline (`Compare-*`, `Publish-*`) only checks repo-vs-database drift in the repo→BlogEngine direction; it has no step that discovers content that changed or was added on the live/deployed side first.

## Why It Matters

The pipeline that exists today already solves the hard, boring part: safe, auditable, read-before-write synchronization between a git repo and a live BlogEngine database (export → compare → guarded update → reload → verify-by-route). That is the expensive part to build. What is missing is generalizing it beyond this one repo and this one BlogEngine instance.

For an organization with a complex, multi-repo system, a synchronized blog/documentation layer that always reflects current source code — rather than documentation that silently drifts from the code it describes — has real value: onboarding, architecture comprehension, and search all improve when the narrative content is provably tied to the code, not just adjacent to it in a wiki someone forgot to update.

## Strategic Alignment

Directly aligned with this project's own stated direction (`SolutionFolder/docs/blogs/README.md`, "Future Direction") and with the repo-is-source-of-truth discipline already used throughout `vs-mcp-bridge` (`AI_START.md`, `ARCHITECTURE.md`, session handoffs, diagrams, and now blog posts, per the "Understanding AI Chat Sessions, Models, and Agents" post's own argument that durable, source-controlled artifacts survive context loss better than any single chat session).

## WDNA Tier Placement

Per this organization's WDNA (Windows DNA — Presentation / Business / Data Services) convention for architectural layering (`architecture-rosetta-stone/Standards/glossary.md`, WDNA entry):

- **Business Services** — the synchronization logic itself: export, compare, guarded update, reload-trigger, and route-verification. This is the layer the existing `scripts/blog-publishing/*.ps1` suite and the proposed `Adventures.Blog` library both belong to — business rules mediating between a source-of-truth repo and a runtime content store, not the storage or the UI.
- **Data Services** — two data stores this feature reconciles, not one: the repo-side canonical source (`docs/blogs/posts/<slug>/post.json` + `content.html`, plus preserved exports under `docs/blogs/source-of-truth/`) and BlogEngine's own database (`dbo.be_Posts`, `dbo.be_DataStoreSettings`).
- **Presentation Services** — BlogEngine's live rendered site (`BlogAI.Web`, the Blazor project) is the existing presentation surface. A future search/comprehension UI for exploring a codebase through its synchronized blog history — not yet designed — would also live in this tier.

## Process Gap: Discovery Must Come Before Sync

The existing pipeline is correctly described as "primitive" (Bill's word) — it assumes the repo-side canonical post is already correct and only guards the write from repo into BlogEngine. It has no phase that asks "did the deployed content change or grow since the repo was last updated?"

Any future evolution of this into a real tool must add a discovery phase **before** the existing compare/publish phases, not fold detection into them:

1. **Discover** — scan the deployed BlogEngine site/database for posts that are new, or whose live content no longer matches the last-known repo-side snapshot. Output a flagged candidate list; take no action on it automatically.
2. **Flag, don't assume** — each flagged candidate is a proposal that the repo-side source-of-truth may need to be updated *from* the live post, reviewed by a human before the repo file changes. Never treat the live version as automatically correct, and never treat the repo version as automatically correct either — the existing "never assumed" discipline this project already applies to citations and glossary entries applies here too.
3. **Reconcile** — only after a flagged candidate is reviewed does it become a repo-side update (new `posts/<slug>/` entry or a content refresh to an existing one).
4. **Then** the existing compare/publish/verify pipeline runs as it does today, repo → BlogEngine.

Today's pipeline only implements step 4. Steps 1–3 do not exist yet in any form (script, manual process, or otherwise) — this is not a refinement of existing code, it's a missing phase.

## Risk If Ignored

Low now. The existing per-project script suite keeps working as-is; nothing breaks by leaving this as a backlog item.

Medium over time if multiple projects independently reinvent the same export/compare/publish pattern instead of sharing one audited implementation — the risk is duplicated, unreviewed synchronization logic across repos, not a missing feature.

## Disruption Risk If Pursued Now

High. This would mean extracting and generalizing a pipeline that is still actively being used and refined for this project's own posts, before the current beta-release path is stable. Not required to complete the active slice.

## Suggested Priority

Post-beta research / extraction spike.

## Status

Captured.

## Notes

- Existing implementation to generalize from: `SolutionFolder/scripts/blog-publishing/` (10 scripts + README) and `SolutionFolder/docs/blogs/` (canonical posts, source-of-truth exports, publish-review reports).
- The pipeline is explicitly read-before-write and draft-forced at the script level (`Publish-BlogPostDraft.ps1` "forces `IsPublished = false`"); any future generalized library should preserve that same guarded-write discipline rather than relax it for convenience.
- First concrete content produced under the "save this for future blog content" pattern this backlog item is meant to support: `SolutionFolder/docs/blogs/posts/what-is-ai-a-grounded-definition/` (captured 2026-07-16, sourced from `architecture-rosetta-stone/Standards/glossary.md`'s AI and RDD entries). Note this post has not been checked against the deployed site for a same-slug conflict — do that check before any future publish attempt, per the Process Gap section above.
- Verified 2026-07-16 by loading `https://adventuresontheedge.net` directly (not assumed from prior repo state): live content includes at least "Models and Agents and Tools, oh my!" and "Direct Corpus Interaction - Introduction," neither present under `SolutionFolder/docs/blogs/posts/`.
- This entry is considered closed for now per Bill's direction ("that will close the book on what we need to know about this topic, until we are ready to prioritize and put it in a sprint for development") — captured with enough context to resume without re-deriving it, not a request to start the discovery-phase work now.
- Do not implement for beta. Revisit only when explicitly prioritized, per `backlog/shiny-object-policy.md`.
