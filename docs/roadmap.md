# Loren Roadmap

Updated 2026-09-19. Loren advances by **proven capability and trust**, not by calendar date.

The authoritative detailed sequence lives in [`docs/plans/master-plan.md`](plans/master-plan.md). Current verified delivery state lives in [`status.md`](status.md).

## Current status

**Version:** `v0.1 — Useful Trustworthy Assistant`  
**Current milestone:** `M6B — Daily Driver Readiness`  
**Last verified baseline:** PR #37 merge `ba60246a410b257097ecd537f4d977b402a37b35`, main CI #280 PASS Ubuntu + Windows + launcher.  
**Paused:** broader GitHub file/commit/PR mutation until owner live acceptance proves it is the right next value.

The product path is:

```text
CONVERSE
 -> REMEMBER
 -> READ CURRENT INFORMATION
 -> RESEARCH / SYNTHESIZE
 -> ORGANIZE
 -> PROPOSE ACTION
 -> OWNER APPROVES
 -> ACT / VERIFY / AUDIT
 -> later BACKGROUND / PROACTIVE / VOICE
```

GitHub is one integration, not Loren's product identity.

---

## v0.0 — Architecture and feasibility [COMPLETE]

Proved the core ownership model and implementation stack:

- Loren owns identity/state/policy/action authorization;
- brain/model/tool providers are replaceable adapters;
- .NET 10 / ASP.NET Core / SQLite + EF Core / bounded Loren-owned AgentLoop are viable.

Gates A and B passed.

---

## v0.1 — Useful Trustworthy Assistant [ACTIVE]

Goal: Loren is useful enough to keep open as an owner-controlled personal secretary while remaining trustworthy around memory, current information and external actions.

### Completed foundation

```text
M1 Engineering Foundation                     ✓
M2 Conversation/tool Walking Skeleton         ✓
M3 Canonical Project/Repository State         ✓
M4 Trusted Durable Memory                     ✓
Gate D External Action Safety                 ✓
M5 Slices 1–3 narrow GitHub branch proof      ✓
M6A.1 Conversation Primary Surface            ✓
M6A.2 Current Information                     ✓
M6A.3 Source-aware Research                   ✓
M6A.4 Notes / Decisions / Tasks               ✓
M6A.5 Conversational Approval Code            ✓ code; live owner proof pending
Conversation Persistence                       ✓
Windows Launcher                               ✓
Logical Export/Restore                         ✓
Retained Audit                                 ✓
M6B.1 Transient Audit Lifetime                ✓
```

### M6B — Daily Driver Readiness [CURRENT]

The purpose of M6B is not to add another capability family. It makes the already-built assistant diagnosable and proves it works as a daily product.

```text
M6B.1 bound transient request audit            ✓ complete
M6B.2 safe readiness + docs rebaseline         <- current delivery
M6B.3 owner live daily-driver acceptance       next product gate
v0.1 closeout / release decision               after acceptance
```

M6B.2 keeps `/health` as liveness and adds authenticated `/api/readiness` for safe configuration/state posture. It does not perform external provider probes and never returns secrets.

M6B.3 uses real providers and the existing branch action to test useful conversation, current information, research, project context, restart continuity, durable memory/tasks, explicit Cancel/Approve and exact GitHub SHA read-back.

### v0.1 release gate

Do not tag `v0.1.0` until:

- ordinary conversation is usable;
- current information is grounded with sources;
- bounded research is source-aware;
- canonical project + memory + live read work together;
- durable organization state survives restart;
- conversation continuity survives restart;
- one consequential action follows proposal → explicit approval → execute → verify → audit;
- read-only/revocation fail closed;
- recovery is usable;
- no raw credentials leak;
- exact-head and post-merge CI are green.

---

## v0.2 — Personal Secretary Integrations

Start only after v0.1 closeout.

First prove a gap already exposed by the architecture: `IBrain` is provider-neutral, but production composition is currently Ollama-only. Implement and accept a second real brain provider before advertising operational provider portability.

Then prefer **one owner-visible read-only personal integration** over a generic connector framework. Strong candidate:

```text
Calendar read/search
 -> ask what is next
 -> inspect schedule/conflicts
 -> answer with source/time context
```

Other candidates: mail read/search/summarize, files/documents, contacts/people context, richer task due-date data, on-demand daily brief.

Writes for personal systems require their own explicit policy/credential/approval semantics. Background delivery still waits for Gate E.

---

## v0.3 — Personal / Project Operations

Candidate scope after useful read integrations:

- approved calendar/mail/project writes;
- cross-tool workflows;
- server/VPS health and constrained actions;
- stronger per-integration credential scopes;
- bounded background jobs only after Gate E.

---

## v0.4 — Voice + Device Presence

Candidate scope:

- trusted device enrollment;
- PWA/mobile presence;
- push-to-talk;
- STT/TTS;
- notifications.

Gate F is required before voice/device trust can authorize sensitive actions.

---

## v0.5 — Proactive / Background Loren

Candidate scope:

- normalized events/watchers;
- recurring work;
- proactive notifications;
- bounded standing permissions;
- quotas/cancellation/global pause.

Gate G is required. Gate E foundations are prerequisites for execution without active owner presence.

---

## v0.6+ — Real-use hardening

Let actual daily usage determine priorities: memory consolidation, cost/performance, provider diversity, more integrations, packaging, private/local execution, Home Assistant, computer use and richer UX.

---

## v1.0 — Stable Personal Daily Driver

v1.0 means Loren's core is safe and maintainable as a long-lived assistant across upgrades/providers: stable workflows, tested recovery/migrations, secret rotation, reconstructable audit, background/device controls, documented privacy/security defaults and proven real use.

Gate H must pass before release.

---

## Decision gates

```text
Gate A  core ownership                         PASSED
Gate B  v0.1 implementation stack              PASSED
Gate C  canonical state + memory lifecycle     PASSED
Gate D  action/approval/credential boundary    PASSED
Gate E  background execution                   NOT YET
Gate F  trusted device / voice approval        LATER
Gate G  proactive autonomy                     LATER
Gate H  v1 stable contract                     LATER
```

## Ongoing rule

At every slice ask:

> Does this make Loren more useful as the owner's persistent intelligence, or are we merely adding infrastructure/automation because we can?

Build the smallest owner-visible vertical slice that answers that question.
