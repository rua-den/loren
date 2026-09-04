# Loren

**English** · [Tiếng Việt](README.vi.md)

Loren is a long-lived personal intelligence system with persistent memory, explicit permissions, tool use, and eventually proactive behavior across the owner's digital life.

> **The model is replaceable compute. Loren owns identity, memory, context, policy, action boundaries, and history.**

## Core principles

1. **Memory-first** — durable state survives conversations, restarts, and provider changes.
2. **Tool-first** — external facts/actions come from authoritative tools instead of model guessing.
3. **Permission-first** — a model may request an action; Loren authorizes and executes it.
4. **Model-independent** — model providers are replaceable adapters.
5. **Auditable** — consequential behavior must be reconstructable.
6. **Progressive autonomy** — proactive/background behavior comes only after lower-level trust boundaries are proven.

## Current status

**Last updated:** 2026-09-04  
**Phase:** `v0.1 — Trustworthy Core development`  
**Current milestone:** `M4 — Trusted Durable Memory`

Completed:

- **Gate A / ADR-001:** Loren-owned core/runtime boundary accepted.
- **Gate B / ADR-002:** provider-neutral v0.1 stack accepted.
- **Gate C / ADR-003:** canonical state + memory lifecycle rules accepted.
- **M0:** technical feasibility proofs complete.
- **M1:** engineering foundation complete.
- **M2:** Walking Skeleton complete; first owner-testable Loren preview proven.
- **M3:** Canonical Project/Repository State complete.

Detailed status: [`docs/status.md`](docs/status.md).

## What M3 proved

M3 Slice 1 merged in PR #15 at `00fbba08587ba8275c121fd7f9532a785f55314d`. M3 Slice 2 merged in PR #16 at `56fd988d3b74c754604355e3c97a5d3656675bbb`.

Validation:

- Slice 1 exact-head CI #99 / run `33842440251` — PASS;
- Slice 2 final PR exact-head CI #108 / run `33843405386` — PASS;
- Slice 2 post-merge main CI #109 / run `33843524467` — PASS.

Canonical identity path:

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

Runtime path:

```text
Owner request + optional exact Project alias
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

Unknown aliases fail before model execution. Runtime and brain adapters never receive EF `DbContext`. Configured Project/Repository identity is trusted canonical context, but current external facts still have to be fetched through authorized tools.

## Gate C / ADR-003

Before durable memory code begins, Loren now has locked rules for:

- opaque Loren-owned GUID IDs;
- explicit EF Core migration policy;
- Project/Repository canonical schema boundaries;
- memory source/trust classes;
- append/supersede corrections;
- memory deletion versus audit retention;
- logical export format versioning independent from EF schema migrations.

Required durable-memory source classes:

```text
OWNER_EXPLICIT
OWNER_CORRECTION
VERIFIED_TOOL
OWNER_APPROVED_INFERENCE
MODEL_INFERENCE
EXTERNAL_CONTENT
```

See [`ADR-003`](docs/decisions/003-canonical-state-and-memory-lifecycle.md) and [`docs/memory.md`](docs/memory.md).

## Current M4 target

M4 builds durable memory under those trust rules.

First vertical flow:

```text
Owner: "Nhớ wedding-online là web đám cưới của tao."
        |
        v
OWNER_EXPLICIT MemoryRecord
        |
Project scope + provenance
        |
SQLite persistence
        |
restart
        |
trusted retrieval
        |
small prepared memory context
```

Then M4 must prove owner correction/supersession and that `MODEL_INFERENCE` / `EXTERNAL_CONTENT` cannot silently become owner truth or policy.

## Canonical storage

```text
database file: loren.db
default directory: OS local application data / Loren
override: LOREN_DATA_DIRECTORY
migrations: automatic at host startup
```

A fresh database currently has no configured Projects; owner-facing Project CRUD/configuration UI remains deferred.

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

Do not commit real secrets. Use HTTPS or a trusted TLS-terminating reverse proxy when exposing the host beyond localhost. More details: [`docs/development.md`](docs/development.md).

## Accepted v0.1 stack

```text
C# 14 / .NET 10 LTS
ASP.NET Core
small Loren-owned bounded agent loop
provider-neutral IBrain
MCP C# SDK behind Loren action contracts
SQLite + EF Core
Blazor Web App
xUnit / Microsoft Testing Platform
```

## Next

```text
M4 Slice 1
MemoryRecord + source authority model
 -> SQLite migration/persistence
 -> OWNER_EXPLICIT save + Project scope
 -> restart-safe retrieval
 -> correction/supersession
 -> poisoning tests
 -> prepared memory context
 -> M4 exit gate
```

Gate D remains mandatory before any GitHub write capability.

## Version path

```text
v0.0  architecture / feasibility        ✓ complete
v0.1  trustworthy core                 <- current / M4
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
- [`docs/development.md`](docs/development.md) — build/test/configuration guidance
- [`docs/vision.md`](docs/vision.md) — product vision
- [`docs/architecture.md`](docs/architecture.md) — active system boundaries
- [`docs/plans/master-plan.md`](docs/plans/master-plan.md) — version milestones and gates
- [`docs/plans/v0.1.md`](docs/plans/v0.1.md) — detailed v0.1 implementation plan
- [`docs/decisions/001-agent-runtime-strategy.md`](docs/decisions/001-agent-runtime-strategy.md)
- [`docs/decisions/002-v0.1-technology-stack.md`](docs/decisions/002-v0.1-technology-stack.md)
- [`docs/decisions/003-canonical-state-and-memory-lifecycle.md`](docs/decisions/003-canonical-state-and-memory-lifecycle.md)
- [`docs/memory.md`](docs/memory.md)
- [`docs/permissions.md`](docs/permissions.md)
- [`docs/security.md`](docs/security.md)

This repository is the source of truth for Loren's product decisions, architecture, delivery plans, implementation, progress, and release history.
