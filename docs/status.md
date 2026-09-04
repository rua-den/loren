# Loren Project Status

**Last updated:** 2026-09-04  
**Current version phase:** `v0.1 — Trustworthy Core development`  
**Current decision gates:** `Gate A — PASSED`, `Gate B — PASSED`, `Gate C — PASSED`; `Gate D — NEXT`  
**Completed milestone:** `M4 — Trusted Durable Memory`  
**Current execution target:** `Gate D — Action/Credential Policy before M5 external writes`

This file is the authoritative progress ledger for the repository. `README.md` and `README.vi.md` summarize it.

No GitHub write path exists yet. Gate D remains mandatory before the first real external write.

---

# Completed milestones

- `v0.0 — Architecture / Feasibility` — complete.
- `M1 — Engineering Foundation` — complete.
- `M2 — Walking Skeleton` — complete.
- `M3 — Canonical Project/Repository State` — complete.
- `M4 — Trusted Durable Memory` — complete when the M4 exit changeset in PR #22 is present on `main`.

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

PR #21 merged at:

```text
87b5a39ccae7c931de9668fed5283a4742be73f7
```

Evidence:
- implementation CI #133 / `33865419023` — **PASS**;
- final exact-head CI #137 / `33865716479` at `04104efd90c47216275544812dbc27284408920b` — **PASS**.

For a correction chain `A -> B -> C(current)`, `ForgetAsync(C)` validates a same-scope linear history and physically purges A, B, C in one SQLite transaction. This prevents older corrected claims from resurrecting. Restart tests prove the whole forgotten chain stays absent while unrelated memory survives. Forgetting does not cascade into audit retention.

## Slice 5 — Poisoning / trust-boundary acceptance [COMPLETE IN M4 EXIT CHANGESET]

PR #22 closes the M4 trust gate.

Production hardening:
- records entering trusted prepared context require non-empty provenance;
- provenance/source references are independently bounded;
- the whole serialized memory payload — content, provenance, IDs, scope, timestamps — is explicitly inert data, never instructions, permission, policy, or action authorization;
- `MODEL_INFERENCE` and `EXTERNAL_CONTENT` remain excluded even with owner-looking provenance;
- `OWNER_APPROVED_INFERENCE` and `VERIFIED_TOOL` require provenance;
- `VERIFIED_TOOL` does not automatically represent current external state or grant owner permission.

Adversarial acceptance proves:
- spoofed model/external records cannot enter trusted prepared context;
- unproven approved-inference/tool records are excluded;
- explicit owner-approved inference stays distinguishable from ordinary model inference;
- owner correction wins current owner truth while superseded and conflicting untrusted records stay out;
- malicious provenance text is bounded and treated as data;
- `MODEL_INFERENCE` cannot use the correction boundary even with owner-looking provenance;
- a normal `LorenRunService` turn reads prepared memory without calling `AddAsync`, `CorrectAsync`, or `ForgetAsync`.

CI history:
- CI #139 / `33866064004`: zero-warning build passed; two wording assertions exposed safety-text phrasing mismatches;
- hardening commit `8b3c6c2a45ad06046ac8150efe8322a362cdac0b` clarified the boundary;
- implementation CI #140 / `33866182751` — **PASS** across restore, zero-warning build, all tests, format, secret scan, dependency scan, and web/auth smoke.

## M4 exit gate [PASSED]

M4 now proves:
- owner-explicit memory survives restart with canonical identity and provenance;
- owner correction appends/supersedes without destructive content rewrite;
- current retrieval excludes superseded claims;
- owner forget purges the full correction chain and stays forgotten after restart;
- runtime receives a small, inspectable, bounded prepared package rather than database access;
- low-authority model/external content cannot silently become trusted owner state;
- ordinary runtime turns do not silently mutate durable memory;
- memory forgetting and audit retention remain separate concepts;
- deterministic tests work without a live model.

**M4 is complete once PR #22 is merged to `main`.**

---

# Next milestone — M5 Action/Credential Boundary + Narrow GitHub Writes

Before implementing the first real GitHub write, **Gate D must pass** and lock:
- write action contracts and policy dimensions;
- exact approval binding and non-replay rules;
- write credential storage/resolution and read/write separation;
- secret redaction, rotation, and revocation;
- global read-only / kill behavior;
- post-write verification and audit expectations.

Only after Gate D may M5 add the narrow v0.1 write set: create branch, create/update file/commit path, commit, and open pull request. Merge-main, force-push, repository deletion/admin changes, and production deploy remain outside the v0.1 write scope.

## Progress-update rule

Any merge that changes capability, milestone completion, ADR status, validated dependencies/providers, or next execution target must synchronize:
1. `docs/status.md`;
2. `README.md`;
3. `README.vi.md`;
4. relevant ADRs/plans/roadmap.

A milestone is not closed until implementation/tests and repository documentation agree.
