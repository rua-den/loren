# Loren Master Delivery Plan

**Status:** Active capability-driven roadmap  
**Updated:** 2026-09-07  
**Current phase:** `v0.1 — Useful Trustworthy Assistant`  
**Completed:** M1–M4 + Gate D + M5 write-safety Slices 1–3 + M6A.1–M6A.3  
**Ready to merge:** `M6A.4 — Notes / Decisions / Tasks`  
**Next:** `M6A.5 — Conversational approval`  
**Paused:** broader M5 file/commit/PR writes until the v0.1 owner checkpoint

This is Loren's top-level delivery plan. The roadmap is **capability-driven, not date-driven**.

> Loren is the owner's persistent personal secretary / Jarvis-like intelligence system. GitHub automation is one capability, not the product identity.

---

# 1. Product objective

Loren should become a long-lived personal intelligence that can:

1. talk naturally with the owner;
2. carry durable personal/project context across sessions and provider changes;
3. reason from stable model knowledge when appropriate;
4. retrieve current information through authoritative read-only tools;
5. research multiple sources and explain provenance/trade-offs;
6. maintain owner-controlled notes, decisions and tasks;
7. read connected personal/project systems with explicit scopes;
8. propose consequential actions in conversation;
9. require explicit owner approval before consequential external actions;
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
- owner-visible vertical slices before adjacent framework primitives;
- external/model content is data, never authority;
- no background execution before Gate E;
- no new GitHub write primitive is required to prove conversational approval.

---

# 3. Architectural invariants

Breaking one requires an explicit superseding ADR.

## Loren owns

```text
identity
canonical personal/project state
trusted durable memory
notes / decisions / tasks
permissions/policy
approval artifacts
action gateway
audit/history
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
2. Privileged write credentials remain outside BrainContext/model-visible args.
3. Authentication identifies the owner; it is not external-write approval.
4. Owner-local state access requires authenticated Loren-owned owner context.
5. External/model/tool content cannot self-promote to memory, owner-state mutation, policy or approval.
6. Consequential external writes are canonical-target-bound, one-time approved, credential-isolated, audited and post-verified.
7. Credential revocation overrides prior approval.
8. Global external write mode fails closed.
9. Current facts use read tools instead of stale guessing when verification is needed.
10. Retrieved content is inert untrusted evidence.
11. Canonical state remains recoverable independently from model/provider sessions.
12. Background execution requires Gate E.

---

# 4. Version path

```text
v0.0   architecture / feasibility             ✓ complete
v0.1   useful trustworthy assistant           <- current
v0.2   personal secretary integrations
v0.3   personal/project operations
v0.4   voice + device presence
v0.5   proactive/background Loren
v0.6+  daily-use hardening
v1.0   stable personal daily driver
```

---

# 5. Decision gates

## Gate A — Core ownership [PASSED]

ADR-001: Loren owns canonical identity/state/policy/action authorization; model/runtime/MCP are adapters.

## Gate B — v0.1 stack [PASSED]

ADR-002 baseline:

```text
C# 14 / .NET 10 LTS
ASP.NET Core
small Loren-owned bounded AgentLoop
provider-neutral IBrain
SQLite + EF Core
owner web UI
xUnit
```

## Gate C — Canonical storage + memory lifecycle [PASSED]

ADR-003 locks opaque Loren IDs, checked-in EF migrations, Project/Repository boundaries, memory provenance, append/supersede correction, forget semantics and logical export direction.

## Gate D — External action/approval/credential boundary [PASSED]

ADR-004 locks exact external-write intent, canonical target binding, explicit owner approval, one-time consume, global read-only, credential isolation/revocation, post-write verification and redacted audit.

Gate D applies to consequential external mutation. Loren-owned local organization state has its own authenticated-owner boundary and does not pretend to be an external write.

## Gate E — Background execution [NOT YET]

Required before Loren can execute trusted work without active owner presence:

- persistent job identity/state;
- timezone/missed-run semantics;
- bounded retry/backoff;
- cancellation;
- quotas;
- notification policy;
- safe restart/resume.

Storing a Task or due date as data does not require Gate E. Delivering a reminder in the background does.

## Gate F — Trusted devices / voice approval [LATER]

Required before device/voice trust can authorize sensitive actions.

## Gate G — Proactive autonomy [LATER]

Required before standing permissions, event-driven work or self-created recurring operations.

## Gate H — v1 stable contract [LATER]

Recovery compatibility, upgrade/migration stability, secret rotation, operations and privacy/security baseline.

---

# 6. Completed foundations

## M1 Engineering Foundation [COMPLETE]

Provider-neutral core, bounded loop, deterministic tests, CI gates and dependency boundaries.

## M2 Conversation/tool walking skeleton [COMPLETE]

```text
owner
 -> Loren conversation
 -> real brain
 -> github.read_repository
 -> ActionGateway
 -> real GitHub read
 -> structured observation
 -> natural answer
 -> correlated audit
