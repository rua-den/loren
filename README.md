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

- Gate A / ADR-001
- Gate B / ADR-002
- Gate C / ADR-003
- M0 technical feasibility
- M1 engineering foundation
- M2 Walking Skeleton
- M3 Canonical Project/Repository State
- M4 Slice 1 OWNER_EXPLICIT persistence
- M4 Slice 2 correction/supersession
- M4 Slice 3 authority-aware prepared memory context

Detailed status: [`docs/status.md`](docs/status.md).

## M4 memory path

```text
owner durable fact
 -> MemoryRecord / MemoryRecordId
 -> Project / Repository scope + provenance
 -> SQLite
 -> restart-safe retrieval

owner correction
 -> append OWNER_CORRECTION
 -> supersede old claim atomically

project request
 -> current memories
 -> authority/lifecycle filtering
 -> hard context bounds
 -> prepared memory data
 -> BrainContext

owner forget
 -> current memory
 -> purge its full correction chain transactionally
 -> restart
 -> forgotten claim stays absent
```

### Slice 1 [COMPLETE]

PR #18 merged at `78adc287f7ae3744352b7019e3b8a838a5de499e`. Final CI #117 / run `33860985267` and post-merge main CI #118 / run `33861089270` passed.

### Slice 2 [COMPLETE]

PR #19 merged at `201b83eff0c6c3143856e348b4c9f029cc14a8b1`. Implementation CI #119 / run `33861345949` and final CI #123 / run `33861630472` passed.

Correction is explicit append + supersede; old content is preserved and invalid/stale/scope-changing replacements fail closed.

### Slice 3 [COMPLETE]

PR #20 merged at `732b85db3a799638bcd73558f98232b276f3cb5e`. Implementation CI #127 / run `33864695658` and final exact-head CI #131 / run `33864946328` passed.

Prepared memory:
- includes owner correction, owner explicit, owner-approved inference, and verified-tool records;
- excludes model inference and external content by default;
- excludes superseded records;
- retains IDs, scope, provenance and timestamps;
- has deterministic ordering plus hard record/character bounds;
- is explicitly data, not action authorization or policy override.

### Slice 4 [IMPLEMENTED / PR #21 FINAL GATE]

PR #21 adds `IMemoryStore.ForgetAsync(...)`.

For a chain `A -> B -> C(current)`, forgetting C walks the same-scope linear history and physically deletes A, then B, then C inside one SQLite transaction. This prevents an old corrected claim from reappearing as current truth.

Real SQLite acceptance proves:
- the whole chain remains absent after restart;
- prepared context cannot resurrect forgotten content;
- unrelated memories survive;
- forgetting a stale/superseded or unknown target fails closed.

Implementation CI #133 / run `33865419023` is **PASS** across restore, zero-warning build, all tests, format, secret scan, dependency scan, and web/auth smoke.

Memory forgetting does not use an audit cascade; audit retention remains a separate concern under ADR-003. PR #21 still needs the documentation-synchronized final exact-head CI before merge.

## Durable-memory source classes

```text
OWNER_EXPLICIT
OWNER_CORRECTION
VERIFIED_TOOL
OWNER_APPROVED_INFERENCE
MODEL_INFERENCE
EXTERNAL_CONTENT
```

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
dotnet run --project src/Loren.Web/Loren.Web.csproj
```

PowerShell:

```powershell
$env:LOREN_OWNER_PASSWORD='choose-a-local-owner-password'
$env:OLLAMA_API_KEY='your-provider-secret'
dotnet run --project src/Loren.Web/Loren.Web.csproj
```

Do not commit real secrets.

## Next

```text
PR #21 final exact-head CI
 -> merge M4 Slice 4
 -> M4 Slice 5 poisoning/trust acceptance
 -> M4 exit gate
 -> Gate D / M5
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
