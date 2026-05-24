# AdventuresAuth Local API Validation

## Purpose

Capture durable evidence that the local/dev `AdventuresAuth` Minimal API host skeleton works through thin endpoints while preserving service-layer auth decisions, correlation, redaction, and local-only scope.

## Evidence Artifacts

- `SolutionFolder/artifacts/logs/adventures-auth-local-api-trace-20260517.log`
- `SolutionFolder/artifacts/logs/adventures-auth-local-api-trace-20260517.metadata.json`
- `SolutionFolder/docs/diagrams/adventures-auth-local-api-trace-20260517.mmd`

## Inspected Inputs

- `Adventures.Auth.LocalApi/`
- `VsMcpBridge.Shared.Tests/AdventuresAuthLocalApiTests.cs`
- `SolutionFolder/docs/adventures-auth-local-api-boundary-design.md`
- `SolutionFolder/docs/adventures-auth-local-prototype-design.md`

## Validation Summary

The current Minimal API skeleton is validated through `VsMcpBridge.Shared.Tests/AdventuresAuthLocalApiTests.cs`.

Covered behavior:

- `POST /auth/login` with valid local development credential allowed
- `POST /auth/login` with invalid credential denied
- `POST /auth/validate` with valid local session allowed through current-principal and logout flows
- `POST /auth/validate` with invalid session denied
- `GET /auth/me` returns current principal placeholder for a valid session
- `POST /auth/logout` invalidates the local session placeholder
- supplied `CorrelationId`, `RequestId`, and `AuthDecisionId` are preserved
- missing correlation identifiers are generated as non-secret values
- `ClientApplication=BlogAI` and `Environment=LocalDevelopment` are preserved or defaulted
- secret-like marker values are redacted from serialized response evidence
- endpoint handlers are thin and delegate to `IAdventuresAuthApiService`
- auth decisions remain in `IAdventuresAuthDecisionService` / `AdventuresAuthDecisionService`
- no persistence/database behavior
- no BlogEngine.NET dependency introduced

## Boundary Status

Preserved:

- local/dev-only Minimal API host skeleton
- `/auth` endpoint group in `Program.cs`
- thin endpoint handlers
- service-layer auth decisions
- deterministic local allow/deny behavior
- redacted audit/event evidence
- correlation metadata across the API boundary
- no production deployment
- no OAuth/OpenID
- no RBAC
- no tenant/org model
- no persistence/database
- no real password storage
- no external identity provider
- no BlogEngine.NET auth coupling
- no Blazor UI

## Validation Commands

Required commands for this slice:

- `git diff --check`
- `dotnet test ./VsMcpBridge.Shared.Tests/VsMcpBridge.Shared.Tests.csproj`
- `dotnet build ./Adventures.Auth.LocalApi/Adventures.Auth.LocalApi.csproj`
- `dotnet build ./VsMcpBridge.Shared/VsMcpBridge.Shared.csproj`

Result: passed.

Known warning context:

- Existing nullable/analyzer warnings may appear in broader shared/MCP test/build paths.
- The warnings are not introduced by the AdventuresAuth Local API validation artifacts and did not block validation.

## Resume Guidance

Future sessions can verify the local `AdventuresAuth` Minimal API host behavior from the artifacts above without reconstructing the test run from chat history.

The next useful slice, if approved, should stay narrow. Options include a local-only manual host run trace, an API-client adapter for BlogAI, or a small ergonomics cleanup if the endpoint DTO shape needs refinement. Do not add production auth, persistence, OAuth/OpenID, RBAC, BlogEngine.NET auth coupling, Blazor UI, or deployment changes without a separate explicit slice.
