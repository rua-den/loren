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

## Run the M2 owner preview

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

Open the root URL printed by ASP.NET Core. Unauthenticated access redirects to `/login`. After owner login, the M2 console can submit a request through the protected `/api/run` path and displays the correlated audit returned for that run.

The current owner credential is a host configuration secret for the M2 one-owner preview. It is digested immediately by the authentication service for comparison, is never sent to the brain/tool context, and must not be committed. Use HTTPS or a trusted local/reverse-proxy boundary when the host is reachable beyond localhost.

Health endpoint remains public:

```text
GET /health
```

The temporary proof route remains disabled by default:

```text
/internal/dev/run -> 404 by default
```

It may only be explicitly enabled in `Development` with `LOREN_ENABLE_DEVELOPMENT_RUN_ENDPOINT=true`. Normal owner use must go through authenticated `/api/run`.

## Configuration and secrets

Copy `.env.example` only as a reference. Do not commit real owner, provider, or tool credentials.

Provider selection is configuration. Loren-owned state must not depend on a provider account or provider session.

Expected host/provider secrets may include:

```text
LOREN_OWNER_PASSWORD
OLLAMA_API_KEY
OPENAI_API_KEY
```

They belong in the local process environment or an external secret store, never canonical Loren memory.

## Dependency policy

- exact shared NuGet versions live in `Directory.Packages.props`;
- floating package versions are not allowed;
- SDK version is pinned in `global.json`;
- dependency updates are deliberate PRs with build/test/vulnerability checks;
- provider SDK packages must remain inside provider adapter projects;
- `Loren.Core` must not reference provider, MCP, EF Core, ASP.NET Core, or Blazor packages.

## M1/M2 project boundaries

The production scaffold remains deliberately smaller than the long-term target:

```text
Loren.Core
Loren.Runtime
Loren.Brain.Ollama
Loren.Brain.OpenAI
Loren.Infrastructure
Loren.Tools.GitHub
Loren.Web
```

Projects are split only when a real dependency boundary justifies it. The `spikes/` directory is technical validation evidence and is not production code.
