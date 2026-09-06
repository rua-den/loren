# Loren Project Status

**Last updated:** 2026-09-06  
**Current version phase:** `v0.1 — Trustworthy Core development`  
**Current decision gates:** `Gate A — PASSED`, `Gate B — PASSED`, `Gate C — PASSED`, `Gate D — PASSED via ADR-004`  
**Completed milestone:** `M4 — Trusted Durable Memory`  
**Current milestone:** `M5 — Action/Credential Boundary + Narrow GitHub Writes`  
**Completed M5 slices:** `Slice 1 — typed action policy + one-time approval + global read-only`, `Slice 2 — write credential isolation/revocation/redaction`  
**Current execution target:** `M5 Slice 3 — explicit-owner verified create-non-default-branch checkpoint`

This file is the authoritative progress ledger for the repository. `README.md` and `README.vi.md` summarize it. For fresh-thread continuation, read [`handoff.md`](handoff.md) immediately after this file.

Gate D authorizes M5 implementation only. Slices 1–2 are merged and green on `main`. Slice 3 is the first deliberately narrow real GitHub mutation capability: create a non-default branch from an exact approved source commit SHA and report success only after an independent ref/SHA readback matches.

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

- PR #18 merged at `78adc287f7ae3744352b7019e3b8a838a5de499e`; final CI #117 / `33860985267`, post-merge #118 / `33861089270` — **PASS**.
- PR #19 merged at `201b83eff0c6c3143856e348b4c9f029cc14a8b1`; final CI #123 / `33861630472` — **PASS**.
- PR #20 merged at `732b85db3a799638bcd73558f98232b276f3cb5e`; final CI #131 / `33864946328` — **PASS**.
- PR #21 merged at `87b5a39ccae7c931de9668fed5283a4742be73f7`; final CI #137 / `33865716479` — **PASS**.
- PR #22 merged at `41396bf0f78b109d0af8f562039ce5f5cf1ad787`; final CI #148 / `33870438763`, post-merge #149 / `33870545850` — **PASS**.
- PR #23 merged at `1cdd849126310745652d87f1d100c34aed624079`; PR CI #162 / `33893832128`, post-merge main CI #163 / `33894104116`, and owner local Windows integration suite — **PASS**.

M4 proves owner memory survives restart; correction preserves append/supersede history/current-truth semantics; full correction chains can be forgotten without resurrection; prepared memory is bounded/inspectable; model/external content cannot silently become owner truth; ordinary runtime turns do not silently mutate durable memory; and memory deletion remains separate from audit retention.

---

# Gate D — Action/Credential Policy [PASSED]

**Decision:** ADR-004 — Action Approval and Credential Boundary.  
**PR:** #24  
**Merge:** `b8649cb563e30af845a0b383103797632bed79a4`  
**Validation:** exact-head CI #164 / `33896004193` — Ubuntu full gate + Windows integration **PASS**.

Gate D locks the first write-capable trust contract:

- explicit Loren-owned action read/write/risk semantics;
- canonical Project/Repository target binding before authorization;
- every real v0.1 GitHub mutation requires explicit authenticated-owner approval;
- authentication alone is not approval;
- approval is exact normalized-request-bound, expiring/task-bounded, atomically one-time, and non-replayable;
- write credentials resolve only behind the authorized executor boundary and remain outside brain context, memory, model-visible action parameters, audit payloads, and owner-visible results;
- read/write credential purposes remain separated;
- global read-only is host-controlled and fail-closed when write-enable configuration is absent/invalid;
- credential revocation overrides prior approval;
- external writes require deterministic post-write verification before success;
- audit must reconstruct request -> policy -> approval -> credential purpose -> execution -> verification without secrets;
- model/external content cannot create approval, expand scope, change read-only mode, choose credentials, or declare verification success.

Allowed M5 v0.1 mutation scope:

```text
create non-default branch
controlled file/commit path on non-default branch
open pull request
```

Still forbidden:

```text
direct default-branch write
merge pull request
force push / history rewrite
delete repo/branch/data
repo admin/security changes
secret-management actions
production deployment
```

