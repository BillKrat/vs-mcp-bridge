# Range Edit Evidence

## Purpose

This folder stores manual validation evidence (PSR captures) for range-edit behavior.

## File Format

Each test run is stored as a single `.zip` produced by Problem Steps Recorder (`psr.exe`).

## Naming Convention

`yyyy-mm-dd-hhmm-<scenario>-<expected-outcome>.zip`

## Example Filenames

- `2026-04-12-1754-ambiguity-safe-failure.zip`
- `2026-04-12-1822-drift-failure-after-submit.zip`
- `2026-04-12-1840-unique-target-success.zip`
- `2026-04-12-1855-insertion-only-success.zip`
- `2026-04-12-1905-deletion-only-success.zip`
- `2026-04-12-1915-start-of-file-success.zip`
- `2026-04-12-1925-end-of-file-success.zip`

## Scenario Naming Guidelines

Use short, descriptive identifiers such as:

- `ambiguity-safe-failure`
- `drift-failure-after-submit`
- `unique-target-success`
- `insertion-only-success`
- `deletion-only-success`
- `start-of-file-success`
- `end-of-file-success`

## Expected Outcome Suffix

Use one of:

- `success`
- `failure`
- `safe-failure` for intentional protective failures like ambiguity or drift

## Contents Of Each Zip

Each archive should contain the raw PSR output:

- `ProblemStepsRecord.html`
- `Screenshot*.png`

## Scope

These artifacts are for manual validation only.
They are not part of automated tests and are not used by the runtime system.
