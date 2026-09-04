# Loren Project Status

**Last updated:** 2026-09-04  
**Current version phase:** `v0.1 — Trustworthy Core development`  
**Current decision gates:** `Gate A — PASSED`, `Gate B — PASSED`, `Gate C — PASSED`  
**Current milestone:** `M4 — Trusted Durable Memory`  
**Current execution target:** `M4 Slice 1 — canonical MemoryRecord + owner-explicit persistence`

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

PR #15 merged at:

```text
00fbba08587ba8275c121fd7f9532a785f55314d
```

Exact-head CI run `33842440251` / #99 — **PASS**.

Delivered:

- Loren-owned `ProjectId` and `RepositoryId`;
- `Project`, `Repository`, `RepositoryLocator`, aliases, `ProjectSnapshot`;
- provider/EF-neutral `IProjectCatalog` in `Loren.Core`;
- SQLite + EF Core `10.0.11` persistence in `Loren.Infrastructure`;
- production schema + initial migration `202609040001_InitialCanonicalState`;
- real SQLite restart tests;
- normalized alias collision fails closed;
- repeated update behavior covered;
- external GitHub locator stored as integration metadata rather than Loren primary identity.

Acceptance:

```text
"wedding project"
"web đám cưới"
"wedding-online"
 -> same Loren ProjectId
 -> Repository locator rua-den/wedding-online
```

## Slice 2 — Deterministic alias resolution + prepared context [COMPLETE]

PR #16 merged at:

```text
56fd988d3b74c754604355e3c97a5d3656675bbb
```

Evidence:

- implementation CI run `33843033700` / #102 — **PASS**;
- final PR exact-head CI run `33843405386` / #108 — **PASS**;
- post-merge main CI run `33843524467` / #109 — **PASS**.

Delivered:

```text
owner request + optional projectAlias
 -> IProjectCatalog
 -> ProjectSnapshot
 -> small prepared BrainContext
 -> AgentLoop / IBrain
```

Properties:

- canonical DB migrates at host startup;
- `/api/run` accepts optional `projectAlias`;
- exact configured alias resolves before model execution;
- unknown alias fails with `404` before the brain runs;
- runtime/brain never receive EF `DbContext`;
- prepared context contains Loren-owned Project/Repository identity and external locator metadata;
- prepared context explicitly says configured identity is not live external state;
- current GitHub facts still require authorized tools;
- owner console can submit an alias and inspect resolved canonical metadata;
- real-SQLite restart tests and deterministic fake-brain tests prove the path.

Current product limitation remains: a fresh DB contains no Project records and owner-facing Project CRUD/configuration UI is not implemented yet.

## Slice 3 — Gate C [COMPLETE]

Decision: [`ADR-003 — Canonical State and Memory Lifecycle`](decisions/003-canonical-state-and-memory-lifecycle.md).

Gate C locks:

- opaque Loren-owned GUID IDs;
- explicit EF Core migration policy;
- Project/Repository canonical schema boundary;
- durable-memory source/trust classes;
- append/supersede correction semantics;
- memory deletion versus audit retention separation;
- logical portable export versioning independent from EF schema versioning.

Required memory source classes:

```text
OWNER_EXPLICIT
OWNER_CORRECTION
VERIFIED_TOOL
OWNER_APPROVED_INFERENCE
MODEL_INFERENCE
EXTERNAL_CONTENT
```

First portable export direction uses logical `format_version = 1`; raw SQLite copies may be backups but are not the portable contract.

**Gate C PASSED. M3 COMPLETE.**

---

# Current milestone — M4 Trusted Durable Memory

## Goal

Give Loren durable memory with provenance/authority, correction, deletion semantics, restart survival, and safe prepared retrieval — without turning transcripts, model guesses, or hostile external content into owner truth.

## First vertical slice

```text
Owner: "Nhớ wedding-online là web đám cưới của tao."
        |
        v
OWNER_EXPLICIT MemoryRecord
        |
Project scope + provenance
        |
SQLite persistence
        |
restart
        |
trusted retrieval
        |
small prepared memory context
```

Initial implementation target:

- Loren-owned `MemoryRecordId`;
- `MemoryRecord` canonical model;
- source class enum/type from ADR-003;
- Project/Repository scope references where applicable;
- content + timestamps + provenance/source reference;
- current/superseded lifecycle representation;
- EF migration + persistence/query boundary outside `Loren.Core`;
- owner-explicit save/restart/retrieve acceptance test.

## Required M4 follow-up behavior

### Correction

```text
old OWNER_EXPLICIT record
 -> owner correction
 -> new OWNER_CORRECTION record
 -> old record superseded
 -> current-truth query returns correction
```

### Poisoning resistance

`MODEL_INFERENCE` and `EXTERNAL_CONTENT` cannot silently become current owner-authoritative memory or policy.

### Retrieval

The brain receives a small provenance-bearing memory package; it does not receive raw database access.

## M4 non-goals

Do not add GitHub writes, broad vector infrastructure, generic graph entities, scheduler/background behavior, or v0.2 research capabilities while implementing the memory core.

## Next execution sequence

```text
NOW
MemoryRecord + source authority model
 -> SQLite migration/persistence
 -> OWNER_EXPLICIT save + Project scope
 -> restart-safe retrieval
 -> correction/supersession
 -> poisoning tests
 -> prepared memory context
 -> M4 exit gate
```

## Progress-update rule

Any merge that changes capability, milestone completion, ADR status, validated dependencies/providers, or next execution target must synchronize:

1. `docs/status.md`;
2. `README.md`;
3. `README.vi.md`;
4. relevant ADRs/plans/roadmap.

A milestone is not closed until implementation/tests and repository documentation agree.
