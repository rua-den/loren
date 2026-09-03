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

Run the development host:

```bash
dotnet run --project src/Loren.Web/Loren.Web.csproj
```

Health endpoint:

```text
GET /health
```

## Configuration and secrets

Copy `.env.example` only as a reference. Do not commit real provider or tool credentials.

Provider selection is configuration. Loren-owned state must not depend on a provider account or provider session.

Expected provider secrets may include:

```text
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

## M1 project boundaries

The initial production scaffold intentionally starts smaller than the long-term target:

```text
Loren.Core
Loren.Runtime
Loren.Brain.Ollama
Loren.Brain.OpenAI
Loren.Infrastructure
Loren.Web
```

Tools/GitHub-specific projects are added in M2 when the walking skeleton needs them. Splitting an `Api` project is also deferred until a real boundary justifies it.

The `spikes/` directory is technical validation evidence and is not production code.