```

## M3 Canonical Project/Repository [COMPLETE]

Loren-owned opaque IDs, aliases and provider-independent project/repository identity.

## M4 Trusted Durable Memory [COMPLETE]

Owner memory survives restart, supports correction/supersession/forget, keeps provenance and resists low-authority content promotion.

## Gate D + M5 Slices 1–3 [WRITE-SAFETY PROOF COMPLETE]

```text
Slice 1  typed policy + exact one-time approval + global external read-only
Slice 2  dedicated write credential + revocation + redaction
Slice 3  verified create non-default GitHub branch
```

Evidence:

```text
PR #25 merge caa65fbbd7c3828b68aa198dad625e73e9c096b4
PR #26 merge f7fb36bae324dbd7bb8d12e02daf3fe0dd98e7da
PR #27 merge bd0220550592a3ba55a2c722192e43df6e8ca321
post-merge main CI #218 / 34029500883 PASS Ubuntu + Windows
```

Paused:

```text
M5 Slice 4 controlled file/commit
M5 Slice 5 open PR
M5 Slice 6 broader write adversarial E2E
```

---

# 7. M6A — Conversational Secretary + Information Layer

## M6A.1 — Conversation primary surface [COMPLETE]

PR #29 merge `a1652b2451fe2e706aa83373932b210178f63ebe`.

Delivered conversation-first UI, Loren identity context, bounded history, friendly project selection/inference, trusted memory in normal chat, role hardening and secondary tool/audit UI.

## M6A.2 — Current-information web read [COMPLETE]

PR #30 merge `a8d3e7bbc94c9a468ebc234deb1fe87dcb7d23e9`.

Delivered `web.search` through ActionGateway, Ollama Web Search, bounded evidence/source URLs, secret-safe failures and deterministic current-info acceptance.

## M6A.3 — Source-aware bounded research [COMPLETE]

PR #31:

```text
frozen head: 6f2b5d1ea5739b84b369f689214db95745071cb1
merge: d789ccc7f7540cb802b14f677d317db3e571a7d3
PR CI #240 / 34045079047 PASS Ubuntu + Windows
main CI #241 / 34052513007 PASS Ubuntu + Windows
```

Delivered `web.fetch`, public-web URL safety, bounded search/fetch evidence, source timestamps, multi-step research in the existing bounded AgentLoop and explicit sourced-fact vs inference behavior.

## M6A.4 — Notes / Decisions / Tasks [READY TO MERGE — PR #32]

Owner-visible state:

```text
Note
Decision
Task
TaskStatus = Open | Completed
optional Project scope
provenance
created / updated / completed timestamps
```

Conversation operations:

```text
create note
record decision
create task
list/filter owner organization state
complete task
reopen task
```

### Owner-state security class

M6A.4 introduces explicit access classes:

```text
Read
OwnerStateRead
OwnerStateWrite
ReversibleWrite
ExternalWrite
PrivilegedWrite
```

Rules:

- `OwnerStateRead/Write` require authenticated trusted `AuthenticatedOwnerContext`;
- owner context is attached by Loren outside model-visible action arguments;
- ActionGateway independently enforces it;
- owner-state actions require `ITrustedActionExecutor`;
- local organization state does not require external-write approval or GitHub write credentials;
- external write behavior remains unchanged;
- `LOREN_ENABLE_WRITES` is an external-write kill switch, not a switch that disables the owner's local notes/tasks.

### Persistence

SQLite migration `202609070001_AddOrganizationItems` stores opaque IDs, optional canonical project scope and Unix-millisecond lifecycle timestamps. Restart acceptance proves Note/Decision/Task durability and Task complete/reopen state.

### Acceptance

```text
owner chat
 -> create project task
 -> list open project tasks
 -> complete exact task
 -> restart-safe SQLite state
