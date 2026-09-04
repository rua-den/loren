# Loren Project Status

**Last updated:** 2026-09-04  
**Current version phase:** `v0.1 — Trustworthy Core development`  
**Current decision gates:** `Gate A — PASSED`, `Gate B — PASSED`  
**Current milestone:** `M3 — Canonical State`  
**Current execution target:** `M3 Slice 2 — prepared canonical project context`

This file is the authoritative progress ledger for the repository. `README.md` and `README.vi.md` summarize it.

## Completed milestones

- `v0.0 — Architecture / Feasibility` — complete.
- `M1 — Engineering Foundation` — complete.
- `M2 — Walking Skeleton` — complete.

M2's trusted exact-main production proof passed in workflow run `33840149005`: unauthenticated `/api/run` was rejected, owner login succeeded, authenticated production execution crossed real Ollama -> Loren ActionGateway -> real GitHub read -> Ollama final response, and owner-visible correlated audit completed. Owner/provider credentials were absent from the response and `/internal/dev/run` remained `404` in Production.

No GitHub write path exists yet.

---

# Current milestone — M3 Canonical State

## Goal

Give Loren durable, provider-independent Project/Repository identity and feed that state into runtime as a small prepared context rather than database access.

Acceptance identity:

```text
"wedding project"
"web đám cưới"
"wedding-online"
        |
        v
same Loren ProjectId
        |
        v
canonical RepositoryId
        |
        v
integration locator: github / rua-den/wedding-online
```

This mapping must survive process/database-context restart and must not depend on provider conversation/session identity.

## M3 Slice 1 — Canonical identity + persistence [COMPLETE]

Merged PR: `#15`  
Main commit: `00fbba08587ba8275c121fd7f9532a785f55314d`  
Exact-head CI: run `33842440251` / run #99 — **PASS**

Delivered:

- Loren-owned `ProjectId` and `RepositoryId` backed by stable GUID values;
- canonical `Project`, `Repository`, `RepositoryLocator`, normalized aliases, and `ProjectSnapshot`;
- provider/EF-neutral `IProjectCatalog` in `Loren.Core`;
- EF Core SQLite `10.0.11` only in `Loren.Infrastructure`;
- production `Projects`, `ProjectAliases`, and `Repositories` schema;
- initial production migration `202609040001_InitialCanonicalState`;
- `SqliteProjectCatalog` persistence/query boundary;
- deterministic restart acceptance using a real temporary SQLite database;
- three configured aliases resolving to the same Loren Project after restart;
- external GitHub locator stored as integration metadata rather than canonical primary identity;
- normalized alias collision fails closed;
- repeated update of one canonical project in the same context is covered.

CI #99 passed restore, zero-warning build, deterministic tests, format, secret scan, dependency vulnerability scan, and web/auth smoke checks.

## M3 Slice 2 — Deterministic alias resolution + prepared context [IMPLEMENTED / PR #16]

Current PR: `#16 — feat: add M3 canonical prepared project context`

Implementation candidate now provides:

```text
owner request + optional exact projectAlias
        |
        v
Loren.Web / IProjectCatalog
        |
        +-- unknown alias -> fail closed before model
        |
        v
ProjectSnapshot
        |
        v
small trusted configured system context
        |
        v
AgentLoop -> IBrain
```

Properties:

- production host wires a scoped `CanonicalStateDbContext` and `IProjectCatalog`;
- canonical database is migrated at host startup;
- `/api/run` accepts optional `projectAlias` without breaking message-only M2 callers;
- alias lookup occurs before the brain runs;
- unknown configured project alias returns `404` and does not invoke the brain;
- runtime/brain receive only prepared `BrainContext`, never EF/DbContext;
- prepared context explicitly distinguishes configured canonical identity from live external facts;
- current external facts must still come from authorized tools;
- owner console exposes project alias and displays resolved canonical Project/Repository metadata;
- real-SQLite restart tests prove all three aliases produce the same prepared project context;
- deterministic fake-brain integration proves the prepared context reaches `AgentLoop`/`IBrain`.

Pre-doc exact-head implementation CI run `33843033700` / run #102 passed build, tests, format, secret scan, dependency scan, and web/auth smoke. Because this PR also synchronizes documentation/configuration, the final PR head must pass CI again before merge.

### Current limitation

A fresh canonical database contains no Projects. M3 currently proves the persistence/resolution/context boundary but does **not** add owner-facing Project CRUD/configuration UI. Explicitly configured canonical data is required before the Project alias field can resolve anything.

Canonical database configuration:

```text
file: loren.db
default directory: OS local application data / Loren
override directory: LOREN_DATA_DIRECTORY
migration: automatic at host startup
```

## M3 Slice 3 — Gate C checkpoint [NEXT]

Before M4 expands memory, lock/document:

- canonical ID rules and serialization format;
- SQLite/EF migration policy;
- Project/Repository schema boundary;
- durable-memory source/trust classes;
- correction/supersession direction;
- memory versus audit deletion distinction;
- export format versioning approach.

A new storage ADR is required only if persistence becomes materially more complex than the accepted SQLite/EF Core baseline.

## M3 non-goals

Do not add `Person`, `Task`, `Decision`, `Preference`, a generic graph, memory semantics, permission semantics, or GitHub writes merely because the schema could support them.

## Next execution sequence

```text
PR #16 final exact-head CI
 -> merge M3 Slice 2
 -> M3 Slice 3 / Gate C checkpoint
 -> M3 COMPLETE
 -> M4 Trusted Memory
```

## Progress-update rule

Any merge that changes capability, milestone completion, ADR status, validated dependencies/providers, or the next execution target must synchronize:

1. `docs/status.md`;
2. `README.md`;
3. `README.vi.md`;
4. the relevant ADR/plan when a decision or milestone changes.

A milestone is not closed until implementation/tests and repository documentation agree.
