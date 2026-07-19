# Current Bridge Capabilities

## Purpose

Set expectations for what `vs-mcp-bridge` currently is, so nothing here is
mistaken for working or supported functionality.

This document describes current repository state. It does not create new
scope, roadmap commitments, runtime behavior, tooling changes, or deployment
behavior.

## Current Stage: Early Design

`vs-mcp-bridge` is in early design. Basic infrastructure exists — host
projects, MCP transport, named-pipe plumbing, a tool-window shell — but there
is no finished, designed, or supported functionality. Nothing in this
repository should be treated as working at any level, and no capability
should be assumed to exist, be stable, or be safe to depend on.

Anything that has previously run during development — a tool call answered,
a build succeeding, a session connecting — was an isolated technical spike
while building infrastructure, not delivered or validated functionality.
Spikes are not commitments and are not covered by this document.

## Process Going Forward

Functionality will be added deliberately, in this order:

1. complete architectural design
2. run a gap analysis against that design
3. prioritize the resulting backlog
4. execute sprints against the prioritized backlog

Capability claims for this repository are made only once a sprint delivers
and validates them — never ahead of that, and never based on a spike or a
one-off manual test. Until a sprint says otherwise, assume nothing works.

## Common Misconceptions

Misconception: Because infrastructure exists (hosts, transport, pipe,
tool-window shell), the bridge does something useful today.

Reality: Infrastructure is not functionality. No feature is considered
working or supported until a sprint delivers it.

Misconception: Something observed running once during development (a tool
call, a build, a connection) means that capability is available now.

Reality: Development-time spikes are not delivered functionality and are not
safe to rely on. See "Current Stage" above.

Misconception: This repository supports autonomous execution, automatic
Codex execution, autonomous mutation, autonomous deployment, or background
agents.

Reality: None of that exists, is designed, or is planned for the near term.
Any future automation would require its own explicit design and approval,
not an incremental addition to existing code.

## Future Direction

Future work is chosen only through the process above — architectural design,
gap analysis, prioritized backlog, sprints — not from interesting ideas,
speculative automation, theoretical architecture improvements, or capability
for capability's sake.
