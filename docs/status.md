# Loren Project Status

**Last updated:** 2026-09-06  
**Current version phase:** `v0.1 — Useful Trustworthy Assistant`  
**Current product target:** `M6A — Conversational Secretary + Information Layer`  
**Write expansion status:** `M5 Slices 4–6 paused after Slice 3 proof`  
**Decision gates passed:** `Gate A`, `Gate B`, `Gate C`, `Gate D`

This file is the authoritative progress ledger. Read [`handoff.md`](handoff.md) immediately after this file when continuing in a fresh thread.

> **Roadmap correction:** Loren is a personal secretary / Jarvis-like assistant first. The next owner checkpoint is natural conversation + current information + research + durable context/organization + conversational approval. Additional GitHub write primitives are not the current priority.

---

# 1. Completed foundations

```text
v0.0 Architecture / Feasibility            ✓ complete
M1 Engineering Foundation                  ✓ complete
M2 Conversation/tool Walking Skeleton      ✓ complete
M3 Canonical Project/Repository State      ✓ complete
M4 Trusted Durable Memory                  ✓ complete
Gate D Action/Approval/Credential Policy   ✓ passed
M5 Slice 1 write policy/approval            ✓ complete
M5 Slice 2 credential isolation/redaction  ✓ complete
M5 Slice 3 verified create branch          ✓ complete
```

## M2 proved

Authenticated owner -> real brain -> real GitHub read -> structured tool result -> natural final answer -> correlated audit.

That conversation/tool loop already exists and is now being promoted back to the center of the product.

## M3 proved

Loren owns canonical Project/Repository IDs and aliases independent of provider/session identity.

## M4 proved

- owner memory survives restart;
- corrections append/supersede rather than destructively rewrite history;
- forgotten correction chains do not resurrect;
- prepared memory is bounded and provenance-aware;
- model/external text cannot silently become owner truth or authority.

---

# 2. Gate D + M5 safety proof

Gate D / ADR-004 locks:

```text
canonical target
 -> deterministic policy
 -> explicit owner approval
 -> exact intent fingerprint
 -> atomic one-time consume
 -> write-specific credential boundary
 -> controlled executor
 -> post-write verification
 -> redacted audit
```

Authentication is not approval. Model/external content cannot authorize itself, choose credentials, disable read-only, broaden target scope, or declare success.

## M5 Slice 1 [COMPLETE]

PR #25 merged:

```text
merge: caa65fbbd7c3828b68aa198dad625e73e9c096b4
PR CI #194 / 33973579862: PASS Ubuntu + Windows
post-merge main CI #195 / 33973694524: PASS Ubuntu + Windows
```

Delivered typed access classes, trusted authorization context, exact fingerprint, SQLite one-time approvals, replay/expiry/revocation/mismatch rejection, fail-closed `LOREN_ENABLE_WRITES`, and migration drift tests.

## M5 Slice 2 [COMPLETE]

PR #26 merged:

```text
merge: f7fb36bae324dbd7bb8d12e02daf3fe0dd98e7da
PR CI #201 / 34027113298: PASS Ubuntu + Windows
post-merge main CI #202 / 34027255592: PASS Ubuntu + Windows
```

Delivered dedicated GitHub write credential purpose/reference, `GITHUB_WRITE_TOKEN`, revocation, no fallback, and redaction across result/audit/brain boundaries.

## M5 Slice 3 [COMPLETE — WRITE PROOF]

PR #27 merged:

```text
frozen PR head: 658fc550f5fd1660a05590a06c4285add4e50490
merge: bd0220550592a3ba55a2c722192e43df6e8ca321
PR CI #217 / 34029409983: PASS Ubuntu + Windows
post-merge main CI #218 / 34029500883: PASS Ubuntu + Windows
```

Delivered the first narrow real write capability:

```text
explicit owner request
 -> canonical Project/Repository
 -> exact branch + source SHA intent
 -> one-time approval
 -> trusted executor
 -> dedicated credential
 -> GET default-branch preflight
 -> reject unsafe/default branch
 -> POST git/refs
 -> GET exact branch
 -> verified SHA must equal approved SHA
```

The branch-create owner form is a **technical harness / safety proof**, not the intended final Loren UX.

---

# 3. Product roadmap rebaseline — why

The previous roadmap made M5 GitHub write slices the linear path before the daily-use assistant experience. That sequence was technically defensible but product-wrong for Loren's intended identity.

