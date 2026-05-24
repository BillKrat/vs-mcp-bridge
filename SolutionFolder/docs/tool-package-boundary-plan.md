# Tool Package Boundary Plan

## Purpose

This document records intended future package and namespace boundaries for the bridge tool ecosystem.

It is planning guidance only. The current implementation remains in `VsMcpBridge.Shared/Tools` and `VsMcpBridge.Shared/Security`, and this slice does not move code, create projects, rename namespaces, change runtime behavior, or introduce packaging/publishing infrastructure.

## Current State

Current bridge tool execution is repo-local:

- tool contracts, descriptors, manifests, discovery, catalog, requests, results, and executor live under `VsMcpBridge.Shared/Tools`
- policy, approval, audit, redaction, capability, and secret-reference seams live under `VsMcpBridge.Shared/Security`
- `RegexTextSearchTool` and `Bm25TextSearchTool` remain compiled in-memory tools registered through current shared composition
- MEF remains a discovery-only seam when explicitly configured
- all bridge tool execution still flows through `BridgeToolExecutor`

No extraction is happening in this planning slice.

## Intended Future Boundaries

### Adventures.Mcp

Host-agnostic MCP and tool execution foundation.

Responsibilities:

- host-agnostic MCP/tool execution contracts
- `BridgeToolExecutor`-style execution boundary
- policy, approval, audit, redaction, and security seams
- manifest, capability, request, result, and audit contracts
- transport-independent abstractions

Non-goals:

- Visual Studio SDK dependencies
- app-host workflow dependencies
- BlogAI-specific publishing behavior
- package publishing until external reuse is real

### Adventures.Tools

Reusable host-neutral tools.

Responsibilities:

- regex search
- BM25 or other request-scoped search utilities
- log, document, and text utilities that do not require host APIs
- shared tool tests that prove behavior without Visual Studio, app-host, or BlogAI dependencies

Non-goals:

- Visual Studio APIs
- host-specific proposal/apply behavior
- BlogAI route/cache/database workflows

### Adventures.Auth

Possible future shared auth boundary package or namespace.

Responsibilities, if later approved:

- reusable `AdventuresAuth` contracts
- auth decision contracts and result shapes
- redacted audit event contracts
- correlation field conventions
- secret redaction and secret-reference expectations for auth flows

Non-goals:

- implementation during the local prototype design slice
- production identity provider behavior
- OAuth/OpenID
- RBAC, tenant, or organization model
- BlogAI-specific identity ownership
- BlogEngine.NET auth integration
- MCP tunnel integration before a separate design slice

### McpVsBridge.Tools

Visual Studio-specific tool pack.

Responsibilities:

- active document tools
- selected text tools
- solution project tools
- error list tools
- proposal/apply integration where Visual Studio is the mutation host
- Visual Studio threading, package, DTE, shell, and editor constraints

Boundary rule:

- Visual Studio dependencies stay isolated here or in the VSIX host, not in `Adventures.Mcp` or `Adventures.Tools`.

### McpAppBridge.Tools

Standalone app-host-specific tool pack.

Responsibilities:

- app-host-specific tools and workflows
- local workspace/file orchestration that is not Visual Studio-specific
- reference-host validation surfaces for reusable tool execution and approval behavior

Boundary rule:

- app-host orchestration can depend on host-neutral contracts, but host-neutral contracts must not depend on the app host.

### BlogAIBridge.Tools

BlogAI and publishing workflow tool pack.

Responsibilities:

- BlogAI/blog publishing review workflows
- repository-to-database reconciliation helpers
- route validation workflows
- cache diagnostic workflows
- reusable blog synchronization and publish-readiness checks

Boundary rule:

- BlogAI-specific concepts must not leak into `Adventures.Mcp`, `Adventures.Tools`, or Visual Studio-specific tool packs.

## Boundary Rules

- Tools must execute through `BridgeToolExecutor` or its future extracted equivalent.
- Plugins and tools must not own core policy, approval, redaction, audit, or correlation behavior.
- Host-specific tools must not leak dependencies into host-agnostic packages.
- Security and observability contracts move before host-specific implementations.
- Manifest and capability contracts are shared contracts, not package-manager metadata.
- MEF remains discovery-only unless a future explicit design slice changes it.
- Remote tools, OAuth/RBAC/user identity, signed package provenance, sandboxing, persistent policy, and package publishing remain out of scope until separate design and validation slices justify them. `Adventures.Auth` is a planning label only until a later auth implementation or extraction slice approves code.
- Extraction should happen only after stable seams, tests, and trace artifacts exist.

## Phased Extraction Plan

### Phase 0: Current Repo-Local Seams

Keep contracts and implementations in `VsMcpBridge.Shared`.

Exit criteria:

- descriptor/manifest metadata remains stable
- executor-owned policy, approval, redaction, audit, and correlation behavior is covered by tests
- durable trace artifacts can reconstruct compiled, MEF discovery, approval, and manifest metadata flows
- durable inventory artifacts can reconstruct deterministic read-only catalog snapshots

### Phase 1: Stabilize Manifests, Security, And Tool Execution

Refine the current repo-local contracts before moving them.

Focus:

- manifest defaults
- deterministic read-only catalog inventory snapshots
- capability declarations
- approval requirement semantics
- audit metadata shape
- redaction boundaries
- deterministic duplicate id handling
- discovery/catalog behavior

Exit criteria:

- tests prove defaults and extension seams
- docs identify what is contract versus implementation
- no host-specific dependencies are required by the contract surface

### Phase 2: Extract Host-Neutral Contracts

Move only stable host-neutral contracts into a future `Adventures.Mcp` boundary.

Candidate surface:

- tool request/result contracts
- tool descriptor/manifest contracts
- capability contracts
- policy/approval/audit/redaction abstractions
- executor/catalog/discovery abstractions where host-neutral

Exit criteria:

- existing bridge behavior remains unchanged
- current tests can run against the extracted contracts
- no Visual Studio, app-host, BlogAI, or transport dependency enters the extracted contract package

### Phase 3: Extract Reusable Tools

Move host-neutral tools into a future `Adventures.Tools` boundary.

Candidate tools:

- regex text search
- BM25 text search
- reusable text/log/document utilities that do not depend on host APIs

Exit criteria:

- reusable tools execute through the extracted executor boundary
- tool manifests remain stable
- no host-specific dependency is introduced

### Phase 4: Split Host-Specific Tool Packs

Create host-specific packages only after the host-neutral foundation is stable.

Candidate packs:

- `McpVsBridge.Tools` for Visual Studio-specific tools
- `McpAppBridge.Tools` for app-host-specific workflows
- `BlogAIBridge.Tools` for BlogAI publishing, reconciliation, route validation, and cache diagnostics

Exit criteria:

- host-specific dependencies are isolated to the relevant pack
- all tools still run through the shared execution/security boundary
- trace artifacts prove each pack's discovery and execution path

### Phase 5: Package And Publish Only When External Reuse Is Real

Introduce package publishing only after real consumers outside this repository need it.

Prerequisites:

- stable versioning policy
- compatibility tests
- documented support boundaries
- package provenance and release process
- explicit decision on signed plugin/package behavior if needed

Until then, package names are planning labels, not active infrastructure.

## Near-Term Guidance

Near-term work should continue improving contracts, tests, docs, and trace evidence inside the current repo.

Do not start extraction just because a name exists in this plan. Start extraction only when a concrete slice can move one stable boundary without changing behavior.
