# Loren Master Delivery Plan

**Status:** Active planning baseline — product roadmap rebaselined 2026-09-06  
**Current phase:** `v0.1 — Useful Trustworthy Assistant`  
**Completed foundations:** M1–M4 + Gate D + M5 write-safety Slices 1–3 + M6A.1 conversation surface  
**Current product target:** `M6A.2 — Current-information / web read`  
**Paused expansion:** `M5 Slices 4–6 — file/commit/PR writes` until M6A is owner-testable

This is Loren's top-level delivery plan. The roadmap is **capability-driven, not date-driven**.

> **Product correction (2026-09-06): Loren is a personal secretary / Jarvis-like assistant first, not a GitHub automation agent. The product must learn to converse, understand context, retrieve current information, remember, and organize before broadening its ability to mutate external systems.**

---

# 1. Product objective

Loren should become the owner's persistent personal intelligence / secretary that can:

1. talk naturally with the owner;
2. understand the owner's durable personal/project context without being re-taught every session;
3. answer ordinary knowledge questions from the selected brain provider;
4. retrieve **current** information through authoritative read-only tools when model knowledge is not enough;
5. research, compare, summarize, and explain where information came from;
6. read the owner's connected information sources with explicit scopes;
7. remember owner-provided facts, preferences, rules, decisions, and project context;
8. organize information into useful notes/tasks/decisions rather than dumping transcripts;
9. ask for approval before consequential external actions;
10. act through Loren-owned policy/credential/audit boundaries;
11. gradually gain scheduling, voice, device presence, and proactive behavior only after lower trust layers are proven.

The long-term product is **not** a model wrapper, **not** a GitHub bot, and **not** a generic agent framework.

A useful mental model is:

```text
Owner
  |
  v
Loren — personal secretary / long-lived intelligence
  |
  +--> conversation + reasoning
  +--> durable memory/context
  +--> current information / research
  +--> personal/project information sources
  +--> notes / tasks / decisions
  +--> approval UI
  +--> safe actions
  +--> later reminders / background work / voice / proactive behavior
```

---

# 2. Product ordering principle — understand before act

The delivery order must optimize for a useful assistant, not for the most interesting infrastructure primitive.

Default capability order:

```text
CONVERSE
  -> REMEMBER
  -> READ / RETRIEVE CURRENT INFORMATION
  -> RESEARCH / SYNTHESIZE
  -> ORGANIZE
  -> PRESENT PROPOSED ACTION
  -> OWNER APPROVES
  -> ACT
  -> VERIFY / AUDIT
  -> later SCHEDULE / PROACT
```

Consequences:

- read-only information capabilities come before broad external writes;
- a user-visible conversation checkpoint comes before adding more GitHub mutation primitives;
- external writes remain narrow until Loren can explain what it knows, what it found, and what it proposes to do;
- technical security foundations may be implemented early, but they do **not** define the product roadmap by themselves.

---

# 3. Architectural invariants across all versions

Breaking one requires an explicit superseding ADR.

## Loren owns

```text
identity
canonical personal/project state
memory + provenance
permissions/policy
approval artifacts
action gateway
audit/history
context assembly
personal organization state
```

## Replaceable infrastructure

```text
brain/model provider
agent-loop implementation details
web/search providers
MCP servers
vendor APIs
UI clients
database engine after migration
computer-use/device runtimes
notification channels
secret-store backend
```

## Security invariants

1. The model may request actions; it may not authorize itself.
2. Privileged tool credentials remain outside model/runtime context.
3. Authentication proves owner identity; it is not write approval.
4. External/model content cannot silently promote itself to owner policy, approval, or trusted memory.
5. Consequential writes are canonical-target-bound, permission-checked, one-time approved, audited, and post-verified.
6. Credential revocation overrides prior approval.
7. Global privileged writes can fail closed into read-only mode.
8. Canonical state is exportable/recoverable independently of model-provider session state.
9. Runtime/provider-specific IDs never become Loren's durable primary identity.
10. Retrieved information is data, not instruction or permission.
11. Read-only tool access should use the least privilege necessary for the requested information.

