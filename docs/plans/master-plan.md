# Loren Master Delivery Plan

**Status:** Active planning baseline  
**Current phase:** `v0.1 — Trustworthy Core development`  
**Current milestone:** `M4 — Trusted Durable Memory`

This is Loren's top-level delivery plan. The roadmap is **capability-driven, not date-driven**. Versions advance only when their trust/usefulness exit gates pass.

---

# 1. Product objective

Loren should become a persistent personal intelligence system that:

- knows the owner's durable context without being re-taught every session;
- uses authoritative tools for external facts and actions;
- reasons through replaceable brain providers;
- acts only through explicit Loren-owned policy and credential boundaries;
- can explain and audit consequential behavior;
- gradually becomes more proactive only after lower-level trust boundaries are proven.

The long-term product is **not** a model wrapper and **not** a generic agent framework.

---

# 2. Architectural invariants across all versions

Breaking one requires an explicit superseding ADR.

## Loren owns

```text
identity
canonical personal/project state
memory + provenance
permissions/policy
action gateway
audit/history
context assembly
```

## Replaceable infrastructure

```text
brain/model provider
agent-loop implementation details
MCP servers
vendor APIs
UI clients
database engine after migration
computer-use/device runtimes
notification channels
```

## Security invariants

1. The model may request actions; it may not authorize itself.
2. Privileged tool credentials remain outside model/runtime context.
3. External content is untrusted and cannot silently promote itself to owner policy or trusted memory.
4. Consequential writes are validated, permission-checked, audited, and post-verified.
5. Canonical state is exportable/recoverable independently of model-provider session state.
6. Runtime/provider-specific IDs never become Loren's durable primary identity.

---

# 3. Versioning model

```text
v0.0   architecture / feasibility        ✓ complete
v0.1   trustworthy core                 <- current development / M4
v0.2   useful project assistant
v0.3   personal operations
v0.4   voice + device presence
v0.5   proactive/background agent
v0.6+  hardening based on real daily use
v1.0   stable personal daily driver
```

Patch releases (`v0.x.y`) may add narrow capabilities or hardening without changing the main trust boundary of the minor version.

---

# 4. Decision gates

## Gate A — Core ownership [PASSED]

**Decision:** ADR-001.

Loren owns canonical identity/state/policy/action authorization; models, runtimes, MCP, and external frameworks are adapters.

## Gate B — v0.1 implementation stack [PASSED]

**Decision:** ADR-002 — Accepted on 2026-09-03.

Accepted baseline:

```text
C# 14 / .NET 10 LTS
ASP.NET Core
small Loren-owned bounded agent loop
provider-neutral IBrain
MCP C# SDK behind Loren action contracts
SQLite + EF Core
Blazor Web App
xUnit
```

M0 proved provider/tool/MCP/persistence/host feasibility. M1 rebuilt production code behind Loren-owned interfaces. M2 proved the real authenticated production read path end to end.

## Gate C — Canonical storage and memory lifecycle [PASSED]

**Decision:** ADR-003 — Accepted on 2026-09-04.

M3 made the Project/Repository boundary concrete and Gate C locked:

- opaque Loren-owned GUID identity rules;
- SQLite + explicit EF Core migration policy;
- Project/Repository canonical schema boundary;
- durable-memory source/trust classes;
- append/supersede correction semantics;
- memory deletion versus audit retention separation;
- logical portable export versioning independent of EF schema migration IDs.

The accepted memory source classes are:

```text
OWNER_EXPLICIT
OWNER_CORRECTION
VERIFIED_TOOL
OWNER_APPROVED_INFERENCE
MODEL_INFERENCE
EXTERNAL_CONTENT
```

Portable export begins with a logical `format_version = 1` contract; raw SQLite copies may be backups but are not the portable canonical format.

Gate C authorizes M4 Trusted Memory. It does **not** authorize external writes.

## Gate D — Action/credential policy [before first real external write]

Must settle:

- action contract and policy dimensions;
- approval binding/replay rules;
- credential storage/resolution;
- secret redaction and rotation/revocation;
- global read-only/kill behavior.

No write-capable integration may ship before this gate passes.

## Gate E — Background execution [before trusted scheduler/background operations]

