# Blocked Row Pre-Publish Diff - 2026-05-16

## Scope

This report inspects the six rows blocked by the ready-post batch compare because the current live DB row no longer matched the preserved `db-export-20260516` baseline.
It compares preserved DB export metadata/content, current live DB metadata/content, and canonical repo metadata/content.
No database writes, reload calls, public site changes, or canonical post rewrites were performed.

## Summary

| Metric | Count |
| --- | ---: |
| Blocked rows inspected | 6 |
| Rows with live body content changes | 0 |
| Rows with title/slug/status changes | 0 |
| Rows with category/tag changes | 6 |
| Rows with dateModified changes | 0 |
| Rows safe after inspection | 6 |
| Rows needing manual review | 0 |

## Per-Row Diff Classification

| Slug | DB PostID | Body changed | Title/slug/status changed | Categories/tags changed | DateModified changed | Canonical differs from current DB | Likely cause | Recommended action |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| vs-mcp-bridge-blog-series-part-3 | 78dbd347-397e-4185-b6d5-d67558cc06be | False | False | True | False | True | mechanical-taxonomy-drift | Likely safe after taxonomy metadata review; body content is unchanged, but publishing canonical content may intentionally replace live taxonomy metadata. |
| vs-mcp-bridge-blog-series-part-4 | f62f7756-269a-4d49-a87d-c0394c7627d9 | False | False | True | False | True | mechanical-taxonomy-drift | Likely safe after taxonomy metadata review; body content is unchanged, but publishing canonical content may intentionally replace live taxonomy metadata. |
| vs-mcp-bridge-blog-series-part-5 | bd97e5de-4b4e-4660-98f1-465bd53eddec | False | False | True | False | True | mechanical-taxonomy-drift | Likely safe after taxonomy metadata review; body content is unchanged, but publishing canonical content may intentionally replace live taxonomy metadata. |
| vs-mcp-bridge-blog-series-part-6 | 12db1be9-4143-476d-a12a-04c7ca045a71 | False | False | True | False | True | mechanical-taxonomy-drift | Likely safe after taxonomy metadata review; body content is unchanged, but publishing canonical content may intentionally replace live taxonomy metadata. |
| how-stdio-works-in-vs-mcp-bridge | d0541943-0de1-4c25-a7af-9950c55f1591 | False | False | True | False | True | mechanical-taxonomy-drift | Likely safe after taxonomy metadata review; body content is unchanged, but publishing canonical content may intentionally replace live taxonomy metadata. |
| understanding-ai-chat-sessions-models-and-agents | 5465cc54-65ab-4c4f-b6ac-4539de01c365 | False | False | True | False | True | mechanical-taxonomy-drift | Likely safe after taxonomy metadata review; body content is unchanged, but publishing canonical content may intentionally replace live taxonomy metadata. |

## Detailed Findings

### vs-mcp-bridge-blog-series-part-3

| Field | Export baseline | Current live DB | Canonical repo |
| --- | --- | --- | --- |
| DateModified | 2026-05-12T18:00:24.280 | 2026-05-12T18:00:24.280 | N/A |
| Content SHA-256 | 821ca8dbd244f37df6485abde8de6347a34bd6a08ea460c26e67a36952d7ee06 | 821ca8dbd244f37df6485abde8de6347a34bd6a08ea460c26e67a36952d7ee06 | 6ee4125b9c1ed9630a910786442c3b2fe2092e05028b129425d0a9bde492458e |
| Categories | AI Systems Author, MCP Bridge | AI Systems Author MCP Bridge | AI Systems Author, MCP Bridge |
| Tags | None | None | VS MCP Bridge, Visual Studio, VSIX, MCP, Approval, Proposal Lifecycle, UI Threading, Diagnostics, Architecture |
| Description | Async Work, Approval Flow, and UI Thread Safety. This post continues the developer ramp-up series for VS MCP Bridge and explains where async behavior matters in the current implementation, what must stay on the UI thread, and how approval flow stays predictable.<br><br>One small correction: the current slug field you showed, vs-mcp-bridge-blog-series-part, is too generic. It will make all posts look the same and will not identify Part 3 cleanly. I recommend using the numbered slug above. | Async Work, Approval Flow, and UI Thread Safety. This post continues the developer ramp-up series for VS MCP Bridge and explains where async behavior matters in the current implementation, what must stay on the UI thread, and how approval flow stays predictable.<br><br>One small correction: the current slug field you showed, vs-mcp-bridge-blog-series-part, is too generic. It will make all posts look the same and will not identify Part 3 cleanly. I recommend using the numbered slug above. | How VS MCP Bridge keeps AI-assisted workflows host-correct through Visual Studio threading discipline, UI state ownership, IProposalManager, approval callbacks, completed previews, and reset/new-chat cleanup. |