---

# Current milestone — M5 Action/Credential Boundary + Narrow GitHub Writes

## M5 Slice 1 — policy + one-time approval foundation [COMPLETE]

PR #25 is merged to `main` at:

```text
caa65fbbd7c3828b68aa198dad625e73e9c096b4
```

Final validation evidence:

```text
final frozen PR head: c9bfb9f82b70963c196a689d4b0be2feb9bfedb5
PR CI #194 / 33973579862: Ubuntu full gate PASS, Windows integration PASS
squash merge: caa65fbbd7c3828b68aa198dad625e73e9c096b4
post-merge main CI #195 / 33973694524: Ubuntu full gate PASS, Windows integration PASS
```

Delivered:

- `ActionAccessClass`: `READ`, `REVERSIBLE_WRITE`, `EXTERNAL_WRITE`, `PRIVILEGED_WRITE`, with legacy `IsReadOnly` compatibility;
- trusted `ActionAuthorizationContext` carrying canonical `ProjectId`, `RepositoryId`, repository locator, owner principal reference, and normalized target outside model-visible action arguments;
- model-visible `ActionRequest.Arguments` and trusted normalized-target dictionaries are defensively copied into immutable/frozen snapshots so approved intent cannot be changed between fingerprinting and executor use;
- deterministic SHA-256 `ActionIntentFingerprint` binding action/access class, canonical target, owner principal, normalized target fields, and sorted model arguments;
- Loren-owned `ApprovalId`, `ActionApproval`, lifecycle/status types, and EF-neutral `IActionApprovalStore`;
- `GateDActionPolicy`: reads allowed, privileged writes denied, missing trusted canonical context denied, global read-only denied, eligible writes require approval;
- `ActionGateway` independently requires approval for every non-read action even if a permissive policy accidentally returns `Allow`;
- executor registration is confirmed before approval consumption, so host misconfiguration cannot burn a valid approval without an executor attempt;
- exact fingerprint recomputation + atomic approval consume immediately before the first consequential executor invocation;
- missing/expired/revoked/mismatched/replayed approval fails closed;
- model-visible text such as an `approvalId` argument cannot substitute for trusted `ActionExecutionRequest.ApprovalId`;
- `ApprovalEvaluated` audit event without secret payloads;
- SQLite `ActionApprovals` persistence via migration `202609040003_AddActionApprovals`;
- atomic compare-and-consume so concurrent attempts have exactly one winner;
- restart, expiry, revocation, mismatch, replay, concurrent-consume, policy-bypass, fake-model-approval, mutable-input TOCTOU, and missing-executor acceptance tests;
- permanent canonical migration-drift regression test comparing the EF migration snapshot with the current design-time model;
- production host uses `GateDActionPolicy`, scoped approval persistence/gateway/runtime composition, and fail-closed `LOREN_ENABLE_WRITES` semantics.

Important non-replay rule: approval is consumed before the first consequential executor attempt. An independent retry after failure/ambiguity requires fresh approval.

## M5 Slice 2 — credential boundary [COMPLETE]

PR #26 merged to `main` at:

```text
f7fb36bae324dbd7bb8d12e02daf3fe0dd98e7da
```

Validation evidence:

```text
frozen PR head: e9e2b07378e1435e62e6090829619603ac7df42b
PR CI #201 / 34027113298: Ubuntu full gate PASS, Windows integration PASS
post-merge main CI #202 / 34027255592: Ubuntu full gate PASS, Windows integration PASS
```

Delivered:

- provider-neutral `CredentialPurpose`, `CredentialReference`, resolution status, and secret-bearing `CredentialLease` with no public secret property;
- exact environment binding for the dedicated GitHub write identity `github.write / github.write.local-v0.1`;
- local v0.1 host contract `GITHUB_WRITE_TOKEN` plus `LOREN_GITHUB_WRITE_CREDENTIAL_REVOKED`;
- no fallback to `OLLAMA_API_KEY`, read credentials, or broader credential identities;
- missing, revoked, malformed revocation state, and unconfigured purpose/reference fail closed;
- `CredentialBoundActionExecutor` materializes the secret only inside the consequential executor callback;
- executor results and exceptions are redacted before returning to gateway/audit/brain boundaries;
- resolver exception detail is not propagated;
- acceptance tests prove secret values do not escape into result/audit/brain serialization;
- Slice 2 production composition registered the resolver while intentionally registering no mutation executor until Slice 3.