---

# 4. Versioning model — rebaselined around usefulness

```text
v0.0   architecture / feasibility             ✓ complete
v0.1   useful trustworthy assistant           <- current
v0.2   personal secretary integrations
v0.3   personal/project operations
v0.4   voice + device presence
v0.5   proactive/background Loren
v0.6+  hardening based on real daily use
v1.0   stable personal daily driver
```

Patch releases (`v0.x.y`) may add narrow capabilities or hardening without changing the main trust boundary of the minor version.

---

# 5. Decision gates

## Gate A — Core ownership [PASSED]

ADR-001: Loren owns canonical identity/state/policy/action authorization; models, runtimes, MCP, and external frameworks are adapters.

## Gate B — v0.1 implementation stack [PASSED]

ADR-002 accepted baseline:

```text
C# 14 / .NET 10 LTS
ASP.NET Core
small Loren-owned bounded agent loop
provider-neutral IBrain
MCP C# SDK behind Loren action contracts
SQLite + EF Core
Blazor/Web owner surface
xUnit
```

## Gate C — Canonical storage and memory lifecycle [PASSED]

ADR-003 locked opaque Loren IDs, SQLite/EF migration policy, Project/Repository canonical boundaries, durable-memory source classes, correction/supersession, memory-delete vs audit separation, and portable logical export versioning.

## Gate D — Action/approval/credential policy [PASSED]

ADR-004 locks exact write intent, canonical target binding, explicit owner approval, non-replay, host-controlled read-only, credential isolation/revocation, post-write verification, and redacted audit.

Gate D is a **safety foundation**, not the primary product persona.

## Gate E — Background execution

Required before trusted reminders/background operations that can execute when the owner is not actively present:

- persistent job ownership/state;
- timezone/missed-run semantics;
- bounded retry/backoff;
- cancellation;
- quotas;
- notification policy;
- safe restart/resume behavior.

Simple owner-created tasks/checklists that do not execute in the background do **not** require Gate E.

## Gate F — Trusted devices and voice approval

Required before voice/device trust can authorize sensitive operations.

## Gate G — Proactive autonomy

Required before standing permissions, proactive evaluations, event-driven work, or self-created recurring tasks.

## Gate H — v1 stable contract

Required before v1.0: recovery compatibility, migrations/upgrades, stable core contracts, secret rotation, operational monitoring, and privacy/security baseline.

---

# 6. Completed engineering foundations

## v0.0 — Architecture and feasibility [COMPLETE]

ADR-001/002 feasibility complete.

## M1 — Engineering Foundation [COMPLETE]

Production scaffold, provider-neutral contracts, bounded loop, deterministic tests, CI gates, dependency boundaries.

## M2 — Walking Skeleton [COMPLETE]

A real authenticated production flow proved:

```text
owner
 -> Loren conversation
 -> real brain provider
 -> github.read_repository ActionRequest
 -> ActionGateway
 -> real GitHub read
 -> structured result
 -> final natural-language answer
 -> correlated audit
```

## M3 — Canonical Project/Repository State [COMPLETE]

Canonical IDs, aliases, durable project/repository state, prepared project context.

## M4 — Trusted Durable Memory [COMPLETE]

Durable owner memory, correction/supersession, bounded trusted retrieval, poisoning resistance, owner forget semantics, Windows SQLite hardening.

## Gate D + M5 Slices 1–3 — Write-safety foundation [FOUNDATION COMPLETE / EXPANSION PAUSED]

Already delivered and green on `main`:

```text
Slice 1  typed action policy + exact one-time approval + global read-only
Slice 2  dedicated write credential resolver + revocation + redaction
Slice 3  verified create-non-default-branch capability + owner test harness
```

