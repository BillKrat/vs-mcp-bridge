# VS MCP Bridge

**Status: Early design.** Basic infrastructure exists; no functionality is committed, working, or supported yet. See [SolutionFolder/docs/current-bridge-capabilities.md](SolutionFolder/docs/current-bridge-capabilities.md) for what that means and what happens next (architectural design → gap analysis → prioritized backlog → sprints).

`vs-mcp-bridge` is a local integration that exposes selected IDE and workspace state to AI tooling through the Model Context Protocol (MCP).

AI session entry point:

- for starting or resuming an AI-assisted session, read [AI_START.md](AI_START.md) first
- for repository-level AI operating rules, read [AGENTS.md](AGENTS.md); task-specific workflows use lightweight `SKILL.md` files under [.agents/skills/](.agents/skills/)
- for the project's current early-design stage and what that means, read [SolutionFolder/docs/current-bridge-capabilities.md](SolutionFolder/docs/current-bridge-capabilities.md)
- for MCP search diagnostics, use [.agents/skills/mcp-search-diagnostics/SKILL.md](.agents/skills/mcp-search-diagnostics/SKILL.md)
- for ending or pausing an AI-assisted session cleanly, use [SolutionFolder/docs/AI_STOP.md](SolutionFolder/docs/AI_STOP.md)
- for required local-only files, safe templates, and secret redaction rules, use [SolutionFolder/docs/local-only-files.md](SolutionFolder/docs/local-only-files.md)
- for the trace-capture methodology (still valid for future sprint validation), see the `*-trace-workflow.md` docs under `SolutionFolder/docs/`; any dated log/diagram artifacts they reference predate the early-design reset and are historical evidence only, not current validation

The solution is split into host-specific runtimes plus shared infrastructure:

- `VsMcpBridge.McpServer`: a local MCP server that speaks stdio to an AI client
- `VsMcpBridge.Vsix`: a Visual Studio extension that runs inside the IDE
- `VsMcpBridge.App`: a standalone WPF host that demonstrates non-VSIX reuse of the bridge
- `VsMcpBridge.Shared`: shared contracts, abstractions, diagnostics, pipe dispatch, compiled in-memory bridge tools, and tool-window orchestration
- `VsMcpBridge.Shared.Wpf`: reusable WPF views for the tool window UI
- `VsMcpBridge.Shared.Tests`: unit tests for the shared layer
- `VsMcpBridge.Vsix.Tests`: unit tests for VSIX-specific composition and service logic

## What This Repository Is Today

This repository holds early-stage infrastructure only: host projects, MCP
transport, named-pipe plumbing between the MCP server and the VSIX, and a
tool-window UI shell. None of that adds up to working or supported
functionality yet, and nothing here should be relied on.

Functionality will be designed and delivered deliberately: architectural
design, then a gap analysis, then a prioritized backlog, then sprints.
Capability claims belong in this README only once a sprint has delivered and
validated them. See [SolutionFolder/docs/current-bridge-capabilities.md](SolutionFolder/docs/current-bridge-capabilities.md) for the full explanation.

## Solution Structure

```text
VsMcpBridge.slnx
|- VsMcpBridge.Shared/       shared contracts, services, presenter/viewmodel, diagnostics abstractions
|- VsMcpBridge.Shared.Wpf/   shared WPF tool-window views
|- VsMcpBridge.Shared.Tests/ unit tests for shared logic
|- VsMcpBridge.McpServer/    MCP stdio host and named-pipe client
|- VsMcpBridge.App/          standalone WPF host and non-VSIX service implementations
|- VsMcpBridge.Vsix/         Visual Studio extension and Visual Studio-specific implementations
`- VsMcpBridge.Vsix.Tests/   unit tests for VSIX infrastructure and logic
```

## Architecture

Runtime flow:

```text
AI client
  -> MCP over stdio
VsMcpBridge.McpServer
  -> JSON over named pipe "VsMcpBridge"
host implementation
  -> Visual Studio SDK / DTE APIs    (VSIX host)
  -> workspace / file system / build (App host)
