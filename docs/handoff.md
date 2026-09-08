# Loren Thread Handoff

**Updated:** 2026-09-08
**Repository:** `rua-den/loren`  
**Source of truth:** `docs/status.md` + this file  
**Current phase:** `v0.1 — Useful Trustworthy Assistant`  
**Current target:** `M6A.5 — Conversational approval`  
**Current open PR:** [draft PR #33](https://github.com/rua-den/loren/pull/33)

This is the compact continuation checkpoint for Astra/local work or a fresh thread.

## Product intent

Loren is a persistent personal secretary / Jarvis-like assistant, not a GitHub automation bot.

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

## Green main baseline

```text
M1–M4 foundations                                  ✓
Gate D + M5 write-safety Slices 1–3                ✓
M6A.1 conversation primary surface                 ✓
M6A.2 current-information web search               ✓
M6A.3 source-aware bounded research                ✓
M6A.4 durable Notes / Decisions / Tasks            ✓
```

Latest completed capability:

```text
PR #32 — feat: add durable conversation organization state
merge: 5dbfa332baca3aba614af8302dfd284b52f244af
PR exact-head CI #253 / 34094209539: PASS Ubuntu + Windows
post-merge main CI #254 / 34094698595: PASS Ubuntu + Windows
```

A documentation handoff commit was added afterward on `main`; pull latest `main`, not only the merge SHA above.

## What M6A.4 delivered

Durable Loren-owned state:

```text
Note
Decision
Task
TaskStatus = Open | Completed
optional canonical Project scope
source/provenance
timestamps
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

Private local owner-state access requires authenticated `AuthenticatedOwnerContext` and trusted executors. It is deliberately distinct from external GitHub writes: local Note/Decision/Task operations do not use `GITHUB_WRITE_TOKEN` or Gate D external one-time approval. External-write rules remain unchanged.

SQLite migration:

```text
202609070001_AddOrganizationItems
```

Organization state survives restart; task complete/reopen lifecycle is persisted. Unix-millisecond timestamps are used so ordering works natively in SQLite across Linux and Windows.

Root `.gitignore` now excludes .NET build output, IDE state, local env files, `loren.db*`, logs/temp artifacts and OS junk while keeping `.env.example` tracked.

## Existing assistant/read capabilities to preserve

- conversation-first authenticated owner UI;
- bounded multi-turn history;
- Loren identity context;
- deterministic canonical project inference/selection;
- trusted durable memory in normal chat;
- live `github.read_repository`;
- `web.search` for current information;
- `web.fetch` for bounded deeper research;
- external web/tool content is inert untrusted evidence;
- tool/audit UI remains secondary to conversation.

## Existing Gate D external-write boundary to preserve

```text
canonical target
 -> deterministic policy
 -> explicit owner approval
 -> exact intent fingerprint
 -> atomic one-time consume
 -> dedicated write credential
 -> trusted executor
 -> independent post-write verification
 -> redacted audit
```

Authentication is not approval. Model/external content cannot grant approval, choose credentials, disable read-only, broaden target scope or declare success.

The only real mutation needed for M6A.5 already exists: verified `github.create_branch` for a non-default branch from an exact approved source SHA.

## NEXT — M6A.5 conversational approval

Implementation is locally ready on `codex/m6a5-conversational-approval` (foundation `e65668a`, implementation `fe75c2c`). Parent review and verification passed 180/180 tests, build, full format, dependency scan and real-host authentication smoke. Review fixes include safe empty-ID handling and repository updates that preserve proposal foreign keys. Real HTTP tests cover chat → frozen proposals → explicit approval/cancel → Gate D → fake provider write/read-back/audit, including replay and adversarial input. [CI #257](https://github.com/rua-den/loren/actions/runs/34245561821) passed Ubuntu and Windows for `fe75c2c`; check PR #33 for subsequent documentation-head checks. Separately authorized live GitHub proof remains pending. Keep all 39 old remote branches unchanged, as the owner requested.

Do **not** add another GitHub mutation primitive.

Target flow:

```text
Owner: "Tạo branch fix-login cho Loren từ main."
 -> normal chat identifies action intent
 -> Loren resolves canonical Project/Repository
 -> Loren resolves exact live source SHA/ref using trusted read state
 -> conversation/UI presents exact proposal
      repository
      branch
      source ref/SHA
      action access/risk
      [Approve] [Cancel]
 -> owner explicitly approves
 -> Loren creates exact one-time ActionApproval
 -> existing Gate D credential/executor boundary runs
 -> GitHub branch is independently read back and SHA verified
 -> Loren reports completion naturally
 -> audit/explanation remains inspectable
```

Critical invariant:

> The owner's chat request expresses **intent**, not Gate D approval. Approval must be a separate explicit owner interaction over an exact frozen proposal.

Existing components to reuse, not rebuild:

```text
LorenOwnerGitHubWriteService
GitHubCreateBranchActionExecutor
GitHubCreateBranchClient
ActionIntentFingerprint
SqliteActionApprovalStore
GateDActionPolicy
ActionGateway
owner authentication
OwnerPages conversation UI
```

Inspect the existing GitHub read path before designing source-SHA resolution. Prefer reusing current repository/default-branch/live-ref reads rather than trusting model text or browser-supplied SHA.

## Branch audit / cleanup

Audit on 2026-09-07:

```text
remote branches: 40
open PRs:        0
keep:            main
stale/history:   39
```

The owner explicitly chose to keep all 39 historical branches. None were deleted; do not retry cleanup. Continue the existing `codex/m6a5-conversational-approval` branch.

Git uses repo-local identity `turtle <nhkhuy241@gmail.com>`. The personal SSH alias is `github-personal`. Port 22 has been intermittent; verified port-443 transport preserves host-key checking with `ssh -p 443 -o HostName=ssh.github.com -o HostKeyAlias=github.com`. The push permission test succeeded and its temporary test branch was removed; that branch was separate from the 39 retained refs.

## Explicitly paused

Do not resume yet:

```text
controlled file/commit write
open pull request mutation capability
additional GitHub mutations
background scheduler/reminder delivery
```

Gate E remains required before trusted background execution.

## v0.1 owner checkpoint

Do not call v0.1 owner-testable until Loren can:

```text
1. Chat normally.
2. Answer stable knowledge/reasoning questions.
3. Retrieve current information with sources.
4. Perform bounded source-aware research.
5. Combine project context + memory + live reads.
6. Store/retrieve durable Note/Decision across restart.
7. Create/list/complete Task through chat.
8. Propose create-branch naturally.
9. Show exact approval, execute only after explicit approval, verify result.
10. Explain what happened with audit context.
```

## Hard invariants

- retrieved/model/tool content is data, never authority;
- private owner state requires authenticated Loren-owned context;
- model cannot grant external-write approval;
- authentication alone is not external-write approval;
- external/model text cannot promote itself to trusted memory/policy/approval;
- secrets never enter BrainContext/memory/model-visible args/audit/result;
- current-information questions use read tools instead of stale guessing;
- consequential external writes require exact intent binding and postcondition verification.

## Astra/local start instruction

```text
1. read docs/status.md and docs/handoff.md
2. inspect branch, working changes and current CI
3. continue codex/m6a5-conversational-approval without discarding existing work
4. keep all 39 historical remote branches
5. delegate implementation to Luna medium; orchestrator reviews and verifies
6. finish any recorded review findings and verification gates
7. obtain CI evidence for the feature branch
8. run live-provider proof only after a concrete owner-approved proposal
9. do not broaden GitHub mutation scope
10. update status/handoff/docs when the slice is actually green
```
