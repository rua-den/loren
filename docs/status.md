# Loren Project Status

**Last updated:** 2026-09-04  
**Current version phase:** `v0.1 — Trustworthy Core development`  
**Current decision gates:** `Gate A — PASSED`, `Gate B — PASSED`, `Gate C — PASSED`  
**Current milestone:** `M4 — Trusted Durable Memory`  
**Current execution target:** `M4 Slice 2 — final exact-head gate for owner correction/supersession`

This file is the authoritative progress ledger for the repository. `README.md` and `README.vi.md` summarize it.

## Completed milestones

- `v0.0 — Architecture / Feasibility` — complete.
- `M1 — Engineering Foundation` — complete.
- `M2 — Walking Skeleton` — complete.
- `M3 — Canonical Project/Repository State` — complete.

No GitHub write path exists yet. Gate D remains mandatory before external writes.

---

# M3 completion summary

M3 delivered provider-independent Project/Repository identity, deterministic aliases, prepared EF-neutral runtime context, and Gate C / ADR-003. Gate C merged in PR #17 at `69223e8c4923510bb26fa50f77a3c44c1683b172` after exact-head CI #110 / run `33860095412` passed.

---

# Current milestone — M4 Trusted Durable Memory

## Goal

Give Loren durable memory with provenance/authority, correction, deletion semantics, restart survival, and safe prepared retrieval — without turning transcripts, model guesses, or hostile external content into owner truth.

## M4 Slice 1 — Canonical MemoryRecord + OWNER_EXPLICIT persistence [COMPLETE]

PR #18 merged at:

```text
78adc287f7ae3744352b7019e3b8a838a5de499e
```

Evidence:

- implementation CI #113 / run `33860641367` — **PASS**;
- final documentation-synchronized exact-head CI #117 / run `33860985267` at `899fddc0f30ef29cb4dad6241aeb4b2da6b568be` — **PASS**;
- post-merge main CI #118 / run `33861089270` — **PASS**.

Delivered:

- Loren-owned `MemoryRecordId`;
- canonical `MemoryRecord` and all six ADR-003 `MemorySourceClass` values;
- Project/Repository scope, source reference/provenance, timestamps, and `SupersededById` lifecycle representation;
- EF-neutral `IMemoryStore`;
- no generic content-update API;
- SQLite `MemoryRecords` schema + migration `202609040002_AddMemoryRecords`;
- restrictive Project/Repository/self-supersession foreign keys;
- fail-closed repository/project scope validation;
- production DI registration;
- real SQLite OWNER_EXPLICIT save -> restart -> retrieve acceptance.

Slice 1 acceptance proves canonical memory ID, content, authority, provenance, scope, timestamps, and current state survive restart independently of provider/runtime session state.

## M4 Slice 2 — Owner correction + supersession [IMPLEMENTED / PR #19]

Current PR: `#19 — feat: add M4 owner memory correction supersession`.

Implemented correction boundary:

```text
old current memory
        |
explicit CorrectAsync(oldId, OWNER_CORRECTION)
        |
        +--> append new correction record
        +--> old.SupersededById = new.Id
        |
        v
one SQLite transaction
        |
        v
current query returns correction only
history still reconstructs old -> new
```

Properties:

- `IMemoryStore.CorrectAsync(...)` is an explicit business mutation; generic destructive content update remains absent;
- replacement must use `OWNER_CORRECTION`;
- replacement must have a new `MemoryRecordId` and begin current;
- target must exist and still be current;
- Project/Repository scope must remain exactly the same;
- correction lifecycle timestamp cannot move backward;
- correction ID must not already exist;
- append + supersession lifecycle mutation is transactional;
- old content is never rewritten;
- default `ListCurrentForProjectAsync` excludes superseded records;
- model inference passed as a correction source is rejected before mutation;
- stale/already-superseded target is rejected;
- failed scope/source/stale corrections leave no partial replacement record.

Real SQLite tests prove:

- OWNER_EXPLICIT -> OWNER_CORRECTION survives restart;
- old content and old ID remain reconstructable;
- old record points to the correction via `SupersededById`;
- correction remains current with `OWNER_CORRECTION` authority and owner provenance;
- current query returns only the correction;
- invalid source, changed scope, and stale-target correction attempts fail closed without partial inserts.

Implementation CI #119 / run `33861345949` — **PASS** across restore, zero-warning build, tests, format, secret scan, dependency scan, and web/auth smoke.

Slice 2 is **not closed yet**. Documentation is now synchronized; the final PR head must pass exact-head CI again before merge.

## M4 Slice 3 — Authority-aware retrieval + prepared memory context [NEXT AFTER MERGE]

Next target:

- retrieve only current durable memories for the canonical Project/Repository scope;
- preserve source authority/provenance in an application-owned prepared structure;
- define deterministic authority ordering/filtering needed for the first product flow;
- bound the number/size of memory records inserted into model context;
- feed prepared memory context to `AgentLoop` / `IBrain` without EF/DbContext;
- prove superseded records do not enter prepared context by default.

## M4 follow-up

- Slice 4: owner forget/delete semantics separated from audit retention;
- Slice 5: poisoning/trust-boundary acceptance for model inference and external content.

## M4 non-goals

Do not add GitHub writes, broad vector infrastructure, generic graph entities, scheduler/background behavior, or v0.2 research capabilities while implementing the memory core.

## Next execution sequence

```text
PR #19 final exact-head CI
 -> merge M4 Slice 2
 -> M4 Slice 3 authority-aware prepared memory context
 -> M4 Slice 4 forget/delete
 -> M4 Slice 5 poisoning/trust acceptance
 -> M4 COMPLETE
```

## Progress-update rule

Any merge that changes capability, milestone completion, ADR status, validated dependencies/providers, or next execution target must synchronize:

1. `docs/status.md`;
2. `README.md`;
3. `README.vi.md`;
4. relevant ADRs/plans/roadmap.

A milestone is not closed until implementation/tests and repository documentation agree.
