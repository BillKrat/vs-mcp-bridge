# Blog Alignment Inventory - 2026-05-16

## Scope

This inventory compares the preserved BlogEngine database export, current repo-backed canonical blog posts, and the rendered HTTPS site at `https://www.global-webnet.com`.

No database records were changed.
No blog content was rewritten.
No public site behavior was changed.

## Sources Reviewed

- `AI_START.md`
- `docs/session-handoffs/2026-05-16-end-of-day-architecture-handoff.md`
- `docs/ARCHITECTURE.md`
- `docs/blogs/README.md`
- `scripts/blog-publishing/README.md`
- `docs/blogs/source-of-truth/db-export-20260516/manifest.json`
- Rendered site root: `https://www.global-webnet.com`
- Rendered post URLs under `https://www.global-webnet.com/post/YYYY/MM/DD/<slug>`

## Baseline Counts

| Source | Count | Notes |
| --- | ---: | --- |
| DB-exported rows | 22 | From `dbo.be_Posts`, includes deleted rows. |
| DB published rows | 20 | Active, non-deleted published rows. |
| DB deleted rows | 2 | Preserved for history, not candidates for public rewrite. |
| Repo canonical post folders | 11 | Under `docs/blogs/posts/`. |
| DB rows with matching canonical repo slug | 9 | Matching by `post.json` slug, not folder name. |
| DB rows missing canonical repo slug | 13 | Includes 2 deleted rows and 11 active rows. |
| Repo canonical slugs absent from DB | 2 | Existing repo-only trial/draft material. |

## DB Posts With Canonical Repo Posts

All matching repo canonical posts differ from the current DB export at least slightly when normalized HTML is compared. Treat the DB export as the preserved runtime baseline and the repo canonical post as an editable source candidate, not as a proven byte-for-byte copy of production.

| DB slug | DB status | Repo folder | Normalized HTML match | Rendered HTTPS URL |
| --- | --- | --- | --- | --- |
| `how-stdio-works-in-vs-mcp-bridge` | published | `how-stdio-works-in-vs-mcp-bridge` | no | `https://www.global-webnet.com/post/2026/04/19/how-stdio-works-in-vs-mcp-bridge` |
| `understanding-ai-chat-sessions-models-and-agents` | published | `understanding-ai-chat-sessions-models-and-agents` | no | `https://www.global-webnet.com/post/2026/04/12/understanding-ai-chat-sessions-models-and-agents` |
| `understanding-a-named-pipe-listener` | published | `understanding-a-named-pipe-listener` | no | `https://www.global-webnet.com/post/2026/04/17/understanding-a-named-pipe-listener` |
| `vs-mcp-bridge-blog-series-part-1` | published | `vs-mcp-bridge-blog-series-part-1` | no | `https://www.global-webnet.com/post/2026/04/11/vs-mcp-bridge-blog-series-part-1` |
| `vs-mcp-bridge-blog-series-part-2` | published | `vs-mcp-bridge-blog-series-part-2` | no | `https://www.global-webnet.com/post/2026/04/11/vs-mcp-bridge-blog-series-part-2` |
| `vs-mcp-bridge-blog-series-part-3` | published | `vs-mcp-bridge-blog-series-part-3` | no | `https://www.global-webnet.com/post/2026/04/11/vs-mcp-bridge-blog-series-part-3` |
| `vs-mcp-bridge-blog-series-part-5` | published | `vs-mcp-bridge-blog-series-part-5` | no | `https://www.global-webnet.com/post/2026/04/11/vs-mcp-bridge-blog-series-part-5` |
| `vs-mcp-bridge-blog-series-part-6` | published | `vs-mcp-bridge-blog-series-part-6` | no | `https://www.global-webnet.com/post/2026/04/11/vs-mcp-bridge-blog-series-part-6` |
| `vs-mcp-bridge-blog-series-part-7` | published | `vs-mcp-bridge-blog-series-part-7` | no | `https://www.global-webnet.com/post/2026/04/11/vs-mcp-bridge-blog-series-part-7` |

## DB Posts Missing Canonical Repo Posts

