# Loren Master Delivery Plan

**Status:** Active capability-driven roadmap  
**Updated:** 2026-09-19  
**Current phase:** `v0.1 — Useful Trustworthy Assistant`  
**Current milestone:** `M6B — Daily Driver Readiness`  
**Current delivery:** `M6B.2 — readiness diagnostics + source-of-truth rebaseline`  
**Next product gate:** real-provider owner acceptance  
**Paused:** broader GitHub file/commit/PR writes until real daily-driver evidence says otherwise.

> Loren is the owner's persistent personal secretary / Jarvis-like intelligence system. GitHub automation is one capability, not the product identity.

---

# 1. Product objective

Loren should become a long-lived personal intelligence that can:

1. talk naturally with the owner;
2. carry durable personal/project context across sessions and provider changes;
3. answer stable questions directly when external facts are unnecessary;
4. retrieve current information from authoritative read tools;
5. research multiple sources and expose provenance/uncertainty;
6. maintain owner-controlled notes, decisions and tasks;
7. read connected personal/project systems with explicit scopes;
8. propose consequential actions in conversation;
9. require explicit owner approval for consequential external actions;
10. execute through Loren-owned policy/credential/audit boundaries;
11. verify external writes independently;
12. later gain background, proactive and voice behavior only behind new trust gates.

The model is replaceable compute. Loren owns identity, state, context, organization, authorization and history.

---

# 2. Capability order

```text
CONVERSE
  -> REMEMBER
  -> READ CURRENT INFORMATION
  -> RESEARCH / SYNTHESIZE
  -> ORGANIZE
  -> PRESENT PROPOSED ACTION
  -> OWNER APPROVES
  -> ACT
  -> VERIFY / AUDIT
  -> later SCHEDULE / PROACT / VOICE
```

Consequences:

- read/understand before broad external mutation;
- owner-visible vertical slices before generic frameworks;
- external/model/retrieved content is data, never authority;
- no background execution before Gate E;
- current daily-use failures outrank speculative capability expansion.

---

# 3. Architectural invariants

Breaking one requires an explicit superseding ADR.

## Loren owns

```text
identity
canonical personal/project state
trusted durable memory
notes / decisions / tasks
permissions / policy
approval artifacts
action gateway
audit / history
context assembly
```

## Replaceable infrastructure

```text
brain/model provider
web/search provider
MCP servers
vendor APIs
UI clients
secret-store backend
execution runtime
database engine after migration
notification channels
device/voice runtime
```

## Trust invariants

1. The model may propose actions; it may not authorize itself.
2. Privileged credentials stay outside model-visible context/arguments.
3. Authentication identifies the owner; it is not external-write approval.
4. Private owner state requires authenticated Loren-owned owner context.
5. External/model/tool content cannot self-promote to memory, policy, owner state or approval.
6. Consequential external writes are canonical-target-bound, one-time approved, credential-isolated, audited and post-verified.
7. Credential revocation overrides prior approval.
8. Global external-write mode fails closed.
9. Current facts use read tools instead of stale guessing when verification is required.
10. Canonical state remains recoverable independently from model/provider sessions.
11. Diagnostics never expose secret values.
12. Background execution requires Gate E.

---

# 4. Version path

```text
v0.0   architecture / feasibility             ✓ complete
v0.1   useful trustworthy assistant           <- current
v0.2   provider portability + secretary reads
v0.3   personal/project operations
v0.4   voice + device presence
v0.5   proactive/background Loren
v0.6+  daily-use hardening
v1.0   stable personal daily driver
```

---

# 5. Decision gates

## Gate A — Core ownership [PASSED]

ADR-001: Loren owns canonical identity/state/policy/action authorization; providers/runtimes are adapters.

## Gate B — v0.1 stack [PASSED]

ADR-002 baseline: C#/.NET 10, ASP.NET Core, Loren-owned bounded AgentLoop, provider-neutral `IBrain`, SQLite + EF Core, owner web UI, xUnit.

## Gate C — Canonical state + memory lifecycle [PASSED]

ADR-003: opaque Loren IDs, checked-in migrations, Project/Repository boundary, memory provenance, append/supersede correction, forget semantics, logical export direction.

## Gate D — External action/approval/credential boundary [PASSED]

ADR-004: exact external-write intent, canonical target binding, explicit owner approval, one-time consume, global read-only, credential isolation/revocation, post-write verification and redacted audit.

## Gate E — Background execution [NOT YET]

Required before trusted work can run without active owner presence:

- persistent job identity/state;
- timezone + missed-run semantics;
- bounded retry/backoff;
- cancellation;
- quotas;
- notification policy;
- safe restart/resume.

A stored task/due date is data; background reminder delivery requires Gate E.

## Gate F — Trusted devices / voice approval [LATER]

Required before voice/device trust can authorize sensitive actions.

## Gate G — Proactive autonomy [LATER]

Required before standing permissions/event-driven work/self-created recurring operations.

## Gate H — v1 stable contract [LATER]

Recovery compatibility, upgrade/migration stability, secret rotation, operations and privacy/security baseline.

---

# 6. Proven v0.1 foundation

```text
M1 Engineering Foundation                      COMPLETE
M2 Conversation/tool Walking Skeleton          COMPLETE
M3 Canonical Project/Repository                 COMPLETE
M4 Trusted Durable Memory                      COMPLETE
Gate D                                          PASSED
M5 Slices 1–3                                  COMPLETE
M6A.1 Conversation Primary Surface             COMPLETE
M6A.2 Current-information web read             COMPLETE
M6A.3 Source-aware bounded research            COMPLETE
M6A.4 Notes / Decisions / Tasks                COMPLETE
M6A.5 Conversational approval code             COMPLETE; live proof pending
Conversation continuity                         COMPLETE
Windows launcher                                COMPLETE
Logical export/restore                          COMPLETE
Retained audit                                  COMPLETE
M6B.1 transient audit lifetime                 COMPLETE
```

