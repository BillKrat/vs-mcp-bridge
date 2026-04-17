# Multi-Range Preview Clarity Handoff

## Summary

This slice improves operator clarity for single-file multi-range proposals without changing proposal or apply semantics.

What was added:

- a simple reviewed change list for multi-range proposals
- one item per reviewed range
- each item shows sequence number, original segment, and updated segment
- the same reviewed change list appears in both pending review and last completed proposal review

The unified diff remains the primary preview surface.

## Review-Surface Usability Fix

The tool window review surface was adjusted so proposal review remains practically usable after submit.

What changed:

- the bottom review workspace now lives inside the resizable area below the main row splitter
- the bottom review content scrolls instead of expanding indefinitely
- the splitter now meaningfully resizes the top comparison area against the bottom review workspace
- the top comparison panes keep practical minimum height during review

## Validation

Automated:

- focused MVP/VM tests cover multi-range reviewed change population for pending review
- focused MVP/VM tests cover single-range coherence with no reviewed change list
- focused MVP/VM tests cover preservation of the same reviewed change list in last completed proposal review
- shared tests passed for this slice

Manual:

- reviewed change list is visible for multi-range proposals
- pending and completed proposal review remain inspectable
- bottom review content is scrollable
- the main splitter now provides meaningful resizing range for practical review

## Current Limitation

Preview is still diff-first.

- the reviewed change list improves clarity but is not a dedicated multi-range review mode
- the tool window still uses the compact review model rather than a purpose-built review layout