The create-branch capability is retained as a proof that the safety boundary works. It is **not** the next product priority.

Paused for now:

```text
Slice 4  controlled file/commit write
Slice 5  open pull request
Slice 6  broad write/replay/injection E2E
```

## M6A.1 — Conversation primary surface [COMPLETE]

PR #29 delivered conversation-first UI, Loren identity context, bounded multi-turn history, friendly project selection, deterministic project inference, trusted memory in the normal conversation path, browser history role hardening, and secondary activity/audit UI.

```text
merge: a1652b2451fe2e706aa83373932b210178f63ebe
PR CI #224 / 34042192552: PASS Ubuntu + Windows
post-merge main CI #225 / 34042352724: PASS Ubuntu + Windows
```

---

# 7. v0.1 — Useful Trustworthy Assistant [ACTIVE]

## v0.1 goal

The first version the owner should actually want to keep open during the day.

Before v0.1 is called useful, Loren must support these classes of interaction:

### A. Ordinary conversation

```text
"Loren, giải thích dependency injection cho tao dễ hiểu."
"So sánh 2 hướng thiết kế này giúp tao."
"Tóm tắt đoạn này cho tao."
```

No tool is required when stable model knowledge/reasoning is sufficient.

### B. Current-information questions

```text
"Hôm nay có tin gì đáng chú ý về .NET?"
"Tìm giúp tao tài liệu mới nhất về X."
"Giá / lịch / phiên bản / trạng thái hiện tại của Y là gì?"
```

Loren must know when it needs a read-only external information tool instead of guessing from stale model knowledge.

### C. Personal/project context

```text
"Project wedding-online hiện sao rồi?"
"Mày nhớ tao chốt framework nào cho Loren không?"
"Quyết định trước của tao về production deploy là gì?"
```

Answer uses canonical state + trusted durable memory + authoritative read tools as appropriate.

### D. Research/synthesis

```text
"Research giúp tao 3 lựa chọn, đưa nguồn và trade-off."
"Đọc mấy nguồn này rồi chốt khác nhau ở đâu."
```

Retrieved content remains inert data. Loren must distinguish sourced fact from inference/opinion.

### E. Personal organization

Initial non-background form:

```text
"Ghi lại quyết định này."
"Tạo task: review X."
"Cho tao xem các việc đang pending."
"Đánh dấu task Y xong."
```

Tasks/notes/decisions are Loren-owned durable state. Scheduled reminders/background execution come later behind Gate E.

### F. Proposed consequential action

```text
"Tạo branch abc cho Loren."
```

The desired UX is not an admin form. The conversation should:

```text
brain understands request
 -> Loren resolves exact canonical target
 -> Loren presents proposed action in conversation/UI
 -> owner explicitly approves
 -> existing Gate D boundary executes
 -> Loren verifies
 -> Loren explains what happened
```

---

# 8. Current milestone — M6A Conversational Secretary + Information Layer

M6 is pulled forward and expanded because **interaction + information is the product**, while additional write primitives are secondary.

## Slice M6A.1 — Conversation becomes the primary surface [COMPLETE]

Delivered and green in PR #29.

Acceptance includes:

```text
"Mày là Loren đúng không?"
"Project Loren hiện sao rồi?"
normal multi-turn chat
trusted project memory in conversation
```

## Slice M6A.2 — General current-information / web read capability [ACTIVE — PR #30]

Current implementation adds `web.search` through a read-only ActionGateway path backed by Ollama Web Search.

Delivered contract in the active PR:

- existing `OLLAMA_API_KEY` is reused for the read service;
- default endpoint is `https://ollama.com/api/web_search` with optional trusted override;
- query, response, source count, title, URL and content are bounded;
- unsafe or overlong URLs are excluded rather than turned into broken citations;
- external search evidence is explicitly untrusted data, not instruction/memory/permission;
- missing credential fails before external call;
- provider failure bodies and secrets are not surfaced;
- deterministic executor tests and an agent-loop sourced-answer acceptance test exist;
- normal conversation exposes `github.read_repository` and `web.search` as read actions.