```

For current system behavior and request flow, see [SolutionFolder/docs/ARCHITECTURE.md](SolutionFolder/docs/ARCHITECTURE.md).
For future tool package and namespace boundary planning, see [SolutionFolder/docs/tool-package-boundary-plan.md](SolutionFolder/docs/tool-package-boundary-plan.md).
For the detailed living technical reference and roadmap, see [SolutionFolder/docs/VS_MCP_BRIDGE_TECHNICAL_ANALYSIS.md](SolutionFolder/docs/VS_MCP_BRIDGE_TECHNICAL_ANALYSIS.md).

Blog series:

- [VS MCP Bridge Blog Series: Part 1](http://adventuresontheedge.net/post/2026/04/11/vs-mcp-bridge-blog-series-part-1)
- [VS MCP Bridge Blog Series: Part 2](http://adventuresontheedge.net/post/2026/04/11/vs-mcp-bridge-blog-series-part-2)
- [VS MCP Bridge Blog Series: Part 3](http://adventuresontheedge.net/post/2026/04/11/vs-mcp-bridge-blog-series-part-3)
- [VS MCP Bridge Blog Series: Part 4](http://adventuresontheedge.net/post/2026/04/11/vs-mcp-bridge-blog-series-part-4)
- [VS MCP Bridge Blog Series: Part 5](http://adventuresontheedge.net/post/2026/04/11/vs-mcp-bridge-blog-series-part-5)
- [VS MCP Bridge Blog Series: Part 6](http://adventuresontheedge.net/post/2026/04/11/vs-mcp-bridge-blog-series-part-6)
- [VS MCP Bridge Blog Series: Part 7](http://adventuresontheedge.net/post/2026/04/11/vs-mcp-bridge-blog-series-part-7)

## Build

### MCP Server And Shared

```powershell
dotnet build .\VsMcpBridge.Shared\VsMcpBridge.Shared.csproj
dotnet build .\VsMcpBridge.McpServer\VsMcpBridge.McpServer.csproj
```

### VSIX

The VSIX project targets Visual Studio 2022 on Windows and requires the Visual Studio SDK tooling/workload.
The project also references `CommunityToolkit.Mvvm` for tool window bindings and commands.

From a Developer PowerShell or Visual Studio MSBuild environment:

```powershell
.\scripts\build-vsix.ps1 -Restore
```

You can also open `VsMcpBridge.slnx` in Visual Studio and build `VsMcpBridge.Vsix` there.

### Standalone App

The standalone app can be built with the normal .NET SDK:

```powershell
dotnet build .\VsMcpBridge.App\VsMcpBridge.App.csproj
```

### Logging Configuration (Diagnostic Mode)

The App, VSIX, and MCP host now share the same configuration bootstrap model through `IConfiguration`.

Current shared configuration source order is:

- environment variables
- `VSMCPBRIDGE_`-prefixed environment variables
- `appsettings.json`
- `%LocalAppData%\VsMcpBridge\appsettings.user.json`

Because later configuration sources override earlier ones, the user settings file is currently the highest-precedence shared source.

The App and VSIX hosts support switchable logging output with shared UI log surfacing.

App configuration key (from `VsMcpBridge.App/appsettings.json`):

- `VsMcpBridge:Logging:Provider` (`Debug` or `StdErr`)
- `VsMcpBridge:Logging:MinimumLevel` (`Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical`, `None`)

Environment variable overrides use `VSMCPBRIDGE_` with `__` separators:

- `VSMCPBRIDGE_VsMcpBridge__Logging__Provider`
- `VSMCPBRIDGE_VsMcpBridge__Logging__MinimumLevel`

Notes:

- `Information` is the default user-facing level and should be the primary mechanism used to surface information to the user
- `Trace` is intended for verbose class/process flow so the decoupled host/runtime boundaries do not become a black box during triage
- avoid redundant paired `Trace` and `Information` messages for the same event; log at the level that best matches the purpose
- logs are still surfaced in the shared MVP/VM UI log view through the shared log sink, and Trace-level output should be surfaced there when Trace is enabled so it is visible during AI-assisted triage
- selecting `StdErr` writes transport-safe out-of-band diagnostics to standard error and is the preferred channel for information that must not pollute MCP stdio JSON traffic
- Warning and Error should be used for actionable failures or exceptional conditions
- new request paths should be observable end-to-end; when a workflow matters for development or triage, preserve enough logging and repeatable automation to run it and generate a Mermaid sequence diagram from captured evidence

App chat/OpenAI configuration keys:

- `Adventures:ChatEngine:Provider` (`Fake` or `OpenAI`)
- `Adventures:ChatEngine:OpenAI:UseRealApi` (`true`/`false`)
- `Adventures:ChatEngine:OpenAI:ApiKey`
- `Adventures:ChatEngine:OpenAI:Model` (preferred)
- `Adventures:ChatEngine:Model` (compatibility fallback)

VSIX and MCP host chat/OpenAI configuration keys now participate in the shared `IConfiguration` bootstrap path and may come from any configured source:

- `Adventures__ChatEngine__Provider`
- `Adventures__ChatEngine__OpenAI__UseRealApi`
- `Adventures__ChatEngine__OpenAI__ApiKey`
- `Adventures__ChatEngine__OpenAI__Model` (preferred)
- `Adventures__ChatEngine__Model` (compatibility fallback)

Logging design direction:

- each environment may resolve its own appropriate logger implementation, but configuration should drive provider selection rather than host-specific ad hoc environment reads
- the shared logging pipeline now includes a persistence/forwarding seam, initially backed by a file forwarder, so file-backed or UI-backed behavior can later be extended to SQL-backed sinks without changing core callers

Operational logging note:

- request-path diagnostics now include correlation IDs and elapsed timing across App chat requests, MCP chat tools, pipe transport, shared pipe dispatch, and VSIX read operations (`GetActiveDocument`, `GetSelectedText`, `ListSolutionProjects`, `GetErrorList`)
- every new host/process/tool workflow should either reuse that pattern or document why it is not applicable; the default expectation is that AI can run the workflow, capture logs, produce a Mermaid diagram, and isolate the first failing boundary without reverse-engineering the code path

## MCP Streaming Policy (STRICT)

### Decision

The MCP server in this repository **does NOT support streaming responses over stdio**.

All MCP tool calls must:

- accept a request
- return a **single, final, structured JSON result**

This is a deliberate architectural constraint.

---

### Rationale

The MCP stdio transport is based on:

- JSON-RPC request/response
- newline-delimited messages
- strict stdout protocol boundaries

Partial output, token streaming, or incremental emission over stdout:

- violates protocol expectations
- risks corrupting MCP communication
- introduces non-deterministic behavior

---

### Required Design Rule

**Streaming is NOT allowed at the MCP tool layer.**

Specifically:

- `VsTools` MUST NOT:
  - emit partial responses
  - buffer streaming tokens
  - switch between streaming and non-streaming paths
  - call `StreamAsync` for tool responses

- All tool methods MUST:
  - call non-streaming execution paths (`SendAsync`)
  - return a single final result

---

### Allowed Usage of Streaming

Streaming is allowed ONLY:

- inside `Adventures.ChatEngine`
- for internal execution concerns
- not observable at the MCP boundary

The MCP layer must always return a fully materialized result.

---

### Enforcement

Any PR that:

- introduces streaming into MCP tools
- adds buffering logic for streamed tokens
- emits partial responses

will be rejected.

---

### Future Work (Explicitly Deferred)

If streaming support is ever required, it must be implemented as a **separate, explicit design slice** that includes:

- protocol-level changes
- client expectations
- structured incremental messaging (not stdout token streaming)
- new tests and validation

It MUST NOT be introduced incrementally or implicitly.

---

### Summary

MCP tools in this repository are:

> **Request → Execute → Single Final Response**

No exceptions.

## Safe AI Editing v1

- Core invariant:
  - AI may suggest and propose.
  - Only validated, approved proposals may mutate.
- Required flow:
  - `AI -> Suggest -> Propose -> Approve -> Validate -> Apply`
- AI tools are read-only unless they are explicitly target-based.
- Target-based AI tools create proposals only.
- Approval and apply remain required for any file mutation.
- Apply-time validation prevents stale, missing, ambiguous, or partial mutations.
- Multi-file AI proposals remain deferred until a deterministic output schema exists.

### Tool Surface

Read-only tools:

- `bridge_bm25_text_search`
- `bridge_get_tool_inventory`
- `bridge_regex_text_search`
- `chat_engine_chat`
- `chat_engine_summarize`
- `chat_engine_rewrite`
- `chat_engine_suggest_fixes`
- `chat_engine_explain_code`
- `chat_engine_explain_error`
- `chat_engine_suggest_error_fix`

Mutation-capable tools (proposal-only):

- `chat_engine_rewrite_with_target`
- `chat_engine_suggest_fixes_with_target`
- `chat_engine_suggest_error_fix_with_target`

Rules:

- mutation-capable means proposal creation only
- no direct file writes
- no apply path
- no auto-apply

## ChatEngine-Backed MCP Tool Pattern

ChatEngine-backed MCP tools in this repository must follow a strict, repeatable pattern.

Required rules:

- tools must be non-streaming
- tools must use `SendAsync` only
- tools must validate input before calling ChatEngine
- tools must return serialized `ChatEngineChatResult` JSON
- tools must populate `RequestId`
- tools must populate `ErrorCode` on failure
- tools must use controlled error messages
- tools must use `ErrorCode` values consistently

Testing requirements for any new ChatEngine-backed MCP tool:

- add or update MCP tool allowlist tests
- add a success-path test
- add an invalid-input test
- add a provider-failure test

This pattern is the baseline for safe AI-backed MCP tool additions in this repository.

## ChatEngine Proposal-Ready Boundary

ChatEngine-backed MCP tools return `ChatEngineChatResult` only.

`ProposalReadyChatResult` is an internal preparation structure only. It exists to normalize ChatEngine output for future proposal workflows, but it does not change the MCP contract.

Rules:

- no ChatEngine-backed MCP tool may apply edits directly
- AI-generated changes must flow through proposal, approval, and apply
- the adapter does not change the MCP tool JSON shape
- ChatEngine tools must not create proposals unless the caller supplies an explicit target file path and original text context
- proposal tools require explicit `filePath`
- proposal tools require explicit `originalText`
- error-fix proposal tools also require explicit `errorText`
- invalid inputs must not call ChatEngine or proposal APIs
- no implicit active-document targeting is allowed

The MCP boundary remains read-only for ChatEngine-backed tools unless and until a separate proposal-driven workflow explicitly routes output into the existing approval/apply path.

Multi-file ChatEngine proposal tools are deferred.

They require an explicit deterministic output contract before implementation.

Free-form model output must not be split heuristically into per-file edits.

The following are not allowed:

- delimiter guessing
- section guessing
- file-order guessing

Any future multi-file ChatEngine proposal implementation must define a strict output schema before creating multi-file proposals.

## VSIX Review Experience

Design intent, not a working feature: proposals will need a review UI that
shows source/type, target scope, status, a diff-first preview, and clear
approve/reject affordances, with no durable proposal history in this early
scope. Nothing described here is built, working, or committed until a sprint
delivers it — see [SolutionFolder/docs/current-bridge-capabilities.md](SolutionFolder/docs/current-bridge-capabilities.md).

## Test

Shared layer:

```powershell
dotnet test .\VsMcpBridge.Shared.Tests\VsMcpBridge.Shared.Tests.csproj
```

VSIX-facing tests:

```powershell
.\scripts\build-vsix.ps1 -Restore
& 'C:\Program Files\Microsoft Visual Studio\18\Insiders\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe' .\VsMcpBridge.Vsix.Tests\bin\Debug\net472\VsMcpBridge.Vsix.Tests.dll
```

The build script probes these MSBuild locations in order and uses the first one found:

```text
C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\arm64\MSBuild.exe
C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\MSBuild.exe
C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\amd64\MSBuild.exe
C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\arm64\MSBuild.exe
C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe
C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\amd64\MSBuild.exe
```

To build a different project with the same toolchain:

```powershell
.\scripts\build-vsix.ps1 -Restore -Project 'VsMcpBridge.Vsix.Tests\VsMcpBridge.Vsix.Tests.csproj'
& 'C:\Program Files\Microsoft Visual Studio\18\Insiders\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe' .\VsMcpBridge.Vsix.Tests\bin\Debug\net472\VsMcpBridge.Vsix.Tests.dll
```

Important build note:

- `dotnet test .\VsMcpBridge.slnx` is not the correct top-level runner for the repo because the legacy VSIX project depends on Visual Studio MSBuild/VSSDK tooling rather than the SDK-hosted `dotnet` MSBuild path.

## Current Status

Early design. Nothing in this repository is verified, working, or supported
functionality — only early infrastructure exists. See
[SolutionFolder/docs/current-bridge-capabilities.md](SolutionFolder/docs/current-bridge-capabilities.md)
for what that means.

## Next Steps

1. complete architectural design
2. run a gap analysis against that design
3. prioritize the resulting backlog
4. execute sprints against the prioritized backlog, committing only to what each sprint delivers

## Documentation Guidance

Use these docs together:

- `README.md`: quick orientation and build/test entry point
- `SolutionFolder/docs/ARCHITECTURE.md`: single source of truth for current system behavior
- `SolutionFolder/docs/gated_turn-based_workflow-Codex.txt`: Bill and Codex collaboration workflow for gated execution
- `SolutionFolder/docs/MVPVM_OVERVIEW.md`: developer guide to the repository's MVP/VM split with concrete examples
- `SolutionFolder/docs/VS_MCP_BRIDGE_TECHNICAL_ANALYSIS.md`: living architecture and roadmap document

## AI Workflow

For AI role separation and execution rules, see [SolutionFolder/docs/AI_WORKFLOW.md](SolutionFolder/docs/AI_WORKFLOW.md).

See: `MCP Streaming Policy (STRICT)`

This rule is enforced to preserve protocol integrity and system determinism.
