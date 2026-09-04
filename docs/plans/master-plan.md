# Loren Master Delivery Plan

**Status:** Active planning baseline  
**Current phase:** `v0.1 — Trustworthy Core development`  
**Current milestone:** `M3 — Canonical Project/Repository State`

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
v0.1   trustworthy core                 <- current development / M3
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
  ├─ Ollama adapter
  ├─ OpenAI adapter
  └─ future cloud/local providers
MCP C# SDK behind Loren action contracts
SQLite + EF Core
Blazor Web App
xUnit
```

M0 proved provider/tool/MCP/persistence/host feasibility. M1 rebuilt production code behind Loren-owned interfaces. M2 then proved the real authenticated production read path end to end.

## Gate C — Canonical storage and memory schema [ACTIVE PREPARATION]

Must settle before broad memory/write workflows stabilize:

- canonical database schema/migration policy;
- stable Loren ID rules;
- durable-memory source/trust classes;
- correction/supersession semantics;
- export/restore versioning;
- retention/deletion behavior for memory versus audit.

M3 is responsible for making the Project/Repository storage boundary concrete enough to close this gate before M4 expands memory.

A dedicated ADR is required if storage becomes materially more complex than the accepted SQLite/EF Core baseline.

## Gate D — Action/credential policy [before first real external write]

Must settle:

- action contract;
- policy dimensions;
- approval binding/replay rules;
- credential storage/resolution;
- secret redaction;
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

Completed:

- M0.1 product vision and architecture baseline;
- M0.2 agent/runtime landscape research;
- M0.3 ADR-001 accepted;
- M0.4 ADR-002 provider-neutral technical validation;
- M0.5 v0.1 plan and repository engineering/progress rules finalized.

**Transition completed:** `v0.0 -> v0.1 development` on 2026-09-03.

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
- **M2 walking skeleton: owner -> brain -> Action Gateway -> GitHub read -> audit — COMPLETE**;
- **M3 canonical project/repository state — ACTIVE**;
- M4 trusted durable memory + correction/retrieval;
- M5 action/credential boundary + narrow GitHub writes;
- M6 minimal daily-use UI;
- M7 export/restore and recovery proof;
- M8 adversarial security/reliability E2E.

### M1 completion evidence

M1 established the production scaffold with .NET SDK `10.0.400`, provider-neutral Core contracts, bounded Runtime loop, deterministic tests, central dependency versions, development documentation, and CI gates for restore/build/test/format/secret/dependency/health checks.

### M2 completion evidence

M2 completed on 2026-09-04.

Main implementation commit:

```text
94ce6d1e74f2dfdf0584b8dbf8a4edbbb3774f7d
```

Main CI run `33840135772` passed restore/build/test/format/secret/dependency/auth smoke checks.

Trusted exact-main workflow run `33840149005` proved:

```text
unauthenticated /api/run -> 401
owner login -> authenticated cookie session
authenticated /api/run
 -> real Ollama gpt-oss:120b
 -> ActionRequest(github.read_repository)
 -> Loren ActionGateway / ReadOnlyActionPolicy
 -> real GitHub GET rua-den/loren
 -> structured ActionResult
 -> Ollama final answer
 -> correlated owner-visible audit
```

Observed result:

```text
runId:       5bb9cc341387430c82759d58309da85a
turns:       2
actionCount: 1
final:       rua-den/loren / main
```

Audit passed `ActionRequested -> PolicyEvaluated -> ActionCompleted`, ending in `succeeded`. Owner/provider credentials were absent from the owner-visible response and the temporary development run route remained unavailable in Production.

This closes the M2 architecture gate: the first owner-testable Loren preview is proven on the normal production owner path.

### M3 active target

Build only the minimum provider-independent world model required by v0.1:

```text
Owner
Project
Repository
MemoryRecord
PermissionRule
AuditEvent
```

M3 starts with the canonical Project/Repository identity and persistence pieces only.

Acceptance target:

```text
"wedding project"
"web đám cưới"
"wedding-online"
 -> same Loren Project
 -> Repository rua-den/wedding-online