| DB slug | DB status | Rendered HTTPS status | Notes |
| --- | --- | --- | --- |
| `Welcome-to-Developments-blog` | published | 404 at top-level post route | Older multi-blog or blog-specific content; not currently canonicalized. |
| `creating-a-post` | deleted | not checked | Deleted row preserved only as history. |
| `difference-between-post-and-page` | deleted | not checked | Deleted row preserved only as history. |
| `welcome-to-our-site` | published | 404 at top-level post route | Older site content; contains a large embedded data URI in the DB export. |
| `this-one-will-only-be-seen-by-subscribers` | published | 404 at top-level post route | Subscriber-oriented legacy content. |
| `blog-engine-billkrat-fork-documentation` | published | 404 at top-level post route | BlogEngine documentation content, not VS MCP Bridge narrative. |
| `how-to-publish-your-own-blog-smarterasp` | published | 404 at top-level post route | Hosting/tutorial content, not current VS MCP Bridge narrative. |
| `understanding-dependency-injection` | published | 404 at top-level post route | General DI article, not current VS MCP Bridge narrative. |
| `wpf-vsix-threading-understanding-ui-switching-async-behavior-and-pipe-safety` | published | 200 | Relevant VSIX background; not canonicalized yet. |
| `why-vsix-project-should-target-net-framework-4-7-2` | published | 200 | Relevant VSIX background; not canonicalized yet. |
| `understanding-local-mcp-server-over-stdio-and-local-only-communication-over-a-named-pipe` | published | 200 | Overlaps current stdio/named-pipe architecture; contains stale GitHub branch link. |
| `vs-mcp-bridge-blog-series-part-4` | published | 200 | The repo has a Part 4 folder, but its `post.json` slug is `vs-mcp-bridge-blog-series-part-4-repo-trial`, so it does not match this DB slug. |
| `inference-driven-software-design-with-copilot-pros-and-cons` | published | 200 | Strong BlogAI narrative candidate; not canonicalized yet. |

## Repo Canonical Posts Absent From DB

| Repo slug | Repo folder | Status | Notes |
| --- | --- | --- | --- |
| `vs-mcp-bridge-publish-create-trial` | `vs-mcp-bridge-publish-create-trial` | draft | Repo-only publishing trial material. Do not overwrite DB without an explicit decision. |
| `vs-mcp-bridge-blog-series-part-4-repo-trial` | `vs-mcp-bridge-blog-series-part-4` | draft | Repo folder name resembles the live Part 4 post, but `post.json` points to a different trial slug and post id. Treat as unsafe to publish over live Part 4 until reconciled. |

## Rendered HTTPS Reachability

The HTTPS root `https://www.global-webnet.com` is reachable and renders the current Adventures On The Edge content stream.

Top-level BlogEngine post-route checks for active DB rows:

| DB slug | URL checked | Result |
| --- | --- | --- |
| `Welcome-to-Developments-blog` | `https://www.global-webnet.com/post/2023/07/29/Welcome-to-Developments-blog` | 404 |
| `welcome-to-our-site` | `https://www.global-webnet.com/post/2023/09/25/welcome-to-our-site` | 404 |
| `this-one-will-only-be-seen-by-subscribers` | `https://www.global-webnet.com/post/2023/09/25/this-one-will-only-be-seen-by-subscribers` | 404 |
| `blog-engine-billkrat-fork-documentation` | `https://www.global-webnet.com/post/2025/02/08/blog-engine-billkrat-fork-documentation` | 404 |
| `how-to-publish-your-own-blog-smarterasp` | `https://www.global-webnet.com/post/2025/02/13/how-to-publish-your-own-blog-smarterasp` | 404 |
| `understanding-dependency-injection` | `https://www.global-webnet.com/post/2025/03/30/understanding-dependency-injection` | 404 |
| `wpf-vsix-threading-understanding-ui-switching-async-behavior-and-pipe-safety` | `https://www.global-webnet.com/post/2026/03/28/wpf-vsix-threading-understanding-ui-switching-async-behavior-and-pipe-safety` | 200 |
| `why-vsix-project-should-target-net-framework-4-7-2` | `https://www.global-webnet.com/post/2026/04/01/why-vsix-project-should-target-net-framework-4-7-2` | 200 |
| `understanding-local-mcp-server-over-stdio-and-local-only-communication-over-a-named-pipe` | `https://www.global-webnet.com/post/2026/04/01/understanding-local-mcp-server-over-stdio-and-local-only-communication-over-a-named-pipe` | 200 |
| `vs-mcp-bridge-blog-series-part-1` | `https://www.global-webnet.com/post/2026/04/11/vs-mcp-bridge-blog-series-part-1` | 200 |
| `vs-mcp-bridge-blog-series-part-2` | `https://www.global-webnet.com/post/2026/04/11/vs-mcp-bridge-blog-series-part-2` | 200 |
| `vs-mcp-bridge-blog-series-part-3` | `https://www.global-webnet.com/post/2026/04/11/vs-mcp-bridge-blog-series-part-3` | 200 |
| `vs-mcp-bridge-blog-series-part-4` | `https://www.global-webnet.com/post/2026/04/11/vs-mcp-bridge-blog-series-part-4` | 200 |
| `vs-mcp-bridge-blog-series-part-5` | `https://www.global-webnet.com/post/2026/04/11/vs-mcp-bridge-blog-series-part-5` | 200 |
| `vs-mcp-bridge-blog-series-part-6` | `https://www.global-webnet.com/post/2026/04/11/vs-mcp-bridge-blog-series-part-6` | 200 |
| `vs-mcp-bridge-blog-series-part-7` | `https://www.global-webnet.com/post/2026/04/11/vs-mcp-bridge-blog-series-part-7` | 200 |
| `understanding-ai-chat-sessions-models-and-agents` | `https://www.global-webnet.com/post/2026/04/12/understanding-ai-chat-sessions-models-and-agents` | 200 |
| `understanding-a-named-pipe-listener` | `https://www.global-webnet.com/post/2026/04/17/understanding-a-named-pipe-listener` | 200 |
| `how-stdio-works-in-vs-mcp-bridge` | `https://www.global-webnet.com/post/2026/04/19/how-stdio-works-in-vs-mcp-bridge` | 200 |
| `inference-driven-software-design-with-copilot-pros-and-cons` | `https://www.global-webnet.com/post/2026/04/23/inference-driven-software-design-with-copilot-pros-and-cons` | 200 |

