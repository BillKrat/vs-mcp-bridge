# Multi-Range Edit Manual Fixtures

These files exist only for live manual validation of single-file, multi-range edit behavior.

They are not wired into builds, test projects, or product code.

Manual validation scope for the current UI:

- focus live validation on multi-range success
- focus live validation on drift failure after submit and before approve
- focus live validation on adjacent or nearby multi-range behavior
- treat ambiguity failure as an automated safety proof unless the live UI naturally generates weak enough proposal metadata to reproduce it safely

Fixture intent:

- `MultiRangeSuccess.cs`: validates successful application of multiple separated replacements in one file while preserving untouched surrounding content.
- `MultiRangeDriftFailure.cs`: validates failure when the document changes after submit and before approve for one of the intended ranges.
- `MultiRangeAmbiguityReference.cs`: reference fixture for repeated-content ambiguity; primarily useful as an automated safety proof rather than a required live manual scenario.
- `MultiRangeAdjacentChanges.cs`: validates adjacent or nearby replacement ranges in one file.
