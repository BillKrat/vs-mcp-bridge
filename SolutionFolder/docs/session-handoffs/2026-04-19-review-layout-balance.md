# Review Layout Balance Handoff

## Summary

This slice corrected the review-focused layout after the first pass over-collapsed the top comparison/editor area during pending review and last completed review.

The review surface still receives more space than in proposal-entry mode, but the Original Text and Proposed Text panes now remain visible in compact form so operators can inspect several lines without losing the comparison context.

## Initial Issue

The first review-focused layout pass was too aggressive:

- after `Submit Proposal`, the Original Text / Proposed Text area was effectively lost
- the empty log surface still occupied prominent space
- review felt worse because the main comparison context was no longer practically visible

## Corrected Behavior

The corrected layout now behaves this way:

- review-focused mode activates for both pending proposal review and last completed proposal review
- the Original Text / Proposed Text comparison surface remains visible instead of being collapsed
- the comparison region keeps a practical compact height for inspection
- the lower review surface still gets more space than in authoring mode
- the log/status area is deprioritized when empty so it does not dominate the layout
- returning to proposal-entry mode restores the normal authoring emphasis cleanly

This remains a layout-emphasis change only. Preview is still diff-first and uses the same underlying review data.

## Validation

Manual validation succeeded for the corrected review-balance behavior.

Automated/shared verification also remained clean:

- `dotnet test .\VsMcpBridge.Shared.Tests\VsMcpBridge.Shared.Tests.csproj`
- `dotnet build .\VsMcpBridge.Shared.Wpf\VsMcpBridge.Shared.Wpf.csproj`

## Remaining Limitations

- review is still a diff-first model rather than a dedicated review mode
- there is still no per-file review mode for multi-file proposals
- the log area still shares space with review content when it is populated

## Stability Note

At this milestone, the review-focused layout correction is stable and preserves the existing workflow, preview semantics, and proposal/apply behavior.
