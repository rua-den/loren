# Loren Master Delivery Plan

**Status:** Active planning baseline  
**Current phase:** `v0.1 — Trustworthy Core development`  
**Completed milestone:** `M4 — Trusted Durable Memory`  
**Next decision gate:** `Gate D — Action/Credential Policy before M5 writes`

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
3. External/model content cannot silently promote itself to owner policy or trusted memory.
4. Consequential writes are validated, permission-checked, audited, and post-verified.
5. Canonical state is exportable/recoverable independently of model-provider session state.
6. Runtime/provider-specific IDs never become Loren's durable primary identity.

---

# 3. Versioning model

```text
v0.0   architecture / feasibility        ✓ complete
v0.1   trustworthy core                 <- current / Gate D before M5
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

Gate C locked:

- opaque Loren-owned GUID identity rules;
- SQLite + explicit EF Core migration policy;
- Project/Repository canonical schema boundary;
- durable-memory source/trust classes;
- append/supersede correction semantics;
- memory deletion versus audit retention separation;
- logical portable export versioning independent of EF schema migration IDs.

Accepted memory source classes:

```text
OWNER_EXPLICIT
OWNER_CORRECTION
VERIFIED_TOOL
OWNER_APPROVED_INFERENCE
MODEL_INFERENCE
EXTERNAL_CONTENT
```

Portable export begins with logical `format_version = 1`; raw SQLite copies may be backups but are not the portable canonical format.

Gate C authorized M4 Trusted Durable Memory. M4 has now completed without authorizing external writes.

## Gate D — Action/credential policy [NEXT — before first real external write]

Must settle:

- write action contracts and policy dimensions;
- approval binding to action/target/parameters and non-replay rules;
- credential storage/resolution and read/write separation;
- secret redaction and rotation/revocation;
- global read-only/kill behavior;
- post-write verification and audit expectations.

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

Completed ADR-001/ADR-002 feasibility and architecture work. Transitioned to v0.1 development on 2026-09-03.

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

- **M1 Engineering Foundation — COMPLETE**;
- **M2 Walking Skeleton — COMPLETE**;
- **M3 Canonical Project/Repository State — COMPLETE**;
- **M4 Trusted Durable Memory — COMPLETE**;
- M5 Action/Credential Boundary + narrow GitHub writes;
- M6 Minimal daily-use UI;
- M7 Export/Restore and recovery proof;
- M8 Adversarial security/reliability E2E.

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

- PR #15 merged at `00fbba08587ba8275c121fd7f9532a785f55314d` — canonical IDs/persistence/aliases.
- PR #16 merged at `56fd988d3b74c754604355e3c97a5d3656675bbb` — deterministic alias resolution and prepared project context.
- PR #17 merged at `69223e8c4923510bb26fa50f77a3c44c1683b172` — ADR-003 / Gate C.

M3 established durable Project/Repository identity, restart-safe aliases, EF-neutral prepared project context, and the locked memory lifecycle semantics used by M4.

### M4 completion evidence

M4 completed on 2026-09-04 across five slices:

```text
OWNER_EXPLICIT persistence
 -> owner correction / append+supersede
 -> authority-aware bounded prepared memory
 -> owner forget / full correction-chain purge
 -> poisoning/provenance trust-boundary acceptance
```

- PR #18 — canonical OWNER_EXPLICIT durable memory; final CI #117 / `33860985267`.
- PR #19 — correction/supersession; final CI #123 / `33861630472`.
- PR #20 — prepared memory context; final CI #131 / `33864946328`.
- PR #21 — owner forget/delete; final CI #137 / `33865716479`.
- PR #22 — poisoning/provenance hardening; implementation CI #140 / `33866182751`, with final exact-head gate required before merge.

M4 proves durable memory survives restart; correction preserves history/current-truth semantics; full claim chains can be forgotten without resurrection; prepared memory is bounded/inspectable; model/external content cannot silently become owner truth; ordinary runtime turns do not silently mutate durable memory; and memory deletion remains separate from audit retention.

### Current next action — Gate D

Before M5 adds any write-capable GitHub integration, Gate D must freeze the action/approval/credential boundary. No GitHub write implementation begins until that decision gate passes.

### v0.1 exit gate

Do not tag v0.1 until:

- all four required flows run end to end from the UI;
- external state answers are tool-grounded;
- memory survives restart, correction, and owner forgetting;
- hostile external/model content cannot silently become trusted policy/memory;
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

Do not continue if model/runtime can bypass ActionGateway, privileged credentials leak, canonical state depends on provider sessions, external/model content can self-promote to trusted policy/memory, recovery is known broken, or deterministic core logic cannot be tested without live model behavior.

Fix the boundary first, then continue.

---

# 8. Current next action

```text
M4 Trusted Durable Memory       ✓ complete
        |
        v
Gate D Action/Credential Policy <- next
        |
        v
M5 narrow GitHub writes
        |
        v
M6 UI -> M7 Recovery -> M8 E2E -> v0.1 release gate
```

Gate D remains mandatory before any GitHub write capability. Do not implement v0.2 capabilities before the v0.1 exit gate is satisfied.
