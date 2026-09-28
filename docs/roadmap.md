# Loren Roadmap

Updated 2026-09-28. Loren advances by **proven capability and trust**, not by calendar date.

The authoritative detailed sequence lives in [`docs/plans/master-plan.md`](plans/master-plan.md). Current verified delivery state lives in [`status.md`](status.md).

## Current status

**Version:** `v0.1 — Useful Trustworthy Assistant`  
**Current milestone:** `M6B — Daily Driver Readiness`  
**Current delivery:** `M6B.3 — real owner daily-driver acceptance`  
**Last verified baseline:** PR #38 merge `67ade6e6459296fad2ca149720420070922e3ebe`; main CI #288 / `36412656169` PASS Ubuntu full gate + Windows integration + launcher.  
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
M6B.2 Safe Readiness + Docs Rebaseline        ✓
```

### M6B — Daily Driver Readiness [CURRENT]

M6B does not add another capability family. It proves the already-built assistant is diagnosable and usable as a daily product.

```text
M6B.1 bound transient request audit            ✓ complete
M6B.2 safe readiness + docs rebaseline         ✓ complete — PR #38 / CI #288
M6B.3 owner live daily-driver acceptance       <- current product gate
v0.1 closeout / release decision               after acceptance
```

M6B.2 keeps `/health` as liveness and adds authenticated `/api/readiness` for secret-safe configuration/state posture. It performs no external provider probes.

M6B.3 uses real providers and the existing branch action to test useful conversation, current information, research, project context, restart continuity, durable memory/tasks, explicit Cancel/Approve and exact GitHub SHA read-back. Real failures from this proof define the next implementation work.

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
- push-to-talk / STT / TTS;
- notifications;
- Gate F before sensitive voice/device approval.

---

## v0.5 — Proactive Loren

Candidate scope only after Gate E/G:

- event ingestion;
- bounded scheduled/background work;
- proactive evaluation and notifications;
- quotas, cancellation, retry/backoff and global pause.

---

## v0.6+ / v1.0

Daily-use hardening, recovery/upgrade compatibility, more providers/integrations, privacy/security/operations maturity. Gate H before v1.0.