Must settle persistent job ownership/state, timezone/missed-run semantics, bounded retry/backoff, cancellation, quotas, notification policy, and safe restart/resume behavior.

## Gate F — Trusted devices and voice approval [before v0.4]

Must settle trusted-device enrollment/revocation, session/device identity, voice privacy/retention, remote-access transport, and actions that can never rely on weak voice-only confirmation.

## Gate G — Proactive autonomy [before v0.5]

Must settle standing permissions, event trust, notification/rate limits, quotas, self-created-task bounds, global pause, and event/prompt-injection testing.

## Gate H — v1 stable contract [before v1.0]

Must settle export/restore compatibility, upgrade/migration path, stable core interfaces, backup/recovery, secret rotation/revocation, operational monitoring/incident procedure, and minimum privacy/security baseline.

---

# 5. Version milestones

## v0.0 — Architecture and feasibility [COMPLETE]

Completed ADR-001/ADR-002 feasibility and architecture work. **Transition completed:** `v0.0 -> v0.1 development` on 2026-09-03.

---

## v0.1 — Trustworthy Core [ACTIVE]

Detailed plan: `docs/plans/v0.1.md`

### Goal

Create the smallest Loren that remembers project context, reads real GitHub state, performs a narrow authorized GitHub write, and can explain what happened.

### Required product flows

```text
"Loren, repo wedding hiện sao rồi?"
"Nhớ rằng project này production deploy phải hỏi tao."
"Tạo branch và chuẩn bị thay đổi X."
"Tại sao mày vừa làm việc đó?"
```

### Milestones

- **M1 engineering foundation — COMPLETE**;
- **M2 walking skeleton — COMPLETE**;
- **M3 canonical project/repository state — COMPLETE**;
- **M4 trusted durable memory + correction/retrieval — ACTIVE**;
- M5 action/credential boundary + narrow GitHub writes;
- M6 minimal daily-use UI;
- M7 export/restore and recovery proof;
- M8 adversarial security/reliability E2E.

### M1 completion evidence

M1 established the production scaffold with .NET SDK `10.0.400`, provider-neutral Core contracts, bounded Runtime loop, deterministic tests, central dependency versions, development documentation, and CI gates.

### M2 completion evidence

M2 completed on 2026-09-04. Main implementation commit `94ce6d1e74f2dfdf0584b8dbf8a4edbbb3774f7d` plus trusted workflow run `33840149005` proved:

```text
owner auth
 -> real Ollama
 -> github.read_repository ActionRequest
 -> Loren ActionGateway / read-only policy
 -> real GitHub GET
 -> structured result
 -> Ollama final answer
 -> owner-visible correlated audit
```

Credential isolation and the production-only owner surface were also verified.

### M3 completion evidence

M3 completed on 2026-09-04 in three slices.

**Slice 1 — canonical identity + persistence**

- PR #15 merged at `00fbba08587ba8275c121fd7f9532a785f55314d`;
- exact-head CI run `33842440251` / #99 passed;
- Loren-owned `ProjectId`/`RepositoryId`, Project/Repository models, normalized aliases, `IProjectCatalog`, SQLite/EF persistence, initial migration, collision/update/restart tests.

**Slice 2 — deterministic alias resolution + prepared context**

- PR #16 merged at `56fd988d3b74c754604355e3c97a5d3656675bbb`;
- final PR exact-head CI run `33843405386` / #108 passed;
- post-merge main CI run `33843524467` / #109 passed;
- explicit aliases resolve before model execution;
- unknown aliases fail before the brain runs;
- host prepares a small EF-neutral Project/Repository `BrainContext`;
- runtime/brain never receive `DbContext`;
- current external facts still require authorized tools.

Acceptance:

```text
"wedding project"
"web đám cưới"
"wedding-online"
 -> same Loren ProjectId
 -> Repository locator rua-den/wedding-online
```

The mapping survives SQLite context restart and is independent of provider session identity.

**Slice 3 — Gate C**

ADR-003 locks canonical IDs, migrations, memory source authority, correction/supersession, deletion/audit separation, and export versioning. With ADR-003 merged and source-of-truth docs synchronized, Gate C and M3 are complete.

### M4 active target

Implement durable memory under ADR-003 without creating a transcript dump or letting external/model content silently become owner truth.

Initial M4 vertical flow:

