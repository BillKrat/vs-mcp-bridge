# Request-First UI Beta Checkpoint

## Current State
- MCP Bridge engine is functionally strong
- request-first UI is now the primary interaction model
- flow is: type request -> see progress -> review -> Keep/Reject
- advanced/developer surfaces are secondary and should stay out of the default user path

## What Was Achieved In This Slice
- request-centric UI direction established
- user request is echoed in activity stream as:
  > request text
- in-progress/working state is visible
- approval/review flow remains intact
- user-mode UX was prioritized over bridge-development UX

## Important UX Decisions
- the UI should be thought about from the developer/user perspective, not from the bridge-internals perspective
- validation-harness labels and redundant context were intentionally reduced
- detailed/developer tooling should remain secondary and hidden/collapsed unless intentionally needed
- send/submit behavior should feel like a normal AI chat/tool request flow

## Key Behavior Expectations Going Forward
- Send should enable only from meaningful request text
- Reset and New Chat are separate user intents:
  - Reset = clear current request/review/activity state but keep selected file context
  - New Chat = return to fully blank session, including clearing selected files
- request textbox should be height-constrained with scrollbars
- activity/review area should take the available middle space naturally
- Open Git Changes remains a strong future handoff point, but commit integration is still deferred

## What Is Already Proven Elsewhere
- engine/proposal/apply loop is proven
- repo-driven BlogEngine publishing pipeline is proven
- existing update path is proven
- first-time create path is proven
- rerun/idempotency is proven
- batch wrapper exists
- publishing/blog work should remain secondary to MCP Bridge progress

## Strategic Direction
- primary goal remains: get MCP Bridge good enough to use for Blog AI development
- maintain 80/20 balance:
  - 80 percent MCP Bridge progress
  - 20 percent lightweight documentation only when a slice closes cleanly
- blogs should follow completed slices, never lead them

## Recommended Next Step
- continue refining the request-first MCP Bridge UX for real developer usage
- preserve the proven engine underneath
- avoid new infrastructure unless it directly improves usability of the bridge

## Notes
- local convenience wrappers remain intentionally untracked where applicable
- do not let documentation/blog work derail MCP Bridge progress
- this checkpoint exists specifically to recover quickly if local chat context is lost
