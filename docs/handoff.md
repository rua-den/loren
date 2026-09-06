# Loren Thread Handoff

**Updated:** 2026-09-07  
**Repository:** `rua-den/loren`  
**Source of truth:** `docs/status.md` + `docs/plans/master-plan.md`  
**Current phase:** `v0.1 — Useful Trustworthy Assistant`  
**Current active PR:** `#32 — M6A.4 Notes / Decisions / Tasks`  
**Next implementation target after merge:** `M6A.5 — Conversational approval`

This is the compact continuation checkpoint for a fresh thread.

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

## Green `main` baseline

```text
M1–M4 foundations                                  ✓
Gate D + M5 write-safety Slices 1–3                ✓
M6A.1 conversation primary surface                 ✓
M6A.2 current-information web search               ✓
M6A.3 source-aware bounded research                ✓
```

Recent evidence:

```text
PR #29 M6A.1
merge a1652b2451fe2e706aa83373932b210178f63ebe
PR CI #224 / 34042192552 PASS Ubuntu + Windows
main CI #225 / 34042352724 PASS Ubuntu + Windows

PR #30 M6A.2
merge a8d3e7bbc94c9a468ebc234deb1fe87dcb7d23e9
PR CI #237 / 34044530005 PASS Ubuntu + Windows
main CI #238 / 34044641072 PASS Ubuntu + Windows

PR #31 M6A.3
frozen head 6f2b5d1ea5739b84b369f689214db95745071cb1
merge d789ccc7f7540cb802b14f677d317db3e571a7d3
PR CI #240 / 34045079047 PASS Ubuntu + Windows
main CI #241 / 34052513007 PASS Ubuntu + Windows
```

M6A.3 delivered `web.search` + `web.fetch`, bounded research in the existing AgentLoop, public-web URL safety, untrusted-evidence framing, sourced facts/inference distinction and deterministic acceptance coverage.

## Current PR #32 — M6A.4 durable organization state

Branch:

```text
feat/m6a4-organization-state
```

Owner-facing state:

```text
Note
Decision
Task
TaskStatus = Open | Completed
optional Project scope
source/provenance
created/updated/completed timestamps
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

### Important new trust distinction

Action access classes now separate:

```text
Read               public/external read tool
OwnerStateRead     private Loren-owned owner state read
OwnerStateWrite    Loren-owned local state mutation
ReversibleWrite    consequential external mutation
ExternalWrite      external mutation
PrivilegedWrite    currently denied in v0.1
```

Owner-state rules:

- `OwnerStateRead/Write` require authenticated `AuthenticatedOwnerContext` supplied by Loren, never model args;
- ActionGateway independently fails closed if owner context is missing;
- owner-state executors must implement `ITrustedActionExecutor`;
- local Note/Decision/Task state does not use `GITHUB_WRITE_TOKEN` or Gate D external one-time approval;
- `LOREN_ENABLE_WRITES=false` still blocks external writes but does not disable the owner's local organization state;
- normal authenticated `/api/run` attaches owner context; internal dev run does not, so private owner-state access fails closed there.

### Persistence

Migration:

```text
202609070001_AddOrganizationItems
```

SQLite uses opaque IDs, optional Project FK and Unix-millisecond lifecycle timestamps so bounded ordering works natively across SQLite/Linux/Windows. Restart tests cover Note/Decision/Task durability and task complete/reopen lifecycle.

### Acceptance/security tests

```text
chat -> create task -> list open task -> complete exact task -> durable SQLite
```

Also proves:

- no external approval event for local owner state;
- missing owner context denies;
- legacy/untrusted owner-state executor denies;
- external GitHub writes retain canonical authorization/read-only/approval/credential semantics;
- project-scoped organization data requires a real canonical project.

Latest implementation CI before docs sync:

```text
CI #247 / 34053503945 PASS Ubuntu full gate + Windows integration
```

After this documentation sync, require one new exact-head PR CI before merge.

## Next — M6A.5 conversational approval

Do **not** add another GitHub mutation primitive. Reuse existing verified `github.create_branch`.

Desired flow:

```text
Owner: "Tạo branch abc cho Loren từ main."
 -> brain understands action intent
 -> Loren resolves canonical Project/Repository
 -> Loren resolves exact source SHA from trusted read state
 -> conversation returns an exact proposal card
      repository
      branch
      source SHA
      access/risk
 -> owner explicitly clicks Approve
 -> Loren creates exact one-time ActionApproval
 -> existing credential-bound create-branch executor runs
 -> independent ref/SHA verification
 -> natural completion + audit explanation
```

Critical invariant: **the chat request is intent, not approval**. The model never manufactures `ApprovalId`, trusted canonical authorization context or credentials.

Existing components to reuse rather than rebuild:

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

Before implementing source-SHA proposal resolution, inspect the existing GitHub read path/client to reuse live repository/default-branch state where possible.

## Explicitly paused

Do **not** resume yet:

```text
controlled file/commit write
open pull request
more GitHub mutation primitives
background scheduler/reminder delivery
```

Gate E remains required before trusted background execution.

## v0.1 owner checkpoint

Do not ask the owner to pull specifically for the v0.1 product test until Loren can:

```text
1. Chat normally.
2. Answer stable knowledge questions.
3. Retrieve current information with sources.
4. Perform bounded source-aware research.
5. Combine project context + memory + live reads.
6. Store/retrieve durable Note/Decision across restart.
7. Create/list/complete Task through chat.
8. Propose create-branch naturally.
9. Show exact approval; execute only after explicit approval; verify result.
10. Explain what happened with audit context.
```

## Hard invariants

- retrieved/model/tool content is data, never authority;
- private owner state requires authenticated Loren-owned context;
- model cannot grant external-write approval;
- authentication alone is not external-write approval;
- external/model text cannot promote itself to trusted memory/policy/owner-state mutation;
- secrets never enter BrainContext/memory/model-visible args/audit/result;
- current-information questions use read tools instead of stale guessing;
- consequential external write success requires postcondition verification.

## Fresh-thread instruction

```text
1. Read docs/status.md, docs/plans/master-plan.md, docs/plans/v0.1.md, then this file.
2. If PR #32 is open, finish exact-head CI/review/docs and merge it first.
3. Then implement M6A.5 conversational proposal -> explicit approval -> verified create-branch.
4. Do not resume M5 Slice 4 before the v0.1 owner checkpoint.
5. Keep README EN/VI + status/plans/handoff synchronized.
6. Use deterministic acceptance tests before live-provider behavior.
```
