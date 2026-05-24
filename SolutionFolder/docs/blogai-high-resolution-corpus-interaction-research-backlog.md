# BlogAI Research Backlog - High-Resolution Corpus Interaction

## Status

Research backlog / future direction.

This document preserves a research thread for BlogAI and engineering knowledge synthesis workflows. It is not an implementation plan, runtime change, deployment task, ingestion rewrite, or commitment to replace current retrieval behavior.

## Research Theme

Investigate high-resolution corpus interaction for BlogAI and engineering knowledge synthesis.

The motivating question:

> Documents are navigable evidence workspaces, not flat retrieval blobs.

This research direction is inspired by:

- Direct Corpus Interaction (DCI)
- retrieval interface resolution
- evidence localization
- runtime context management
- agentic search workflows

Reference:

- `Beyond Semantic Similarity: Rethinking Retrieval for Agentic Search via Direct Corpus Interaction`
- arXiv: `2605.05242`
- URL: `https://arxiv.org/abs/2605.05242`

The paper argues that fixed similarity interfaces can be a bottleneck for agentic search because top-k retrieval can hide exact lexical constraints, sparse clue combinations, local context checks, and iterative hypothesis refinement.

## Problem Statement

Traditional RAG pipelines commonly flatten uploaded documents into embeddings and top-k retrieval flows.

For BlogAI and engineering workflow synthesis, that flattening can discard relationships that matter operationally:

- trace causality
- architecture hierarchy
- execution evidence
- diagnostic context
- local verification spans
- cross-artifact relationships

BlogAI should eventually investigate retrieval interfaces that preserve:

- local evidence inspection
- composable search and refinement
- provenance
- structured artifact relationships
- claim-to-evidence localization

## Proposed Direction

Research a hybrid retrieval architecture that combines:

- semantic retrieval
- lexical retrieval
- metadata filtering
- high-resolution local corpus interaction

The goal is not to replace semantic retrieval. The goal is to augment it with evidence-level interaction so a user or agent can inspect, verify, and refine claims against exact source material.

Potential capabilities:

- regex search
- exact match verification
- span localization
- artifact relationship traversal
- diagram-to-log linking
- trace-to-blog grounding
- architecture validation
- evidence category filtering
- iterative hypothesis refinement

## Potential Future Components

### Evidence Graph

Represent relationships between:

- blogs
- logs
- code
- diagrams
- handoffs
- architecture docs
- validation artifacts
- generated summaries

The graph should help answer questions such as:

- Which validation trace supports this generated blog section?
- Which architecture decision does this code path depend on?
- Which diagram corresponds to this observed log flow?
- Which handoff records the latest known state for this area?

### Localization Layer

Support operations that can:

- isolate exact supporting spans
- validate generated claims
- correlate artifacts
- refine hypotheses iteratively
- distinguish canonical/current evidence from historical or diagnostic evidence

This layer should preserve source identity, line/span metadata where available, artifact category, and currentness assumptions.

### Runtime Context Management

Research how long-horizon workflows should handle:

- truncation
- compaction
- summarization
- selective forgetting
- working-state preservation
- resumption after days or weeks

This matters for:

- large engineering corpora
- multi-session continuity
- long-running BlogAI synthesis
- agentic search workflows that need to keep local evidence available without overloading context

## Suggested Near-Term Prototype

Extend the current BlogAI ingestion and evidence workflow with explicit high-resolution search support.

Candidate prototype capabilities:

- exact lexical search
- regex search
- local snippet extraction
- provenance metadata
- artifact relationship tracking
- evidence category labels
- basic source-to-summary grounding records

Keep semantic retrieval in the system. Use higher-resolution interaction to validate and refine semantic retrieval output rather than replace it.

## Research Questions

- What retrieval interface resolution is required for BlogAI to reliably ground generated narratives in traces, diagrams, handoffs, and code?
- Which artifact relationships are most valuable: trace-to-diagram, trace-to-blog, code-to-architecture, handoff-to-validation, or another path?
- How much structured metadata is useful before it becomes a maintenance burden?
- What should be represented as an evidence graph versus a simple file-level index?
- How should evidence currentness be represented so preserved diagnostics do not masquerade as canonical state?
- What snippets or spans should be exposed to an agent for claim verification?
- How should runtime context be compacted without losing the active evidence chain?

## Non-Goals

Do not use this backlog item to justify:

- replacing semantic retrieval wholesale
- adding a database before the artifact model is understood
- broad repository crawling without an explicit scope
- hidden mutation behavior
- autonomous publishing or deployment
- production auth or tenant/user/role work
- unreviewed generated blog publication
- unbounded agentic loops

## Relationship To Current Repo Direction

This research direction aligns with existing repo themes:

- durable evidence over chat memory
- anti-black-box engineering workflows
- trace artifacts as first-class evidence
- explicit evidence classification
- narrow, approval-gated slices
- search tools that preserve provenance and exact inputs

Current relevant building blocks include:

- compiled regex search
- compiled BM25 search
- explicit document selection
- trace logs and Mermaid diagrams
- session handoffs
- evidence classification guidance
- preview-only mutation boundaries

## Long-Term Vision

Evidence-grounded engineering knowledge synthesis:

- traceable blog generation
- architecture-aware retrieval
- validation-linked narratives
- observability-first AI workflows
- anti-black-box engineering intelligence
- corpus interaction that supports inspection, not just similarity ranking

The long-term target is a BlogAI workflow where generated claims can be traced back to precise local evidence and refined through composable search, metadata, and artifact relationship traversal.
