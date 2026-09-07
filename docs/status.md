# Loren Project Status

**Last updated:** 2026-09-07  
**Current version:** `v0.1 — Useful Trustworthy Assistant`  
**Current product target:** `M6A.4 — Notes / Decisions / Tasks`  
**Next target after merge:** `M6A.5 — Conversational approval`  
**Write expansion:** `M5 Slices 4–6 paused after verified create-branch proof`  
**Decision gates passed:** `Gate A`, `Gate B`, `Gate C`, `Gate D`

This file is the authoritative progress ledger. Read [`handoff.md`](handoff.md) next in a fresh thread.

> Loren is a persistent personal secretary / Jarvis-like assistant first. Conversation, memory, current information, research, organization and explicit conversational approval come before broader automation.

---

# 1. Completed foundations

```text
v0.0 Architecture / Feasibility                ✓ complete
M1 Engineering Foundation                      ✓ complete
M2 Conversation/tool Walking Skeleton          ✓ complete
M3 Canonical Project/Repository State          ✓ complete
M4 Trusted Durable Memory                      ✓ complete
Gate D Action/Approval/Credential Policy       ✓ passed
M5 Slice 1 policy + one-time approval           ✓ complete
M5 Slice 2 credential isolation/redaction      ✓ complete
M5 Slice 3 verified create branch              ✓ complete
M6A.1 conversation primary surface             ✓ complete
M6A.2 current-information web search           ✓ complete
M6A.3 source-aware bounded research            ✓ complete
```

## Proven safety foundation

```text
canonical target
 -> deterministic policy
 -> explicit owner approval for consequential external writes
 -> exact intent fingerprint
 -> atomic one-time consume
 -> write-specific credential boundary
 -> controlled trusted executor
 -> post-write verification
 -> redacted audit
```

Authentication is not external-write approval. Model/external content cannot authorize itself, choose write credentials, disable read-only, broaden a target or declare an unverified write successful.

M5 evidence:

```text
PR #25 merge caa65fbbd7c3828b68aa198dad625e73e9c096b4
PR #26 merge f7fb36bae324dbd7bb8d12e02daf3fe0dd98e7da
PR #27 merge bd0220550592a3ba55a2c722192e43df6e8ca321
PR #27 exact-head CI #217 / 34029409983 PASS Ubuntu + Windows
post-merge main CI #218 / 34029500883 PASS Ubuntu + Windows
```

The existing create-branch form is a safety harness, not the intended daily UX.

---

# 2. M6A assistant-first execution

Correct capability order:

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

## M6A.1 — Conversation primary surface [COMPLETE]

PR #29:

```text
merge: a1652b2451fe2e706aa83373932b210178f63ebe
PR CI #224 / 34042192552: PASS Ubuntu + Windows
post-merge main CI #225 / 34042352724: PASS Ubuntu + Windows
```

Delivered conversation-first owner UI, bounded multi-turn history, Loren identity context, friendly project selection, deterministic project inference, trusted memory in the normal chat path, browser-history role hardening and secondary tool/audit UI.

## M6A.2 — Current-information web read [COMPLETE]

PR #30:

```text
merge: a8d3e7bbc94c9a468ebc234deb1fe87dcb7d23e9
PR exact-head CI #237 / 34044530005: PASS Ubuntu + Windows
post-merge main CI #238 / 34044641072: PASS Ubuntu + Windows
```

Delivered read-only `web.search` through Ollama Web Search using the existing `OLLAMA_API_KEY`, bounded search evidence, safe source URLs, secret-safe failures and deterministic current-information acceptance.

## M6A.3 — Source-aware bounded research [COMPLETE]

PR #31:

```text
frozen PR head: 6f2b5d1ea5739b84b369f689214db95745071cb1
merge: d789ccc7f7540cb802b14f677d317db3e571a7d3
PR exact-head CI #240 / 34045079047: PASS Ubuntu + Windows
post-merge main CI #241 / 34052513007: PASS Ubuntu + Windows
```

Delivered:

```text
web.search
 -> select sources
 -> web.fetch
 -> bounded page evidence
 -> compare / synthesize in existing bounded AgentLoop
 -> sourced facts + explicit Loren inference
```

`PublicWebUrlPolicy` rejects unsafe schemes, credentials, localhost/private literal IPs and non-standard ports before fetch. Search/fetch evidence stays inert untrusted data. Provider failure bodies and secrets are not surfaced.

---