## Representative Content Comparison

The sample comparison confirmed three independent views:

- DB export body: `docs/blogs/source-of-truth/db-export-20260516/<folder>/content.html`
- repo canonical body when present: `docs/blogs/posts/<folder>/content.html`
- rendered page: `https://www.global-webnet.com/post/YYYY/MM/DD/<slug>`

| Slug | DB export body present | Repo canonical present | Rendered page accessible | Finding |
| --- | --- | --- | --- | --- |
| `vs-mcp-bridge-blog-series-part-1` | yes | yes | yes | Rendered title matches DB title; repo and DB HTML are not normalized-identical. |
| `vs-mcp-bridge-blog-series-part-4` | yes | no matching slug | yes | Live DB post exists, but repo folder is currently a separate trial slug. |
| `how-stdio-works-in-vs-mcp-bridge` | yes | yes | yes | Rendered title matches DB title; repo and DB HTML are not normalized-identical. |
| `inference-driven-software-design-with-copilot-pros-and-cons` | yes | no | yes | Rendered post exists and should be canonicalized before BlogAI editing. |

## Link Findings

### Rendered links still pointing at `adventuresontheedge.net`

The DB export manifest does not list `adventuresontheedge.net` body links, but rendered pages include generated or runtime-resolved inter-post links pointing to the old HTTP domain.

| Rendered source post | Old-domain rendered links |
| --- | --- |
| `vs-mcp-bridge-blog-series-part-1` | `http://adventuresontheedge.net/post.aspx?id=6484fa94-5d8b-429a-99c6-779b300bc336`, `http://adventuresontheedge.net/post.aspx?id=d0541943-0de1-4c25-a7af-9950c55f1591` |
| `understanding-ai-chat-sessions-models-and-agents` | `http://adventuresontheedge.net/post.aspx?id=b3da6b1c-a955-4ec2-afda-b281bd5d46fd` |
| `understanding-a-named-pipe-listener` | `http://adventuresontheedge.net/post.aspx?id=f0c7a958-f41a-4143-b601-82ce84fd4af0` |
| `inference-driven-software-design-with-copilot-pros-and-cons` | `http://adventuresontheedge.net/post.aspx?id=5465cc54-65ab-4c4f-b6ac-4539de01c365` |

These should be treated as public rendering/link-generation findings, not direct DB body evidence, until the BlogEngine link source is confirmed.

### GitHub links still pointing at old feature branches

| DB slug | Link |
| --- | --- |
| `understanding-local-mcp-server-over-stdio-and-local-only-communication-over-a-named-pipe` | `https://github.com/BillKrat/vs-mcp-bridge/blob/feature/approval-apply-ui-slice/docs/ARCHITECTURE.md` |
| `vs-mcp-bridge-blog-series-part-3` | `https://github.com/BillKrat/vs-mcp-bridge/blob/feature/approval-apply-ui-slice/docs/ARCHITECTURE.md` |
| `vs-mcp-bridge-blog-series-part-4` | `https://github.com/BillKrat/vs-mcp-bridge/blob/feature/approval-apply-ui-slice/docs/ARCHITECTURE.md` |
| `vs-mcp-bridge-blog-series-part-5` | `https://github.com/BillKrat/vs-mcp-bridge/blob/feature/approval-apply-ui-slice/docs/ARCHITECTURE.md` |
| `vs-mcp-bridge-blog-series-part-6` | `https://github.com/BillKrat/vs-mcp-bridge/blob/feature/approval-apply-ui-slice/docs/ARCHITECTURE.md` |

