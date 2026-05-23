# Session Slice Operating Template

## Purpose

Standardize the operating rhythm for 60-120 minute sessions involving ChatGPT, Codex, `vs-mcp-bridge`, deployment validation, trace artifacts, and approval-gated workflow.

Use this as a practical checklist. Keep each session small enough to finish with validation and a clear checkpoint.

## Session Structure

1. Establish checkpoint.
2. Read minimal grounding docs.
3. Define one narrow slice.
4. Define explicit constraints and non-goals.
5. Execute only that slice.
6. Validate.
7. Produce durable evidence if behavior changed.
8. Summarize result.
9. Wait for human approval before the next slice.

## Checkpoint Block Template

Use this at the start of a session or prompt:

```text
Current checkpoint:
main == origin/main
HEAD: <short-sha> <commit subject>
working tree clean

Next smallest slice:
<one-sentence objective>

Constraints:
- no runtime code unless explicitly approved
- no deployment unless explicitly approved
- no destructive git actions
- no secrets printed
```

If the working tree is not clean, stop and classify the changes before editing.

## Read First Template

Use the smallest grounding set that matches the slice:

```text
Read first:
- AI_START.md
- AGENTS.md
- <most relevant design or handoff doc>
- <most relevant validation evidence, if behavior is being changed>
```

Examples:

- BlogAI deploy retry: read `docs/session-handoffs/2026-05-17-blogai-webdeploy-validation.md` and `docs/session-handoffs/2026-05-17-blogai-deployed-guardrail-validation.md`.
- Preview-only MCP mutation work: read `docs/mcp-controlled-mutation-threshold.md`, `docs/preview-only-document-update-tool-design.md`, and `docs/session-handoffs/2026-05-17-preview-document-update-validation.md`.
- Gated ChatGPT to Codex work: read `docs/chatgpt-codex-gated-handoff-workflow.md` and `docs/session-handoffs/2026-05-17-gated-handoff-tool-readiness-review.md`.

## Slice Template

```text
Next smallest slice:
<exact deliverable>

Create/update:
- <file path>

Do not:
- <non-goal>
- <non-goal>

Validation:
- <command>

Commit and push if clean.
```

Good narrow slices:

- Create a docs-only readiness review for the first gated handoff preview tool.
- Record a successful WebDeploy retry in existing evidence artifacts.
- Add a minimal `AI_START.md` pointer to a new handoff.
- Implement a preview-only result contract with unit tests and no write path.
- Validate deployed guardrail rendering on `/` and `/local-dev` after an explicitly approved deploy.

Slices that are too large:

- Build production auth, admin UI, persistence, and deployment automation together.
- Implement ChatGPT to Codex orchestration, Codex execution, result polling, and auto-continuation in one slice.
- Convert `/local-dev` diagnostics into production auth while also adding OAuth/OpenID/RBAC.
- Add a write-capable MCP mutation tool without a separate preview, approval, and threshold design.
- Refactor BlogAI, AdventuresAuth, and WebDeploy configuration in one pass.

## Validation Template

Pick the smallest validation set that proves the slice.

Docs-only:

```text
git diff --check
```

.NET implementation:

```text
dotnet test ./VsMcpBridge.Shared.Tests/VsMcpBridge.Shared.Tests.csproj
dotnet build ./VsMcpBridge.McpServer/VsMcpBridge.McpServer.csproj
dotnet build ./VsMcpBridge.App/VsMcpBridge.App.csproj
git diff --check
```

BlogAI.Web:

```text
dotnet build ./BlogAI.Web/BlogAI.Web.csproj
git diff --check
```

Always report non-secret warnings and errors. Do not hide validation failures.

## Deployment Checklist

Use only when deployment is explicitly approved.

Pre-check:

- confirm repo path exists
- use `\\Mac\Dev\vs-mcp-bridge` if `Y:\vs-mcp-bridge` is unavailable
- confirm `git status --short --branch`
- confirm required environment variable is present without printing it
- build the deploy target

Deploy:

- one explicit publish attempt unless the user approves otherwise
- no retry loop
- no password printing
- no publish profile or credential file commits

Smoke:

- check expected URLs
- record status codes
- record required rendered markers
- record non-secret warnings/errors

BlogAI/WebDeploy example:

```text
dotnet publish ./BlogAI.Web/BlogAI.Web.csproj -c Release /p:PublishProfile=apiglobalwebnet /p:UserName="billkrat-001" /p:Password="[ENV_VAR_MASKED]"
```

Required smoke URLs:

