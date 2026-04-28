# ChatGPT Handoff: UI Prompt Path vs MCP Bridge Reality

Date: 2026-04-19

## Executive Summary

The VS MCP Bridge currently supports two different categories of behavior, and they are not the same:

1. **Codex-originated MCP tool calls into Visual Studio**
   - Proven working end-to-end.
   - Example: `mcp__vs_mcp_bridge__.vs_get_active_document` and `vs_propose_text_edit` succeeded from Codex.
   - A proposal surfaced in the VS tool window.
   - The user clicked `Keep`.
   - The change applied successfully in Visual Studio.

2. **UI-originated prompt text entered into the VS MCP Bridge tool window**
   - Not currently a true chat transport.
   - The visible UX looks chat-like, but the wired backend path is still the manual proposal submission workflow.
   - Entering freeform text such as `ping` does **not** currently dispatch to Codex/ChatGPT and does **not** currently map prompt text to MCP tool execution.

This distinction is the main source of confusion.

## What Is Proven Working

### 1. Codex -> MCP Bridge -> Visual Studio round-trip

The following has been proven in practice:

- The Codex MCP config path is working.
- The Experimental instance can be launched successfully.
- The VS MCP Bridge tool window must be open.
- Active document operations require a real open editor with focus/caret context.
- `mcp__vs_mcp_bridge__.vs_get_active_document` can succeed when the bridge and VS context are valid.
- `mcp__vs_mcp_bridge__.vs_propose_text_edit` can surface a proposal in the VS MCP Bridge UI.
- The user can click `Keep`.
- The edit then applies successfully.

This is the first fully successful Codex-originated bridge round-trip that was confirmed and checkpointed.

### 2. Experimental launcher

The launcher script now works correctly:

- `launch-exp.cmd` was fixed to open `VsMcpBridge.slnx` explicitly.
- The solution file had previously been corrupted on disk and was restored from `HEAD`.
- Running `launch-exp.cmd` was verified by process command line to launch:
  - `devenv.exe`
  - `VsMcpBridge.slnx`
  - `/RootSuffix Exp`
  - `/Log`

So the launcher is now confirmed to start the Experimental hive, not just the normal Insiders process.

### 3. No-op manual submission no longer stays stuck on `Working`

A narrow UI-state fix was implemented and committed:

- The manual UI submission path previously set `IsRequestInProgress = true` and could remain stuck when no reviewable proposal was created.
- This occurred for cases such as:
  - no files selected
  - selected file(s) but proposed text identical to original
- The presenter now:
  - logs narrow entry diagnostics
  - detects no-op return cases
  - clears `IsRequestInProgress`
  - surfaces a concrete status message
  - releases the active request id

Focused tests were added and passed for that behavior.

## What The VS MCP Bridge Tool Window Actually Does Today

The current `Send` button behavior is **not** generic chat dispatch.

The current path is:

- request text is captured by the tool window view model
- the presenter treats the action as a **manual proposal submission**
- the presenter gathers proposal file draft state
- it calls:
  - `IVsService.ProposeTextEditAsync(...)`, or
  - `IVsService.ProposeTextEditsAsync(...)`

That means the UI `Send` path is presently tied to:

- selected file state
- original/proposed text state
- proposal/review/apply workflow

It is **not** currently tied to:

- freeform chat request dispatch
- Codex/ChatGPT message transport
- tool routing from plain prompt text
- a `run <tool>` parser or tool executor

## What Is Not Supported Today

The following should be treated as **not currently supported** by the present VS tool window wiring:

### 1. Freeform chat to Codex/ChatGPT from the tool window

Typing:

- `ping`
- `summarize this file`
- `run get_active_document`
- `list solution projects`

into the prompt box and clicking `Send` does **not** currently behave like Cursor chat.

There is no confirmed prompt-to-agent transport behind that visible input.

### 2. Generic prompt-to-tool execution from the UI input box

There is currently no evidence that the tool window:

- parses tool names out of prompt text
- chooses a tool based on intent
- dispatches MCP commands from plain text
- returns tool output into the transcript area

### 3. Reliable bridge log surfacing in the in-window Log panel