Changed fields: categories
Classification: metadata-taxonomy-or-description-change

### vs-mcp-bridge-blog-series-part-4

| Field | Export baseline | Current live DB | Canonical repo |
| --- | --- | --- | --- |
| DateModified | 2026-04-12T10:30:50.303 | 2026-04-12T10:30:50.303 | N/A |
| Content SHA-256 | 7057316908f8d1b111d82eae0e82a2e96616e97113f8527ed1e0764e3eb55762 | 7057316908f8d1b111d82eae0e82a2e96616e97113f8527ed1e0764e3eb55762 | ca34e445f2dbc801f3584549f0c85b04f228b25410d3f9a22af32733db895ba4 |
| Categories | AI Systems Author, MCP Bridge | AI Systems Author MCP Bridge | AI Systems Author, MCP Bridge |
| Tags | AI Tooling, Failure Recovery, MCP, Named Pipes, Runtime validation, Visual Studio, VS MCP Bridge, VSIX | AI Tooling Failure Recovery MCP Named Pipes Runtime validation Visual Studio VS MCP Bridge VSIX | VS MCP Bridge, MCP, Bridge Tools, Compiled Tools, Approval, Audit, Diagnostics, Regex Search, Architecture |
| Description | Runtime Validation, Failure Loops, and Clean Recovery. This post explains how to validate the current VS MCP Bridge end to end, what evidence to look for during testing, and why clean failure and recovery behavior matter as much as success.<br><br>The next likely topic is the concrete validation playbook: exact order of checks, what to open, what to run, and what signals confirm each step. | Runtime Validation, Failure Loops, and Clean Recovery. This post explains how to validate the current VS MCP Bridge end to end, what evidence to look for during testing, and why clean failure and recovery behavior matter as much as success.<br><br>The next likely topic is the concrete validation playbook: exact order of checks, what to open, what to run, and what signals confirm each step. | How VS MCP Bridge turns compiled bridge tools into observable contracts through IBridgeTool, descriptors, requests, results, catalog discovery, BridgeToolExecutor, approval-aware execution, audit metadata, and correlation-preserving tests. |

Changed fields: categories, tags
Classification: metadata-taxonomy-or-description-change

### vs-mcp-bridge-blog-series-part-5

| Field | Export baseline | Current live DB | Canonical repo |
| --- | --- | --- | --- |
| DateModified | 2026-04-23T06:18:01.040 | 2026-04-23T06:18:01.040 | N/A |
| Content SHA-256 | 7a7b85913f888b3f21ee2a641ef1b836fcff8da3fb3137fd1ad640cd43bc1007 | 7a7b85913f888b3f21ee2a641ef1b836fcff8da3fb3137fd1ad640cd43bc1007 | d2f944be69eb510f626b02eba7f850b2abf8f22ac1bdbd727eb76a54ed283811 |
| Categories | AI Systems Author, MCP Bridge | AI Systems Author MCP Bridge | AI Systems Author, MCP Bridge |
| Tags | AI Tooling, Failure Recovery, MCP, Runtime validation, testing, Visual Studio, VS MCP Bridge, VSIX | AI Tooling Failure Recovery MCP Runtime validation testing Visual Studio VS MCP Bridge VSIX | VS MCP Bridge, MCP, Bridge Tools, MEF, Discovery, Extensibility, Diagnostics, Mermaid, Architecture |
| Description | Validation Playbook for the Current Bridge Slice. This post turns runtime validation into a practical sequence of checks for the current VS MCP Bridge, showing what to test first, what signals to watch, and how the playbook also shapes implementation quality. | Validation Playbook for the Current Bridge Slice. This post turns runtime validation into a practical sequence of checks for the current VS MCP Bridge, showing what to test first, what signals to watch, and how the playbook also shapes implementation quality. | How VS MCP Bridge keeps tool discovery and future extensibility observable through compiled discovery, opt-in MEF discovery, catalog metadata, executor-owned policy/audit boundaries, and durable Mermaid traces. |

Changed fields: categories, tags
Classification: metadata-taxonomy-or-description-change

### vs-mcp-bridge-blog-series-part-6

