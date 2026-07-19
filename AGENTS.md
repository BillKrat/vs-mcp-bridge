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
- For root-cause/triage requests such as "let's investigate,"
  "investigate this," "root cause," "why does X happen/fail," or
  "triage this bug," or proactively before writing any document that
  states a cause, a defect classification, or a quality/compliance-
  relevant conclusion, route to `investigate`.

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

## Evidence Discipline (unconditional — added 2026-07-19)

This applies to every claim made about system or code behavior, in
every repo, whether or not `investigate` is explicitly invoked. It does
not require a trigger phrase — that's the point.

- Never state a hypothesis, inference, or pattern-matched guess about
  system/code behavior as settled fact. Attach a basis: `authoritative`
  (real external citation), `primary-source` (the actual code/log/data,
  cited precisely — file:line, timestamp, commit), `claude-reasoning`
  (labeled inference, state what it's inferring from), or `unverified`
  (say so plainly). Vocabulary from
  `architecture-rosetta-stone/Standards/source-of-truth.md` — kept
  consistent across repos on purpose.
- If a claim can't currently be verified, say that directly rather than
  smoothing the gap over with confident-sounding language.
- Before concluding a root cause, verify the full causal chain, not just
  the most plausible-looking link — a root cause is only as strong as
  its weakest unverified link.
- Motivated by a real 2026-07-19 incident: an AI assistant stated an
  unverified hypothesis as fact during multi-subsystem bug triage in a
  regulated-industry workplace; subsequent work built on it as if
  settled; it nearly triggered an incorrect quality/compliance
  escalation before the actual cause was confirmed. See
  `.claude/skills/investigate/SKILL.md` (in this repo) for the full
  structured procedure this rule scales up to for formal triage or
  stakeholder-facing causation summaries.

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

## Regulated-Industry Default (added 2026-07-19)

Treat the least common denominator for this product's users — the VSIX
and the standalone app both — as a regulated-industry client, not a
special case to design for later. That's not hypothetical: it includes
Bill's own employer, expected within a few years, and beta is the right
time to build the habit rather than bolt it on after release.

Practical effect: the Evidence Discipline section above is not optional
rigor reserved for `investigate` — it's the baseline for this repo,
because a wrong causal claim about this product's behavior has the same
shape of consequence as the incident that motivated it (an unverified
hypothesis nearly triggering an incorrect quality/compliance escalation
at Bill's workplace). Treat any output that could be read as a defect
classification, a root-cause conclusion, or a stakeholder-facing status
claim as needing the full `investigate` procedure, not just the
unconditional minimum.