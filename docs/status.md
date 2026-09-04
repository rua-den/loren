# Loren Project Status

**Last updated:** 2026-09-04  
**Current version phase:** `v0.1 — Trustworthy Core development`  
**Current decision gates:** `Gate A — PASSED`, `Gate B — PASSED`, `Gate C — PASSED`, `Gate D — PASSED via ADR-004`  
**Completed milestone:** `M4 — Trusted Durable Memory`  
**Current milestone:** `M5 — Action/Credential Boundary + Narrow GitHub Writes`  
**Current execution target:** `M5 Slice 1 — typed write policy context + one-time approval boundary + global read-only control`

This file is the authoritative progress ledger for the repository. `README.md` and `README.vi.md` summarize it.

Gate D authorizes M5 implementation only. No GitHub write path exists on `main` until the corresponding M5 implementation slices are merged and validated.

---

# Completed milestones

- `v0.0 — Architecture / Feasibility` — complete.
- `M1 — Engineering Foundation` — complete.
- `M2 — Walking Skeleton` — complete.
- `M3 — Canonical Project/Repository State` — complete.
- `M4 — Trusted Durable Memory` — complete.

---

# M4 completion evidence

M4 gives Loren durable, provider-independent memory with provenance, owner correction, forgetting, bounded prepared retrieval, and explicit poisoning resistance.

## Slice 1 — OWNER_EXPLICIT persistence [COMPLETE]

PR #18 merged at `78adc287f7ae3744352b7019e3b8a838a5de499e`.

Evidence:
- implementation CI #113 / `33860641367` — **PASS**;
- final exact-head CI #117 / `33860985267` — **PASS**;
- post-merge main CI #118 / `33861089270` — **PASS**.

Delivered Loren-owned `MemoryRecordId`, canonical `MemoryRecord`, all six ADR-003 source classes, Project/Repository scope, provenance, timestamps, supersession lifecycle, EF-neutral `IMemoryStore`, migration `202609040002_AddMemoryRecords`, fail-closed scope validation, and real SQLite restart acceptance.

## Slice 2 — Owner correction + supersession [COMPLETE]

PR #19 merged at `201b83eff0c6c3143856e348b4c9f029cc14a8b1`.

Evidence:
- implementation CI #119 / `33861345949` — **PASS**;
- final exact-head CI #123 / `33861630472` — **PASS**.

Correction is explicit append + supersede in one SQLite transaction. Old content is preserved. Invalid authority, changed scope, stale target, duplicate replacement, and partial failure all fail closed.

## Slice 3 — Authority-aware prepared memory context [COMPLETE]

PR #20 merged at `732b85db3a799638bcd73558f98232b276f3cb5e`.

Evidence:
- implementation CI #127 / `33864695658` — **PASS**;
- final exact-head CI #131 / `33864946328` — **PASS**.

Prepared-memory path:

```text
canonical Project
 -> current IMemoryStore records
 -> exclude superseded
 -> source/provenance filtering
 -> deterministic authority ordering
 -> hard record/content/provenance bounds
 -> Loren-owned prepared memory package
 -> BrainContext -> AgentLoop / IBrain
```

Default included classes are `OWNER_CORRECTION`, `OWNER_EXPLICIT`, `OWNER_APPROVED_INFERENCE`, and `VERIFIED_TOOL`. `MODEL_INFERENCE` and `EXTERNAL_CONTENT` are excluded from default model context.

## Slice 4 — Owner forget/delete [COMPLETE]

PR #21 merged at `87b5a39ccae7c931de9668fed5283a4742be73f7`.

Evidence:
- implementation CI #133 / `33865419023` — **PASS**;
- final exact-head CI #137 / `33865716479` — **PASS**.

For a correction chain `A -> B -> C(current)`, `ForgetAsync(C)` validates a same-scope linear history and physically purges A, B, C in one SQLite transaction. This prevents older corrected claims from resurrecting. Restart tests prove the whole forgotten chain stays absent while unrelated memory survives. Forgetting does not cascade into audit retention.

## Slice 5 — Poisoning / trust-boundary acceptance [COMPLETE]

PR #22 merged at `41396bf0f78b109d0af8f562039ce5f5cf1ad787`.

