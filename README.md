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

**Last updated:** 2026-09-05  
**Phase:** `v0.1 — Trustworthy Core development`  
**Completed milestone:** `M4 — Trusted Durable Memory`  
**Passed decision gates:** `Gate A`, `Gate B`, `Gate C`, `Gate D / ADR-004`  
**Current milestone:** `M5 — Action/Credential Boundary + Narrow GitHub Writes`  
**Completed M5 slice:** `Slice 1 — typed policy + one-time approval + global read-only`  
**Current target:** `M5 Slice 2 — write credential resolver + secret redaction/revocation`

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
- M5 Slice 1 — policy/approval/read-only foundation.

Detailed status: [`docs/status.md`](docs/status.md). Fresh-thread continuation checkpoint: [`docs/handoff.md`](docs/handoff.md).

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

M4 merged through PRs #18–#22. PR #23 then hardened Windows SQLite integration cleanup and added a permanent `windows-latest` integration job. PR #23 merged at `1cdd849126310745652d87f1d100c34aed624079`; PR CI #162 / `33893832128`, main CI #163 / `33894104116`, and the owner's local Windows integration suite all passed.

## Gate D / ADR-004 [PASSED]

PR #24 merged to `main` at `b8649cb563e30af845a0b383103797632bed79a4`. Exact-head CI #164 / `33896004193` passed the Ubuntu full gate and Windows integration.

Gate D freezes the first write-capable trust boundary:

```text
brain requests write
 -> canonical target resolution
 -> deterministic policy
 -> exact Loren-owned owner approval
 -> one-time atomic consume / replay rejection
 -> host-controlled global read-only
 -> write-specific credential resolver
 -> controlled executor
 -> independent post-write verification
 -> correlated redacted audit
```

Authentication proves owner identity; it is **not** write approval. Every first-version real GitHub mutation requires explicit owner approval. Model/external content cannot create approval, broaden it, select credentials, disable read-only, or declare a write verified.

Allowed v0.1 mutation scope after the required M5 foundations are green:

```text
create non-default branch
controlled file/commit path on a non-default branch
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

## M5 Slice 1 — policy + one-time approval foundation [COMPLETE]

PR #25 merged to `main` at `caa65fbbd7c3828b68aa198dad625e73e9c096b4`.

Final evidence:

```text
frozen PR head: c9bfb9f82b70963c196a689d4b0be2feb9bfedb5
PR CI #194 / 33973579862: Ubuntu full gate PASS + Windows integration PASS
post-merge main CI #195 / 33973694524: Ubuntu full gate PASS + Windows integration PASS
```

Delivered:

- typed `ActionAccessClass`: `READ`, `REVERSIBLE_WRITE`, `EXTERNAL_WRITE`, `PRIVILEGED_WRITE`;
- trusted `ActionAuthorizationContext` carrying canonical Project/Repository target outside model-visible action arguments;
- model-visible action arguments and trusted normalized-target data are defensive immutable snapshots, preventing TOCTOU mutation between approval fingerprinting and executor use;
- deterministic SHA-256 action-intent fingerprint over action/access/canonical target/owner/normalized target/model arguments;
- Loren-owned `ApprovalId`, `ActionApproval`, and provider-neutral `IActionApprovalStore`;
- `GateDActionPolicy` and an ActionGateway invariant requiring approval for every non-read action even if a permissive policy accidentally returns `Allow`;
- executor registration is checked before consuming approval, so host misconfiguration cannot burn an otherwise valid approval;
- exact one-time approval consume immediately before the first consequential executor attempt;
- missing, expired, revoked, mismatched, unknown, or replayed approvals fail closed;
- model-visible `approvalId` text has no authority;
- SQLite `ActionApprovals` via migration `202609040003_AddActionApprovals`;
- atomic compare-and-consume with exactly one concurrent winner;
- fail-closed host configuration `LOREN_ENABLE_WRITES`;
- permanent EF migration-drift regression test.

Safe default:

```text
LOREN_ENABLE_WRITES missing/false/malformed -> read-only
LOREN_ENABLE_WRITES=true -> eligible writes may reach approval evaluation
production still has no GitHub mutation executor
```

One important rule is intentional: approval is consumed before the first consequential executor attempt. An independent retry after failure or ambiguity needs fresh approval, preventing one approval from becoming a replay token.

## Current — M5 Slice 2 credential boundary

Before the first real GitHub mutation executor is added, Slice 2 must prove:

- a write-specific credential resolver abstraction;
- an opaque write credential purpose/reference separate from the secret value;
- secret values exist only inside the controlled executor boundary;
- read/write credential purposes remain logically separated;
- missing/revoked credentials fail closed with no broader-token fallback;
- revocation overrides already-approved intent;
- secret redaction across logs, exceptions, audit, action results, and brain context;
- deterministic acceptance tests without a live mutation.

Only after Slices 1–2 are green on `main` does Loren proceed to verified create-branch, controlled file/commit, and open-PR slices.

## Canonical storage

```text
database file: loren.db
default directory: OS local application data / Loren
override: LOREN_DATA_DIRECTORY
migrations: automatic at host startup
```

A fresh database currently has no configured Projects; owner-facing Project CRUD/configuration, memory management, and approval UX remain later v0.1 UI work.

## Run locally

```bash
export LOREN_OWNER_PASSWORD='choose-a-local-owner-password'
export OLLAMA_API_KEY='your-provider-secret'
export LOREN_ENABLE_WRITES='false'
dotnet run --project src/Loren.Web/Loren.Web.csproj
```

PowerShell:

```powershell
$env:LOREN_OWNER_PASSWORD='choose-a-local-owner-password'
$env:OLLAMA_API_KEY='your-provider-secret'
$env:LOREN_ENABLE_WRITES='false'
dotnet run --project src/Loren.Web/Loren.Web.csproj
```

Do not commit real secrets. `LOREN_ENABLE_WRITES=false` is the recommended current posture; setting it true does not create a mutation capability by itself because no production mutation executor exists yet.

## Test

```bash
dotnet restore Loren.slnx
dotnet build Loren.slnx --configuration Release --no-restore
dotnet test Loren.slnx --configuration Release --no-build --no-restore
```

Windows is a first-class integration-test CI platform in addition to the Ubuntu full gate.

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
- [`docs/handoff.md`](docs/handoff.md) — compact continuation checkpoint for a fresh thread
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
