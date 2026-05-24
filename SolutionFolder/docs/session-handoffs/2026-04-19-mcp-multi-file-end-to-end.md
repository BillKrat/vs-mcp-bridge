# MCP Multi-File End-to-End

## Summary

The MCP bridge now supports multi-file proposal creation end to end. One MCP request can produce one approval-gated proposal spanning multiple files, and that proposal flows through the same approval storage, review surface, and apply pipeline already used for single-file proposals.

This is an additive capability. Existing single-file callers remain supported without request-shape changes, while multi-file callers can now author one proposal that carries multiple file edit entries.

## Request Shape and Tool Surface

The bridge now accepts two compatible proposal request shapes:

- legacy single-file: `filePath`, `originalText`, `proposedText`
- multi-file: `fileEdits: [{ filePath, originalText, proposedText }]`

The MCP tool surface now includes:

- `vs_propose_text_edit` for backward-compatible single-file proposal creation
- `vs_propose_text_edits` for additive multi-file proposal creation

Both paths preserve request and proposal correlation and flow into the same approval/apply safety model.

## Validation

Manual validation completed successfully for:

- multi-file success case
- drift-safe multi-file failure case

The success path produced one multi-file proposal and applied it correctly through the existing approval flow. The drift-safe failure path correctly failed the overall proposal without partial apply.

## Observed UX Behavior

After a failed apply, the reload of live file content back into the editor panes can feel ambiguous during review because the panes reflect current file state rather than a dedicated failed-proposal snapshot. This is currently classified as a non-blocking UX clarity item rather than a correctness issue.

## Milestone Status

At this milestone, the system is stable. MCP-originated multi-file proposals are now supported end to end, backward compatibility for existing single-file callers remains intact, and the approval/apply safety model remains all-or-nothing across the full proposal.
