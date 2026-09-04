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

The optional Project alias field is the M3 prepared-context path. If a canonical Project has already been explicitly configured, an exact configured alias is resolved before the model runs and the returned Project/Repository identity is shown in the console. A fresh database has no Project records yet; M3 does not add project-management CRUD UI.

Health remains public:

```text
GET /health
```

The temporary proof route remains disabled by default:

```text
/internal/dev/run -> 404 by default
```

It may only be explicitly enabled in `Development` with `LOREN_ENABLE_DEVELOPMENT_RUN_ENDPOINT=true`. Normal owner use goes through authenticated `/api/run`.

## Canonical state storage

M3 production canonical Project/Repository state uses SQLite + EF Core in `Loren.Infrastructure`.

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

Canonical state must remain independent of model/provider conversation state. `Loren.Core` exposes provider/EF-neutral Project contracts; EF Core and SQLite types stay in `Loren.Infrastructure`. The application/host layer resolves aliases and prepares a small `BrainContext`; `AgentLoop` and brain adapters never receive `DbContext`.

## Configuration and secrets

Copy `.env.example` only as a reference. Do not commit real owner, provider, or tool credentials.

Expected host/provider secrets may include:

```text
LOREN_OWNER_PASSWORD
OLLAMA_API_KEY
OPENAI_API_KEY
```

They belong in the local process environment or an external secret store, never canonical Loren memory. `LOREN_DATA_DIRECTORY` is configuration, not a secret.

The owner credential is digested immediately by the authentication service for comparison and is never sent to brain/tool context. Use HTTPS or a trusted local/reverse-proxy boundary when the host is reachable beyond localhost.

## Dependency policy

- exact shared NuGet versions live in `Directory.Packages.props`;
- floating package versions are not allowed;
- SDK version is pinned in `global.json`;
- dependency updates are deliberate PRs with build/test/vulnerability checks;
- provider SDK packages remain inside provider adapter projects;
- `Loren.Core` must not reference provider, MCP, EF Core, ASP.NET Core, or Blazor packages.

## Current project boundaries

```text
Loren.Core              canonical contracts + brain/action abstractions
Loren.Runtime           bounded agent loop + ActionGateway
Loren.Brain.Ollama      provider adapter
Loren.Brain.OpenAI      optional provider adapter
Loren.Infrastructure    SQLite/EF canonical persistence + audit implementation
Loren.Tools.GitHub      GitHub action executor
Loren.Web               host/auth/UI/context preparation/composition
```

Projects are split only when a real dependency boundary justifies it. The `spikes/` directory is technical validation evidence and is not production code.
