# Ready Posts Pre-Publish Compare - 2026-05-16

## Scope

This report batches the read-only pre-publish compare across every post marked ready for publishing review.
It invokes `Compare-BlogPostBeforePublish.ps1` for each slug, reads the live BlogEngine database through parameterized `SELECT` statements, compares against the preserved `db-export-20260516` baseline and canonical repo source, and writes this summary.
No database writes, reload calls, or public site changes were performed.

## Summary

| Metric | Count |
| --- | ---: |
| Total ready posts checked | 14 |
| Safe for human draft-publish review | 8 |
| Blocked or needs review | 6 |
| Current DB no longer matches preserved export | 6 |
| Canonical stale direct-link findings | 0 |
| Unsafe token findings | 0 |

## Per-Post Results

| Slug | DB PostID | Current DB matches preserved export | Canonical differs from current DB | Stale direct links | Intentional BlogEngine tokens | Publish safety recommendation |
| --- | --- | --- | --- | ---: | --- | --- |
| vs-mcp-bridge-blog-series-part-1 | f0c7a958-f41a-4143-b601-82ce84fd4af0 | True | True | 0 | [NamedPipeListener], [Page:VS MCP Bridge\|VsMcpBridge], [Stdio] | Safe for human draft-publish review: current DB still matches preserved export, and canonical content intentionally differs from runtime baseline. |
| vs-mcp-bridge-blog-series-part-2 | cad63f28-5739-40be-b8f6-0288a1e3da20 | True | True | 0 | None | Safe for human draft-publish review: current DB still matches preserved export, and canonical content intentionally differs from runtime baseline. |
| vs-mcp-bridge-blog-series-part-3 | 78dbd347-397e-4185-b6d5-d67558cc06be | False | True | 0 | None | Do not publish yet: current DB no longer matches the preserved export baseline. Re-export or review live edits before overwrite. |
| vs-mcp-bridge-blog-series-part-4 | f62f7756-269a-4d49-a87d-c0394c7627d9 | False | True | 0 | None | Do not publish yet: current DB no longer matches the preserved export baseline. Re-export or review live edits before overwrite. |
| vs-mcp-bridge-blog-series-part-5 | bd97e5de-4b4e-4660-98f1-465bd53eddec | False | True | 0 | [Page:Playbook] | Do not publish yet: current DB no longer matches the preserved export baseline. Re-export or review live edits before overwrite. |
| vs-mcp-bridge-blog-series-part-6 | 12db1be9-4143-476d-a12a-04c7ca045a71 | False | True | 0 | [Page:Evidence] | Do not publish yet: current DB no longer matches the preserved export baseline. Re-export or review live edits before overwrite. |
| vs-mcp-bridge-blog-series-part-7 | 5520e2d5-c597-492d-8b41-e467152364cd | True | True | 0 | None | Safe for human draft-publish review: current DB still matches preserved export, and canonical content intentionally differs from runtime baseline. |
| how-stdio-works-in-vs-mcp-bridge | d0541943-0de1-4c25-a7af-9950c55f1591 | False | True | 0 | [Page:Stdio] | Do not publish yet: current DB no longer matches the preserved export baseline. Re-export or review live edits before overwrite. |
| understanding-a-named-pipe-listener | 6484fa94-5d8b-429a-99c6-779b300bc336 | True | True | 0 | [Page:NamedPipeListener] | Safe for human draft-publish review: current DB still matches preserved export, and canonical content intentionally differs from runtime baseline. |
| understanding-local-mcp-server-over-stdio-and-local-only-communication-over-a-named-pipe | 43bf6aae-15c9-4c90-b3b2-66ac51c4a7c8 | True | True | 0 | None | Safe for human draft-publish review: current DB still matches preserved export, and canonical content intentionally differs from runtime baseline. |
| wpf-vsix-threading-understanding-ui-switching-async-behavior-and-pipe-safety | 46828793-1f3d-4031-906e-87c1c31dce7e | True | True | 0 | None | Safe for human draft-publish review: current DB still matches preserved export, and canonical content intentionally differs from runtime baseline. |
| why-vsix-project-should-target-net-framework-4-7-2 | ae00d3f4-7c9a-4084-b690-d974e945d69e | True | True | 0 | None | Safe for human draft-publish review: current DB still matches preserved export, and canonical content intentionally differs from runtime baseline. |
| inference-driven-software-design-with-copilot-pros-and-cons | b3da6b1c-a955-4ec2-afda-b281bd5d46fd | True | True | 0 | [Display:ChatSessionsModelsAndAgents], [Page:InferenceDriven] | Safe for human draft-publish review: current DB still matches preserved export, and canonical content intentionally differs from runtime baseline. |
| understanding-ai-chat-sessions-models-and-agents | 5465cc54-65ab-4c4f-b6ac-4539de01c365 | False | True | 0 | [Display:inference-driven\|InferenceDriven], [Page:ChatSessionsModelsAndAgents] | Do not publish yet: current DB no longer matches the preserved export baseline. Re-export or review live edits before overwrite. |

## Rows Where Current DB No Longer Matches Export

- vs-mcp-bridge-blog-series-part-3
- vs-mcp-bridge-blog-series-part-4
- vs-mcp-bridge-blog-series-part-5
- vs-mcp-bridge-blog-series-part-6
- how-stdio-works-in-vs-mcp-bridge
- understanding-ai-chat-sessions-models-and-agents

## Stale Links Or Unsafe Tokens

None.

## Publish Safety Recommendation

Do not batch publish. Review blocked rows first, especially any current DB rows that no longer match the preserved export baseline.

## Recommended Next Slice

Run the draft-only publish workflow for one safe post, starting with `vs-mcp-bridge-blog-series-part-1`, then verify BlogAI/global-webnet rendering before publishing the remaining ready set.