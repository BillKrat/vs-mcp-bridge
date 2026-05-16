# Blog Source Reconciliation - 2026-05-16

## Scope

This reconciliation slice materializes missing repo-backed canonical posts for active BlogAI candidate records from the preserved database export.

No database records were changed.
No public site behavior was changed.
No article bodies were rewritten.
Deleted database rows were not canonicalized.

## Inputs

- Inventory: `docs/blogs/blog-alignment-inventory-20260516.md`
- DB export manifest: `docs/blogs/source-of-truth/db-export-20260516/manifest.json`
- DB row folders: `docs/blogs/source-of-truth/db-export-20260516/<slug>/`
- Canonical post root: `docs/blogs/posts/`

## Canonical Posts Added

The following active DB-exported BlogAI candidate posts now have canonical repo folders under `docs/blogs/posts/`.
Each `content.html` was copied from the preserved DB export without body rewrites.
Each `post.json` preserves the DB title, description, author, slug, publication status, categories, tags, and source database identifiers.

| Canonical slug | Source DB row | Source DB post id | Rendered status from inventory | Notes |
| --- | ---: | --- | --- | --- |
| `wpf-vsix-threading-understanding-ui-switching-async-behavior-and-pipe-safety` | 135 | `46828793-1f3d-4031-906e-87c1c31dce7e` | 200 | VSIX/threading background candidate. |
| `why-vsix-project-should-target-net-framework-4-7-2` | 141 | `ae00d3f4-7c9a-4084-b690-d974e945d69e` | 200 | VSIX target framework background candidate. |
| `understanding-local-mcp-server-over-stdio-and-local-only-communication-over-a-named-pipe` | 142 | `43bf6aae-15c9-4c90-b3b2-66ac51c4a7c8` | 200 | Stdio/named-pipe architecture candidate. |
| `vs-mcp-bridge-blog-series-part-4` | 146 | `f62f7756-269a-4d49-a87d-c0394c7627d9` | 200 | Live DB Part 4 post; see Part 4 mapping below. |
| `inference-driven-software-design-with-copilot-pros-and-cons` | 156 | `b3da6b1c-a955-4ec2-afda-b281bd5d46fd` | 200 | BlogAI narrative candidate. |

The active BlogAI candidate set from the inventory is now represented in `docs/blogs/posts/`.
Older support/out-of-scope posts remain preserved only in the DB export until there is an explicit decision to canonicalize them.

## Part 4 Mapping Resolution

Before this slice, `docs/blogs/posts/vs-mcp-bridge-blog-series-part-4/` contained a repo trial draft whose `post.json` declared slug `vs-mcp-bridge-blog-series-part-4-repo-trial`.
That folder name collided with the live DB slug `vs-mcp-bridge-blog-series-part-4`.

The meanings are now separated:

| Meaning | Folder | Slug | Status |
| --- | --- | --- | --- |
| Live DB/exported Part 4 post | `docs/blogs/posts/vs-mcp-bridge-blog-series-part-4/` | `vs-mcp-bridge-blog-series-part-4` | published metadata copied from DB export |
| Repo publishing trial draft | `docs/blogs/posts/vs-mcp-bridge-blog-series-part-4-repo-trial/` | `vs-mcp-bridge-blog-series-part-4-repo-trial` | draft trial artifact |

Future publishing work must not treat the repo trial draft as the live Part 4 post.
The live Part 4 canonical folder now maps directly to DB row 146 and BlogEngine post id `f62f7756-269a-4d49-a87d-c0394c7627d9`.

## Old-Domain Link Source Findings

Rendered `adventuresontheedge.net` inter-post links are not stored as explicit old-domain URLs in the DB body export or canonical post bodies.

Repo search found no `adventuresontheedge.net` or `post.aspx?id=...` references under:

- `docs/blogs/source-of-truth/db-export-20260516/`
- `docs/blogs/posts/`

The rendered old-domain links are produced from bracket-style BlogEngine tokens preserved in DB body content, for example:

- `vs-mcp-bridge-blog-series-part-1` contains `[NamedPipeListener]` and `[Stdio]`.
- `understanding-a-named-pipe-listener` contains `[VS MCP Bridge|VsMcpBridge]`.
- `understanding-ai-chat-sessions-models-and-agents` contains `[Display:inference-driven|InferenceDriven]`.
- `inference-driven-software-design-with-copilot-pros-and-cons` contains `[Display:ChatSessionsModelsAndAgents]`.

The public renderer expands those tokens into links such as:

- `http://adventuresontheedge.net/post.aspx?id=6484fa94-5d8b-429a-99c6-779b300bc336`
- `http://adventuresontheedge.net/post.aspx?id=d0541943-0de1-4c25-a7af-9950c55f1591`

The source of the old domain therefore appears to be BlogEngine token expansion or site configuration outside the canonical article HTML.
This slice intentionally does not change those tokens or public rendering behavior.

## Remaining Non-Canonical DB Rows

The following active DB rows remain export-only because they were classified as support, legacy, or out of current BlogAI scope in the inventory:

- `Welcome-to-Developments-blog`
- `welcome-to-our-site`
- `this-one-will-only-be-seen-by-subscribers`
- `blog-engine-billkrat-fork-documentation`
- `how-to-publish-your-own-blog-smarterasp`
- `understanding-dependency-injection`

The deleted rows remain export-only:

- `creating-a-post`
- `difference-between-post-and-page`

## Recommended Next Slice

Start small per-post canonical cleanup without touching the database:

1. Normalize bracket-style BlogEngine tokens in canonical source for one post, starting with `vs-mcp-bridge-blog-series-part-1`.
2. Replace old feature-branch GitHub links in canonical content with `main` or repo-relative documentation links.
3. Keep the DB export unchanged as the preserved baseline.
4. After canonical content is reviewed, publish only as draft through the existing draft-only publishing script.
