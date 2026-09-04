# Loren Project Status

**Last updated:** 2026-09-04  
**Current version phase:** `v0.1 — Trustworthy Core development`  
**Current decision gates:** `Gate A — PASSED`, `Gate B — PASSED`, `Gate C — PASSED`  
**Current milestone:** `M4 — Trusted Durable Memory`  
**Current execution target:** `M4 Slice 3 — final exact-head gate for authority-aware prepared memory context`

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

Give Loren durable memory with explicit authority/provenance, correction, forgetting, restart survival, bounded prepared retrieval, and poisoning resistance — without turning transcripts, model guesses, or hostile external content into owner truth.

## M4 Slice 1 — Canonical MemoryRecord + OWNER_EXPLICIT persistence [COMPLETE]

PR #18 merged at `78adc287f7ae3744352b7019e3b8a838a5de499e`.

Evidence:

- implementation CI #113 / run `33860641367` — **PASS**;
- final exact-head CI #117 / run `33860985267` — **PASS**;
- post-merge main CI #118 / run `33861089270` — **PASS**.

Delivered Loren-owned `MemoryRecordId`, canonical `MemoryRecord`, all six ADR-003 source classes, Project/Repository scope, provenance, timestamps, supersession lifecycle, EF-neutral `IMemoryStore`, migration `202609040002_AddMemoryRecords`, fail-closed scope validation, and real SQLite restart acceptance.

## M4 Slice 2 — Owner correction + supersession [COMPLETE]

PR #19 merged at:

```text
201b83eff0c6c3143856e348b4c9f029cc14a8b1
```

Evidence:

- implementation CI #119 / run `33861345949` — **PASS**;
- final exact-head CI #123 / run `33861630472` at `3b4247bed05b5df9d0af0df586dbc5597ea508d1` — **PASS**.

Delivered:

```text
old current memory
 -> CorrectAsync(oldId, OWNER_CORRECTION)
 -> append new correction
 -> old.SupersededById = new.Id
 -> one SQLite transaction
 -> current query returns correction only
 -> retained history reconstructs old -> new
```

Correction requires a new ID, current target, identical scope, non-regressing lifecycle time, `OWNER_CORRECTION` authority, and no duplicate ID. Old content is never rewritten. Invalid source/scope/stale corrections fail without partial inserts.

## M4 Slice 3 — Authority-aware prepared memory context [IMPLEMENTED / PR #20]

Current PR: `#20 — feat: add M4 authority-aware prepared memory context`.

Implemented candidate:

```text
canonical Project
        |
current IMemoryStore records
        |
        +--> exclude superseded in store query
        +--> exclude MODEL_INFERENCE / EXTERNAL_CONTENT by default
        +--> deterministic authority ordering
        +--> hard record + character bounds
        |
        v
Loren-owned prepared memory package
        |
        v
BrainContext -> AgentLoop / IBrain
```

Properties:

- application-layer `LorenMemoryContextBuilder`; runtime/brain never receive EF/DbContext;
- default included classes: `OWNER_CORRECTION`, `OWNER_EXPLICIT`, `OWNER_APPROVED_INFERENCE`, `VERIFIED_TOOL`;
- default excluded classes: `MODEL_INFERENCE`, `EXTERNAL_CONTENT`;
- deterministic inclusion order: owner correction, owner explicit, owner-approved inference, verified tool; recency and canonical ID break ties;
- hard `MaxRecords` and `MaxContentCharacters` bounds before model execution;
- prepared entries retain MemoryRecordId, source class, Project/Repository scope, source reference, and timestamps;
- memory payload is explicitly framed as data, not action authorization or policy override;
- `VERIFIED_TOOL` is framed as verified external fact at its recorded source/time, not automatically current external state or owner permission;
- project context and memory context are separate prepared system messages before the user message.

Real SQLite + fake-brain tests prove:

- current `OWNER_CORRECTION` reaches the brain;
- superseded owner memory does not reach the brain;
- `MODEL_INFERENCE` and `EXTERNAL_CONTENT` poison markers do not reach default brain context;
- owner-explicit, owner-approved inference, verified-tool provenance survive into the prepared package;
- hard record/character bounds are deterministic.

CI history:

- CI #125 / run `33864441364`: analyzer caught collection-size assertion style before tests;
- CI #126 / run `33864591806`: build passed; one integration assertion exposed safety-text wording mismatch;
- implementation CI #127 / run `33864695658` at `179a203d6c3d11ff85eb8529d4107ae2edc7f720`: **PASS** across restore, zero-warning build, all tests, format, secret scan, dependency scan, and web/auth smoke.

Slice 3 is **not closed yet**. This documentation-synchronized head must pass exact-head CI before PR #20 merges.

## M4 Slice 4 — Forget/delete semantics [NEXT AFTER MERGE]

Target: explicit owner forget that removes durable memory from future retrieval/context after restart without using a generic audit cascade. The implementation must respect correction chains and keep memory deletion conceptually separate from audit retention per ADR-003.

## M4 Slice 5 — Poisoning / trust-boundary acceptance [FOLLOW-UP]

Prove source/provenance fields are data, not instructions; `MODEL_INFERENCE`/`EXTERNAL_CONTENT` cannot silently become owner truth/policy; `OWNER_APPROVED_INFERENCE` requires explicit promotion semantics; `VERIFIED_TOOL` cannot grant permission; and owner correction remains current owner truth in scope.

## M4 non-goals

Do not add GitHub writes, broad vector infrastructure, generic graph entities, scheduler/background behavior, or v0.2 research capabilities while implementing the memory core.

## Next execution sequence

```text
PR #20 final exact-head CI
 -> merge M4 Slice 3
 -> M4 Slice 4 forget/delete
 -> M4 Slice 5 poisoning/trust acceptance
 -> M4 exit gate
 -> M4 COMPLETE
```

## Progress-update rule

Any merge that changes capability, milestone completion, ADR status, validated dependencies/providers, or next execution target must synchronize:

1. `docs/status.md`;
2. `README.md`;
3. `README.vi.md`;
4. relevant ADRs/plans/roadmap.

A milestone is not closed until implementation/tests and repository documentation agree.
