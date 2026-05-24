# Range Edit Manual Fixtures

These files exist only for live manual validation of single-file, single-range edit behavior.

They are not wired into builds, test projects, or product code.

Manual validation scope for the current UI:

- focus live validation on uniquely targeted success cases
- focus live validation on drift failure after submit and before approve
- treat ambiguity failure as an automated safety proof, not a required live runtime scenario for the current tool-window flow

Fixture intent:

- `RepeatedSegmentAmbiguity.cs`: reference fixture for the automated ambiguity-safety proof; not a required live manual scenario under the current UI-generated metadata.
- `RepeatedSegmentDisambiguated.cs`: validates a repeated-content case that remains uniquely targetable with stronger surrounding context.
- `InsertionOnlyRange.cs`: validates insertion-only range application.
- `DeletionOnlyRange.cs`: validates deletion-only range application.
- `StartOfFileReplacement.cs`: validates a replacement range anchored at the start of the file.
- `EndOfFileReplacement.cs`: validates a replacement range anchored at the end of the file.
