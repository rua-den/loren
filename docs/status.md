# Loren Project Status

**Last updated:** 2026-09-04  
**Current version phase:** `v0.1 — Trustworthy Core development`  
**Current decision gates:** `Gate A — PASSED`, `Gate B — PASSED`, `Gate C — PASSED`  
**Current milestone:** `M4 — Trusted Durable Memory`  
**Current execution target:** `M4 Slice 4 — final exact-head gate for owner forget/purge`

This file is the authoritative progress ledger for the repository. `README.md` and `README.vi.md` summarize it.

## Completed milestones

- `v0.0 — Architecture / Feasibility` — complete.
- `M1 — Engineering Foundation` — complete.
- `M2 — Walking Skeleton` — complete.
- `M3 — Canonical Project/Repository State` — complete.

No GitHub write path exists yet. Gate D remains mandatory before external writes.

---

# Current milestone — M4 Trusted Durable Memory

## Goal

Give Loren durable memory with explicit authority/provenance, correction, forgetting, restart survival, bounded prepared retrieval, and poisoning resistance — without turning transcripts, model guesses, or hostile external content into owner truth.

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

Correction is explicit append + supersede in one transaction. Old content is preserved. Invalid authority, changed scope, stale target, duplicate replacement, and partial failure all fail closed.

## Slice 3 — Authority-aware prepared memory context [COMPLETE]

PR #20 merged at:

```text
732b85db3a799638bcd73558f98232b276f3cb5e
```

Evidence:
- implementation CI #127 / `33864695658` — **PASS**;
- final documentation-synchronized exact-head CI #131 / `33864946328` at `611e3737081bbce70396f249781d247e8cd50268` — **PASS**.

Delivered:

```text
canonical Project
 -> current IMemoryStore records
 -> exclude superseded
 -> exclude MODEL_INFERENCE / EXTERNAL_CONTENT by default
 -> deterministic authority ordering
 -> hard record/character bounds
 -> Loren-owned prepared memory package
 -> BrainContext -> AgentLoop / IBrain
```

Default prepared-context classes:
- included: `OWNER_CORRECTION`, `OWNER_EXPLICIT`, `OWNER_APPROVED_INFERENCE`, `VERIFIED_TOOL`;
- excluded: `MODEL_INFERENCE`, `EXTERNAL_CONTENT`.

Prepared memory retains canonical ID, scope, source class, provenance/source reference and timestamps. Runtime/brain receive no EF/DbContext. The prompt boundary explicitly states memory is data, not action authorization or policy override, and `VERIFIED_TOOL` is time/source-scoped external fact rather than automatic live state or owner permission.

## Slice 4 — Owner forget/delete [IMPLEMENTED / PR #21]

Current PR: `#21 — feat: add M4 memory forget purge`.

Implemented candidate:

```text
A OWNER_EXPLICIT
 -> B OWNER_CORRECTION
 -> C OWNER_CORRECTION (current)

ForgetAsync(C)
 -> verify C exists and is current
 -> walk reverse correction chain C <- B <- A
 -> require one linear chain + identical Project/Repository scope
 -> delete A, then B, then C in one SQLite transaction
 -> restart
 -> A/B/C remain absent
 -> prepared context cannot resurrect forgotten content
```

Properties:
- explicit `IMemoryStore.ForgetAsync(currentMemoryRecordId)`; no generic delete API;
- only a current record can be forgotten directly;
- correction history must be a single linear chain or forgetting fails closed;
- the whole correction chain is physically purged so an older claim cannot become current again;
- each delete checks its expected supersession pointer, making concurrent/history changes roll back instead of partially deleting;
- unrelated memories remain untouched;
- memory forgetting has no generic cascade into audit; audit retention remains a separate concept under ADR-003;
- no schema migration is required.

Real SQLite tests prove:
- A -> B -> C is completely purged when C is forgotten;
- all chain records remain absent after restart;
- prepared memory context contains no forgotten marker after restart;
- unrelated memory survives;
- forgetting a superseded record fails without changing history/current truth;
- forgetting an unknown record fails closed.

Implementation CI #133 / run `33865419023` — **PASS** across restore, zero-warning build, all tests, format, secret scan, dependency scan, and web/auth smoke.

Slice 4 is not closed until this documentation-synchronized PR head passes final exact-head CI and PR #21 merges.

## Slice 5 — Poisoning / trust-boundary acceptance [NEXT]

Final M4 adversarial gate will prove:
- `MODEL_INFERENCE` cannot silently become owner truth or correction;
- `EXTERNAL_CONTENT` cannot self-promote into trusted memory/policy/permission;
- content and provenance/source references are data, not executable instructions;
- `VERIFIED_TOOL` cannot grant owner permission and remains source/time scoped;
- `OWNER_APPROVED_INFERENCE` is distinguishable from an unapproved model inference;
- owner correction remains current owner truth in scope;
- malicious excluded records do not leak into prepared brain context.

## M4 non-goals

Do not add GitHub writes, broad vector infrastructure, generic graph entities, scheduler/background behavior, or v0.2 research capabilities while implementing the memory core.

## Next execution sequence

```text
PR #21 final exact-head CI
 -> merge M4 Slice 4
 -> M4 Slice 5 poisoning/trust acceptance
 -> M4 exit gate
 -> M4 COMPLETE
 -> Gate D / M5
```

## Progress-update rule

Any merge that changes capability, milestone completion, ADR status, validated dependencies/providers, or next execution target must synchronize:
1. `docs/status.md`;
2. `README.md`;
3. `README.vi.md`;
4. relevant ADRs/plans/roadmap.

A milestone is not closed until implementation/tests and repository documentation agree.
