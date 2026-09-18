# Loren Thread Handoff

Updated 2026-09-19. Read in this order before changing code:

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
- Current delivery: `M6B.2 — safe readiness diagnostics + docs rebaseline`.
- Last fully verified baseline: PR #37 merge `ba60246a410b257097ecd537f4d977b402a37b35`.
- CI #280 / `35373858830`: Ubuntu full gate PASS; Windows integration PASS; Windows launcher smoke PASS.
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
- request-scoped transient current-run audit collector.

## M6B.2 contract

Public:

```text
GET /health
```

remains liveness only.

Owner-authenticated:

```text
GET /api/readiness
```

reports safe status for storage, project catalog/count, owner auth, brain configuration, web research configuration and external-write posture. It never returns secret values and does not perform live network/provider probes.

Important semantics:

- read-only `externalWrites=disabled` is a valid ready state;
- zero projects reports `projects=empty` but is not fatal;
- project catalog failure reports `unavailable` and overall `needs_setup`;
- if writes are enabled, missing/revoked/not-configured credential makes overall readiness `needs_setup`;
- provider reachability is still proven by the real owner checkpoint, not this endpoint.

Regression coverage must prove:

1. `/health` remains public;
2. `/api/readiness` requires owner auth;
3. normal read-only configuration can report `ready` without exposing `OLLAMA_API_KEY`;
4. enabled writes with missing token report `missing_credential`;
5. revoked token reports `revoked` without exposing either provider or write secret.

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

## Continue from here

For this M6B.2 delivery:

1. inspect the exact branch diff;
2. require one coherent branch commit/push;
3. require exact-head Ubuntu + Windows CI before merge;
4. merge only that exact green head;
5. require post-merge `main` CI on the exact merge SHA.

After M6B.2 is green on `main`, run `docs/owner-checkpoint.md`. The next coding work should be driven by failures from real daily use, not by automatically expanding GitHub mutation scope.

If the owner checkpoint is clean enough for v0.1 closeout, verify release gates before tagging `v0.1.0`. After that, prefer proving provider portability and one read-only personal-secretary integration over a generic connector framework.
