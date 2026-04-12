# Range Edit Manual Fixtures

These files exist only for live manual validation of single-file, single-range edit behavior.

They are not wired into builds, test projects, or product code.

Fixture intent:

- `RepeatedSegmentAmbiguity.cs`: validates that repeated content with weak context fails safely as ambiguous.
- `RepeatedSegmentDisambiguated.cs`: validates that repeated content can still be targeted when nearby context makes the intended range unique.
- `InsertionOnlyRange.cs`: validates insertion-only range application.
- `DeletionOnlyRange.cs`: validates deletion-only range application.
- `StartOfFileReplacement.cs`: validates a replacement range anchored at the start of the file.
- `EndOfFileReplacement.cs`: validates a replacement range anchored at the end of the file.
