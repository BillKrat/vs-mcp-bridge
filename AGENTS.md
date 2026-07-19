# Workspace Instructions

## Skill Routing

- When the user message matches an installed skill name or a known
  skill trigger phrase, follow that skill's workflow before improvising
  an ad hoc one.
- For session-start requests such as "let's code," "let's get
  started," "pick up where we left off," or "where were we," route to
  `lets-code`.
- For skill-discovery requests such as "help," "what skills are
  available," "list skills," or "which skill should I use," route to
  `help-skills`.
- For new context-bootstrap requests such as "initialize context," "set
  up project context," "bootstrap context," or "prepare this repo for
  lets-code/update-context," route to `initialize-context`.
- For session-end requests such as "update context," "save the
  session," "checkpoint this," or "wrap up," route to
  `update-context`.
- For skill-backup requests such as "sync skills," "backup skills," or
  "restore skills," route to `sync-skills`.

## Shared Skill Behavior

- Follow the standardized repo-context workflow used by the skills:
  prefer `.project-context/`, fall back to legacy root-level
  `INDEX.md` + `Sessions/` + `Topics/`, and only use lightweight git
  orientation when no context structure exists.
- Keep skills repo-agnostic. Do not hardcode machine-specific paths,
  repo names, GitHub owners, or one project's folder layout unless the
  task explicitly requires it.
- For skill backup and restore, derive the namespace from the directory
  that owns `.claude/skills/` rather than from a fixed path.
- When instructions and a skill cover the same behavior, let the skill
  define the detailed procedure and keep the instruction file limited to
  routing and consistency.

## Reuse Standard

- Changes in this repo should stay reusable across `ai-skills`,
  `architecture-rosetta-stone`, `BlogAI`, and `vs-mcp-bridge`.
- Prefer updating the shared convention over adding per-repo special
  cases.

## vs-mcp-bridge-Specific Note (added 2026-07-19)

This repo does **not** use the `.project-context/` or legacy
`INDEX.md` + `Sessions/` + `Topics/` layout the shared skills above
expect. It has its own, more mature, already-working convention:
`AI_START.md` for session-start orientation, and
`SolutionFolder/docs/session-handoffs/` for session-end persistence.

Do **not** run `initialize-context` in this repo — it would create a
`.project-context/` structure that duplicates and conflicts with the
existing one. If `lets-code`/`update-context` are invoked here anyway,
they'll correctly report "context not initialized" per their own
documented fallback (no shared context structure found); when that
happens, read `AI_START.md` directly instead of initializing a new one.

This is a deliberate exception to the Reuse Standard above, not an
oversight — `AI_START.md`/`session-handoffs` predates the shared
`.project-context/` convention and already works, so it isn't being
replaced just for consistency's sake.