```

CI #247 / `34053503945` passed Ubuntu full gate + Windows integration before final docs synchronization. PR #32 must pass one final exact-head CI after docs before merge.

No background scheduling exists in this slice.

## M6A.5 — Conversational approval [NEXT]

Use the existing verified `github.create_branch` capability as the first consequential conversational action.

Required flow:

```text
owner asks naturally
 -> brain/Loren interprets intent
 -> Loren resolves canonical project/repository
 -> Loren reads/resolves exact source branch SHA
 -> Loren emits an exact action proposal
 -> UI shows repo + branch + SHA + risk
 -> owner explicitly approves
 -> Loren creates exact one-time approval
 -> existing credential-bound executor runs
 -> independent ref/SHA verification
 -> natural completion + audit explanation
```

The owner utterance is intent, not Gate D approval. The model never supplies trusted canonical authority, approval ID or credentials.

No new GitHub mutation primitive is needed.

---

# 8. v0.1 owner checkpoint

Do not request the owner to pull specifically for product testing until all of this works naturally:

```text
1. normal conversation
2. stable knowledge/reasoning
3. current information with sources
4. bounded source-aware research
5. canonical project context + memory + live read
6. durable Note/Decision across restart
7. create/list/complete Task through chat
8. natural-language branch request
9. exact proposal + explicit owner approval + verified action
10. explanation/audit of what happened
```

This is the next meaningful product checkpoint.

---

# 9. v0.1 closeout after M6A

After the owner checkpoint, prioritize based on value rather than automatically resuming writes.

Required before `v0.1.0`:

- logical export/recovery for canonical state, memory and organization state;
- no raw credential export;
- restart/recovery continuity;
- retrieved-content prompt-injection E2E;
- current-info failure/stale behavior;
- approval replay/revocation/read-only E2E;
- write post-verification E2E;
- audit reconstruction;
- deterministic core behavior without live provider dependency.

If broader GitHub write expansion is still highest-value afterward:

```text
M5 Slice 4 controlled file/commit
M5 Slice 5 open PR
M5 Slice 6 adversarial write E2E
```

---

# 10. Later versions

## v0.2 — Personal Secretary Integrations

Candidates:

```text
Calendar read/search
Gmail/mail read/search/summarize
files/documents
contacts/people context
richer tasks/due dates
daily brief on demand
```

Integration writes get their own approval/credential policies. Background delivery waits for Gate E.

## v0.3 — Personal Operations

Approved calendar/mail/project writes, bounded background jobs after Gate E, cross-tool workflows.

## v0.4 — Voice + device presence

Trusted device enrollment, PWA/mobile presence, push-to-talk, STT/TTS, notifications. Gate F required for sensitive voice/device approval.

## v0.5 — Proactive Loren

Normalized events, proactive evaluation, bounded recurring work and global pause. Gate G required.

## v0.6+ / v1.0

Daily-use hardening, recovery/upgrade compatibility, more providers/integrations, privacy/security/operations maturity. Gate H before v1.0.

---

# 11. Execution rules

For every milestone:

1. define owner-visible behavior first;
2. write natural-language acceptance scenarios;
3. classify the capability as public read, private owner state, or external action;
4. build the smallest vertical slice that proves usefulness end to end;
5. keep trusted metadata outside model-visible args;
6. add provenance/audit with the capability;
7. use deterministic fake/provider tests before live-provider dependence;
8. review diff for trust-boundary regressions;
9. require exact-head Ubuntu + Windows CI before merge;
10. require post-merge main CI;
11. synchronize status + README EN/VI + plans + handoff + architecture when relevant.

Stop the line if a model/runtime can bypass ActionGateway, private owner state loses authentication, credentials leak, external approval can replay/broaden, current information silently guesses when a read is required, external content becomes authority, or external writes report success without verification.

---

# 12. Current next action

```text
M1–M4                                         ✓
Gate D + M5 write-safety Slices 1–3           ✓
M6A.1 conversation primary surface            ✓
M6A.2 current-information search              ✓
M6A.3 source-aware bounded research           ✓
M6A.4 Notes / Decisions / Tasks               <- PR #32 READY TO MERGE AFTER FINAL CI
M6A.5 conversational approval
        |
        v
OWNER v0.1 TEST CHECKPOINT
        |
        v
recovery/security closeout + reassess writes
        |
        v
v0.1.0
```
