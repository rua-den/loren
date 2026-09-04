# Loren Project Status

**Last updated:** 2026-09-04  
**Current version phase:** `v0.1 — Trustworthy Core development`  
**Current decision gates:** `Gate A — PASSED`, `Gate B — PASSED`  
**Current milestone:** `M3 — Canonical state`

This file is the authoritative progress ledger for the repository. `README.md` and `README.vi.md` summarize it.

## Current status

Loren has completed `v0.0 — Architecture / Feasibility`, `M1 — Engineering Foundation`, and `M2 — Walking Skeleton`.

**M2 is COMPLETE.** The first owner-testable Loren preview now has a trusted exact-main proof through the normal authenticated production owner path.

Current development moves to **M3 — Canonical Project/Repository State**.

## M2 completion evidence

### Deterministic/main CI

Main commit: `94ce6d1e74f2dfdf0584b8dbf8a4edbbb3774f7d`  
Main CI run: `33840135772` / run #89 — **PASS**

Passed:

- restore;
- zero-warning release build;
- deterministic tests;
- format verification;
- tracked-secret scan;
- dependency vulnerability scan;
- web health/auth/default-surface smoke test.

### Trusted exact-main owner-authenticated live proof

Workflow: `M2 Trusted Live Read Proof`  
Run: `33840149005` / run #2 — **PASS**  
Trusted commit: `94ce6d1e74f2dfdf0584b8dbf8a4edbbb3774f7d`  
Trigger branch: `proof/m2-live-read`, verified to point exactly at current `main` before the provider secret was used.

Observed production path:

```text
unauthenticated POST /api/run
 -> HTTP 401

Owner login
 -> POST /auth/login
 -> HTTP 200 + owner cookie session

Authenticated POST /api/run
 -> production LorenRunService
 -> production OllamaBrain (gpt-oss:120b)
 -> POST https://ollama.com/api/chat        200
 -> ActionRequest(github.read_repository)
 -> production AgentLoop / ActionGateway
 -> ReadOnlyActionPolicy                    allow
 -> production GitHubReadRepositoryExecutor
 -> GET https://api.github.com/repos/rua-den/loren   200
 -> structured ActionResult
 -> production OllamaBrain second turn
 -> POST https://ollama.com/api/chat        200
 -> final answer
 -> correlated owner-visible audit
```

Live result:

```text
runId:       5bb9cc341387430c82759d58309da85a
turns:       2
actionCount: 1
final:       Repository rua-den/loren
             Default branch: main
```

Audit for action `5b5d3a6a059942d9bed81b7cfa00003d`:

```text
ActionRequested   github.read_repository   requested
PolicyEvaluated   github.read_repository   allow
ActionCompleted   github.read_repository   succeeded
```

The same trusted step asserted:

- provider credential absent from the owner-visible response;
- owner credential absent from the owner-visible response;
- `/internal/dev/run` remained HTTP `404` in the production host.

## M2 delivered capability

```text
Owner browser
 -> one-owner authentication/session
 -> protected owner console
 -> protected /api/run
 -> Loren Runtime / bounded AgentLoop
 -> configured provider-neutral IBrain
 -> ActionRequest(github.read_repository)
 -> ActionGateway / read-only policy
 -> real GitHub read executor
 -> structured ActionResult
 -> final answer
 -> correlated owner-visible audit
```

M2 keeps the important trust boundaries intact:

- the model may request actions but cannot authorize them;
- Loren owns trusted run/action IDs;
- every action crosses ActionGateway;
- unregistered/non-read-only actions fail closed;
- owner/provider credentials remain outside model/tool context;
- GitHub read has no write credential path;
- normal owner use no longer depends on the temporary development proof endpoint.

No GitHub write path was added in M2.

---

# Current milestone — M3 Canonical State

## Goal

Give Loren durable, provider-independent canonical identity for the minimum world model needed by v0.1.

Initial entities:

```text
Owner
Project
Repository
MemoryRecord
PermissionRule
AuditEvent
```

M3 should implement only the Project/Repository/identity pieces needed now and avoid prematurely building a broad graph.

## Required behavior

- stable Loren IDs independent of GitHub/provider/runtime IDs;
- Project -> Repository relation;
- project aliases;
- timestamps and migrations;
- external/provider IDs only as integration metadata;
- no secret fields in canonical entities;
- prepared context can resolve owner language to canonical Project/Repository state;
- provider conversation/session deletion cannot destroy canonical project state.

## Acceptance target

When configured as aliases:

```text
"wedding project"
"web đám cưới"
"wedding-online"
```

all resolve deterministically to the same Loren Project, whose Repository resolves to `rua-den/wedding-online`.

Restarting Loren or discarding model/provider session state must not remove that mapping.

## M3 decision checkpoint / Gate C preparation

Before broad memory work begins, M3 must make the storage choices expensive enough to evaluate and then lock:

- canonical ID rules;
- EF Core/SQLite migration policy;
- Project/Repository schema boundary;
- memory versus audit deletion distinction;
- export format versioning approach.

Do not add `Person`, `Task`, `Decision`, `Preference`, or a generic graph until an actual product flow requires them.

## Next execution sequence

```text
NOW
M3 canonical ID + Project/Repository schema
    |
    v
SQLite / EF Core migration + repository persistence
    |
    v
alias resolution + restart tests
    |
    v
Project/Repository context available to runtime
    |
    v
M3 acceptance flow
    |
    v
Gate C checkpoint
    |
    v
M4 — Trusted Memory
```

## Progress-update rule

Any merge that changes capability, milestone completion, ADR status, validated dependencies/providers, or the next execution target must update:

1. `docs/status.md`;
2. `README.md`;
3. `README.vi.md`;
4. the relevant ADR/plan when a decision or milestone changes.

A milestone is not considered closed until implementation/tests and repository documentation agree.