Future content work should replace these with `main` links or durable documentation links after the canonical rewrite plan is chosen.

## Stale Relative To `docs/ARCHITECTURE.md`

The following posts are likely stale or incomplete relative to the current architecture document and recent handoffs:

| Post | Staleness risk |
| --- | --- |
| `understanding-local-mcp-server-over-stdio-and-local-only-communication-over-a-named-pipe` | Predates current security seams, approval-aware execution, capability policy, secret-reference seam, audit classification, MEF discovery notes, and VSIX activation diagnostics. |
| `understanding-a-named-pipe-listener` | Explains the named-pipe listener but should be reconciled with the current operator rule that the VS Experimental Instance and VS MCP Bridge tool window activation are required for successful live validation. |
| `how-stdio-works-in-vs-mcp-bridge` | Still useful for transport boundaries, but should be updated with current tool list, activation diagnostics, and anti-black-box logging discipline. |
| `vs-mcp-bridge-blog-series-part-1` through `vs-mcp-bridge-blog-series-part-7` | Core narrative predates the full security/diagnostic phase now documented in `docs/ARCHITECTURE.md` and session handoffs. |
| `wpf-vsix-threading-understanding-ui-switching-async-behavior-and-pipe-safety` | Relevant but not yet canonicalized; should be checked against current VSIX activation and named-pipe behavior. |
| `why-vsix-project-should-target-net-framework-4-7-2` | Relevant background but not enough to describe the current host/runtime architecture. |

Older non-VS MCP posts may be accurate in their own domains, but they are not aligned with the new BlogAI narrative unless explicitly retained as background content.

## BlogAI Narrative Candidates

Strong candidates to bring into the BlogAI narrative after canonicalization:

- `inference-driven-software-design-with-copilot-pros-and-cons`
- `understanding-ai-chat-sessions-models-and-agents`
- `vs-mcp-bridge-blog-series-part-1`
- `vs-mcp-bridge-blog-series-part-2`
- `vs-mcp-bridge-blog-series-part-3`
- `vs-mcp-bridge-blog-series-part-4`
- `vs-mcp-bridge-blog-series-part-5`
- `vs-mcp-bridge-blog-series-part-6`
- `vs-mcp-bridge-blog-series-part-7`
- `understanding-local-mcp-server-over-stdio-and-local-only-communication-over-a-named-pipe`
- `understanding-a-named-pipe-listener`
- `how-stdio-works-in-vs-mcp-bridge`
- `wpf-vsix-threading-understanding-ui-switching-async-behavior-and-pipe-safety`
- `why-vsix-project-should-target-net-framework-4-7-2`

Likely supporting or out-of-scope content for BlogAI:

- `blog-engine-billkrat-fork-documentation`
- `how-to-publish-your-own-blog-smarterasp`
- `understanding-dependency-injection`
- `Welcome-to-Developments-blog`
- `welcome-to-our-site`
- `this-one-will-only-be-seen-by-subscribers`
- deleted rows `creating-a-post` and `difference-between-post-and-page`

## Ambiguous Or Unsafe-To-Overwrite Records

- `vs-mcp-bridge-blog-series-part-4` is live in the DB, but the repo folder `docs/blogs/posts/vs-mcp-bridge-blog-series-part-4/` currently declares slug `vs-mcp-bridge-blog-series-part-4-repo-trial` and a different draft identity. This is the highest-risk overwrite ambiguity.
- `vs-mcp-bridge-publish-create-trial` exists only in the repo and should remain a trial artifact unless intentionally promoted.
- Rendered old-domain inter-post links appear to be generated outside the exported body HTML. Their source should be located before mechanically editing post bodies.
- Six active DB posts returned 404 at the tested top-level `global-webnet.com/post/YYYY/MM/DD/<slug>` route. They may belong to older multi-blog paths, legacy routing, or hidden/subscriber contexts.
- The large embedded data URI in `welcome-to-our-site` is preserved in the DB export body but represented only by hash/length in the manifest. Avoid reformatting that post until a rewrite plan explicitly handles embedded assets.

## Recommended Next Slice

Before rewriting public content, reconcile canonical source coverage:

1. Create canonical repo post folders for active DB posts that are BlogAI candidates but currently missing from `docs/blogs/posts/`.
2. Resolve the Part 4 repo trial mismatch before any publish operation.
3. Locate the source of rendered `adventuresontheedge.net` `post.aspx?id=...` links.
4. Decide whether old feature-branch GitHub links should be updated directly to `main` or replaced with repo-relative documentation links in canonical content.
5. Only after those decisions, begin small per-post rewrites aligned to `docs/ARCHITECTURE.md`.
