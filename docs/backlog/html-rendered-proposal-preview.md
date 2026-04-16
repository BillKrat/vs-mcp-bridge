# Backlog: HTML-Rendered Proposal Preview

Status: BACKLOG

Date: 2026-04-14

## Summary

Consider replacing the plain text proposal preview panes with an HTML-rendered comparison surface hosted in a browser control.

The goal is to present proposal changes in a more familiar review style for operators, especially for approval-gated text edits.

## Motivation

The current text-pane preview can show full-document context, but it does not naturally communicate additions and removals with strong visual emphasis.

An HTML renderer could support:

- red/green before-and-after styling similar to modern editor diff experiences
- stronger visual isolation of the affected range
- clearer read-only review once a proposal is submitted

## Proposed Direction

Introduce a preview-rendering seam so presenter and workflow code do not depend directly on one visual format.

Initial target shape:

- keep proposal and apply semantics unchanged
- keep existing diff generation unchanged
- make preview rendering depend on an interface rather than direct UI-specific formatting logic
- preserve the current text-based implementation as one renderer
- add an HTML/browser-hosted renderer as a second implementation and likely default

## Constraints

- no change to RangeEdit generation
- no change to approval gating
- no multi-range support in this phase
- rendering remains read-only once a proposal is submitted

## Risks

- browser control hosting complexity inside the VSIX/WPF tool window
- theme and focus behavior inside Visual Studio
- HTML escaping and content safety for arbitrary file text
- additional implementation/test surface for a currently low-priority UI

## Current Decision

Do not pursue this yet.

First complete the smaller fix that preserves a trustworthy original-versus-proposed comparison in the existing WPF text-pane UI.
