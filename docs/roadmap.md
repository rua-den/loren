# Loren Roadmap

This roadmap is capability-driven rather than date-driven. Loren should advance only when the previous stage is trustworthy enough to support the next one.

## Phase 0 — Product and architecture definition

**Current phase.**

Goals:

- define Loren's product boundary;
- research existing agent runtimes;
- establish memory, permission, skill, and security principles;
- resolve the v0.1 runtime strategy through ADR-001;
- define the credential/action boundary before external writes;
- avoid implementation before the highest-impact architecture choices are understood.

Exit criteria:

- ADR-001 accepted from implementation-level evidence;
- v0.1 architecture is specific enough to implement;
- canonical ownership of identity/memory/policy is clear;
- first skill and action-execution boundary are defined;
- threat model, memory trust model, approval path, and credential boundary are reflected in design.

## Phase 1 — Loren v0.1: trustworthy personal project assistant

Primary objective:

> Loren can be used repeatedly for project-oriented work, remembers trusted context across restarts, and can perform a narrow GitHub workflow through an enforceable permission boundary.

Core capabilities:

- owner-authenticated minimal interface;
- persistent Loren identity;
- minimal world model for projects and repositories;
- durable memory with provenance, trust classes, correction, and forgetting;
- canonical-state export/restore proof;
- one selected agent/runtime path behind a Loren-owned boundary;
- GitHub read + narrow write capabilities;
- deterministic action/permission gateway;
- credential isolation from the reasoning runtime;
- minimal audit from the first vertical slice;
- end-to-end security/reliability tests;
- fake runtime support for core tests.

Required release flows:

```text
"Loren, repo wedding hiện sao rồi?"
"Nhớ rằng project này production deploy phải hỏi tao."
"Tạo branch và chuẩn bị thay đổi X."
"Tại sao mày vừa làm việc đó?"
```

Not required for the v0.1 tag:

- general web research;
- reminders/scheduler;
- voice;
- native mobile app;
- smart home;
- unrestricted desktop automation;
- broad email/calendar access;
- fully autonomous production changes;
- multi-user product support.

Exit criteria:

- Loren survives restart without losing canonical identity/project context;
- trusted memory corrections work and hostile external content cannot silently become owner policy;
- canonical state can be exported, wiped, restored, and used again;
- GitHub status comes from GitHub rather than model assumptions;
- all external writes pass the action/permission gateway;
- privileged credentials are resolved only at the controlled executor boundary;
- consequential writes are post-verified and auditable;
- core/domain tests can run using a fake runtime without framework internals;
- owner can repeatedly complete the four v0.1 release flows from the application interface.

## Phase 1.x — Immediate capability extensions

These capabilities intentionally follow the v0.1 trust boundary rather than expanding the initial release.

### v0.1.1 candidate — Web research

Potential capabilities:

- public web research;
- source provenance/citations;
- explicit promotion of research conclusions into durable project memory;
- untrusted-content isolation;
- SSRF/private-network protections and fetch policy.

### v0.1.2 candidate — Reminders and scheduler

Potential capabilities:

- persistent one-time reminders;
- timezone-aware scheduling;
- cancellation and visible task list;
- restart/missed-run semantics;
- bounded retries;
- notification delivery.

The ordering of these extensions should follow real usage rather than version-number aesthetics.

## Phase 2 — Personal operations

Primary objective:

> Loren expands from project assistant to personal digital operator.

Potential capabilities:

- Gmail;
- Google Calendar;
- server/VPS operations;
- richer filesystem access;
- notifications;
- improved scheduler/background jobs;
- event ingestion;
- project-aware daily brief;
- stronger procedural memory;
- trusted-device/session management.

Example:

```text
"What matters today?"
"Do I have any important email related to this project?"
"Check the server before tonight's release."
```

Exit criteria:

- cross-tool context works without leaking excessive data into prompts;
- approval experience remains understandable despite more tools;
- event/background execution has visible controls and cancellation;
- secret isolation and credential scopes are mature.

## Phase 3 — Voice and device presence

Primary objective:

> Loren becomes easy to access without sitting at a computer.

Potential capabilities:

- PWA/mobile client;
- push-to-talk;
- speech-to-text and text-to-speech;
- notification actions;
- trusted phone/device enrollment;
- optional messaging channels;
- optional desktop node/computer-use adapter.

Voice is an interface over the same core. It must not create a second memory or permission system.

Exit criteria:

- voice interactions can safely handle approval and ambiguity;
- trusted-device identity is reliable;
- privacy expectations for microphones/audio are explicit;
- critical actions do not rely on weak voice-only confirmation by default.

## Phase 4 — Proactive Loren

Primary objective:

> Loren can notice meaningful changes and act or notify within explicit standing policies.

Potential capabilities:

- event bus;
- GitHub/webhook watchers;
- server health triggers;
- calendar preparation;
- email-derived follow-up tasks;
- proactive project health monitoring;
- priority/attention model;
- recurring workflows;
- background research;
- policy-controlled self-created tasks.

Example:

```text
"The production build failed after the latest merge. I found the failing test; I have not deployed anything."

"You have a meeting in 20 minutes. The latest thread with that person changed the scope of Project X."
```

Exit criteria:

- low false-positive notification rate;
- background tasks are bounded, visible, and cancellable;
- prompt injection from event content cannot grant permissions;
- autonomy can be disabled globally;
- standing policies are easy to inspect and revoke.

## Phase 5 — Ambient personal intelligence

Long-term territory, not a committed implementation plan.

Possible directions:

- Home Assistant integration;
- room/device context;
- local wake word;
- private voice hardware;
- cross-device continuity;
- richer personal knowledge graph;
- selective local-model execution for privacy-sensitive tasks;
- learned routines promoted into explicit procedures;
- multiple specialized subagents behind one Loren identity.

The project should reach this phase only if the earlier personal-intelligence core proves useful. A sophisticated ambient interface around an unreliable assistant would not meet Loren's goal.

## Ongoing architectural rule

At every phase ask:

> Does this capability strengthen Loren's personal intelligence, or are we rebuilding infrastructure another mature project already handles better?

Prefer adapters and reuse for infrastructure. Spend custom engineering on identity, memory, policy, world model, personal semantics, and trustworthy autonomy.