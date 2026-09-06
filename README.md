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

**Last updated:** 2026-09-06  
**Phase:** `v0.1 — Trustworthy Core development`  
**Completed milestone:** `M4 — Trusted Durable Memory`  
**Passed decision gates:** `Gate A`, `Gate B`, `Gate C`, `Gate D / ADR-004`  
**Current milestone:** `M5 — Action/Credential Boundary + Narrow GitHub Writes`  
**Completed M5 slices:** `Slice 1 — policy/approval/read-only`, `Slice 2 — credential isolation/revocation/redaction`  
**Current checkpoint:** `Slice 3 — explicit-owner verified create-non-default-branch`

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
- M5 Slice 1 — typed action policy, trusted canonical target, exact one-time approval, fail-closed global read-only.
- M5 Slice 2 — write-specific credential resolver, revocation, no fallback, redaction across result/audit/brain boundaries.

Detailed status: [`docs/status.md`](docs/status.md). Fresh-thread continuation checkpoint: [`docs/handoff.md`](docs/handoff.md).

## Gate D / ADR-004 [PASSED]

Gate D freezes the first write-capable trust boundary:

```text
brain requests write
 -> canonical target resolution
 -> deterministic policy / global read-only
 -> explicit exact owner approval
 -> atomic one-time consume / replay rejection
 -> write-specific credential resolver
 -> trusted controlled executor
 -> independent post-write verification
 -> correlated redacted audit
```

Authentication proves owner identity; it is **not** write approval. Model/external content cannot create approval, broaden it, select credentials, disable read-only, select a different canonical repository, or declare a write verified.

Allowed v0.1 mutation scope:

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

## M5 Slice 1 — policy + one-time approval [COMPLETE]

PR #25 merged at `caa65fbbd7c3828b68aa198dad625e73e9c096b4`.

```text
frozen PR head: c9bfb9f82b70963c196a689d4b0be2feb9bfedb5
PR CI #194 / 33973579862: Ubuntu full gate PASS + Windows integration PASS
post-merge main CI #195 / 33973694524: Ubuntu full gate PASS + Windows integration PASS
```

Key properties:

- typed `ActionAccessClass`;
- trusted `ActionAuthorizationContext` outside model-visible arguments;
- immutable snapshots for proposed + trusted normalized target data;
- deterministic SHA-256 exact-intent fingerprint;
- SQLite-backed `ActionApproval` / `IActionApprovalStore`;
- every non-read action requires approval even if policy accidentally returns `Allow`;
- executor existence is checked before approval consumption;
- exact atomic one-time consume immediately before the consequential executor attempt;
- missing/expired/revoked/mismatched/replayed approval fails closed;
- model-visible `approvalId` text has no authority;
- `LOREN_ENABLE_WRITES` defaults to read-only.

Approval is intentionally consumed before the first consequential executor attempt. A retry after failure/ambiguity needs fresh approval.

## M5 Slice 2 — credential boundary [COMPLETE]

PR #26 merged at `f7fb36bae324dbd7bb8d12e02daf3fe0dd98e7da`.

```text
frozen PR head: e9e2b07378e1435e62e6090829619603ac7df42b
PR CI #201 / 34027113298: Ubuntu full gate PASS + Windows integration PASS
post-merge main CI #202 / 34027255592: Ubuntu full gate PASS + Windows integration PASS
```

Key properties:

- provider-neutral `CredentialPurpose` / `CredentialReference`;
- dedicated GitHub write identity `github.write / github.write.local-v0.1`;
- local secret contract `GITHUB_WRITE_TOKEN`;
- `LOREN_GITHUB_WRITE_CREDENTIAL_REVOKED=true` overrides an already-approved intent;
- malformed revocation state fails closed;
- no fallback to `OLLAMA_API_KEY`, read credentials, or a broader token;
- secret material exists only inside the credential-bound executor callback;
- executor result/exception data is redacted before gateway/audit/brain consumption.

## M5 Slice 3 — verified create branch [CURRENT CHECKPOINT]

The first real mutation is intentionally narrow:

