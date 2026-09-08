# Loren Project Status

**Last updated:** 2026-09-08
**Current version:** `v0.1 — Useful Trustworthy Assistant`  
**Current product target:** `M6A.5 — Conversational approval`  
**Write expansion:** `M5 Slices 4–6 paused until the v0.1 owner checkpoint`  
**Decision gates passed:** `Gate A`, `Gate B`, `Gate C`, `Gate D`

This file is the authoritative progress ledger. Read [`handoff.md`](handoff.md) next when continuing locally or in a fresh thread.

> Loren is a persistent personal secretary / Jarvis-like assistant first. Conversation, durable memory, current information, research, organization and explicit owner approval come before broader automation.

---

# 1. Green baseline on main

M6A.5 is locally ready on `codex/m6a5-conversational-approval`. Parent review and verification passed 180/180 tests, build, format, dependency scan and real-host authentication smoke. The implementation includes trusted conversational proposals, authenticated ID-only decisions, frozen exact targets, one-time approval, verified execution and audit. CI and separately authorized live-provider proof remain pending; the milestone is not closed yet.

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
M6A.4 Notes / Decisions / Tasks                ✓ complete
```

Latest green main baseline:

```text
PR #32 — M6A.4 durable organization state
merge: 5dbfa332baca3aba614af8302dfd284b52f244af
PR exact-head CI #253 / 34094209539: PASS Ubuntu + Windows
post-merge main CI #254 / 34094698595: PASS Ubuntu + Windows
```

PR #32 also added the repository root `.gitignore` for .NET build output, IDE state, local `.env` files, SQLite runtime files including `loren.db*`, logs/temp artifacts and OS junk. `.env.example` remains tracked.

---

# 2. Proven product capabilities

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

Conversation is the primary owner surface. Loren has identity guidance, bounded multi-turn history, friendly project selection, deterministic project inference, trusted project-scoped memory and secondary activity/audit UI.

## M6A.2 — Current information [COMPLETE]

Read-only `web.search` uses Ollama Web Search through the ActionGateway with bounded evidence, source metadata, safe URLs and secret-safe failures.

## M6A.3 — Source-aware research [COMPLETE]

`web.search` + `web.fetch` support bounded multi-source research. External evidence is inert untrusted data. Public URL policy rejects unsafe schemes, credentials, localhost/private literal addresses and non-standard ports.

## M6A.4 — Notes / Decisions / Tasks [COMPLETE]

Durable Loren-owned organization state now exists:

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

Trust distinction:

```text
Read               public/external read
OwnerStateRead     private Loren-owned owner-state read
OwnerStateWrite    Loren-owned local state mutation
ReversibleWrite    consequential external mutation
ExternalWrite      external mutation
PrivilegedWrite    denied in v0.1
```

Owner state requires authenticated Loren-owned `AuthenticatedOwnerContext` plus a trusted executor, but does not consume external-write approval or GitHub write credentials. `LOREN_ENABLE_WRITES` remains an external-write kill switch and does not disable local notes/tasks.

SQLite migration `202609070001_AddOrganizationItems` persists note/decision/task state across restart. Lifecycle timestamps are stored as Unix milliseconds for native SQLite ordering across Linux/Windows.

---

# 3. Proven external-write safety foundation

Gate D + M5 Slices 1–3 remain unchanged:

```text
canonical target
 -> deterministic policy
 -> explicit owner approval for consequential external writes
 -> exact intent fingerprint
 -> atomic one-time consume
 -> write-specific credential boundary
 -> trusted executor
 -> post-write verification
 -> redacted audit
```

Authentication is not external-write approval. Model/external content cannot authorize itself, choose credentials, disable read-only, broaden target scope or declare unverified success.

Existing real mutation proof: verified creation of a non-default GitHub branch from an exact approved source SHA.

---

# 4. Current target — M6A.5 Conversational approval

Do **not** add another GitHub mutation primitive. Reuse the existing verified `github.create_branch` path.

Required owner UX:

```text
Owner: "Tạo branch abc cho Loren từ main."
 -> brain understands/proposes intent
 -> Loren resolves canonical Project/Repository
 -> Loren resolves exact live source SHA
 -> conversation presents exact proposal
      repository
      branch
      source SHA/ref
      access/risk
 -> owner explicitly clicks Approve or Cancel
 -> Loren creates exact one-time ActionApproval
 -> existing credential-bound trusted executor runs
 -> branch ref/SHA is independently verified
 -> Loren reports completion naturally + audit explanation
```

Critical invariant: **the chat request is intent, not approval**. The model never manufactures trusted authorization context, ApprovalId or credentials.

Existing components to reuse:

```text
LorenOwnerGitHubWriteService
GitHubCreateBranchActionExecutor
GitHubCreateBranchClient
ActionIntentFingerprint
SqliteActionApprovalStore
GateDActionPolicy
ActionGateway
owner authentication
conversation-first OwnerPages UI
```

Before implementing source-SHA proposal resolution, inspect/reuse the existing GitHub read path for live repository/default-branch state.

---

# 5. Branch audit / repository hygiene

Audit performed 2026-09-07:

```text
remote branches found: 40
open PRs:             0
branch to keep:       main
stale/history branches eligible for prune: 39
```

The owner explicitly chose to keep all 39 historical remote branches. None were deleted. Do not retry remote cleanup. Continue on `codex/m6a5-conversational-approval`; do not create a replacement branch from main.

---

# 6. v0.1 owner checkpoint

Do not call v0.1 owner-testable until Loren can complete this natural workflow:

```text
1. Chat normally as the default surface.
2. Answer stable knowledge/reasoning questions.
3. Retrieve current information with grounded sources.
4. Perform bounded source-aware research.
5. Combine project context + durable memory + live reads.
6. Record/retrieve durable Note/Decision across restart.
7. Create/list/complete Task through chat.
8. Ask for branch creation in natural language.
9. Review exact proposal and explicitly approve.
10. Receive verified completion and inspect why/audit.
```

**Do not resume controlled file/commit/open-PR mutation expansion before this checkpoint is usable.**

Gate E is still required before trusted background scheduler/reminder behavior.

---

# 7. Local continuation instruction

For Astra/local work:

```text
1. Read docs/status.md and docs/handoff.md first.
2. Inspect current branch, working changes and CI before continuing.
3. Keep all 39 historical remote branches unchanged.
4. Continue codex/m6a5-conversational-approval; preserve existing work.
5. Finish the recorded review/verification gates, then obtain CI evidence.
6. Perform live-provider proof only with a concrete separately approved proposal.
7. Keep Gate D invariants unchanged.
8. Do not broaden GitHub writes before the v0.1 checkpoint.
```

A milestone is not closed until implementation, tests and repository documentation agree.
