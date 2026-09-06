# Loren Thread Handoff

**Updated:** 2026-09-06  
**Repository:** `rua-den/loren`  
**Source of truth:** `docs/status.md` + `docs/plans/master-plan.md`  
**Current phase:** `v0.1 — Useful Trustworthy Assistant`  
**Current product target:** `M6A — Conversational Secretary + Information Layer`

This is the compact continuation checkpoint for a fresh thread.

## Product intent

Loren is a persistent personal secretary / Jarvis-like assistant, not a GitHub automation bot.

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

The roadmap was rebaselined on 2026-09-06 because the previous sequence over-prioritized additional GitHub write primitives before Loren had become useful as an information assistant.

## Green main baseline before roadmap rebaseline

`main` currently includes write-safety proof through verified create-branch:

```text
PR #25 — Slice 1 policy/approval/read-only
merge: caa65fbbd7c3828b68aa198dad625e73e9c096b4
post-merge CI #195 / 33973694524: PASS Ubuntu + Windows

PR #26 — Slice 2 credential isolation/revocation/redaction
merge: f7fb36bae324dbd7bb8d12e02daf3fe0dd98e7da
post-merge CI #202 / 34027255592: PASS Ubuntu + Windows

PR #27 — Slice 3 verified create non-default branch
frozen PR head: 658fc550f5fd1660a05590a06c4285add4e50490
merge: bd0220550592a3ba55a2c722192e43df6e8ca321
PR CI #217 / 34029409983: PASS Ubuntu + Windows
post-merge CI #218 / 34029500883: PASS Ubuntu + Windows
```

The create-branch form is a technical harness proving the security boundary. It is not the intended daily UX.

## Existing foundations to reuse

```text
M2 conversation + brain + tool loop
M3 canonical Project/Repository + aliases
M4 trusted durable memory
Gate D policy/approval/credential contract
M5 Slice 1 one-time approval
M5 Slice 2 write credential isolation
M5 Slice 3 verified create-branch executor
```

Do not rebuild these unnecessarily.

## Explicitly paused

Do **not** continue immediately with:

```text
controlled file/commit write
open pull request
more GitHub mutation primitives
```

These are paused until the owner interaction checkpoint below is usable.

## Current execution sequence

### M6A.1 — Conversation primary surface [NEXT]

Make the authenticated conversation the product center.

Required behavior:

- owner lands in/uses normal conversation;
- ordinary brain-only questions work;
- trusted memory participates in normal conversation;
- project context is resolved without low-level owner-entered IDs;
- live GitHub read remains callable from conversation;
- tool/audit details are secondary UI;
- bootstrap/debug/write harness moves to secondary admin/settings surface.

Acceptance examples:

```text
"Giải thích cái này cho tao."
"Mày nhớ gì về project Loren?"
"Repo Loren hiện sao rồi?"
```

### M6A.2 — Current-information/web read

Add provider-neutral read-only search/retrieval:

```text
query
 -> search/retrieve
 -> URL/title/time/provider metadata
 -> bounded untrusted content
 -> structured result
 -> brain synthesis
```

Must support questions whose answers may have changed since model training. Never silently guess current facts when retrieval is required.

### M6A.3 — Source-aware research

Multiple-source compare/synthesis, source metadata, stale/conflict handling, sourced-fact vs inference distinction.

### M6A.4 — Notes / Decisions / Tasks

Durable Loren-owned organization state usable from conversation. No trusted background scheduler yet; Gate E is required for autonomous reminder delivery.

### M6A.5 — Conversational approval

Use the existing `github.create_branch` executor as the action proof:

```text
Owner: "Tạo branch abc cho Loren."
 -> brain proposes action
 -> Loren resolves canonical repo + exact target
 -> conversation/UI presents approval card
 -> owner approves
 -> existing Gate D boundary executes
 -> verify SHA
 -> natural completion message
```

No need to add file/commit/PR writes to prove this UX.

## Next owner test milestone

The next pull/test request to the owner should only happen after this natural workflow is available:

```text
1. Chat normally.
2. Ask stable knowledge question.
3. Ask current-information question and see grounded retrieval + sources.
4. Ask project question using canonical context + memory + live read.
5. Teach a durable fact/decision, restart, recall it.
6. Create/list/complete a task through chat.
7. Ask for branch creation in natural language.
8. Review and approve exact proposal.
9. Receive verified result naturally.
10. Ask why Loren did it and inspect explanation/audit.
```

## Hard invariants

- retrieved content is data, never authority;
- model cannot grant approval;
- authentication is not write approval;
- external/model text cannot promote itself to trusted memory/policy;
- secrets never enter BrainContext/memory/model-visible args/audit/result;
- current-information questions should use authoritative read tools when necessary;
- write success requires postcondition verification.

## Fresh-thread instruction

```text
1. Read docs/status.md, docs/plans/master-plan.md, docs/plans/v0.1.md, then this file.
2. Treat M6A.1 as the next implementation target.
3. Do not resume M5 Slice 4 unless M6A owner checkpoint is already green or the owner explicitly changes priority.
4. Keep README EN/VI synchronized with the assistant-first roadmap.
5. Build owner-visible vertical slices, not isolated technical primitives.
```