Production hardening:
- records entering trusted prepared context require non-empty provenance;
- provenance/source references are independently bounded;
- the whole serialized memory payload — content, provenance, IDs, scope, timestamps — is explicitly inert data, never instructions, permission, policy, or action authorization;
- `MODEL_INFERENCE` and `EXTERNAL_CONTENT` remain excluded even with owner-looking provenance;
- `OWNER_APPROVED_INFERENCE` and `VERIFIED_TOOL` require provenance;
- `VERIFIED_TOOL` does not automatically represent current external state or grant owner permission.

Adversarial acceptance proves spoofing, correction-boundary abuse, malicious provenance, and silent runtime memory mutation fail closed.

Evidence:
- implementation CI #140 / `33866182751` — **PASS**;
- final exact-head CI #148 / `33870438763` — **PASS**;
- post-merge main CI #149 / `33870545850` — **PASS**.

## Windows integration hardening [COMPLETE]

Owner local testing on Windows exposed SQLite temp-file cleanup behavior hidden by Linux unlink semantics. PR #23 disabled SQLite pooling only for temp integration-test databases and added a permanent `windows-latest` integration job.

PR #23 merged at `1cdd849126310745652d87f1d100c34aed624079`.

Evidence:
- PR CI #162 / `33893832128` — Ubuntu full gate **PASS**, Windows integration **PASS**;
- post-merge main CI #163 / `33894104116` — Ubuntu full gate **PASS**, Windows integration **PASS**;
- owner local Windows full integration suite — **PASS**.

---

# Gate D — Action/Credential Policy [PASSED]

**Decision:** ADR-004 — Action Approval and Credential Boundary.

Gate D locks the first write-capable trust boundary:

- action contracts gain explicit Loren-owned read/write/risk semantics instead of relying only on action names or model text;
- write policy resolves canonical Project/Repository identity and security-relevant target parameters before authorization;
- every real v0.1 GitHub write requires explicit authenticated-owner approval;
- authentication alone is not approval;
- approval is a Loren-owned artifact bound to exact normalized action intent, expiry/task scope, and one-time atomic consumption;
- consumed/expired/mismatched/revoked approvals cannot be replayed;
- write credentials resolve only behind the authorized executor boundary and never enter brain context, memory, action parameters, audit payloads, or owner-visible results;
- read/write credential purposes remain separated;
- global read-only mode is fail-closed and defaults safe when write-enable configuration is absent/invalid;
- credential revocation overrides prior approval;
- external writes are not successful until a deterministic post-write read verifies the intended postcondition;
- audit must reconstruct request -> policy -> approval -> credential purpose -> execution -> verification without storing secrets;
- external/model content cannot create approval, expand scope, change read-only mode, select credentials, or mark verification successful.

Allowed M5 v0.1 write set after the required implementation boundaries are tested:

```text
create non-default branch
create/update file via controlled commit path on non-default branch
create commit/update ref only as required by that path
open pull request
```

Still forbidden in v0.1:

```text
write directly to default branch
merge pull request
force push / history rewrite
delete repo/branch/data
repo admin/security changes
secret-management actions
production deployment
```

Gate D passing does **not** itself enable a GitHub mutation. It authorizes M5 implementation.

---

# Current milestone — M5 Action/Credential Boundary + Narrow GitHub Writes

## M5 Slice 1 — policy + approval foundation [ACTIVE]

First implementation target:

1. typed action access/risk classification and resolved policy context;
2. canonical Project/Repository target binding;
3. Loren-owned `ApprovalId` / one-time approval artifact and exact-request fingerprint;
4. atomic approval consumption / replay rejection;
5. fail-closed global read-only control;
6. deterministic tests proving model/external content cannot manufacture approval.

No real GitHub mutation is enabled in Slice 1.

## M5 Slice 2 — credential boundary

Planned next:

- write-specific credential resolver abstraction;
- secret value never crosses executor boundary;
- missing/revoked credential fails closed;
- redaction tests for request, exception, audit and owner-visible result surfaces.

## M5 Slice 3+ — narrow verified GitHub writes

Only after Slices 1–2 are green:

```text
create branch -> verify ref/SHA
controlled file/commit path -> verify commit/ref/content identity
open PR -> verify repo/base/head/state/PR identity
```

Each write requires matching one-time owner approval and correlated audit.

---

## Progress-update rule

Any merge that changes capability, milestone completion, ADR status, validated dependencies/providers, or next execution target must synchronize:
1. `docs/status.md`;
2. `README.md`;
3. `README.vi.md`;
4. relevant ADRs/plans/roadmap.

A milestone is not closed until implementation/tests and repository documentation agree.