The corrected capability order is:

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

Therefore:

```text
M5 Slice 4 file/commit   PAUSED
M5 Slice 5 open PR       PAUSED
M5 Slice 6 write E2E     PAUSED as a broadening track
```

Existing write foundations remain valid and will be reused after the assistant interaction checkpoint.

---

# 4. Current milestone — M6A Conversational Secretary + Information Layer

## M6A.1 — Conversation primary surface [NEXT]

Goal: Loren should feel like a secretary, not an admin dashboard.

Deliver:

- conversation becomes the first/main surface after login;
- ordinary reasoning/knowledge questions work naturally;
- canonical project context and trusted memory are part of the normal conversation path;
- owner does not manually enter canonical IDs for normal use;
- tool activity is visible but secondary;
- low-level bootstrap/debug controls move behind secondary settings/admin UI.

Initial acceptance:

```text
"Giải thích cái này cho tao."
"Mày nhớ gì về project Loren?"
"Repo Loren hiện sao rồi?"
```

## M6A.2 — Current-information / web read [AFTER M6A.1]

Add provider-neutral read-only information/search capability.

Required:

- search/retrieve current external information;
- source URL/title/time/provider metadata;
- bounded retrieved content;
- external content treated as inert untrusted data;
- deterministic fake-provider tests;
- failure/uncertainty instead of fabricated current facts.

Acceptance examples:

```text
"Tìm tài liệu mới nhất về X."
"Y hiện version mới nhất là gì?"
"Có update gì gần đây về Z?"
```

## M6A.3 — Source-aware research/synthesis

- multiple sources;
- compare/merge/deduplicate;
- sourced fact vs inference distinction;
- stale/conflicting information surfaced;
- bounded iterative retrieval.

## M6A.4 — Notes / Decisions / Tasks

Add durable owner-facing organization primitives usable through conversation:

```text
Note
Decision
Task
TaskStatus
optional Project scope
provenance/timestamps
```

No background reminder execution yet; Gate E is required before trusted scheduler behavior.

## M6A.5 — Conversational approval

Reuse existing safe `github.create_branch` implementation to prove the intended UX:

```text
Owner: "Tạo branch abc cho Loren."
 -> brain proposes typed action
 -> Loren resolves exact target
 -> UI/conversation shows proposal
 -> owner approves
 -> existing one-time approval + credential boundary
 -> verified create branch
 -> natural-language completion
```

No new GitHub write primitive is needed for this slice.

---

# 5. Next owner test checkpoint

The next time the owner should pull `main` specifically to evaluate the product, Loren must support:

```text
1. Normal conversation as the default surface.
2. Stable knowledge/reasoning question.
3. Current-information question with grounded external retrieval + sources.
4. Project question combining canonical context + memory + live GitHub read.
5. Teach a durable fact/decision, restart, recall it.
6. Create/list/complete a task through chat.
7. Ask to create a branch in natural language.
8. Review exact approval proposal and approve.
9. Receive verified completion naturally.
10. Ask why Loren did it and inspect explanation/audit.
```

**Do not resume controlled file/commit or open-PR work before this checkpoint is usable.**

---

# 6. After M6A checkpoint

Then reassess highest-value next capability rather than automatically continuing GitHub automation.

Likely options:

```text
A. resume M5 Slice 4 controlled file/commit
B. richer read-only personal integrations
C. recovery/security closeout
```

If write expansion resumes:

```text
M5 Slice 4 controlled file/commit on non-default branch
M5 Slice 5 open pull request
M5 Slice 6 replay/revocation/injection/audit E2E
```

---

# 7. Version direction

```text
v0.1 useful trustworthy assistant        <- current
v0.2 personal secretary integrations     Calendar/Gmail/files/tasks richer reads
v0.3 personal/project operations         approved writes + background after Gate E
v0.4 voice + device presence
v0.5 proactive/background Loren
v0.6+ daily-use hardening
v1.0 stable personal daily driver
```

Read-before-write is the default integration rule.

---

# 8. Progress-update rule

Any merge that changes capability, milestone completion, ADR status, validated dependencies/providers, or next execution target must synchronize:

1. `docs/status.md`;
2. `README.md`;
3. `README.vi.md`;
4. `docs/plans/master-plan.md`;
5. `docs/plans/v0.1.md`;
6. `docs/handoff.md`.

A milestone is not closed until implementation/tests and repository documentation agree.