Close the slice only after exact-head PR CI and post-merge main CI are green.

The brain decision remains:

```text
stable knowledge -> answer directly
current/external fact -> web.search
multi-source question -> bounded research tool calls -> synthesis
```

## Slice M6A.3 — Information synthesis + source-aware answers [NEXT]

Deliver:

- multiple-source comparison;
- bounded iterative search;
- page fetch for selected search results or owner-provided URLs;
- clear separation of sourced facts vs model inference;
- concise source list in owner-visible output;
- stale/unknown/conflicting information called out instead of guessed;
- deduplication/bounds for retrieved evidence.

Ollama's official web capability exposes both `/api/web_search` and `/api/web_fetch`; page content remains untrusted external data.

## Slice M6A.4 — Personal organization primitives

Deliver Loren-owned durable:

```text
Note
Decision
Task
Task status
optional Project scope
provenance / created / updated timestamps
```

No background execution yet.

Natural conversation examples:

```text
"Nhớ decision này."
"Tạo task cho ngày mai"   -> may store due date, but no autonomous reminder until Gate E
"Các task của wedding-online còn gì?"
"Đánh dấu task deploy checklist xong."
```

## Slice M6A.5 — Conversational approval presentation

Use the already-safe `github.create_branch` capability to prove:

```text
owner asks in chat
 -> model proposes typed action
 -> Loren resolves trusted target
 -> UI/conversation shows exact action summary
 -> owner approves
 -> one-time approval artifact
 -> credential-bound executor
 -> post-write verify
 -> natural-language completion + audit details
```

The owner should not have to manually enter canonical IDs or raw approval objects.

### M6A checkpoint — OWNER TEST MILESTONE

Do not resume broad GitHub writes until the owner can pull `main`, start Loren, and test this flow naturally:

```text
1. Chat normally with Loren.
2. Ask a stable knowledge question and receive a normal answer.
3. Ask a current-information question and see grounded external data + sources.
4. Ask for bounded multi-source research and get source-aware synthesis.
5. Ask about a known project and see canonical context + memory + live read data used.
6. Teach Loren a fact/decision, restart, and ask for it again.
7. Create/list/complete a task or decision through chat.
8. Ask Loren to create a branch; see an approval proposal; approve; get verified result.
9. Ask why Loren did it and inspect audit explanation.
```

---

# 9. Resume M5 write expansion only after M6A

After M6A is owner-tested:

## M5 Slice 4 — Controlled file/commit path

Only on approved non-default branch, exact path/content digest/branch binding, post-write verification.

## M5 Slice 5 — Open pull request

Exact repo/base/head/title/body security-relevant intent binding + post-read verification.

## M5 Slice 6 — Write authorization/adversarial E2E

Replay, revocation, injection, ambiguous result, audit reconstruction.

These capabilities support Loren's work; they do not define Loren's identity.

---

# 10. v0.1 recovery/security closeout

After M6A and the narrow required write flow:

## Recovery

- logical export format;
- wipe -> restore;
- canonical IDs retained;
- memory/tasks/decisions restored;
- raw credentials never exported.

## Adversarial E2E

- restart continuity;
- memory poisoning;
- retrieved-content prompt injection;
- stale/current information handling;
- approval replay;
- credential revocation;
- read-only kill;
- provider/tool failures;
- cancellation;
- audit reconstruction.

---

# 11. v0.1 exit gate

Do not tag `v0.1.0` until:

- conversation is the normal product surface;
- stable knowledge questions work without unnecessary tools;
- current/external questions use grounded read-only tools rather than stale guessing;
- research answers retain source/provenance information;
- owner memory survives restart/correction/forget;
- basic durable notes/decisions/tasks work through conversation;
- project context can combine canonical state, memory, and live read data;
- at least one consequential action can be proposed in conversation, explicitly approved, executed, verified, and explained;
- writes remain fail-closed under read-only/revoked/missing credential conditions;
- recovery works;
- core behavior is deterministically testable without a live model/provider.