| Field | Export baseline | Current live DB | Canonical repo |
| --- | --- | --- | --- |
| DateModified | 2026-04-23T06:18:44.137 | 2026-04-23T06:18:44.137 | N/A |
| Content SHA-256 | 1522c4fbd83598fb6639232b226ccdfffc1618d8e5d9512512cb649690c98c86 | 1522c4fbd83598fb6639232b226ccdfffc1618d8e5d9512512cb649690c98c86 | 820289bbea45c2e9508404259b172b5a8d5b07ab57ad15a707278cecfa79a2c3 |
| Categories | AI Systems Author, MCP Bridge | AI Systems Author MCP Bridge | AI Systems Author, MCP Bridge |
| Tags | AI Tooling, blog/vs-mcp-bridge-part-6, Failure Recovery, MCP, Observability, Runtime validation, Visual Studio, VS MCP Bridge, VSIX | AI Tooling blog/vs-mcp-bridge-part-6 Failure Recovery MCP Observability Runtime validation Visual Studio VS MCP Bridge VSIX | VS MCP Bridge, MCP, Security, Policy, Approval, Audit, Redaction, Secrets, Architecture |
| Description | Evidence Model for the Current Bridge. This post explains what signals the current VS MCP Bridge needs so validation is practical, failures are triageable, and the runtime does not behave like a black box. | Evidence Model for the Current Bridge. This post explains what signals the current VS MCP Bridge needs so validation is practical, failures are triageable, and the runtime does not behave like a black box. | How VS MCP Bridge keeps tool execution security explicit through policy checks, approval-aware execution, capability metadata, secret-reference seams, redaction, audit envelopes, and correlation without claiming production authentication or sandboxing. |

Changed fields: categories, tags
Classification: metadata-taxonomy-or-description-change

### how-stdio-works-in-vs-mcp-bridge

| Field | Export baseline | Current live DB | Canonical repo |
| --- | --- | --- | --- |
| DateModified | 2026-04-27T10:21:07.833 | 2026-04-27T10:21:07.833 | N/A |
| Content SHA-256 | 27874b495d02eb1a60865210b54837ac0f6359c33fe15430f4f99a130296ae81 | 27874b495d02eb1a60865210b54837ac0f6359c33fe15430f4f99a130296ae81 | 29d6e0fb2554f601703e83becda8d577be5546dd1f97b142951853b696f84ea7 |
| Categories | AI Systems Author, MCP Bridge | AI Systems Author MCP Bridge | AI Systems Author, MCP Bridge |
| Tags | AI Tooling, MCP, Named Pipes, stdio, Visual Studio, VS MCP Bridge, VSIX | AI Tooling MCP Named Pipes stdio Visual Studio VS MCP Bridge VSIX | VS MCP Bridge, MCP, stdio, Named Pipes, Visual Studio, VSIX, AI Tooling, Diagnostics |
| Description | A practical walkthrough of how the VS MCP Bridge MCP server uses stdio, where that transport begins and ends, and how work then crosses into Visual Studio through the named-pipe boundary. | A practical walkthrough of how the VS MCP Bridge MCP server uses stdio, where that transport begins and ends, and how work then crosses into Visual Studio through the named-pipe boundary. | A practical walkthrough of how VS MCP Bridge uses stdio as the MCP transport boundary, keeps stdout clean, and routes VS-backed work through the named-pipe activation boundary. |

Changed fields: categories, tags
Classification: metadata-taxonomy-or-description-change

### understanding-ai-chat-sessions-models-and-agents

| Field | Export baseline | Current live DB | Canonical repo |
| --- | --- | --- | --- |
| DateModified | 2026-04-23T05:17:24.893 | 2026-04-23T05:17:24.893 | N/A |
| Content SHA-256 | 01ac45e9ad9ade1ec1c395888f85153b6c60ff4eaa7e3fba7b7f760db89c70d8 | 01ac45e9ad9ade1ec1c395888f85153b6c60ff4eaa7e3fba7b7f760db89c70d8 | 8bbfed2cadbb457da4be57f52fc1b01f473499611995bb3e0949fdfd7f9da8d2 |
| Categories | AI Systems Author, MCP Bridge | AI Systems Author MCP Bridge | AI Systems Author, MCP Bridge |
| Tags | None | None | AI Assisted Development, Agents, Chat Sessions, Models, MCP, VS MCP Bridge, BlogAI, Observability, Architecture |
| Description | For readers who want a practical understanding of AI session behavior without marketing terminology. | For readers who want a practical understanding of AI session behavior without marketing terminology. | A practical explanation of AI chat sessions, models, agents, tools, orchestration, context loss, and why VS MCP Bridge and BlogAI use source-of-truth docs, durable traces, handoffs, and approval-aware boundaries. |

Changed fields: categories
Classification: metadata-taxonomy-or-description-change

## Rows Safe After Inspection

- vs-mcp-bridge-blog-series-part-3
- vs-mcp-bridge-blog-series-part-4
- vs-mcp-bridge-blog-series-part-5
- vs-mcp-bridge-blog-series-part-6
- how-stdio-works-in-vs-mcp-bridge
- understanding-ai-chat-sessions-models-and-agents

## Rows Needing Manual Review

None.

## Recommended Next Slice

Proceed with draft publishing one safe post and verify BlogAI/global-webnet rendering before continuing.