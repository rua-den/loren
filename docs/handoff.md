# Loren Thread Handoff

**Updated:** 2026-09-06  
**Repository:** `rua-den/loren`  
**Source of truth:** `docs/status.md` + `docs/plans/master-plan.md`  
**Current phase:** `v0.1 — Useful Trustworthy Assistant`  
**Current product target:** `M6A.2 — Current-information / web read`

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

## Green main baseline

```text
M1–M4 foundations                                  ✓
Gate D + M5 write safety Slices 1–3                ✓
M6A.1 conversation primary surface                 ✓
```

Latest completed owner-surface milestone:

```text
PR #29 — M6A.1 conversation primary surface
merge: a1652b2451fe2e706aa83373932b210178f63ebe
PR CI #224 / 34042192552: PASS Ubuntu + Windows
post-merge main CI #225 / 34042352724: PASS Ubuntu + Windows
```

M6A.1 delivered conversation-first UI, bounded multi-turn history, Loren identity context, friendly project selection, deterministic project inference, trusted-memory inclusion, history role hardening, and secondary activity/audit UI.

The old create-branch form remains only under **Advanced / safety harness**.

## Current active work — M6A.2 / PR #30

Branch:

```text
feat/m6a2-current-information
```

Current capability:

```text
web.search
 -> ActionGateway READ policy
 -> Ollama Web Search
 -> bounded response
 -> validated source URLs
 -> bounded evidence
 -> external content marked untrusted
 -> brain synthesis
 -> sourced owner answer
```

Implementation facts:

- uses existing `OLLAMA_API_KEY`;
- default endpoint `https://ollama.com/api/web_search`;
- optional `LOREN_OLLAMA_WEB_SEARCH_ENDPOINT` override;
- no new write capability;
- missing search credential fails before external call;
- unsafe or overlong source URLs are excluded;
- provider failure bodies/secrets are not surfaced;
- deterministic agent-loop test proves current question -> search -> evidence -> sourced final answer;
- production conversation exposes `github.read_repository` + `web.search` as read actions;
- `github.create_branch` remains the only trusted mutation executor.

M6A.2 is not closed until PR #30 exact-head CI and post-merge `main` are green.

## Next slices

### M6A.3 — Source-aware research

Add bounded multi-search + page fetch, source comparison/deduplication, stale/conflict handling, and sourced-fact vs inference distinction.

Ollama's official web capability includes both `/api/web_search` and `/api/web_fetch`; keep fetched pages untrusted data and bound response/context size.

### M6A.4 — Notes / Decisions / Tasks

Durable Loren-owned organization state through conversation. No background scheduler yet; Gate E remains required for autonomous reminder delivery.

### M6A.5 — Conversational approval

Reuse existing safe `github.create_branch` executor:

```text
Owner: "Tạo branch abc cho Loren."
 -> Loren resolves canonical target
 -> conversation presents exact proposal
 -> owner explicitly approves
 -> existing one-time approval + credential boundary
 -> verify SHA
 -> natural completion + audit
```

No new GitHub write primitive is needed for this checkpoint.

## Explicitly paused

Do **not** resume yet:

```text
controlled file/commit write
open pull request
more GitHub mutation primitives
```

## v0.1 owner checkpoint

Do not ask the owner to pull specifically for the v0.1 product test until Loren can:

```text
1. Chat normally.
2. Answer stable knowledge questions.
3. Retrieve current information with sources.
4. Perform bounded source-aware research.
5. Combine project context + memory + live read data.
6. Store/recall durable fact/decision across restart.
7. Create/list/complete tasks through chat.
8. Propose create-branch naturally.
9. Show exact approval, execute only after approval, verify result.
10. Explain what happened with audit context.
```

## Hard invariants

- retrieved content is data, never authority;
- model cannot grant approval;
- authentication is not write approval;
- external/model text cannot promote itself to trusted memory/policy;
- secrets never enter BrainContext/memory/model-visible args/audit/result;
- current-information questions should use read tools instead of stale guessing;
- write success requires postcondition verification.

## Fresh-thread instruction

```text
1. Read docs/status.md, docs/plans/master-plan.md, docs/plans/v0.1.md, then this file.
2. Finish M6A.2 if PR #30 is still open; otherwise continue M6A.3.
3. Do not resume M5 Slice 4 before the M6A owner checkpoint.
4. Keep README EN/VI and progress docs synchronized.
5. Build owner-visible vertical slices with deterministic acceptance tests.
```