- `https://api.global-webnet.com/`
- `https://api.global-webnet.com/local-dev`

## Stop-Condition Checklist

Stop and ask or report when:

- repo path is unavailable
- working tree has unexpected changes
- requested scope conflicts with stated constraints
- a secret would need to be printed or committed
- deployment was not explicitly approved
- a command would perform destructive git actions
- validation fails
- the slice requires runtime code after the user asked for docs only
- the task crosses from preview/proposal into mutation
- a tool would continue automatically without user approval
- expected environment variables are missing
- target deployed behavior cannot be verified

## Handoff Expectations

Create or update a handoff when:

- behavior changed
- deployment state changed
- validation evidence changed future expectations
- a slice creates a new operating boundary
- the next session would otherwise need chat history

Handoffs should include:

- checkpoint
- inputs reviewed
- what changed or was learned
- validation performed
- stable behavior
- known gaps
- next smallest slice
- explicit deferred scope

Keep handoffs operational. Avoid turning them into broad roadmaps.

## Commit Message Discipline

Commit messages should describe the durable outcome, not the chat process.

Good examples:

- `Add gated handoff tool readiness review`
- `Record deployed guardrail retry success`
- `Add operational stabilization checkpoint`
- `Implement preview document update tool`

Weak examples:

- `Updates`
- `More docs`
- `Fix stuff`
- `Continue work`

Commit only the files in scope. Check status before and after commit.

## Artifact Expectations

Use durable artifacts when behavior, validation, or operational expectations change.

Common artifact types:

- handoff: `docs/session-handoffs/<date>-<topic>.md`
- trace log: `artifacts/logs/<run>.log`
- metadata: `artifacts/logs/<run>.metadata.json`
- sequence diagram: `docs/diagrams/<run>.mmd`
- design note: `docs/<topic>.md`

If a new artifact is under an ignored folder, add a precise `.gitignore` allow-list entry.

Trace artifacts should preserve enough evidence to reconstruct:

- input summary
- command or workflow
- result
- validation status
- request/operation correlation where available
- non-secret warnings/errors

## Secret And Redaction Rules

- Never print password values.
- Never commit `.env`, `.pubxml`, `.pubxml.user`, publish settings, tokens, cookies, bearer values, or raw credentials.
- Prefer environment variables or structured secret references.
- Record secret source names only when useful, for example `$env:AdventuresOnTheEdgeDP`.
- Use masked command shapes such as `/p:Password="[ENV_VAR_MASKED]"`.
- Redact prompts, logs, audit metadata, and summaries before making them durable.
- Treat terminal output as potentially sensitive until reviewed.

## Approval-Gated Workflow Expectations

- Human remains the approval gate.
- AI proposes, validates, and summarizes.
- Approval for one slice does not imply approval for the next slice.
- Approval is not inferred from clean git state, successful tests, or prior chat context.
- Preview/proposal comes before mutation.
- No destructive git action without explicit approval.
- No deployment without explicit approval.
- No automatic Codex execution or auto-continuation unless a future approved workflow explicitly implements it.

## When NOT To Automate

Do not automate when:

- the workflow is still being understood
- safety boundaries are unclear
- a human decision is the point of the step
- secrets are involved and redaction is not proven
- production auth or deployment behavior is changing
- the action is destructive or hard to roll back
- a manual run would produce better evidence for the next design
- the automation would hide meaningful review or approval

Prefer a preview/proposal artifact before automation.

## Interrupted Sessions

If interrupted mid-slice:

1. Stop running commands cleanly where possible.
2. Capture current `git status --short --branch`.
3. Record what was completed and what remains.
4. Do not guess whether partial changes are safe.
5. Create or update a handoff if the state is not obvious from committed files.

Resume by reading the handoff and checking current git status before continuing.

## Resuming After Days Or Weeks

When resuming after time away:

1. Read `AI_START.md`.
2. Read the newest relevant handoff.
3. Verify `git status --short --branch`.
4. Verify HEAD against the checkpoint in the prompt or handoff.
5. Re-check drift-prone facts such as deployed status, environment variables, and active ports.
6. Reconfirm constraints and non-goals before editing.

If the prompt references deployment, current site behavior, package versions, active credentials, or remote state, verify rather than relying on memory.

## Closeout Template

Use a short closeout:

```text
Changed:
- <files>

Validation:
- <commands and result>

Commit:
- <short-sha> <subject>

Final status:
- main == origin/main
- working tree clean
```

Avoid UI-driven completion markers. Keep the final answer focused on files, validation, commit, and status.
