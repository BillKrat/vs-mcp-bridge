# Single-Post Draft Publish Validated

`Publish-BlogPostDraft.ps1` worked end to end in local validation.

Observed behavior:

- an existing BlogEngine post was resolved by `BlogID + Slug`
- rerunning the script overwrote a manual UI edit with repo content, as intended
- the script forced the post back to draft state
- the reload endpoint made the updated post visible immediately

Operational notes:

- local `publish.cmd` is convenience-only and remains untracked
- earlier confusion came from accidentally invoking `docs\scripts\...` instead of `scripts\...`
