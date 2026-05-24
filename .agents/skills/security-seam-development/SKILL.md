---
name: security-seam-development
description: Work on bridge tool policy, approval, redaction, audit, capability, and secret-reference seams without expanding into deferred security systems.
---

# Security Seam Development

## Use When

- Changing `BridgeToolExecutor` or shared tool execution contracts.
- Adding descriptor capability metadata, approval requirements, redaction, audit, or secret-reference behavior.
- Reviewing MEF/discovered tool execution boundaries.
- Guarding against accidental OAuth, vault, sandbox, RBAC, or compliance scope expansion.

## Workflow

1. Read `SolutionFolder/docs/ARCHITECTURE.md` and `SolutionFolder/docs/session-handoffs/2026-05-16-security-architecture-foundation.md`.
2. Keep `BridgeToolExecutor` as the execution/security/audit/redaction boundary.
3. Ensure discovered tools still execute through the executor.
4. Preserve current defaults unless the slice explicitly changes behavior.
5. Treat capabilities, approval, secret references, and audit classification as contracts or observability metadata, not full security infrastructure.
6. Validate with targeted shared tests and update trace artifacts only when observable behavior changes.

## References

- `SolutionFolder/docs/ARCHITECTURE.md`
- `SolutionFolder/docs/tool-execution-trace-workflow.md`
- `SolutionFolder/docs/session-handoffs/2026-05-16-security-architecture-foundation.md`
- `SolutionFolder/docs/session-handoffs/2026-05-09-tool-security-validation.md`
- `SolutionFolder/docs/session-handoffs/2026-05-16-tool-approval-validation.md`
- `SolutionFolder/docs/diagrams/tool-security-trace-20260509.mmd`
- `SolutionFolder/docs/diagrams/tool-approval-trace-20260516.mmd`