---

# 12. v0.2 — Personal Secretary Integrations

Goal: move from a useful general/personal assistant to a secretary connected to the owner's daily information streams.

Priority candidates:

```text
Calendar read + schedule understanding
Gmail/mail read + summarize/search
files/documents / personal knowledge sources
richer tasks and due dates
contacts/people context
daily brief on demand
project dashboards / GitHub richer reads
```

Writes such as sending mail or calendar changes require their own approval/credential/action policies.

Gate E must pass before autonomous scheduled reminders/background delivery.

---

# 13. v0.3 — Personal Operations

After read-heavy secretary use proves useful:

```text
calendar writes with approval
mail draft/send with approval
bounded reminders/background jobs after Gate E
server/VPS health and constrained operations
cross-tool workflows
richer GitHub/project operations
```

Read before write remains the default integration rule.

---

# 14. v0.4 — Voice and Device Presence

Trusted device enrollment, mobile/PWA presence, push-to-talk, STT/TTS, notifications, lost-device/revocation handling. Gate F required.

---

# 15. v0.5 — Proactive Loren

Normalized events, proactive evaluation, notification prioritization, tiny allowlisted standing permissions, bounded recurring work, active-task visibility, global pause. Gate G required.

---

# 16. v0.6+ — Daily-use hardening

Let actual usage determine priorities: memory consolidation, more providers/local models, Home Assistant, computer use, more integrations, offline/private execution, performance/cost, UX, packaging/deployment simplification.

---

# 17. v1.0 — Stable Personal Daily Driver

Loren is a trusted long-lived personal secretary that can evolve without casually losing state, leaking secrets, confusing retrieved content with authority, or bypassing owner control. Gate H must pass.

---

# 18. Milestone execution rules

For every milestone:

1. define the **owner-visible behavior** first;
2. write acceptance scenarios in natural owner language before low-level implementation details;
3. identify whether the capability is `understand`, `read`, `organize`, or `act`;
4. prefer read-only integration before write integration;
5. build the smallest vertical slice that proves usefulness end-to-end;
6. preserve provider/framework boundaries;
7. add audit/provenance with the capability;
8. test deterministic logic with fakes before relying on live services;
9. synchronize `docs/status.md`, README EN/VI, roadmap, v0.1 plan, and handoff;
10. do not add adjacent technical primitives merely because the framework makes them easy.

A milestone is complete only when its acceptance criteria pass on the main owner path.

---

# 19. Stop-the-line conditions

Stop and fix the boundary if:

- model/runtime can bypass ActionGateway;
- privileged credentials leak;
- approval can be replayed or broadened;
- read-only can be bypassed;
- writes can report success without verification;
- canonical state depends on provider sessions;
- retrieved/model content can self-promote to policy/memory/approval;
- current-information answers silently rely on stale model knowledge when authoritative retrieval is required;
- external content can inject instructions into Loren's authority path;
- recovery is known broken;
- deterministic core logic requires live model behavior.

---

# 20. Current next action

```text
M1–M4 foundations                              ✓ complete
Gate D + M5 write safety Slices 1–3            ✓ complete enough / proof exists
M6A.1 Conversation primary surface             ✓ complete
        |
        v
M6A.2 Current information / web read           <- ACTIVE
M6A.3 Source-aware research/synthesis
M6A.4 Notes / Decisions / Tasks
M6A.5 Conversational approval using create-branch proof
        |
        v
OWNER INTERACTION CHECKPOINT
        |
        +--> then resume file/commit/PR writes if still the highest-value next capability
        +--> recovery/security closeout
        v
v0.1.0
```

**Do not continue with controlled file/commit or open-PR work before the M6A owner interaction checkpoint is usable.**
