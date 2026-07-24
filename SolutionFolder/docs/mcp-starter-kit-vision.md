# MCP Starter Kit — Vision & Research (v1)

## Status

First artifact of the "complete architectural design" step in
`current-bridge-capabilities.md`'s mandated process (design → gap analysis →
prioritized backlog → sprints). This is a vision statement plus the research
trail that led to it — not a design, not a commitment to any implementation
detail, and nothing here should be read as validated/working functionality
(see `current-bridge-capabilities.md`).

## Purpose

Replace GitHub Copilot as the model driving Visual Studio, specifically so
Bill can develop **BlogAI** with Claude instead. That is the original,
concrete problem this project exists to solve. Everything else — the VSIX,
the shared core, eventual reuse by other developers — is in service of that,
not a separate goal pursued for its own sake.

## Vision statement — three components

1. **Claude + the official MCP C# SDK** (`ModelContextProtocol`, NuGet
   package, GitHub org `modelcontextprotocol/csharp-sdk`, developed jointly
   by Anthropic and Microsoft). This is a *protocol* implementation — client,
   server, hosting/DI, ASP.NET Core packages — not the Claude Messages API
   SDK (`Anthropic` NuGet package). The protocol SDK is provider-neutral by
   design; the Claude Messages API SDK is Anthropic-specific. Using the
   former, not the latter, as the foundation is what keeps the starter kit
   from being tied to one provider despite Claude being the intended driver.
2. **VSIX host** — the Visual Studio-side integration surface. Exposes VS
   capabilities (file access, build, debug, solution/workspace context —
   whatever BlogAI development actually needs) as MCP tools Claude can call.
3. **Shared core with a decoupled Desktop API app.** The desktop app is not
   itself a target usage surface — it exists so the VSIX and the desktop
   host both consume the same core library, forcing nothing in the
   tool/protocol layer to become accidentally coupled to VS-specific types
   or the VS process. This restates an existing repo convention, not new
   scope: `AGENTS.md`'s Current Working Guidance already says "preserve the
   decoupled host pattern with no App↔VSIX coupling."

## Scope explicitly dropped: org-wide MCP gateway

An earlier draft of this vision included a fourth component: making the
starter kit "usable by organizations as a gateway into an organization's
tools." Bill pulled this after recognizing it as scope creep once the size
difference was named explicitly — multi-tenant governance and enterprise
auth/identity is a different-shaped problem than a single developer's VS
integration, and it's also exactly the territory the incoming MCP spec's
authorization rewrite targets (see Research below). Reeled back to the
original scope: enabling BlogAI development. If organizational reuse becomes
relevant later, it gets its own gap analysis at that time, not baked into v1
architecture.

---

## Research trail (so this doesn't have to be reconstructed)

### 1. MCP protocol spec — breaking revision incoming

**Basis: secondary — search-engine result summaries, not full primary-source
reads of the linked articles themselves (see caveat at the end of this
section).**

- MCP's maintainers are finalizing spec revision **2026-07-28** — described
  as "the largest revision of the protocol since launch." Release candidate
  locked 2026-05-21; final spec publishes 2026-07-28.
- **Explicitly not fully backwards-compatible.** Quoted claim: "Servers
  using the 2026-07-28 revision may not work with older clients, and vice
  versa. Compatibility requires both sides to share a supported protocol
  era, or for one side to implement deliberate fallback or translation."
- Breaking changes reported:
  - **Sessions removed** — `Mcp-Session-Id` header and protocol-level
    sessions dropped entirely (SEP-2567), moving toward a stateless core
    that scales on ordinary HTTP infrastructure.
  - **Initialization handshake removed** (SEP-2575).
  - **Error codes standardized** — missing-resource error moves from MCP's
    custom `-32002` to the JSON-RPC standard `-32602 Invalid Params`
    (SEP-2164).
  - Three core features deprecated (not itemized in what was found — the
    actual list needs a primary-source read of the spec diff before it's
    treated as known).
  - **Authorization rewritten** to align more closely with OAuth/OpenID
    Connect deployments — the security-motivated piece. Per one source
    (WorkOS): moves auth from "technically possible if you wire everything
    up yourself" to "follow these RFCs and it works," particularly for
    servers behind enterprise identity providers (Okta, Azure AD, Google
    Workspace). This is the piece most relevant to the dropped org-gateway
    scope above.
  - New extension framework (server-rendered UIs via "MCP Apps," long-running
    work via a "Tasks" extension) and a formal deprecation policy going
    forward.
- **Timeline:** final spec ships 2026-07-28, with a 12-month deprecation
  window for legacy protocol versions.
- **Caveat:** none of the linked articles below were read in full — this
  section is built from search-result synthesis only. Before any protocol-
  version-specific design decision, read the primary sources directly
  (`blog.modelcontextprotocol.io` release-candidate post, the spec repo's
  SEPs) rather than relying on this summary.

