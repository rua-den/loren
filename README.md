# Loren

**English** · [Tiếng Việt](README.vi.md)

Loren is a long-lived personal intelligence system with persistent memory, explicit permissions, tool use, and eventually proactive behavior across the owner's digital life.

> **The model is replaceable compute. Loren owns identity, memory, context, policy, approvals, action boundaries, and history.**

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
**Passed decision gates:** `Gate A`, `Gate B`, `Gate C`, `Gate D / ADR-004`  
**Current milestone:** `M5 — Action/Credential Boundary + Narrow GitHub Writes`  
**Now:** `M5 Slice 1 — typed write policy + one-time approval + global read-only`

Completed:

- Gate A / ADR-001 — Loren-owned core/runtime boundary.
- Gate B / ADR-002 — provider-neutral v0.1 stack.
- Gate C / ADR-003 — canonical state + memory lifecycle.
- Gate D / ADR-004 — action approval + credential boundary.
- M0 — technical feasibility.
- M1 — engineering foundation.
- M2 — Walking Skeleton.
- M3 — Canonical Project/Repository State.
- M4 — Trusted Durable Memory.

Detailed status: [`docs/status.md`](docs/status.md).

## What M4 proved

```text
owner durable fact
 -> canonical MemoryRecord + provenance
 -> SQLite
 -> restart-safe retrieval

owner correction
 -> append OWNER_CORRECTION
 -> supersede old claim atomically
 -> history stays reconstructable

project request
 -> current memories
 -> authority/provenance/lifecycle filtering
 -> deterministic hard bounds
 -> prepared Loren memory data
 -> BrainContext

owner forget
 -> purge full correction chain transactionally
 -> restart
 -> forgotten claim stays absent

adversarial content
 -> MODEL_INFERENCE / EXTERNAL_CONTENT cannot silently become owner truth
 -> provenance remains data, never action authorization
```

M4 merged through PRs #18–#22. PR #23 then fixed Windows-specific SQLite temp-file cleanup by disabling pooling only for temporary integration databases and added a permanent `windows-latest` integration job.

Latest verified baseline:

- main commit after Windows hardening: `1cdd849126310745652d87f1d100c34aed624079`;
- PR CI #162 / `33893832128`: Ubuntu full gate + Windows integration — PASS;
- post-merge main CI #163 / `33894104116`: Ubuntu full gate + Windows integration — PASS;
- owner local Windows full integration suite — PASS.

## Gate D / ADR-004 [PASSED]

Gate D freezes the first write-capable trust boundary before any real write executor exists.

### Every first-version real GitHub write requires explicit owner approval

Authentication proves owner identity; it is **not** write approval.

Approval is a Loren-owned artifact bound to exact normalized intent:

```text
ApprovalId
owner/session binding
action identity
ProjectId + RepositoryId
normalized target/resource
security-relevant parameter digest
approved timestamp
expiry/task boundary
one-time consumption state
optional prerequisite digest
```

Material changes to repository, branch, path, content digest, PR base/head, or action intent require a new approval.

Approval is atomically consumed once. Consumed, expired, mismatched, unknown, revoked, or replayed approval fails closed.

### Canonical target before authorization

A model-provided repository string is not authority. Write policy must resolve the request to canonical Loren-owned Project/Repository identity and normalized security-relevant target parameters before authorization.

### Global read-only defaults safe

Before any write executor ships Loren must have a host-controlled global read-only posture.

```text
write-enable missing/invalid -> read-only
read-only -> no write executor
read-only -> no write credential resolution
read actions remain available
```

The model cannot toggle this through an ordinary action.

### Credentials stay behind the executor boundary

Write credential values never enter:

- `BrainContext`;
- model-visible action parameters;
- canonical state;
- durable memory;
- audit payloads;
- owner-visible results.

Only an opaque credential purpose/reference crosses application boundaries. Missing/revoked credentials fail closed. Credential revocation overrides prior approval. Read/write credential purposes remain logically separated.

### External write success requires verification

A successful API response alone is not enough.

```text
create branch -> fetch ref -> confirm exact SHA
file/commit write -> fetch commit/ref/file state -> confirm expected identity
open PR -> fetch PR -> confirm repo/base/head/state/PR identity
```

Ambiguous verification is not reported as success.

### v0.1 write allowlist

Allowed only after M5 foundations are green:

```text
create non-default branch
create/update file via controlled commit path on a non-default branch
create commit/update ref only as required by that path
open pull request
```

Still forbidden:

```text
direct default-branch write
merge pull request
force push / history rewrite
delete repository/branch/data
repository admin/security changes
secret-management actions
production deployment
```

## M5 implementation sequence

```text
Slice 1  typed action policy context + one-time approval + global read-only
Slice 2  write credential resolver + redaction/revocation
Slice 3  create non-default GitHub branch + verify exact ref/SHA
Slice 4  controlled file/commit path + verify
Slice 5  open pull request + verify
Slice 6  replay/revocation/injection/audit E2E
```

No real GitHub mutation is enabled in Slice 1. No mutation should be enabled before the policy/approval/read-only/credential foundations are tested.

## Durable-memory source classes

```text
OWNER_EXPLICIT
OWNER_CORRECTION
VERIFIED_TOOL
OWNER_APPROVED_INFERENCE
MODEL_INFERENCE
EXTERNAL_CONTENT
```

ADR-003 keeps authority contextual instead of collapsing it into one confidence score.

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

## Test

```bash
dotnet restore Loren.slnx
dotnet build Loren.slnx --configuration Release --no-restore
dotnet test Loren.slnx --configuration Release --no-build --no-restore
```

Windows is now a first-class integration-test CI platform in addition to the Ubuntu full gate.

## Version path

```text
v0.0  architecture / feasibility        ✓ complete
v0.1  trustworthy core                 <- current / M5
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
- [`docs/permissions.md`](docs/permissions.md) — active permission/approval baseline
- [`docs/security.md`](docs/security.md) — active security baseline
- [`docs/plans/master-plan.md`](docs/plans/master-plan.md) — version milestones and gates
- [`docs/plans/v0.1.md`](docs/plans/v0.1.md) — detailed v0.1 implementation plan
- [`docs/decisions/003-canonical-state-and-memory-lifecycle.md`](docs/decisions/003-canonical-state-and-memory-lifecycle.md)
- [`docs/decisions/004-action-approval-and-credential-boundary.md`](docs/decisions/004-action-approval-and-credential-boundary.md)
- [`docs/memory.md`](docs/memory.md)

This repository is the source of truth for Loren's product decisions, architecture, delivery plans, implementation, progress, and release history.
