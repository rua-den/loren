# Loren Roadmap

This is the concise product roadmap. The authoritative delivery sequence, milestones, and version gates live in [`docs/plans/master-plan.md`](plans/master-plan.md).

Loren advances by **proven capability and trust**, not by calendar dates.

## Current status

**Current stage:** `v0.1 — Trustworthy Core development`  
**Passed:** `Gate A — Core ownership`, `Gate B — v0.1 implementation stack`, `Gate C — canonical state/memory lifecycle`  
**Completed milestone:** `M4 — Trusted Durable Memory`  
**Next:** `Gate D — Action/Credential Policy before M5 writes`

M2 proved the first owner-testable authenticated model-to-tool path. M3 made Project/Repository identity provider-independent and restart-safe. M4 then made durable memory provider-independent, restart-safe, correctable, forgettable, bounded before brain use, and resistant to model/external-content poisoning.

M4 path:

```text
OWNER_EXPLICIT durable memory
-> restart-safe SQLite persistence
-> OWNER_CORRECTION append/supersede
-> current-memory retrieval
-> source/provenance trust filtering
-> bounded prepared memory context
-> owner forget / full correction-chain purge
-> adversarial poisoning boundary
```

M4 evidence:

- PR #18 — OWNER_EXPLICIT persistence;
- PR #19 — correction/supersession;
- PR #20 — authority-aware prepared context;
- PR #21 — forget/delete without resurrection;
- PR #22 — provenance and poisoning/trust-boundary acceptance.

Implementation CI #140 / run `33866182751` passed the Slice 5 build/test/format/secret/dependency/web-auth gates. PR #22 requires one final exact-head pass before merge.

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
M4 Trusted Durable Memory              ✓ complete
Gate D Action/Credential Policy        <- next
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
- trusted durable memory with provenance, correction and forgetting;
- ActionGateway + approvals;
- credential-isolated GitHub read/narrow writes;
- audit trail;
- export/wipe/restore proof;
- adversarial security/reliability tests.

### Gate D before M5

Gate D must define approval binding/non-replay, write-action contracts, credential storage/resolution and read/write separation, redaction/rotation/revocation, global read-only/kill behavior, post-write verification, and audit expectations.

No real GitHub write path may be enabled until Gate D passes.

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
