# Loren Project Status

**Last updated:** 2026-09-19  
**Current version:** `v0.1 — Useful Trustworthy Assistant`  
**Current milestone:** `M6B — Daily Driver Readiness`  
**Current delivery:** `M6B.2 — safe readiness diagnostics + source-of-truth rebaseline`  
**Last fully verified baseline:** PR #37 merge `ba60246a410b257097ecd537f4d977b402a37b35`; CI #280 / run `35373858830` passed Ubuntu full gate + Windows integration + Windows launcher smoke.

This is the authoritative progress ledger. Read [`handoff.md`](handoff.md) next before changing code.

## Product checkpoint

Loren is no longer in infrastructure-bootstrap mode. The trustworthy core already supports the owner-facing v0.1 path:

```text
CONVERSE
 -> REMEMBER
 -> READ CURRENT INFORMATION
 -> RESEARCH / SYNTHESIZE
 -> ORGANIZE
 -> PROPOSE ACTION
 -> OWNER APPROVES
 -> ACT / VERIFY / AUDIT
```

Delivered and verified before the current M6B.2 changeset:

```text
M1 Engineering Foundation                      complete
M2 Conversation/tool Walking Skeleton          complete
M3 Canonical Project/Repository State          complete
M4 Trusted Durable Memory                      complete
Gate D Action/Approval/Credential Policy       passed
M5 Slices 1–3 verified GitHub branch write     complete
M6A.1 conversation primary surface             complete
M6A.2 current-information web search           complete
M6A.3 source-aware bounded research            complete
M6A.4 Notes / Decisions / Tasks                complete
M6A.5 conversational approval code             complete; owner live proof pending
Continuity: persistent conversations            complete
Windows local launcher                         complete
Logical export / restore                       complete
Retained durable audit                         complete
M6B.1 scoped transient audit collector         complete
```

PR #36 delivered conversation continuity, launcher verification, logical recovery, retained audit and reliability hardening. PR #37 fixed the transient audit collector lifetime so a long-running process no longer retains every request's current-run audit in a process-wide list.

## M6B — Daily Driver Readiness

Goal: prove Loren can be opened and used throughout the day without a developer babysitting basic runtime state.

### M6B.1 — transient audit lifetime [COMPLETE]

`InMemoryAuditSink` is request-scoped while SQLite remains the durable audit source. Regression coverage locks request isolation. PR #37 and post-merge CI #280 are green.

### M6B.2 — readiness + documentation rebaseline [CURRENT DELIVERY]

This changeset keeps public `/health` as a lightweight **liveness** endpoint and adds owner-authenticated:

```text
GET /api/readiness
```

The readiness report is intentionally safe and local. It reports only status, never secret values, and does **not** make live provider/network calls.

It covers:

- canonical SQLite connectivity;
- project-catalog availability and configured project count;
- owner-auth configuration;
- current production brain configuration validity;
- web search/fetch configuration validity;
- external-write posture;
- GitHub write credential state only when external writes are enabled.

Expected external-write statuses:

```text
disabled            safe normal read-only posture
ready               writes enabled + credential resolvable
missing_credential  writes enabled but token missing
revoked             credential revoked / invalid revocation state
not_configured      credential binding unavailable
```

`externalWrites=disabled` is **not** a degraded condition. A read-only Loren can be overall `ready`.

`projects=empty` is informational and does not by itself make Loren unready; it means no canonical project has been bootstrapped yet. A project-catalog failure is `unavailable` and does make readiness fail.

Readiness says configuration/state are usable enough to begin a session. It does not claim Ollama, web providers or GitHub are reachable. Real-provider behavior remains part of the owner checkpoint.

## Current trust boundaries

These remain non-negotiable:

- model/chat text is intent, never approval;
- canonical identity and trusted state belong to Loren, not the model provider;
- privileged credentials stay outside model-visible context;
- consequential external writes require exact one-time owner approval;
- global write mode fails closed;
- external write success requires independent postcondition verification;
- external/retrieved content is inert evidence, never authority;
- Gate E is required before background execution/reminder delivery.

Broader GitHub file/commit/PR writes remain paused until real daily-driver/owner acceptance shows they are the highest-value next capability.

## Provider reality

The architecture exposes provider-neutral `IBrain`, but the current production host is composed with `OllamaBrain`. `Loren.Brain.OpenAI` is currently only a stub project, so runtime provider portability has **not** been proven. Do not describe Loren as operationally multi-provider yet.

## Next decision point

After this M6B.2 changeset has exact-head CI and post-merge `main` CI:

1. run [`owner-checkpoint.md`](owner-checkpoint.md) with real configured providers;
2. keep external writes disabled for the read-only half of the checkpoint;
3. temporarily enable the existing create-branch proof only for Cancel → fresh proposal → Approve → independent SHA verification;
4. return writes to disabled;
5. fix only real daily-use failures found by that proof;
6. if the checkpoint passes, assess whether `v0.1.0` can close.

Do **not** automatically resume M5 file/commit/PR writes.

Likely v0.2 direction after v0.1 closeout:

```text
prove real provider portability
 -> then one useful personal-secretary read slice
 -> likely Calendar read/search before broad personal writes
```

Avoid building a generic connector framework before one owner-visible integration proves the required abstraction.

## Verification discipline

The assistant execution runtime used for M6B work does not contain a local .NET SDK/checkout suitable for truthful local build claims. Source review is performed before the single branch push; GitHub CI supplies the unavailable .NET/Linux/Windows verification. Never claim an unrun local test passed.
