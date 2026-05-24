# First UI Prompt-Dispatch Slice

Date: 2026-04-20

This session established the first successful UI prompt -> bridge response loop in the VS MCP Bridge tool window.

Confirmed outcomes:

- The request-first UI is now capable of real non-edit prompt dispatch.
- Supported prompts currently are:
  - `what is the active file`
  - `list solution projects`
  - `show error list`
- Unsupported prompts currently return a clean message rather than hanging.
- This prompt-dispatch path is separate from the proposal/apply flow.
- Edit requests still remain a separate path.
- This is the first successful UI prompt -> bridge response slice.

Manual validation in the live Experimental instance confirmed:

- `what is the active file` returned the active file path.
- `list solution projects` returned projects.
- `show error list` returned the current error list.
- `ping` returned a clean unsupported-request message.
- No hang occurred.
- No proposal flow was entered for these non-edit requests.