The in-window log pane is currently misleading as a diagnostic source.

What is true:

- production logging is going to `ActivityLog` and debug output
- the tool window `LogText` view is only populated via `AppendLog(...)`
- production flow is not actually driving that `AppendLog(...)` path

So the bridge can be doing real work while the tool window log area stays empty.

## Why `ping` In The UI Did Not Produce A Response

The user expectation was:

- type `ping`
- click `Send`
- receive some kind of agent or bridge response

But the current implementation does not interpret that prompt as chat.

Instead, it treats `Send` as proposal submission.

Given the current state, `ping` with no selected file means:

- no proposal file set
- no proposed change
- no reviewable proposal target
- therefore no real proposal result can be created

After the no-op fix, this should no longer stay stuck on `Working`, but it still does **not** turn into an agent/chat response because that path does not exist yet.

## Important Distinction To Keep In Mind

There are three different things that can be confused with one another:

1. **Codex calling VS MCP tools externally**
   - working

2. **Manual proposal submission from the VS tool window**
   - partially working
   - fixed for no-op UI-state handling

3. **Chat-style prompt entered into the VS tool window expecting agent/tool response**
   - not currently implemented/wired as a real response path

Any future diagnosis should keep those paths separate.

## Current Code-Level Reality

The code path traced so far indicates:

- the visible chat-like shell exists
- but the active backend `Send` path is still proposal-oriented
- the current path dispatches to `ProposeTextEditAsync` / `ProposeTextEditsAsync`
- it does not dispatch to:
  - `GetActiveDocumentAsync`
  - `ListSolutionProjectsAsync`
  - `GetErrorListAsync`
  - external Codex/ChatGPT conversation transport

In short: the UI currently resembles chat more than it actually behaves like chat.

## What Must Be Added For Prompt -> MCP Bridge -> Response To Work

If the intended product behavior is:

- user types a natural-language request in the tool window
- the bridge interprets it
- one or more MCP tools are invoked
- a response appears back in the tool window

then a **new dispatch layer** is needed. At minimum, the system needs:

### 1. A real prompt dispatch path separate from proposal submission

The `Send` button needs a mode that does something other than submit proposal editor state.

That path should:

- accept freeform prompt text as primary input
- not require proposal-file state to exist
- support pure informational/tool requests such as:
  - “what is the active file?”
  - “list projects”
  - “show error list”

### 2. A prompt-to-action resolver

The system needs logic to decide what to do with the entered prompt, for example:

- map specific commands to bridge tools
- or send the prompt to an agent runtime that chooses tools

Without this layer, prompt text is just inert text attached to proposal workflow state.

### 3. A response transport back into the tool window transcript

Even if a tool runs, the user still needs a surfaced response in the UI.

That means the window needs a real transcript/result model, not just:

- request echo
- review state
- proposal outcome state

### 4. Explicit handling for non-edit tool calls

The current design is edit-centric.

For a chat/tool UI, the bridge must support non-edit responses such as:

- active document info
- project lists
- error list output
- status/failure messages

These must render as first-class responses, not as proposal states.

### 5. Clear separation between “proposal mode” and “chat/tool mode”

Right now the UI can imply a capability it does not yet have.

To avoid further confusion, one of the following is needed:

- separate commands/modes in the UI, or
- a single chat path that internally routes edit requests into proposal workflow and non-edit requests into tool execution

But today that separation is not yet implemented.

## Recommended Next Engineering Step

The next most useful engineering step is **not** more work on the no-op proposal fix.

The next step is to explicitly decide and implement whether the VS tool window is intended to be:

1. **Proposal submission UI only**
   - then the UI should stop presenting itself as general chat

or

2. **A true chat/tool front end**
   - then implement prompt dispatch, tool routing, and response rendering

Until that decision is codified in the UI/backend wiring, users will continue to expect Cursor-style behavior that the current implementation does not provide.

## Current Repo State Relevant To This Handoff

Relevant recent checkpoints already exist for:

- first successful Codex-originated round-trip with apply
- no-op UI submission fix
- Experimental launcher support

The repository is now in a good state for continued work, but **UI prompt-to-response is still an open capability gap**, not merely a bug.