Sources (unread beyond search snippets):
- [The 2026-07-28 MCP Specification Release Candidate — Model Context Protocol Blog](https://blog.modelcontextprotocol.io/posts/2026-07-28-release-candidate/)
- [The biggest MCP spec update ships July 28: What changes for AI agent authentication — WorkOS](https://workos.com/blog/mcp-2026-spec-agent-authentication)
- [Model Context Protocol prepares to break with its stateful past — The Register](https://www.theregister.com/devops/2026/07/23/model-context-protocol-prepares-to-break-with-its-stateful-past/5276722)
- [MCP 2026-07-28 spec: what changed, what breaks — Stacktree](https://stacktr.ee/blog/mcp-2026-spec-changes)
- [Versioning — Model Context Protocol Specification](https://spec.modelcontextprotocol.io/specification/basic/versioning/)
- Also surfaced but not reviewed at all: mcpmigrate.dev migration guide,
  tech-insider.org, tokenmix.ai changelog, mcpplaygroundonline.com,
  developersdigest.tech, aaif.io.

### 2. Official C# MCP SDK exists — confirmed

**Basis: secondary — search-engine result summaries; the SDK repo/NuGet
page itself was not fetched and read directly.**

- Package: `ModelContextProtocol` on NuGet. Repo:
  `github.com/modelcontextprotocol/csharp-sdk`. Developed collaboratively by
  Microsoft and Anthropic. Originated from a community project (`mcpdotnet`,
  Peder Holdgaard Pedersen).
- Package split: `ModelContextProtocol.Core` (client / low-level server
  only), `ModelContextProtocol` (main package, hosting + DI extensions),
  `ModelContextProtocol.AspNetCore` (HTTP-based MCP servers).
- This confirms the protocol-layer plumbing (transport, handshake, session
  handling as of the *current* spec) does not need to be built from scratch.

Sources (search snippets only, not fetched):
- [GitHub — modelcontextprotocol/csharp-sdk](https://github.com/modelcontextprotocol/csharp-sdk)
- [Microsoft partners with Anthropic to create official C# SDK for MCP](https://developer.microsoft.com/blog/microsoft-partners-with-anthropic-to-create-official-c-sdk-for-model-context-protocol/)
- [NuGet Gallery — ModelContextProtocol](https://www.nuget.org/packages/ModelContextProtocol/)
- [MCP C# SDK — Model Context Protocol](https://csharp.sdk.modelcontextprotocol.io/)

### 3. Existing VS↔MCP integrations — precedent, but narrow

**Basis: secondary — search-engine result summaries only.**

- `RoslynMcpExtension` — VSIX exposing semantic C# code analysis via MCP,
  powered by the live Roslyn workspace inside VS. Single-purpose (Roslyn
  analysis), not a general starter kit.
- "VS Debugger MCP" — drives VS 2022 source debugging from an AI agent via
  a Python MCP server + a VSIX communicating over a named pipe. Same
  bridging *pattern* as `vs-mcp-bridge` intends, but scoped to debugging
  only.
- Neither is a reusable, general-purpose MCP starter kit for VS.

Sources (search snippets only, not fetched):
- [GitHub — sailro/RoslynMcpExtension](https://github.com/sailro/roslynmcpextension)
- [VS Debugger MCP — LobeHub](https://lobehub.com/mcp/handudew-vsdebugmcp)

### 4. Visual Studio's native MCP support — read in full, primary source

**Basis: `authoritative` — both pages fetched and read directly, not just
search snippets.**

- **Azure MCP tools ship built into VS 2022's Azure workload** (no
  extension required) — 230+ tools across 45 Azure services, surfaced
  through GitHub Copilot Chat. Requires an active GitHub Copilot
  subscription. This is a specific, bundled MCP server (Azure's), not
  general MCP hosting capability.
  Source: [Azure MCP tools now ship built into Visual Studio 2022 — Visual Studio Blog](https://devblogs.microsoft.com/visualstudio/azure-mcp-tools-now-ship-built-into-visual-studio-2022-no-extension-required/)
- **VS itself is a general MCP client** (VS 2022 17.14+, or VS 2026) that
  can connect to *any* MCP-compatible server — `.mcp.json` config (four
  supported file locations for scoping: global/user, solution-local-VS-only,
  solution-tracked, `.vscode`/`.cursor` compatibility paths), tool
  discovery, per-tool permission gating, OAuth for remote servers, an
  org-level allow-list policy, a "rug-pull" protection mechanism (tool
  permissions reset when a server's tool list changes), and a trust dialog
  for changed servers (VS 2026 18.7+).
  Source: [Use MCP Servers to Extend GitHub Copilot — Microsoft Learn](https://learn.microsoft.com/en-us/visualstudio/ide/mcp-servers?view=visualstudio)
- **Critical finding, verbatim from the primary source:** "MCP servers use
  the open Model Context Protocol (MCP) to let **GitHub Copilot** use tools
  and services outside the IDE." Every part of the flow — agent mode, tool
  approval UI, prompts, resources — is a GitHub Copilot feature. There is no
  documented path where VS's built-in MCP client support lets a different
  model (Claude or otherwise) drive VS instead of Copilot.
- **This is the load-bearing conclusion for the whole vision:** Microsoft's
  native MCP investment in VS deepens Copilot; it does not compete with or
  substitute for "get Claude driving VS instead of Copilot." That gap is
  still open as far as this research went.

---

## Open risks / carry-forward items

- **Timing:** the MCP spec's breaking 2026-07-28 revision ships within days
  of this doc being written. Expect a migration pass on the C# SDK shortly
  after starting, and check the SDK repo's own adoption timeline before
  locking any protocol-version-specific design decisions.
- **Research depth:** most of the above is search-snippet-level, not full
  primary-source reads (see basis labels per section). Solid enough to
  validate the current scope; not solid enough to be cited as settled fact
  in, e.g., a stakeholder-facing document without re-verification. Section
  4 (VS's native MCP behavior) is the one exception — read in full.
- **Not yet done:** an actual architectural design (component boundaries,
  data flow, security seams for tool execution) or gap analysis. This
  document is vision + research only, per `current-bridge-capabilities.md`'s
  mandated process — architectural design is the next step, not this one.

## Next step

Per `current-bridge-capabilities.md`: architectural design, then gap
analysis against it, then a prioritized backlog, then sprints. This
document closes out "describe the vision" and opens the door to step 1.