## M5 Slice 3 — verified create-non-default-branch checkpoint [CURRENT]

The first real write slice is intentionally narrow. Its capability contract is:

```text
authenticated owner
 -> explicit “Approve & create branch” request
 -> resolve canonical Project + GitHub Repository
 -> freeze exact branch + 40-char source commit SHA
 -> create exact 5-minute one-time ActionApproval
 -> ActionGateway policy + fingerprint + atomic consume
 -> trusted-execution-only write executor
 -> resolve github.write credential
 -> GET repository/default-branch preflight
 -> reject default branch / unsafe Git ref
 -> POST git/refs
 -> GET exact created ref
 -> require verified SHA == approved source SHA
 -> redacted ActionResult + correlated audit
```

Slice 3 implementation includes:

- `ITrustedActionExecutor`: non-read actions must receive Loren-owned `ActionExecutionRequest`, not only model-visible `ActionRequest`;
- `ActionGateway` refuses a non-read legacy executor before approval consumption;
- `CredentialBoundActionExecutor` refuses direct untrusted invocation and materializes credentials only for trusted execution;
- `github.create_branch` typed as `REVERSIBLE_WRITE`;
- canonical repository locator comes from Loren state, never from model/request-controlled owner/repo strings at execution time;
- exact branch + source SHA are duplicated into trusted normalized target and action arguments, fingerprinted, then re-compared at executor boundary;
- Git ref safety validation and exact 40-hex source SHA requirement;
- default branch creation/replacement is forbidden before mutation;
- independent post-write ref/SHA verification is mandatory for success;
- credential missing/revoked fails closed after approval consumption but before external HTTP mutation;
- authenticated owner console can bootstrap a canonical GitHub project on an empty local database;
- owner console exposes an explicit `Approve & create branch` operation; authentication alone still does not authorize writes;
- deterministic HTTP tests cover request order, default-branch rejection, unsafe refs, verification mismatch, and secret non-leakage;
- owner-workflow acceptance covers project resolution -> approval creation/consume -> credential boundary -> verified write, plus revoked-credential no-HTTP behavior.

Safe host behavior:

```text
LOREN_ENABLE_WRITES missing/false/malformed -> write denied before approval execution
LOREN_ENABLE_WRITES=true + missing GITHUB_WRITE_TOKEN -> approval may be consumed, external mutation does not occur
LOREN_GITHUB_WRITE_CREDENTIAL_REVOKED=true -> approval may be consumed, external mutation does not occur
successful github.create_branch -> only after exact post-write ref/SHA verification
```

Local first-checkpoint test requires:

```text
LOREN_OWNER_PASSWORD=<local password>
LOREN_ENABLE_WRITES=true
GITHUB_WRITE_TOKEN=<GitHub token allowed to create refs in the target repo>
LOREN_GITHUB_WRITE_CREDENTIAL_REVOKED=false
```

Then use the owner console to bootstrap the target GitHub repository if the local database is empty, obtain an exact existing 40-character source commit SHA, and press `Approve & create branch` with a new non-default branch name.

Next narrow M5 target after this checkpoint is green on `main`:

```text
controlled file/commit path on the approved non-default branch
 -> verify commit/ref/content identity
```

---

## Progress-update rule

Any merge that changes capability, milestone completion, ADR status, validated dependencies/providers, or next execution target must synchronize:
1. `docs/status.md`;
2. `README.md`;
3. `README.vi.md`;
4. relevant ADRs/plans/roadmap;
5. `docs/handoff.md` when a fresh-thread continuation point changes.

A milestone or slice is not closed until implementation/tests and repository documentation agree.
