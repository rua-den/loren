# Loren Master Delivery Plan

**Status:** Active planning baseline

This document is the top-level delivery plan for Loren. Detailed implementation plans may exist per version, but no version should expand scope without passing the previous version's exit gate.

The roadmap is **capability-driven, not date-driven**. A version number represents a proven increase in trust and usefulness, not the amount of code written.

---

# 1. Product objective

Loren should become a persistent personal intelligence system that:

- knows the owner's durable context without being re-taught every session;
- uses authoritative tools for external facts and actions;
- reasons through replaceable brain providers;
- acts only through explicit Loren-owned policy and credential boundaries;
- can explain and audit consequential behavior;
- gradually becomes more proactive only after the lower-level trust model is proven.

The long-term product is **not** a model wrapper and **not** a generic agent framework.

---

# 2. Architectural invariants across all versions

These are not optional roadmap items. Breaking one requires an explicit superseding ADR.

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
database engine (after migration)
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

Loren uses pre-1.0 versions as **capability maturity steps**.

```text
v0.0   architecture / feasibility
v0.1   trustworthy core
v0.2   useful project assistant
v0.3   personal operations
v0.4   voice + device presence
v0.5   proactive/background agent
v0.6+  hardening based on real daily use
v1.0   stable personal daily driver
```

Patch releases (`v0.x.y`) may add narrow capabilities or hardening without changing the main trust boundary of the minor version.

No version is promoted because "most tasks are done". Promotion happens only after its **exit gate** passes.

---

# 4. Decision gates

## Gate A — Core ownership [PASSED]

**Decision:** ADR-001.

Loren owns canonical identity/state/policy/action authorization; models, runtimes, MCP, and external frameworks are adapters.

This gate must remain true for every later version.

## Gate B — v0.1 implementation stack [OPEN]

**Decision:** ADR-002.

Default candidate:

```text
C# 14 / .NET 10
ASP.NET Core
small Loren-owned agent loop
OpenAI Responses API as first brain
MCP C# SDK behind adapter
SQLite + EF Core
Blazor Web App
```

Pass after Milestone 0 proves the provider loop, MCP adapter, persistence path, and web host without violating ADR-001.

## Gate C — Canonical storage and memory schema [before v0.1 write workflows stabilize]

Must settle:

- canonical database schema/migration policy;
- durable-memory source/trust classes;
- correction/supersession semantics;
- export/restore versioning;
- retention/deletion behavior for memory versus audit.

A dedicated ADR is required if storage becomes more complex than the initial SQLite/EF Core choice.

## Gate D — Action/credential policy [before first real external write]

Must settle:

- action contract;
- policy dimensions;
- approval binding/replay rules;
- credential storage/resolution;
- secret redaction;
- global read-only/kill behavior.

No write-capable integration may ship before this gate passes.

## Gate E — Background execution [before scheduler/events become trusted]

Required before v0.3 background operations or v0.5 proactive behavior:

- persistent job model;
- timezone/missed-run semantics;
- bounded retry/backoff;
- cancellation;
- quotas;
- notification policy;
- task ownership and visibility;
- safe restart/resume semantics.

## Gate F — Trusted devices and voice approval [before v0.4]

Must settle:

- trusted-device enrollment/revocation;
- session/device identity;
- voice privacy/data retention;
- which actions can never rely on weak voice-only confirmation;
- remote-access transport/authentication.

## Gate G — Proactive autonomy [before v0.5]

Must settle:

- standing-permission representation;
- event trust model;
- notification prioritization/rate limits;
- background cost/tool/runtime quotas;
- self-created task limits;
- global pause and incident controls;
- prompt-injection tests for event-driven workflows.

## Gate H — v1 stable contract [before v1.0]

Must settle and document:

- canonical export/restore compatibility policy;
- supported upgrade/migration path;
- stable core action/brain/skill interfaces;
- backup/recovery procedure;
- secret rotation/revocation procedure;
- operational monitoring and incident procedure;
- minimum privacy/security baseline for daily personal data.

---

# 5. Version milestones

## v0.0 — Architecture and feasibility

