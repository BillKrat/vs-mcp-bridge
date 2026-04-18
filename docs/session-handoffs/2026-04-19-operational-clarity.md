# Operational Clarity

## Summary

Proposal outcomes now use standardized messaging across hosts. The VSIX host and the standalone host both use the same outcome categories and the same terminal message wording, so operators receive consistent status text regardless of where the proposal was created or applied.

The standardized categories are:

- success
- skip
- drift failure
- ambiguity failure
- generic failure
- rejection

Messages now include file count for scope clarity, and proposal-wide failures explicitly state when no changes were applied.

## Validation

Manual validation completed successfully for this slice. Terminal outcomes remained visible after returning to proposal-entry state, and the standardized wording made proposal-wide success and failure states clearer during review.

## Current UX Note

The messages are now clear and consistent, but they are still presented within the existing compact status surface. They are not yet visually emphasized beyond the current review/status model.

## Backlog

A future enhancement could surface terminal outcomes more prominently, for example through a toast or banner treatment, without changing the underlying workflow semantics.