```text
authenticated owner
 -> explicit “Approve & create branch”
 -> resolve canonical Project + GitHub Repository
 -> freeze exact branch + existing 40-char source SHA
 -> create 5-minute exact ActionApproval
 -> policy + trusted-executor check
 -> fingerprint + atomic consume
 -> github.write credential resolution
 -> GET repository/default branch preflight
 -> reject default/unsafe branch
 -> POST git/refs
 -> GET exact created ref
 -> require verified SHA == approved source SHA
 -> return redacted result + audit
```

Slice 3 introduces `ITrustedActionExecutor`: a non-read executor must receive Loren-owned `ActionExecutionRequest`, not only model-visible `ActionRequest`. A legacy non-read executor is rejected before approval is burned. Canonical GitHub owner/repository comes from `ActionAuthorizationContext.RepositoryLocator`; write execution does not trust model-proposed repository identity.

The owner console now includes:

- a bootstrap form to save one canonical GitHub Project/Repository into a fresh local database;
- an explicit `Approve & create branch` form;
- the existing read/chat console and audit display.

Deterministic acceptance covers request order, safe ref validation, default-branch rejection, exact SHA validation, verification mismatch, credential redaction, owner approval consumption, and revoked-credential zero-HTTP behavior.

## Canonical storage

```text
database file: loren.db
default directory: OS local application data / Loren
override: LOREN_DATA_DIRECTORY
migrations: automatic at host startup
```

## Run locally — read-only

PowerShell:

```powershell
$env:LOREN_OWNER_PASSWORD='choose-a-local-owner-password'
$env:OLLAMA_API_KEY='your-provider-secret'
$env:LOREN_ENABLE_WRITES='false'
dotnet run --project src/Loren.Web/Loren.Web.csproj
```

Bash:

```bash
export LOREN_OWNER_PASSWORD='choose-a-local-owner-password'
export OLLAMA_API_KEY='your-provider-secret'
export LOREN_ENABLE_WRITES='false'
dotnet run --project src/Loren.Web/Loren.Web.csproj
```

## Run the first write checkpoint

Only enable this when you intentionally want to test branch creation on the configured repository.

PowerShell:

```powershell
$env:LOREN_OWNER_PASSWORD='choose-a-local-owner-password'
$env:LOREN_ENABLE_WRITES='true'
$env:GITHUB_WRITE_TOKEN='your-write-token'
$env:LOREN_GITHUB_WRITE_CREDENTIAL_REVOKED='false'
dotnet run --project src/Loren.Web/Loren.Web.csproj
```

Bash:

```bash
export LOREN_OWNER_PASSWORD='choose-a-local-owner-password'
export LOREN_ENABLE_WRITES='true'
export GITHUB_WRITE_TOKEN='your-write-token'
export LOREN_GITHUB_WRITE_CREDENTIAL_REVOKED='false'
dotnet run --project src/Loren.Web/Loren.Web.csproj
```

Then sign in to the owner console, bootstrap the canonical repository if the database is empty, enter an **existing exact 40-character source commit SHA**, choose a **new non-default branch name**, review the confirmation, and press **Approve & create branch**.

Do not commit real secrets. `OLLAMA_API_KEY` and `GITHUB_WRITE_TOKEN` are intentionally separate credentials.

## Test

```bash
dotnet restore Loren.slnx
dotnet build Loren.slnx --configuration Release --no-restore
dotnet test Loren.slnx --configuration Release --no-build --no-restore
```

Windows is a first-class integration-test CI platform in addition to the Ubuntu full gate.

## Next narrow M5 target

After the create-branch checkpoint is green on `main`:

```text
controlled file/commit path on an approved non-default branch
 -> bind exact path/content/branch intent
 -> forbid default-branch write
 -> verify commit SHA + branch ref + content identity
```

Open-PR write capability comes after that slice.

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
- [`docs/decisions/004-action-approval-and-credential-boundary.md`](docs/decisions/004-action-approval-and-credential-boundary.md)

This repository is the source of truth for Loren's product decisions, architecture, delivery plans, implementation, progress, and release history.
