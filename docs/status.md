# Loren Project Status

**Last updated:** 2026-09-08
**Current version:** `v0.1 — Useful Trustworthy Assistant`
**Current product target:** `M6A.5 live proof → v0.1 owner checkpoint`
**Write expansion:** `M5 Slices 4–6 paused until the v0.1 owner checkpoint`
**Decision gates passed:** `Gate A`, `Gate B`, `Gate C`, `Gate D`

This file is the authoritative progress ledger. Read [`handoff.md`](handoff.md) next when continuing locally or in a fresh thread.

> Loren is a persistent personal secretary / Jarvis-like assistant first. Conversation, durable memory, current information, research, organization and explicit owner approval come before broader automation.

---

# 1. Green baseline on main

M6A.5 implementation is merged through [PR #33](https://github.com/rua-den/loren/pull/33). Parent verification passed 180/180 tests, build, format, dependency scan and real-host authentication smoke. PR CI #258 and post-merge main CI #259 passed Ubuntu and Windows. Next: [live proof and the owner checkpoint](owner-checkpoint.md). M6A.5/v0.1 are not closed until that evidence is recorded.

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
M6A.5 conversational approval                  ✓ merged; live proof pending
```

Latest green main baseline:

```text
PR #33 — M6A.5 conversational approval
implementation merge: 1cb4fd7f3c21d11118051c5170ae17e9fdd0cbdf
PR exact-head CI #258 / 34245950311: PASS Ubuntu + Windows
post-merge main CI #259 / 34246514926: PASS Ubuntu + Windows
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

# 4. Current target — M6A.5 live proof

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

This flow is implemented and covered by authenticated HTTP tests. Next run it with real providers, inspect and explicitly decide the frozen proposal, independently verify the GitHub ref, and record evidence using [owner-checkpoint.md](owner-checkpoint.md). Do not rebuild the existing proposal/approval path.

---

# 5. Branch audit / repository hygiene

Cleanup verified 2026-09-08 after the owner authorized removal:

```text
obsolete remote branches deleted: 40 (39 historical + merged PR #33 branch)
remaining remote/local branch at cleanup: main
main preserved: 1cb4fd7f3c21d11118051c5170ae17e9fdd0cbdf
```

All deleted branches were integrated: 11 ancestors of main, 27 exact merged PR heads, two ancestors of a merged PR head. No open PR depended on them. Deletion used exact SHA leases and an atomic push. Local recovery artifacts: `.git/branch-cleanup-before-20260908.bundle` and `.git/branch-cleanup-verified.json`. The later cleanup authorization superseded the earlier keep-39 instruction. Do not resume the deleted feature branch.

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
3. Start from current main; historical branches have been cleaned up.
4. Follow docs/owner-checkpoint.md; configure providers locally without exposing secrets.
5. Run real-provider conversational approval proof and the owner checklist.
6. Record evidence; delegate any code fixes to Luna medium, then review/test/PR/merge.
7. Keep Gate D invariants unchanged.
8. Do not broaden GitHub writes before the v0.1 checkpoint.
```

A milestone is not closed until implementation, tests and repository documentation agree.
