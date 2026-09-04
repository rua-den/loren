# Loren Roadmap

This is the concise product roadmap. The authoritative delivery sequence, milestones, and version gates live in [`docs/plans/master-plan.md`](plans/master-plan.md).

Loren advances by **proven capability and trust**, not by calendar dates.

## Current status

**Current stage:** `v0.1 — Trustworthy Core development`  
**Passed:** `Gate A — Core ownership`, `Gate B — v0.1 implementation stack`  
**Current milestone:** `M2 — Walking Skeleton`

M2 has a proven real Ollama -> Loren -> GitHub read -> final-answer backend path and now has the one-owner authentication, protected request console, and owner-visible audit implementation. The remaining M2 exit proof is the exact-main trusted live run through that authenticated production owner path.

Next step:

```text
owner preview CI + merge
-> trusted exact-main owner-authenticated live read proof
-> M2 COMPLETE
-> M3 Canonical State
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

Core capabilities:

- one-owner web interface;
- provider-neutral brain boundary with a proven Ollama implementation and optional OpenAI adapter;
- bounded Loren-owned agent loop;
- Project/Repository canonical state;
- trusted durable memory with correction/provenance;
- Action Gateway + approvals;
- credential-isolated GitHub read/narrow writes;
- audit trail;
- export/wipe/restore proof;
- adversarial security/reliability tests.

Milestones:

```text
M1 Engineering Foundation             ✓ complete
M2 Walking Skeleton                   <- current
M3 Canonical Project/Repository State
M4 Trusted Durable Memory
M5 Action/Credential Boundary + Writes
M6 Minimal Daily-use UI
M7 Export/Restore + Recovery
M8 Security/Reliability E2E
```

Explicitly not required:

- broad web research;
- scheduler/reminders;
- Gmail/Calendar;
- voice;
- proactive/background autonomy.

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
- model/run cost visibility;
- optional second brain-provider proof if useful.

Before moving into private/background personal operations, the scheduler/background execution gate must be proven.

---

## v0.3 — Personal Operations

Goal: expand Loren into a controlled personal digital operator.

Candidate capabilities:

- Gmail;
- Google Calendar;
- server/VPS health and constrained actions;
- filesystem integrations;
- notifications;
- cross-tool project context;
- project-aware/day-aware brief;
- stronger data classification and credential scopes.

Exit requires private-data handling, connector failures, and consequential external actions to remain within the same permission/audit boundaries proven earlier.

---

## v0.4 — Voice and Device Presence

Goal: make Loren convenient away from the desktop without creating a second identity or security model.

Candidate capabilities:

- mobile-friendly/PWA interface;
- trusted devices;
- push-to-talk;
- speech-to-text / text-to-speech;
- notification actions;
- optional desktop/device node;
- optional messaging-channel adapters.

Before this version, trusted-device enrollment/revocation and voice-approval rules must be decided.

---

## v0.5 — Proactive Loren

Goal: let Loren notice meaningful events and perform bounded background work under explicit standing policy.

Candidate capabilities:

- event ingestion;
- GitHub/webhook watchers;
- server/calendar/email triggers;
- proactive notifications;
- recurring/background workflows;
- allowlisted standing permissions;
- quotas/rate limits;
- active-task visibility;
- global pause/kill switch.

This version requires a dedicated proactive-autonomy gate before release.

---

## v0.6+ — Real-use hardening

Do not pre-design these versions deeply. Let actual usage determine priorities.

Possible themes:

- memory consolidation;
- additional brain providers/local models;
- Home Assistant;
- desktop/computer use;
- more integrations;
- offline/private execution;
- performance/cost optimization;
- packaging and deployment simplification.

---

## v1.0 — Stable Personal Daily Driver

v1.0 does not mean "all Jarvis features". It means Loren's core can be trusted as a long-lived daily assistant.

Minimum properties:

- stable daily-use workflows;
- tested backup/export/restore and schema migration;
- upgrades preserve identity/memory/policy;
- brain/action/skill boundaries are maintainable;
- trusted devices/background execution have strong controls;
- secret rotation/revocation is documented and tested;
- consequential behavior is reconstructable through audit;
- integrations can fail without destroying Loren core state;
- model/provider replacement remains possible behind the brain boundary.

The full v1.0 gate is defined in the master plan.

---

## Ongoing rule

At every version ask:

> Does this capability strengthen Loren's personal intelligence, or are we rebuilding infrastructure a mature project already solves better?

Reuse infrastructure where it is safe and replaceable. Spend custom engineering on Loren's identity, memory, policy, personal semantics, and trustworthy behavior.
