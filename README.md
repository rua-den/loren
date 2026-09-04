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
- **M2:** Walking Skeleton complete.
- **M3:** Canonical Project/Repository State complete.
- **M4 Slice 1:** OWNER_EXPLICIT durable persistence complete.
- **M4 Slice 2:** owner correction + supersession complete.

Detailed status: [`docs/status.md`](docs/status.md).

## M4 memory path today

```text
OWNER_EXPLICIT durable fact
 -> canonical MemoryRecord / MemoryRecordId
 -> Project / Repository scope + provenance
 -> SQLite / EF Core
 -> restart-safe retrieval

owner correction
 -> OWNER_CORRECTION replacement
 -> old.SupersededById = new.Id
 -> one transaction
 -> current retrieval returns correction only

current Project request
 -> current memory retrieval
 -> authority-aware filtering + hard bounds
 -> prepared Loren memory package
 -> BrainContext
```

### Slice 1 [COMPLETE]

PR #18 merged at `78adc287f7ae3744352b7019e3b8a838a5de499e` after final CI #117 / run `33860985267`; post-merge main CI #118 / run `33861089270` also passed.

### Slice 2 [COMPLETE]

PR #19 merged at `201b83eff0c6c3143856e348b4c9f029cc14a8b1`. Implementation CI #119 / run `33861345949` and final exact-head CI #123 / run `33861630472` passed.

Correction is explicit append + supersede. Old content is preserved, stale/scope-changing/non-owner correction attempts fail closed, and no generic destructive memory-update API exists.

### Slice 3 [IMPLEMENTED / PR #20 FINAL GATE]

PR #20 adds application-owned prepared memory context:

- `OWNER_CORRECTION`, `OWNER_EXPLICIT`, `OWNER_APPROVED_INFERENCE`, and `VERIFIED_TOOL` can enter the default prepared model context;
- `MODEL_INFERENCE` and `EXTERNAL_CONTENT` are excluded by default;
- superseded records are excluded before preparation;
- authority ordering is deterministic;
- record count and total content characters are hard-bounded before model execution;
- MemoryRecordId, scope, provenance/source reference, and timestamps remain inspectable;
- memory payload is explicitly data, not action authorization or a policy override;
- verified-tool facts are not treated as automatically current external state.

Real SQLite + fake-brain tests prove correction reaches the brain while superseded and poison-marker records do not. Implementation CI #127 / run `33864695658` at `179a203d6c3d11ff85eb8529d4107ae2edc7f720` is **PASS** across restore, zero-warning build, all tests, format, secret scan, dependency scan, and web/auth smoke.

PR #20 still needs the documentation-synchronized final exact-head CI before merge.

## Durable-memory source classes

```text
OWNER_EXPLICIT
OWNER_CORRECTION
VERIFIED_TOOL
OWNER_APPROVED_INFERENCE
MODEL_INFERENCE
EXTERNAL_CONTENT
```

ADR-003 locks append/supersede correction, memory deletion separate from audit retention, and logical export `format_version = 1` independently from EF migrations.

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

Do not commit real secrets. Use HTTPS or a trusted TLS-terminating reverse proxy when exposing the host beyond localhost.

## Next

```text
PR #20 final exact-head CI
 -> merge M4 Slice 3
 -> M4 Slice 4 forget/delete
 -> M4 Slice 5 poisoning/trust acceptance
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

## Documentation

- [`docs/status.md`](docs/status.md) — authoritative current progress
- [`docs/development.md`](docs/development.md) — build/test/configuration guidance
- [`docs/architecture.md`](docs/architecture.md) — active system boundaries
- [`docs/plans/master-plan.md`](docs/plans/master-plan.md) — version milestones and gates
- [`docs/plans/v0.1.md`](docs/plans/v0.1.md) — detailed v0.1 implementation plan
- [`docs/decisions/003-canonical-state-and-memory-lifecycle.md`](docs/decisions/003-canonical-state-and-memory-lifecycle.md)
- [`docs/memory.md`](docs/memory.md)

This repository is the source of truth for Loren's product decisions, architecture, delivery plans, implementation, progress, and release history.