### Goal

Prove that Loren has a coherent ownership boundary and an implementable v0.1 stack.

### Milestones

- M0.1 product vision and architecture baseline;
- M0.2 agent/runtime landscape research;
- M0.3 ADR-001 accepted: Loren-owned core/runtime boundary;
- M0.4 ADR-002 technical validation;
- M0.5 v0.1 plan and repository engineering rules finalized.

### Exit gate

v0.0 is complete when:

- Gate A is passed;
- Gate B is passed;
- a clean engineering stack can be scaffolded;
- one brain/tool round trip has been proven in a disposable spike;
- no unresolved architecture question blocks the v0.1 walking skeleton.

**Version transition:** `v0.0 -> v0.1 development` only after ADR-002 becomes Accepted.

---

## v0.1 — Trustworthy Core

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

- M1 engineering foundation;
- M2 walking skeleton: owner -> brain -> Action Gateway -> GitHub read -> audit;
- M3 canonical project/repository state;
- M4 trusted durable memory + correction/retrieval;
- M5 action/credential boundary + narrow GitHub writes;
- M6 minimal daily-use UI;
- M7 export/restore and recovery proof;
- M8 adversarial security/reliability E2E.

### Exit gate

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

### Goal

Turn the trustworthy v0.1 core into something useful for daily project planning/research, without yet expanding deeply into private personal systems.

### Candidate capabilities

- public web research with provenance/citations;
- controlled promotion of research into project memory/decisions;
- persistent one-time reminders and lightweight scheduler;
- richer Project/Decision/Procedure records only where needed;
- project health summaries from GitHub;
- reusable project operating procedures;
- improved memory retrieval and conflict resolution;
- cost/token/run visibility;
- optional second brain provider spike to prove provider abstraction if useful.

### Milestones

- M2.1 safe web retrieval boundary including SSRF/private-network controls;
- M2.2 research -> sourced conclusion -> explicit memory promotion flow;
- M2.3 persistent reminder scheduler with timezone/restart/cancel semantics;
- M2.4 procedural/project decision memory;
- M2.5 daily project workflow hardening and UX.

### Exit gate

v0.2 is complete when Loren can repeatedly:

```text
research a project question
-> cite current sources
-> distinguish source fact from memory
-> remember an approved conclusion
-> recall the decision later

schedule a project reminder
-> survive restart
-> execute once at the correct local time
-> remain visible/cancellable/auditable
```

Additionally:

- web content cannot grant tool permissions or trusted personal memory;
- scheduler retries are bounded;
- project workflow can be used daily without direct DB/admin manipulation.

**Checkpoint before v0.3:** Gate E must pass for any background work that will touch private integrations.

---

## v0.3 — Personal Operations

### Goal

Expand Loren from project intelligence into a controlled personal digital operator.

### Candidate capabilities

- Gmail read/search/draft first; sending remains tightly gated;
- Google Calendar read/create/update;
- server/VPS health and constrained operational actions;
- richer filesystem integration;
- notifications;
- cross-tool project context;
- project-aware/day-aware brief;
- stronger credential scopes and integration health status.

### Milestones

- M3.1 personal-data classification and connector credential scopes;
- M3.2 Calendar integration;
- M3.3 Gmail integration;
- M3.4 server/VPS read health path, then constrained write actions;
- M3.5 cross-tool context minimization/redaction;
- M3.6 daily brief / personal ops UX.

### Exit gate

- private data is sent to brain providers only according to data policy;
- read/write scopes are separated where practical;
- cross-tool answers preserve provenance;
- sending/modifying external systems remains understandable and approval-bound;
- background operations are visible and cancellable;
- connector failure cannot corrupt canonical Loren state.

**Checkpoint before v0.4:** Gate F must pass.

---

## v0.4 — Voice and Device Presence

### Goal

Make Loren conveniently available away from the desktop while preserving the same core identity, memory, and permission system.

### Candidate capabilities

- installable PWA/mobile-friendly UI;
- push-to-talk;
- speech-to-text;
- text-to-speech;
- trusted device enrollment;
- notification actions;
- optional desktop/device node;
- optional messaging channel adapter.

