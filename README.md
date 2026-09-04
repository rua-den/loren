# Loren

**English** · [Tiếng Việt](README.vi.md)

Loren is a long-lived personal intelligence system with persistent memory, explicit permissions, tool use, and eventually proactive behavior across the owner's digital life.

> **The model is replaceable compute. Loren owns identity, memory, context, policy, action boundaries, and history.**

## Core principles

1. **Memory-first** — durable state survives conversations, restarts, and provider changes.
2. **Tool-first** — external facts/actions come from authoritative tools instead of model guessing.
3. **Permission-first** — a model may request an action; Loren authorizes and executes it.
4. **Model-independent** — Ollama, OpenAI, Claude, local models, and future providers are adapters.
5. **Auditable** — consequential actions and important state changes must be reconstructable.
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
- **M2:** **Walking Skeleton complete.**

M2's trusted exact-main production proof passed on run `33840149005` against commit `94ce6d1e74f2dfdf0584b8dbf8a4edbbb3774f7d`:

```text
unauthenticated /api/run -> 401
owner login -> 200 + cookie session
authenticated /api/run
 -> Ollama gpt-oss:120b
 -> github.read_repository
 -> real GitHub GET rua-den/loren
 -> Ollama final answer
 -> correlated owner-visible audit
```

Observed result:

```text
runId:       5bb9cc341387430c82759d58309da85a
turns:       2
actionCount: 1
final:       Repository rua-den/loren
             Default branch: main
```

Audit passed:

```text
ActionRequested -> PolicyEvaluated -> ActionCompleted
requested       -> allow           -> succeeded
```

The trusted workflow also verified that owner/provider credentials were absent from the owner-visible response and `/internal/dev/run` remained `404` in Production.

**First owner-testable Loren preview: achieved.**

Detailed status: [`docs/status.md`](docs/status.md).

## Current M3 target

M3 gives Loren provider-independent canonical Project/Repository identity.

```text
Owner wording / project alias
        |
        v
canonical Loren Project ID
        |
        v
canonical Repository record
        |
        +--> integration metadata: GitHub owner/repo
        |
        v
prepared runtime context / authoritative tool use
```

Acceptance target:

```text
"wedding project"
"web đám cưới"
"wedding-online"
        |
        v
same Loren Project
        |
        v
rua-den/wedding-online
```

The mapping must survive restart and provider-session deletion.

M3 deliberately starts small. Do not add a generic personal graph or unrelated `Person`, `Task`, `Decision`, or `Preference` entities until real product flows require them.

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

## Run the current owner preview locally

```bash
export LOREN_OWNER_PASSWORD='choose-a-local-owner-password'
export OLLAMA_API_KEY='your-provider-secret'
dotnet run --project src/Loren.Web/Loren.Web.csproj
```

Then open the root URL printed by ASP.NET Core, sign in, and run:

```text
Loren, check repo rua-den/loren.
```

Do not commit real secrets. Use HTTPS or a trusted TLS-terminating reverse proxy when exposing the host beyond localhost. More details: [`docs/development.md`](docs/development.md).

## Next

```text
M3 canonical IDs + Project/Repository schema
 -> SQLite / EF Core persistence
 -> alias resolution + restart tests
 -> canonical Project/Repository context
 -> Gate C checkpoint
 -> M4 Trusted Memory
```

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
- [`docs/decisions/001-agent-runtime-strategy.md`](docs/decisions/001-agent-runtime-strategy.md) — accepted Loren-owned core/runtime boundary
- [`docs/decisions/002-v0.1-technology-stack.md`](docs/decisions/002-v0.1-technology-stack.md) — accepted provider-neutral v0.1 stack and M0 evidence
- [`docs/memory.md`](docs/memory.md) — memory model
- [`docs/permissions.md`](docs/permissions.md) — permission model
- [`docs/security.md`](docs/security.md) — security baseline
- [`docs/skills.md`](docs/skills.md) — skill/tool model

This repository is the source of truth for Loren's product decisions, architecture, delivery plans, implementation, progress, and release history.
