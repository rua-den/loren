# Loren Development

## Prerequisites

- .NET SDK `10.0.400` (pinned by `global.json`)
- Git

## Build and test

```bash
dotnet restore Loren.slnx
dotnet build Loren.slnx --configuration Release --no-restore
dotnet test Loren.slnx --configuration Release --no-build --no-restore
dotnet format Loren.slnx --verify-no-changes --no-restore
```

CI runs the full build/test/format/security/web smoke gate on Ubuntu and the real SQLite integration suite on Windows. Canonical-state integration tests also compare the EF migration snapshot against the current design-time model so a model change without matching migration metadata fails directly instead of surfacing as many unrelated migration failures.

## Run the owner preview

Set the owner password and configured brain credential in the local process environment, then start the host:

```bash
export LOREN_OWNER_PASSWORD='choose-a-local-owner-password'
export OLLAMA_API_KEY='your-provider-secret'
dotnet run --project src/Loren.Web/Loren.Web.csproj
```

On PowerShell:

```powershell
$env:LOREN_OWNER_PASSWORD='choose-a-local-owner-password'
$env:OLLAMA_API_KEY='your-provider-secret'
dotnet run --project src/Loren.Web/Loren.Web.csproj
```

Open the root URL printed by ASP.NET Core. Unauthenticated access redirects to `/login`. After owner login, the console submits requests through protected `/api/run` and displays correlated audit data.

The optional Project alias field is the M3 prepared-context path. If a canonical Project has already been explicitly configured, an exact configured alias is resolved before the model runs and the returned Project/Repository identity is shown in the console. A fresh database has no Project records yet; project-management CRUD UI remains deferred.

Health remains public:

```text
GET /health
```

The temporary proof route remains disabled by default:

```text
/internal/dev/run -> 404 by default
```

It may only be explicitly enabled in `Development` with `LOREN_ENABLE_DEVELOPMENT_RUN_ENDPOINT=true`. Normal owner use goes through authenticated `/api/run`.

## M5 write safety posture

Gate D/ADR-004 is now implemented at the policy/approval foundation layer, but M5 Slice 1 still registers **no real GitHub mutation executor**.

The host has a fail-closed global write switch:

```text
LOREN_ENABLE_WRITES=false
```

Only the exact configured boolean value `true` opts the host out of read-only mode. Missing, false, malformed, or any other value remains read-only. This switch is host configuration, not a model-visible action and not durable memory.

Even when `LOREN_ENABLE_WRITES=true`, Slice 1 does not create a mutation capability by itself. A real write also needs a registered write action/executor, canonical Project/Repository authorization context, exact Loren-owned approval, and later M5 credential resolution/verification boundaries.

Current non-read authorization flow:

```text
ActionRequest from brain
 -> trusted ActionAuthorizationContext from Loren
 -> GateDActionPolicy
 -> global read-only check
 -> exact action-intent fingerprint
 -> trusted ApprovalId
 -> atomic IActionApprovalStore consume
 -> executor only after successful consume
```

Important rules:

- authentication is not approval;
- `ApprovalId` in model-visible action arguments has no authority;
- approval binds action + canonical Project/Repository + normalized target + model arguments;
- consumed, expired, revoked, mismatched, unknown, or replayed approval fails closed;
- approval is consumed before the consequential executor attempt, so an independent retry requires fresh approval;
- `PRIVILEGED_WRITE` remains denied in v0.1;
- write credential values are not implemented in Slice 1 and must not be added to action arguments, memory, or audit.

Approval lifecycle state is stored in the canonical SQLite database through migration:

```text
202609040003_AddActionApprovals
```

The store uses atomic compare-and-consume semantics. Real SQLite tests cover restart persistence, expiry, revocation, mismatch, and concurrent consumption with exactly one winner.

## Canonical state storage

Production canonical Project/Repository/Memory/Approval state uses SQLite + EF Core in `Loren.Infrastructure`.

The host creates/migrates the canonical database on startup. The database filename is:

```text
loren.db
```

By default it lives under the operating system's local application-data directory in a `Loren` subdirectory. Override the directory explicitly when useful:

```bash
export LOREN_DATA_DIRECTORY='/srv/loren/data'
```

PowerShell:

```powershell
$env:LOREN_DATA_DIRECTORY='D:\loren-data'
```

`LOREN_DATA_DIRECTORY` is a directory, not a connection string. Loren appends `loren.db` and owns schema migrations. Do not treat manual SQLite editing as a normal product API.

Canonical state must remain independent of model/provider conversation state. `Loren.Core` exposes provider/EF-neutral contracts; EF Core and SQLite types stay in `Loren.Infrastructure`. The application/host layer resolves aliases and prepares small trusted contexts; `AgentLoop` and brain adapters never receive `DbContext`.

Temporary integration-test SQLite databases use `Pooling=False` so file cleanup is deterministic on Windows. Production SQLite pooling behavior is unchanged.

## Configuration and secrets

Copy `.env.example` only as a reference. Do not commit real owner, provider, or tool credentials.

Expected host/provider secrets may include:

```text
LOREN_OWNER_PASSWORD
OLLAMA_API_KEY
OPENAI_API_KEY
```

They belong in the local process environment or an external secret store, never canonical Loren memory. `LOREN_DATA_DIRECTORY` and `LOREN_ENABLE_WRITES` are configuration, not secrets.

The owner credential is digested immediately by the authentication service for comparison and is never sent to brain/tool context. Use HTTPS or a trusted local/reverse-proxy boundary when the host is reachable beyond localhost.

M5 Slice 2 will add the write-specific credential resolver/redaction/revocation boundary. Until that exists and passes, do not add real external mutation executors.

## Dependency policy

- exact shared NuGet versions live in `Directory.Packages.props`;
- floating package versions are not allowed;
- SDK version is pinned in `global.json`;
- dependency updates are deliberate PRs with build/test/vulnerability checks;
- provider SDK packages remain inside provider adapter projects;
- `Loren.Core` must not reference provider, MCP, EF Core, ASP.NET Core, or Blazor packages.

## Current project boundaries

```text
Loren.Core              canonical contracts + brain/action/approval abstractions
Loren.Runtime           bounded agent loop + ActionGateway + Gate D policy
Loren.Brain.Ollama      provider adapter
Loren.Brain.OpenAI      optional provider adapter
Loren.Infrastructure    SQLite/EF canonical persistence + approval store + audit
Loren.Tools.GitHub      current GitHub read executor; mutations still deferred
Loren.Web               host/auth/UI/context preparation/composition
```

Projects are split only when a real dependency boundary justifies it. The `spikes/` directory is technical validation evidence and is not production code.