### Milestones

- M4.1 trusted-device/session model;
- M4.2 push-to-talk voice path;
- M4.3 TTS response path;
- M4.4 mobile/PWA approval UX;
- M4.5 device revocation and lost-device test;
- M4.6 optional desktop/device-node capability boundary.

### Exit gate

- voice never creates a separate memory/policy path;
- sensitive audio retention policy is explicit;
- privileged approvals require sufficient device/user assurance;
- lost/revoked devices lose access promptly;
- critical actions are not authorized merely because a voice sounded like the owner.

**Checkpoint before v0.5:** Gate G must pass.

---

## v0.5 — Proactive Loren

### Goal

Allow Loren to notice meaningful events and perform bounded background work under explicit standing policy.

### Candidate capabilities

- event bus/envelope;
- GitHub/webhook watchers;
- calendar preparation;
- server health triggers;
- email-derived follow-up candidates;
- recurring workflows;
- background research;
- proactive notifications;
- policy-controlled standing actions;
- global pause/kill switch.

### Milestones

- M5.1 normalized event ingestion;
- M5.2 proactive evaluator with no write authority by default;
- M5.3 notification prioritization/rate limiting;
- M5.4 standing permissions for a tiny allowlisted action set;
- M5.5 bounded recurring/background tasks;
- M5.6 kill switch, incident mode, active-task visibility;
- M5.7 adversarial event/prompt-injection suite.

### Exit gate

- event content cannot grant itself authorization;
- background tasks have visible owner, state, quota, and cancellation;
- false-positive notification rate is acceptable in real use;
- standing permissions are inspectable and revocable;
- global pause works even when a runtime/provider is unhealthy;
- no recursive/self-created task pattern can grow without bounds.

---

## v0.6+ — Daily-use hardening

Do not pre-design these versions in detail. Use actual Loren usage to decide what deserves promotion.

Likely themes:

- better memory consolidation;
- richer world model only from demonstrated needs;
- additional brain providers/local models;
- Home Assistant;
- desktop/computer use;
- more skills/integrations;
- performance/cost optimization;
- improved offline/private execution;
- UX refinement;
- packaging/deployment simplification.

Each new high-risk capability gets its own ADR/gate rather than silently entering the core.

---

## v1.0 — Stable Personal Daily Driver

### Meaning of v1.0

v1.0 does **not** mean Loren has every Jarvis feature. It means the core product can be trusted as the owner's long-lived assistant and can evolve without casually losing state or bypassing security boundaries.

### Minimum v1.0 properties

- daily-use workflows are demonstrably useful;
- canonical state has tested backup/export/restore and migration procedures;
- upgrades preserve identity/memory/policy;
- core brain/action/skill contracts are stable enough for maintained adapters;
- secret rotation/revocation is documented and tested;
- trusted devices and background work have reliable controls;
- audit/incident tooling can reconstruct consequential behavior;
- external integrations can fail without destroying Loren's core state;
- model/provider replacement is possible behind the brain boundary;
- security/privacy defaults are documented rather than implicit.

### v1.0 release gate

Gate H must pass and Loren must have been used as a real daily driver long enough for failure modes to come from observed operation, not only synthetic tests.

---

# 6. Milestone execution rules

For every milestone:

1. define the user-visible or architecture behavior being proven;
2. define acceptance tests before broad implementation;
3. build the smallest vertical slice that proves it;
4. keep provider/framework SDK types outside Loren.Core;
5. add audit/observability with the capability, not months later;
6. update ADRs when a previously open architectural choice becomes expensive to reverse;
7. do not add adjacent features merely because a framework makes them easy;
8. finish with tests/build/lint and document any known gaps.

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

The project is currently at **v0.0 / Gate B**.

Next execution sequence:

```text
ADR-002 Milestone 0 spike
        |
        v
accept/revise ADR-002
        |
        v
scaffold .NET solution
        |
        v
begin v0.1 M1 -> M2 walking skeleton
```

Do not implement v0.2 capabilities before the v0.1 exit gate is satisfied.
