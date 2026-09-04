# Loren

**English** · [Tiếng Việt](README.vi.md)

Loren is a long-lived personal intelligence system with persistent memory, explicit permissions, tool use, and eventually proactive behavior across the owner's digital life.

> **The model is replaceable compute. Loren owns identity, memory, context, policy, action boundaries, and history.**

## Core principles

1. **Memory-first** — durable state survives conversations, restarts, and provider changes.
2. **Tool-first** — external facts/actions come from authoritative tools instead of model guessing.
3. **Permission-first** — a model may request an action; Loren authorizes and executes it.
4. **Model-independent** — Ollama, OpenAI, local models, and future providers are adapters.
5. **Auditable** — consequential behavior must be reconstructable.
6. **Progressive autonomy** — proactive/background behavior comes only after lower-level trust boundaries are proven.

## Current status

**Last updated:** 2026-09-04  
**Phase:** `v0.1 — Trustworthy Core development`  
**Current milestone:** `M3 — Canonical State`

Completed:

- **Gate A / ADR-001:** Loren-owned core/runtime boundary accepted.
- **Gate B / ADR-002:** provider-neutral v0.1 stack accepted.
- **M0:** technical feasibility proofs complete.
- **M1:** engineering foundation complete.
- **M2:** Walking Skeleton complete; first owner-testable Loren preview proven.
- **M3 Slice 1:** canonical Project/Repository identity + SQLite persistence complete.

M3 Slice 1 merged in PR #15 at `00fbba08587ba8275c121fd7f9532a785f55314d`. Exact-head CI run `33842440251` passed restore/build/test/format/secret/dependency/web-auth gates.

Current M3 Slice 2 candidate in PR #16 adds deterministic configured-alias resolution and prepared runtime context. Its implementation CI run `33843033700` passed before the final documentation sync; the final PR head must pass CI again before merge.

Detailed status: [`docs/status.md`](docs/status.md).

## Current M3 architecture

```text
Owner request + optional exact Project alias
        |
        v
Loren.Web
        |
        v
IProjectCatalog
        |
        v
SQLite / EF Core canonical state
        |
        v
ProjectSnapshot
        |
        v
small prepared BrainContext
        |
        v
AgentLoop -> IBrain -> authorized tools
```

Canonical identity example:

```text
"wedding project"
"web đám cưới"
"wedding-online"
        |
        v
same Loren ProjectId
        |
        v
canonical RepositoryId
        |
        v
github locator: rua-den/wedding-online
```

Important boundary: configured Project/Repository identity is trusted canonical context, but it is **not live external state**. Current GitHub facts still have to be fetched through authorized tools.

Unknown project aliases fail before model execution. Runtime and brain adapters never receive EF `DbContext`.

A fresh database has no configured Projects yet; M3 does not add owner-facing Project CRUD UI.

## Canonical storage

M3 uses the accepted SQLite + EF Core baseline.

```text
database file: loren.db
default directory: OS local application data / Loren
override: LOREN_DATA_DIRECTORY
migrations: automatic at host startup
```

Loren-owned `ProjectId` / `RepositoryId` are independent from GitHub, model-provider, or runtime session IDs.

## Run locally

```bash
export LOREN_OWNER_PASSWORD='choose-a-local-owner-password'
export OLLAMA_API_KEY='your-provider-secret'
# optional: export LOREN_DATA_DIRECTORY='/path/to/loren-data'
dotnet run --project src/Loren.Web/Loren.Web.csproj
```

PowerShell:

```powershell
$env:LOREN_OWNER_PASSWORD='choose-a-local-owner-password'
$env:OLLAMA_API_KEY='your-provider-secret'
# optional: $env:LOREN_DATA_DIRECTORY='D:\loren-data'
dotnet run --project src/Loren.Web/Loren.Web.csproj
```

Open the root URL printed by ASP.NET Core and sign in. The Project alias field only resolves aliases already present in canonical state.

Do not commit real secrets. Use HTTPS or a trusted TLS-terminating reverse proxy when exposing the host beyond localhost. More details: [`docs/development.md`](docs/development.md).

## Accepted v0.1 stack

```text
C# 14 / .NET 10 LTS
ASP.NET Core
small Loren-owned bounded agent loop
provider-neutral IBrain
  ├─ Ollama adapter
  ├─ OpenAI adapter
  └─ future providers/local models
MCP C# SDK behind Loren action contracts
SQLite + EF Core
Blazor Web App
xUnit / Microsoft Testing Platform
```

## Next

```text
M3 Slice 2 final CI + merge
 -> M3 Slice 3 / Gate C checkpoint
 -> M3 COMPLETE
 -> M4 Trusted Memory
```

M3 Slice 3 will lock canonical ID rules, migration policy, Project/Repository schema boundaries, memory-vs-audit deletion semantics, and export versioning direction before durable memory implementation begins.

## Version path

```text
v0.0  architecture / feasibility        ✓ complete
v0.1  trustworthy core                 <- current / M3
v0.2  useful project assistant
v0.3  personal operations
v0.4  voice + device presence
v0.5  proactive/background Loren
v0.6+ real-use hardening
v1.0  stable personal daily driver
```

Versions advance by exit gates, not dates or code volume.

## Documentation

- [`docs/status.md`](docs/status.md) — authoritative current progress
- [`docs/development.md`](docs/development.md) — build/test/configuration/dependency guidance
- [`docs/vision.md`](docs/vision.md) — product vision
- [`docs/architecture.md`](docs/architecture.md) — active system boundaries
- [`docs/plans/master-plan.md`](docs/plans/master-plan.md) — version milestones and gates
- [`docs/plans/v0.1.md`](docs/plans/v0.1.md) — detailed v0.1 implementation plan
- [`docs/decisions/001-agent-runtime-strategy.md`](docs/decisions/001-agent-runtime-strategy.md)
- [`docs/decisions/002-v0.1-technology-stack.md`](docs/decisions/002-v0.1-technology-stack.md)
- [`docs/memory.md`](docs/memory.md)
- [`docs/permissions.md`](docs/permissions.md)
- [`docs/security.md`](docs/security.md)
- [`docs/skills.md`](docs/skills.md)

This repository is the source of truth for Loren's product decisions, architecture, delivery plans, implementation, progress, and release history.
