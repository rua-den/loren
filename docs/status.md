# Loren Project Status

**Last updated:** 2026-09-04  
**Current version phase:** `v0.1 — Trustworthy Core development`  
**Current decision gates:** `Gate A — PASSED`, `Gate B — PASSED`, `Gate C — PASSED`  
**Current milestone:** `M4 — Trusted Durable Memory`  
**Current execution target:** `M4 Slice 1 — final exact-head gate for owner-explicit persistence`

This file is the authoritative progress ledger for the repository. `README.md` and `README.vi.md` summarize it.

## Completed milestones

- `v0.0 — Architecture / Feasibility` — complete.
- `M1 — Engineering Foundation` — complete.
- `M2 — Walking Skeleton` — complete.
- `M3 — Canonical Project/Repository State` — complete.

No GitHub write path exists yet. Gate D remains mandatory before external writes.

---

# M2 completion summary

M2's trusted exact-main production proof passed in workflow run `33840149005`:

```text
owner auth
 -> authenticated /api/run
 -> real Ollama
 -> Loren ActionGateway
 -> real GitHub read
 -> final model answer
 -> correlated owner-visible audit
```

Owner/provider credentials were absent from the response and `/internal/dev/run` remained unavailable in Production.

---

# M3 completion evidence

## Slice 1 — Canonical identity + persistence [COMPLETE]

PR #15 merged at `00fbba08587ba8275c121fd7f9532a785f55314d`. Exact-head CI run `33842440251` / #99 — **PASS**.

Delivered Loren-owned `ProjectId`/`RepositoryId`, canonical Project/Repository records and aliases, provider-neutral `IProjectCatalog`, SQLite + EF Core persistence, migration `202609040001_InitialCanonicalState`, restart acceptance, collision fail-closed behavior, and external GitHub locator metadata independent from Loren primary identity.

Acceptance:

```text
"wedding project"
"web đám cưới"
"wedding-online"
 -> same Loren ProjectId
 -> Repository locator rua-den/wedding-online
```

## Slice 2 — Deterministic alias resolution + prepared context [COMPLETE]

PR #16 merged at `56fd988d3b74c754604355e3c97a5d3656675bbb`.

Evidence:

- implementation CI `33843033700` / #102 — **PASS**;
- final PR exact-head CI `33843405386` / #108 — **PASS**;
- post-merge main CI `33843524467` / #109 — **PASS**.

Delivered exact configured-alias resolution before model execution, fail-closed unknown alias behavior, startup canonical migration, prepared EF-neutral Project/Repository `BrainContext`, and owner-console project context visibility. Current external facts still require authorized tools.

## Slice 3 — Gate C [COMPLETE]

Decision: [`ADR-003 — Canonical State and Memory Lifecycle`](decisions/003-canonical-state-and-memory-lifecycle.md).

Gate C locks opaque Loren GUID IDs, explicit EF migrations, Project/Repository boundaries, memory source/trust classes, append/supersede correction, memory-vs-audit deletion separation, and logical export `format_version = 1` independent from EF migration IDs.

Required memory source classes:

```text
OWNER_EXPLICIT
OWNER_CORRECTION
VERIFIED_TOOL
OWNER_APPROVED_INFERENCE
MODEL_INFERENCE
EXTERNAL_CONTENT
```

Gate C PR #17 exact-head CI `33860095412` / #110 passed and merged at `69223e8c4923510bb26fa50f77a3c44c1683b172`.

**Gate C PASSED. M3 COMPLETE.**

---

# Current milestone — M4 Trusted Durable Memory

## Goal

Give Loren durable memory with provenance/authority, correction, deletion semantics, restart survival, and safe prepared retrieval — without turning transcripts, model guesses, or hostile external content into owner truth.

## M4 Slice 1 — Canonical MemoryRecord + OWNER_EXPLICIT persistence [IMPLEMENTED / PR #18]

Current PR: `#18 — feat: add M4 owner-explicit memory persistence`.

Implemented candidate:

```text
OWNER_EXPLICIT durable fact
        |
        v
MemoryRecord + MemoryRecordId
        |
Project / Repository scope + provenance
        |
        v
IMemoryStore
        |
SQLite / EF Core migration
        |
restart
        |
        v
same canonical memory + authority + scope
```

Delivered:

- Loren-owned `MemoryRecordId` with lowercase GUID `N` text representation;
- canonical `MemoryRecord` in `Loren.Core`;
- all six ADR-003 `MemorySourceClass` values;
- Project/Repository scope references;
- content, source reference/provenance, timestamps, and `SupersededById` lifecycle field;
- domain guards for invalid repository-only scope, self-supersession, and invalid timestamps;
- deliberately narrow EF-neutral `IMemoryStore`: `AddAsync`, `GetAsync`, `ListCurrentForProjectAsync`;
- no generic content-update method, preserving append/supersede direction for corrections;
- `MemoryRecords` canonical SQLite schema with Project, Repository, and self-supersession foreign keys;
- migration `202609040002_AddMemoryRecords`;
- `SqliteMemoryStore` with exact source-class storage values;
- fail-closed validation that a repository-scoped memory's Repository belongs to its Project;
- production DI registration for `IMemoryStore`;
- domain tests and real SQLite restart acceptance.

Acceptance proves an `OWNER_EXPLICIT` record with Project/Repository scope and `owner:authenticated` provenance survives context/process-style restart with the same `MemoryRecordId`, content, source authority, scope, timestamps, and current lifecycle state.

CI history:

- CI #112 / run `33860545089`: build passed; one integration test exposed SQLite's inability to translate `ORDER BY DateTimeOffset`;
- fix `15bbecd078bbf04f012464673cd856353b6ea84c`: SQL filters first, then deterministic ordering occurs after materialization;
- implementation CI #113 / run `33860641367`: **PASS** for restore, zero-warning build, all tests, format, secret scan, dependency scan, and web/auth smoke.

Slice 1 is **not closed yet**. This branch is synchronizing documentation; the final PR head must pass exact-head CI again before merge.

### Deliberately deferred from Slice 1

- correction/supersession mutation flow;
- authority-ranked prepared memory context for runtime;
- memory write endpoint/UI;
- forget/delete behavior;
- automatic model-driven memory promotion.

## M4 Slice 2 — Owner correction + supersession [NEXT AFTER MERGE]

```text
old OWNER_EXPLICIT record (current)
 -> explicit owner correction
 -> new OWNER_CORRECTION record (current)
 -> old record points to superseded_by new record
 -> default current query excludes old record
 -> retained history remains reconstructable
```

Correction must be an explicit append + lifecycle mutation, not a generic destructive content update. Model/external content must not supersede owner-authoritative state.

## M4 follow-up

- Slice 3: authority-aware retrieval + bounded prepared memory context;
- Slice 4: owner forget/delete semantics separated from audit retention;
- Slice 5: poisoning/trust-boundary acceptance for model inference and external content.

## M4 non-goals

Do not add GitHub writes, broad vector infrastructure, generic graph entities, scheduler/background behavior, or v0.2 research capabilities while implementing the memory core.

## Next execution sequence

```text
PR #18 final exact-head CI
 -> merge M4 Slice 1
 -> M4 Slice 2 correction/supersession
 -> M4 Slice 3 authority-aware prepared context
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
