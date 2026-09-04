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
**Completed milestone:** `M4 — Trusted Durable Memory`  
**Next decision gate:** `Gate D — Action/Credential Policy before M5 writes`

Completed:

- Gate A / ADR-001 — Loren-owned core/runtime boundary.
- Gate B / ADR-002 — provider-neutral v0.1 stack.
- Gate C / ADR-003 — canonical state + memory lifecycle.
- M0 — technical feasibility.
- M1 — engineering foundation.
- M2 — Walking Skeleton.
- M3 — Canonical Project/Repository State.
- M4 — Trusted Durable Memory.

Detailed status: [`docs/status.md`](docs/status.md).

## M4 memory path [COMPLETE]

```text
owner durable fact
 -> MemoryRecord / MemoryRecordId
 -> Project / Repository scope + provenance
 -> SQLite
 -> restart-safe retrieval

owner correction
 -> append OWNER_CORRECTION
 -> supersede old claim atomically
 -> old history remains reconstructable

project request
 -> current memories
 -> source/provenance + lifecycle filtering
 -> deterministic ordering
 -> hard content/provenance bounds
 -> prepared Loren memory data
 -> BrainContext

owner forget
 -> current memory
 -> purge full correction chain transactionally
 -> restart
 -> forgotten claim stays absent
```

### Slice 1 — OWNER_EXPLICIT persistence

PR #18 merged at `78adc287f7ae3744352b7019e3b8a838a5de499e`. Final CI #117 / run `33860985267` and post-merge main CI #118 / run `33861089270` passed.

### Slice 2 — correction + supersession

PR #19 merged at `201b83eff0c6c3143856e348b4c9f029cc14a8b1`. Implementation CI #119 / run `33861345949` and final CI #123 / run `33861630472` passed.

Correction is explicit append + supersede. Old content is preserved; invalid authority/scope, stale targets, duplicate replacement IDs, and partial failure all fail closed.

### Slice 3 — authority-aware prepared memory

PR #20 merged at `732b85db3a799638bcd73558f98232b276f3cb5e`. Implementation CI #127 / run `33864695658` and final exact-head CI #131 / run `33864946328` passed.

Prepared memory:

- includes `OWNER_CORRECTION`, `OWNER_EXPLICIT`, `OWNER_APPROVED_INFERENCE`, and `VERIFIED_TOOL` only when the source semantics are valid;
- excludes `MODEL_INFERENCE` and `EXTERNAL_CONTENT` from default trusted model context;
- excludes superseded records;
- retains canonical IDs, scope, provenance, and timestamps;
- applies deterministic ordering and hard record/content bounds before model execution;
- is explicitly data, never action authorization or policy override.

### Slice 4 — owner forget/delete

PR #21 merged at `87b5a39ccae7c931de9668fed5283a4742be73f7`. Implementation CI #133 / run `33865419023` and final exact-head CI #137 / run `33865716479` passed.

For `A -> B -> C(current)`, `ForgetAsync(C)` validates a same-scope linear correction history and physically purges A, B, C inside one SQLite transaction. Restart acceptance proves forgotten claims do not resurrect while unrelated memories survive. Memory forgetting does not cascade into audit retention.

### Slice 5 — poisoning / trust boundary

PR #22 closes the M4 trust gate.

Hardening and adversarial acceptance prove:

- trusted prepared-context records require provenance;
- provenance/source references are bounded independently from memory content;
- the entire serialized memory payload — content, provenance, IDs, scope and timestamps — is inert data, never instructions, permission, policy or action authorization;
- spoofed `MODEL_INFERENCE` / `EXTERNAL_CONTENT` stay excluded even with owner-looking provenance;
- unproven `OWNER_APPROVED_INFERENCE` / `VERIFIED_TOOL` records stay excluded;
- `VERIFIED_TOOL` does not automatically represent current external state or grant owner permission;
- owner correction remains current owner truth in scope;
- normal `LorenRunService` execution reads prepared memory without silently calling Add/Correct/Forget mutations.

Implementation CI #140 / run `33866182751` passed restore, zero-warning build, all tests, format, secret scan, dependency scan, and web/auth smoke.

## Durable-memory source classes

```text
OWNER_EXPLICIT
OWNER_CORRECTION
VERIFIED_TOOL
OWNER_APPROVED_INFERENCE
MODEL_INFERENCE
EXTERNAL_CONTENT
```

ADR-003 keeps authority contextual rather than collapsing it into one confidence score.

## Canonical storage

```text
database file: loren.db
default directory: OS local application data / Loren
override: LOREN_DATA_DIRECTORY
migrations: automatic at host startup
```

A fresh database currently has no configured Projects; owner-facing Project CRUD/configuration and memory-management UI remain deferred to later v0.1 UI work.

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

## Next — Gate D / M5

Before any real GitHub write capability is enabled, Gate D must lock:

- write action contracts and policy dimensions;
- exact approval binding and non-replay rules;
- credential storage/resolution and read/write separation;
- secret redaction, rotation, and revocation;
- global read-only / kill behavior;
- post-write verification and audit expectations.

Only after Gate D may M5 implement the narrow v0.1 write set: create branch, create/update file or equivalent commit path, create commit, and open pull request. Merge-main, force-push, repository deletion/admin changes, and production deploy remain outside that write scope.

## Version path

```text
v0.0  architecture / feasibility        ✓ complete
v0.1  trustworthy core                 <- current / Gate D before M5
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