```text
Owner: "Nhớ wedding-online là web đám cưới của tao."
 -> OWNER_EXPLICIT MemoryRecord
 -> canonical Project scope
 -> persist + provenance
 -> restart
Owner: "Web đám cưới repo nào?"
 -> trusted retrieval
 -> small prepared context
 -> correct canonical repository answer
```

Correction must create a new `OWNER_CORRECTION` record that supersedes prior current truth. External-content poisoning tests must prove retrieved text cannot promote itself into trusted memory/policy.

### v0.1 exit gate

Do not tag v0.1 until:

- all four required flows run end to end from the UI;
- external state answers are tool-grounded;
- memory survives restart and correction;
- hostile external content cannot silently become trusted policy;
- all writes pass Loren policy and controlled credential resolution;
- approval cannot be replayed for unrelated actions;
- writes are post-verified and auditable;
- canonical state export -> wipe -> restore works;
- deterministic core tests run with a fake brain/provider;
- runtime/provider session deletion does not destroy Loren state.

### Explicitly deferred

- broad web research;
- reminders/background scheduler;
- Gmail/Calendar;
- voice;
- proactive monitoring;
- unrestricted shell/computer use.

---

## v0.2 — Useful Project Assistant

Goal: make the trusted v0.1 core useful for richer daily project work.

Candidate capabilities include safe public web retrieval/provenance, explicit research-to-memory promotion, persistent reminders, project decisions/procedures, richer GitHub project health, improved memory retrieval/conflict handling, and run cost visibility.

**Checkpoint before private background operations:** Gate E.

---

## v0.3 — Personal Operations

Candidate capabilities include Calendar, Gmail, server/VPS health and constrained actions, filesystem integrations, cross-tool context, daily brief, and stronger data/credential scopes.

**Checkpoint before v0.4:** Gate F.

---

## v0.4 — Voice and Device Presence

Candidate capabilities include trusted devices, mobile/PWA, push-to-talk, STT/TTS, notification actions, revocation/lost-device testing, and optional device nodes.

**Checkpoint before v0.5:** Gate G.

---

## v0.5 — Proactive Loren

Candidate capabilities include normalized events, proactive evaluation without default write authority, notification prioritization, tiny allowlisted standing permissions, bounded recurring work, active-task visibility, global pause, and adversarial event/prompt-injection tests.

---

## v0.6+ — Daily-use hardening

Do not pre-design deeply. Let actual use determine priorities: memory consolidation, more providers/local models, Home Assistant, computer use, more integrations, offline/private execution, performance/cost, UX, and packaging/deployment simplification.

---

## v1.0 — Stable Personal Daily Driver

v1.0 means Loren's core can be trusted as the owner's long-lived assistant and can evolve without casually losing state or bypassing security boundaries. Gate H must pass before release.

---

# 6. Milestone execution rules

For every milestone:

1. define the user-visible or architecture behavior being proven;
2. define acceptance tests before broad implementation;
3. build the smallest vertical slice that proves it;
4. keep provider/framework SDK types outside `Loren.Core`;
5. add audit/observability with the capability;
6. update ADRs when a choice becomes expensive to reverse;
7. do not add adjacent features merely because a framework makes them easy;
8. finish with tests/build/format/static checks and document known gaps;
9. synchronize `docs/status.md`, README EN/VI, roadmap, and relevant plans/ADRs with implementation progress.

A milestone is complete only when its acceptance criteria pass on the main integration path.

---

# 7. Stop-the-line conditions

Do not continue if model/runtime can bypass ActionGateway, privileged credentials leak, canonical state depends on provider sessions, external content can self-promote to trusted policy/memory, recovery is known broken, or deterministic core logic cannot be tested without live model behavior.

Fix the boundary first, then continue.

---

# 8. Current next action

The project is now at **v0.1 / M4 — Trusted Durable Memory**.

```text
ADR-003 source/trust semantics
        |
MemoryRecord canonical model + migration
        |
owner-explicit save with Project scope + provenance
        |
restart-safe trusted retrieval
        |
owner correction -> append/supersede
        |
external-content poisoning rejection
        |
prepared memory context
        |
M4 exit gate
```

Gate D remains mandatory before any GitHub write capability. Do not implement v0.2 capabilities before the v0.1 exit gate is satisfied.