# 3. M6A.4 — Notes / Decisions / Tasks [READY TO MERGE — PR #32]

Branch: `feat/m6a4-organization-state`  
PR: `#32 — feat: add durable conversation organization state`

Implemented owner-visible state:

```text
Note
Decision
Task
TaskStatus = Open | Completed
optional canonical Project scope
source/provenance
created / updated / completed timestamps
```

Conversation actions:

```text
organization.create_note
organization.record_decision
organization.create_task
organization.list
organization.complete_task
organization.reopen_task
```

Trust boundary:

```text
public external read          -> READ
private Loren owner state read -> OWNER_STATE_READ
private Loren owner state write -> OWNER_STATE_WRITE
external mutation             -> REVERSIBLE/EXTERNAL/PRIVILEGED_WRITE
```

`OWNER_STATE_READ/WRITE` require authenticated Loren-owned owner context and an `ITrustedActionExecutor`. They do **not** require GitHub write credentials or external-write one-time approval because they mutate only Loren-owned local state. `LOREN_ENABLE_WRITES` remains an external-write kill switch and does not disable the owner's own local notes/tasks. ActionGateway independently enforces owner context so a permissive policy cannot expose private owner state.

Durability:

- SQLite `OrganizationItems` migration `202609070001_AddOrganizationItems`;
- opaque Loren IDs;
- project foreign key with restrict semantics;
- SQLite-native Unix-millisecond lifecycle timestamps for deterministic bounded sorting;
- note/decision/task state survives process/database restart;
- complete/reopen task lifecycle is persisted.

Acceptance coverage includes:

```text
owner chat
 -> create project task
 -> list open project tasks
 -> complete exact task
 -> durable SQLite state
 -> no external-write approval artifact
```

Security/reliability coverage proves missing owner context fails closed, owner-state executors must be trusted, and external GitHub write semantics remain unchanged.

Latest implementation CI before final documentation sync:

```text
CI #247 / 34053503945: PASS Ubuntu full gate + Windows integration
```

A final exact-head CI is required after documentation synchronization before PR #32 merges.

No background scheduler/reminder execution is introduced. Gate E remains required for background behavior.

---

# 4. M6A.5 — Conversational approval [NEXT]

Reuse the already-safe `github.create_branch` implementation through the intended owner UX:

```text
Owner: "Tạo branch abc cho Loren từ main."
 -> brain understands/proposes intent
 -> Loren resolves canonical project/repository + exact source SHA
 -> conversation shows exact proposal + risk
 -> owner explicitly clicks Approve
 -> Loren creates exact one-time approval
 -> existing credential-bound trusted executor runs
 -> GitHub branch state is independently verified
 -> Loren reports completion naturally + audit context
```

The chat message is intent, **not** Gate D approval. No new GitHub write primitive is needed for this slice.

---

# 5. v0.1 owner checkpoint

Do not ask the owner to pull specifically for the v0.1 product test until Loren supports the complete natural workflow:

```text
1. Chat normally as the default surface.
2. Ask stable knowledge/reasoning questions.
3. Ask current-information questions and get grounded sources.
4. Perform bounded source-aware research.
5. Combine project context + durable memory + live reads.
6. Record/retrieve durable notes or decisions across restart.
7. Create/list/complete tasks through chat.
8. Ask for branch creation in natural language.
9. Review exact proposal and explicitly approve.
10. Receive verified completion and inspect why/audit.
```

**Do not resume controlled file/commit/open-PR mutation expansion before this checkpoint is usable.**

---

# 6. After M6A

Reassess the highest-value next capability instead of automatically adding GitHub writes. Candidates:

```text
A. recovery/security closeout for v0.1
B. richer read-only personal integrations
C. resume M5 Slice 4 controlled file/commit if still highest value
```

Version direction:

```text
v0.1 useful trustworthy assistant        <- current
v0.2 personal secretary integrations
v0.3 personal/project operations
v0.4 voice + device presence
v0.5 proactive/background Loren
v0.6+ daily-use hardening
v1.0 stable personal daily driver
```

---

# 7. Progress-update rule

Any capability merge must synchronize:

1. `docs/status.md`;
2. `README.md`;
3. `README.vi.md`;
4. `docs/plans/master-plan.md`;
5. `docs/plans/v0.1.md`;
6. `docs/handoff.md`;
7. architecture/development docs when the boundary or configuration changes.

A milestone is not closed until implementation, tests and repository documentation agree.