```

The mapping must survive Loren restart and provider-session deletion.

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

Candidate milestones/capabilities:

- safe public web retrieval with provenance and SSRF/private-network controls;
- research -> sourced conclusion -> explicit trusted-memory promotion;
- persistent reminders/light scheduler;
- project decisions/procedures;
- richer GitHub project health summaries;
- improved memory retrieval/conflict handling;
- cost/token/run visibility;
- optional additional provider/local-model validation when useful.

Exit requires sourced research and persistent reminders to remain inspectable, bounded, cancellable, and unable to self-grant trusted permissions/memory.

**Checkpoint before private background operations:** Gate E.

---

## v0.3 — Personal Operations

Candidate milestones/capabilities:

- personal-data classification and connector credential scopes;
- Calendar;
- Gmail read/search/draft before tightly gated send;
- server/VPS read health then constrained actions;
- filesystem integration;
- cross-tool context minimization/redaction;
- daily brief / personal ops UX.

Private data handling, connector failure, and consequential writes must remain within the same permission/audit boundaries established earlier.

**Checkpoint before v0.4:** Gate F.

---

## v0.4 — Voice and Device Presence

Candidate milestones/capabilities:

- trusted-device/session model;
- mobile/PWA interface;
- push-to-talk;
- speech-to-text and TTS;
- notification actions;
- device revocation/lost-device testing;
- optional desktop/device node.

Voice must never create a second memory/policy path or become sufficient authorization for high-risk actions.

**Checkpoint before v0.5:** Gate G.

---

## v0.5 — Proactive Loren

Candidate milestones/capabilities:

- normalized event ingestion;
- proactive evaluator with no write authority by default;
- notification prioritization/rate limiting;
- tiny allowlisted standing permissions;
- bounded recurring/background tasks;
- active-task visibility and global pause/kill switch;
- adversarial event/prompt-injection suite.

Background work must remain owned, visible, bounded, cancellable, and unable to recursively create unbounded work.

---

## v0.6+ — Daily-use hardening

Do not pre-design deeply. Let actual use determine priorities: memory consolidation, more providers/local models, Home Assistant, computer use, more integrations, offline/private execution, cost/performance, UX, and packaging/deployment simplification.

Each new high-risk capability gets its own ADR/gate rather than silently entering the core.

---

## v1.0 — Stable Personal Daily Driver

v1.0 means Loren's core can be trusted as the owner's long-lived assistant and can evolve without casually losing state or bypassing security boundaries.

Minimum properties include stable daily-use workflows, tested backup/export/restore/migration, upgrade continuity, stable brain/action/skill interfaces, secret rotation/revocation, reliable controls for trusted devices/background work, reconstructable audit, integration-failure isolation, replaceable model/provider, and documented privacy/security defaults.

Gate H must pass before release.

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

Do not continue to the next milestone/version if any of these is true:

- model/runtime can bypass the Action Gateway;
- privileged credential appears in model-visible content/logs;
- canonical state requires a provider/runtime session to survive;
- external content can create trusted owner policy without promotion;
- destructive/external writes cannot be post-verified or reconstructed;
- recovery/export is known broken;
- a new abstraction exists only for hypothetical future framework support;
- tests rely exclusively on live model behavior and cannot exercise deterministic core logic.

Fix the boundary first, then continue.

---

# 8. Current next action

The project is currently at **v0.1 / M3 — Canonical Project/Repository State**.

```text
stable Loren canonical IDs
        |
Project + Repository domain model
        |
SQLite / EF Core persistence + migration
        |
project aliases + deterministic resolver
        |
restart/provider-session independence tests
        |
runtime prepared-context integration
        |
M3 acceptance flow
        |
Gate C checkpoint
        |
M4 Trusted Memory
```

Do not implement v0.2 capabilities before the v0.1 exit gate is satisfied.
