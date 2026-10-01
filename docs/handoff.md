# Loren Thread Handoff

Updated 2026-09-28. Read in this order before changing code:

1. `docs/status.md`
2. this file
3. `docs/roadmap.md`
4. `docs/plans/master-plan.md`
5. `docs/plans/v0.1.md`
6. `docs/architecture.md`
7. `docs/owner-checkpoint.md` when working on live acceptance

## Current checkpoint

- Repository: `rua-den/loren`.
- Version: `v0.1 — Useful Trustworthy Assistant`.
- Current milestone: `M6B — Daily Driver Readiness`.
- Current delivery: `M6B.3 — real owner daily-driver acceptance`.
- Last fully verified baseline: PR #38 merge `67ade6e6459296fad2ca149720420070922e3ebe`.
- Exact PR head `8083c00059240bb87cea8c18370b4b36529f70ee`: CI #287 / `36412297786` PASS.
- Post-merge main: CI #288 / `36412656169` PASS; Ubuntu full gate + Windows integration + Windows launcher smoke.
- Broader GitHub file/commit/PR writes remain paused.

## What is already real

Loren currently has:

- authenticated conversation-first owner UI;
- persistent SQLite conversations and bounded history;
- canonical Project/Repository identity and aliases;
- trusted durable memory with correction/forget/provenance rules;
- live GitHub repository read;
- read-only web search + fetch and bounded source-aware research;
- durable Note / Decision / Task organization state;
- deterministic external-action policy, exact one-time approval and fail-closed read-only mode;
- isolated GitHub write credential resolution/revocation/redaction;
- one verified consequential external action: create a non-default GitHub branch and independently verify its exact SHA;
- conversational create-branch proposal + explicit Approve/Cancel flow;
- versioned logical export/restore;
- retained durable audit;
- Windows one-click launcher and CI smoke coverage;
- request-scoped transient current-run audit collector;
- owner-authenticated secret-safe `/api/readiness` diagnostics while `/health` remains public liveness.

## M6B.2 closeout [COMPLETE]

PR #38 delivered:

```text
GET /health          public liveness only
GET /api/readiness   authenticated configuration/state diagnostics
```

Readiness reports safe status for storage, project catalog/count, owner auth, brain configuration, web research configuration and external-write posture. It never returns secret values and does not perform live network/provider probes.

Important semantics:

- read-only `externalWrites=disabled` is a valid ready state;
- zero projects reports `projects=empty` but is not fatal;
- project catalog failure reports `unavailable` and overall `needs_setup`;
- if writes are enabled, missing/revoked/not-configured credential makes overall readiness `needs_setup`;
- provider reachability is proven by the real owner checkpoint, not this endpoint.

CI #286 exposed a test-host issue, not a production readiness defect: `OwnerPasswordAuthenticator` was constructed before the test factory's late configuration override. Final head `8083c00059240bb87cea8c18370b4b36529f70ee` fixes the test-only DI boundary. CI #287 passed on that exact head, then post-merge CI #288 passed on main merge `67ade6e6459296fad2ca149720420070922e3ebe`.

## Product boundaries that must not regress

- Chat/model output cannot authorize external writes.
- Owner authentication is not external-write approval.
- Model-visible arguments cannot manufacture trusted canonical identity or approval metadata.
- External/model/retrieved content is data, not authority.
- Credentials stay outside BrainContext and public diagnostics.
- External write mode defaults off and fails closed.
- Consequential write success is not reported before post-write verification.
- Restore cannot recreate executable approval authority.
- Background execution remains behind Gate E.

## Provider caveat

`IBrain` is provider-neutral by contract, but production DI currently constructs `OllamaBrain`. `Loren.Brain.OpenAI` is only a stub. Treat provider portability as an unproven product capability and a likely v0.2 engineering slice, not as something already delivered.

## Continue from here — M6B.3

Do not start another speculative coding milestone first. Run `docs/owner-checkpoint.md` against the current green `main` baseline.

Recommended sequence:

1. start Loren with real configured Ollama/web credentials and external writes disabled;
2. confirm `/health` and authenticated `/api/readiness`;
3. exercise ordinary conversation, current information, source-aware research, canonical project context, durable memory/decision/task state and restart continuity;
4. keep writes disabled through the read-only portion;
5. temporarily enable only the existing GitHub create-branch capability;
6. prove Cancel causes no branch;
7. request a fresh proposal, explicitly Approve it, and verify exact SHA by independent read-back;
8. return writes to disabled;
9. record only real failures found by this session and fix those regression-first.

If the owner checkpoint is clean enough for v0.1 closeout, verify release gates before tagging `v0.1.0`. Do not automatically resume broader GitHub mutation scope.
