# Multi-File Edit Manual Fixtures

These files exist only for first-pass live manual validation of multi-file proposal and apply behavior.

They are not wired into builds, test projects, or production code.

Current live manual validation focus:

- multi-file success across two files
- multi-file drift failure after submit and before approve with no partial apply
- mixed skip/apply success where one file already matches the approved updated content

Current UI note:

- preview and review still use the existing proposal path and are not yet multi-file-specific

Fixture pair intent:

- `MultiFileSuccess_A.cs` and `MultiFileSuccess_B.cs`: validate successful all-or-nothing apply across two files with clear status-string updates.
- `MultiFileDriftFailure_A.cs` and `MultiFileDriftFailure_B.cs`: validate that a change introduced after submit and before approve causes failure without partial apply across the pair.
- `MultiFileSkipAndApply_A.cs` and `MultiFileSkipAndApply_B.cs`: validate mixed per-file behavior where one file already matches the approved updated text and the other still needs apply.