The latest fully verified baseline before M6B.2 is PR #37 merge `ba60246a410b257097ecd537f4d977b402a37b35`, post-merge CI #280 / `35373858830` PASS on Ubuntu full gate and Windows integration/launcher.

The single verified external mutation primitive remains non-default GitHub branch creation. File/commit/open-PR expansion is deliberately paused.

---

# 7. M6B — Daily Driver Readiness

M6B shifts the project from adding trust primitives to proving the existing product is diagnosable and usable every day.

## M6B.1 — Bound transient request state [COMPLETE]

Request-local audit collection is scoped; SQLite remains durable audit. Long-running Loren no longer accumulates every request's transient audit in one singleton collector.

## M6B.2 — Safe readiness + source-of-truth rebaseline [CURRENT DELIVERY]

Owner-visible contract:

```text
/health          public liveness only
/api/readiness   authenticated configuration/state diagnostics
```

Readiness reports:

- SQLite connectivity;
- project catalog availability + project count;
- owner auth configured;
- current production brain config valid;
- web search/fetch config valid;
- external writes enabled/disabled;
- write credential ready/missing/revoked/not-configured when writes are enabled.

Rules:

- never return secret values;
- no external provider/network calls;
- read-only external-write mode is a valid ready posture;
- zero projects is informational;
- unavailable project catalog/storage is a readiness failure;
- write credential problems matter only when writes are enabled;
- provider reachability is owner live-proof evidence, not readiness configuration evidence.

Acceptance coverage must prove auth boundary, liveness separation, safe read-only ready state, missing write credential and revoked write credential without secret exposure.

## M6B.3 — Real owner daily-driver proof [NEXT PRODUCT GATE]

Use [`../owner-checkpoint.md`](../owner-checkpoint.md).

The owner should prove:

```text
normal conversation
current information + sources
multi-source research
canonical project + live GitHub read
memory/decision across restart
task lifecycle across restart
conversation continuity
read-only readiness
Cancel proposal -> no branch
fresh proposal -> Approve -> exact SHA read-back
return writes to disabled
```

Real failures from this checkpoint define the next bug/UX slices.

---

# 8. v0.1 closeout

Tag `v0.1.0` only after owner acceptance and release-gate review show no material blockers.

Required confidence:

- natural conversation is the primary surface;
- stable/current/research questions choose the right evidence path;
- memory/organization/project context survive restart;
- recovery semantics are usable and fail closed;
- one consequential action is explicitly approved and post-verified;
- replay/revocation/read-only protections remain intact;
- credentials never leak;
- core behavior remains deterministic-testable without live provider dependency;
- exact-head and post-merge CI are green.

Do not resume broad GitHub mutation merely to make v0.1 look larger.

---

# 9. v0.2 direction

There is one important architecture/product gap to prove first: provider neutrality exists at the `IBrain` contract, but current production host composition uses `OllamaBrain` and `Loren.Brain.OpenAI` is only a stub. Implement and accept a second provider before claiming operational provider portability.

Then choose one useful **read-only** personal-secretary integration, likely Calendar read/search, and build the smallest end-to-end flow. Use that real flow to discover the connector abstraction rather than building a generic integration framework first.

Personal-system writes get separate credential/approval semantics. Background delivery still waits for Gate E.

---

# 10. Later versions

## v0.3 — Personal / Project Operations

Approved calendar/mail/project writes, cross-tool workflows, constrained server/VPS actions, bounded background work after Gate E.

## v0.4 — Voice + Device Presence

Trusted devices, PWA/mobile, push-to-talk, STT/TTS, notifications. Gate F for sensitive approval.

## v0.5 — Proactive Loren

Event ingestion, proactive evaluation, bounded recurring work, quotas and global pause. Gate G required.

## v0.6+ / v1.0

Daily-use hardening, recovery/upgrade compatibility, more integrations/providers, privacy/security/operations maturity. Gate H before v1.0.

---

# 11. Execution rules

For every milestone:

1. define owner-visible behavior first;
2. add regression/acceptance coverage before or with the fix;
3. classify trust boundary explicitly;
4. build the smallest coherent vertical slice;
5. keep trusted metadata outside model-visible args;
6. preserve provenance/audit;
7. self-review complete diff;
8. run available local checks honestly;
9. use one coherent commit/push by default;
10. require exact-head Ubuntu + Windows CI before merge;
11. require post-merge main CI;
12. synchronize status/handoff/roadmap/architecture/README when the checkpoint changes.

Stop the line if the model/runtime can bypass ActionGateway, private owner state loses authentication, credentials leak, approval can replay/broaden, current information silently guesses when a read is required, external content becomes authority, or external writes report success without verification.

---

# 12. Current execution sequence

```text
M1–M4                                         ✓
Gate D + M5 Slices 1–3                       ✓
M6A.1–M6A.5 code                             ✓
continuity / launcher / recovery / audit     ✓
M6B.1 transient audit lifetime               ✓
M6B.2 readiness + docs                       <- CURRENT DELIVERY
        |
        v
M6B.3 REAL OWNER DAILY-DRIVER CHECKPOINT
        |
        v
fix real-use blockers / assess v0.1.0
        |
        v
v0.2 provider portability + first personal read integration
```
