# Loren Roadmap

This is the concise product roadmap. The authoritative delivery sequence, milestones, and version gates live in [`docs/plans/master-plan.md`](plans/master-plan.md).

Loren advances by **proven capability and trust**, not by calendar dates.

## Current status

**Current stage:** `v0.1 — Trustworthy Core development`  
**Passed:** `Gate A — Core ownership`, `Gate B — v0.1 implementation stack`, `Gate C — canonical state/memory lifecycle`  
**Current milestone:** `M4 — Trusted Durable Memory`

M2 proved the first owner-testable authenticated model-to-tool path. M3 then made Project/Repository identity provider-independent and restart-safe.

M3 evidence:

```text
M3 Slice 1
canonical ProjectId / RepositoryId
-> SQLite + EF Core persistence
-> migrations
-> alias collision/update/restart tests

M3 Slice 2
exact configured project alias
-> IProjectCatalog
-> ProjectSnapshot
-> small prepared BrainContext
-> AgentLoop / IBrain

M3 Slice 3
ADR-003 / Gate C
-> canonical ID rules
-> migration policy
-> memory source authority
-> append/supersede correction
-> memory-vs-audit deletion boundary
-> logical export format versioning
```

PR #15 merged at `00fbba08587ba8275c121fd7f9532a785f55314d`. PR #16 merged at `56fd988d3b74c754604355e3c97a5d3656675bbb`; final PR CI #108 and post-merge main CI #109 passed all repository gates.

Next step:

```text
MemoryRecord canonical model
-> OWNER_EXPLICIT save + provenance
-> Project-scoped persistence
-> restart-safe trusted retrieval
-> OWNER_CORRECTION append/supersede
-> poisoning tests for EXTERNAL_CONTENT / MODEL_INFERENCE
-> prepared memory context
```

---

## v0.0 — Architecture and feasibility [COMPLETE]

Goal: establish the ownership boundary and prove the concrete v0.1 stack is implementable.

Exit evidence:

- ADR-001 Accepted;
- ADR-002 Accepted after technical spikes;
- v0.1 implementation plan finalized;
- brain/action/MCP/persistence/host boundaries proven enough to scaffold production code.

---

## v0.1 — Trustworthy Core [ACTIVE]

Goal: build the smallest trustworthy Loren.

Required flows:

```text
"Loren, repo wedding hiện sao rồi?"
"Nhớ rằng project này production deploy phải hỏi tao."
"Tạo branch và chuẩn bị thay đổi X."
"Tại sao mày vừa làm việc đó?"
```

Milestones:

```text
M1 Engineering Foundation              ✓ complete
M2 Walking Skeleton                    ✓ complete
M3 Canonical Project/Repository State  ✓ complete
M4 Trusted Durable Memory              <- current
M5 Action/Credential Boundary + Writes
M6 Minimal Daily-use UI
M7 Export/Restore + Recovery
M8 Security/Reliability E2E
```

Core capabilities across v0.1:

- one-owner web interface;
- provider-neutral brain boundary;
- bounded Loren-owned agent loop;
- canonical Project/Repository state;
- trusted durable memory with correction/provenance;
- ActionGateway + approvals;
- credential-isolated GitHub read/narrow writes;
- audit trail;
- export/wipe/restore proof;
- adversarial security/reliability tests.

Detailed plan: [`docs/plans/v0.1.md`](plans/v0.1.md)

---

## v0.2 — Useful Project Assistant

Goal: make the trusted core useful for richer daily project work.

Candidate capabilities:

- public web research with source provenance;
- explicit research-to-memory/decision promotion;
- persistent reminders/light scheduler;
- project decisions/procedures;
- richer GitHub project health summaries;
- improved memory retrieval/conflict handling;
- model/run cost visibility.

Before private/background personal operations, Gate E must pass.

---

## v0.3 — Personal Operations

Candidate capabilities include Gmail, Google Calendar, server/VPS health and constrained actions, filesystem integrations, notifications, cross-tool context, daily brief, and stronger data/credential scopes.

---

## v0.4 — Voice and Device Presence

Candidate capabilities include mobile/PWA, trusted devices, push-to-talk, STT/TTS, notification actions, and optional device nodes. Gate F must pass first.

---

## v0.5 — Proactive Loren

Candidate capabilities include event ingestion, GitHub/webhook watchers, proactive notifications, recurring/background work, bounded standing permissions, quotas, active-task visibility, and global pause. Gate G must pass first.

---

## v0.6+ — Real-use hardening

Let actual usage determine priorities: memory consolidation, more providers/local models, Home Assistant, computer use, offline/private execution, performance/cost, packaging, and UX.

---

## v1.0 — Stable Personal Daily Driver

v1.0 means Loren's core can be trusted as a long-lived daily assistant: stable workflows, tested recovery/migration, continuity across upgrades/providers, maintainable action/skill boundaries, reliable background/device controls, secret rotation, reconstructable audit, and documented privacy/security defaults.

Gate H must pass before release.

---

## Ongoing rule

At every version ask:

> Does this capability strengthen Loren's personal intelligence, or are we rebuilding infrastructure a mature project already solves better?

Reuse infrastructure where it is safe and replaceable. Spend custom engineering on Loren's identity, memory, policy, personal semantics, and trustworthy behavior.
