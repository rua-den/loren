# Loren Project Status

**Last updated:** 2026-09-28  
**Current version:** `v0.1 — Useful Trustworthy Assistant`  
**Current milestone:** `M6B — Daily Driver Readiness`  
**Current delivery:** `M6B.3 — real owner daily-driver acceptance`  
**Last fully verified baseline:** PR #38 merge `67ade6e6459296fad2ca149720420070922e3ebe`; CI #288 / run `36412656169` passed Ubuntu full gate + Windows integration + Windows launcher smoke.

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

Delivered and verified through M6B.2:

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
Continuity: persistent conversations           complete
Windows local launcher                         complete
Logical export / restore                       complete
Retained durable audit                         complete
M6B.1 scoped transient audit collector         complete
M6B.2 readiness + documentation rebaseline     complete
```

PR #36 delivered conversation continuity, launcher verification, logical recovery, retained audit and reliability hardening. PR #37 fixed the transient audit collector lifetime so a long-running process no longer retains every request's current-run audit in a process-wide list. PR #38 added secret-safe owner readiness diagnostics and rebaselined the source-of-truth documentation. Its exact head `8083c00059240bb87cea8c18370b4b36529f70ee` passed CI #287, then merge `67ade6e6459296fad2ca149720420070922e3ebe` passed post-merge CI #288.

## M6B — Daily Driver Readiness

Goal: prove Loren can be opened and used throughout the day without a developer babysitting basic runtime state.

### M6B.1 — transient audit lifetime [COMPLETE]

`InMemoryAuditSink` is request-scoped while SQLite remains the durable audit source. Regression coverage locks request isolation. PR #37 and post-merge CI #280 are green.

### M6B.2 — readiness + documentation rebaseline [COMPLETE]

Public `/health` remains lightweight **liveness**. Owner-authenticated:

```text
GET /api/readiness
```

reports safe local configuration/state posture only. It never returns secret values and performs no live provider/network calls.

It covers canonical SQLite connectivity, project-catalog availability/count, owner-auth configuration, production brain configuration validity, web search/fetch configuration validity, external-write posture, and GitHub write credential state only when writes are enabled.

Expected external-write statuses:

```text
disabled            safe normal read-only posture
ready               writes enabled + credential resolvable
missing_credential  writes enabled but token missing
revoked             credential revoked / invalid revocation state
not_configured      credential binding unavailable
```

`externalWrites=disabled` is **not** degraded. A read-only Loren can be overall `ready`. `projects=empty` is informational and does not by itself make Loren unready; project-catalog failure is `unavailable` and does make readiness fail.

CI #286 exposed a test-host composition regression: the readiness integration factory configured `LOREN_OWNER_PASSWORD` too late for the singleton owner authenticator, producing login 503s. The final head fixes the test DI boundary only; production readiness semantics were unchanged. Exact-head CI #287 and post-merge main CI #288 are green.

### M6B.3 — owner daily-driver acceptance [CURRENT]

Run [`owner-checkpoint.md`](owner-checkpoint.md) with real configured providers. Start with external writes disabled, prove useful read-only daily use, then temporarily enable only the existing create-branch proof for Cancel → fresh proposal → Approve → independent exact-SHA verification. Return writes to disabled afterward.

Real failures from this owner session define the next bug/UX slices. Do not invent new mutation scope before this evidence exists.

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

1. Run [`owner-checkpoint.md`](owner-checkpoint.md) with real configured providers.
2. Keep external writes disabled for the read-only half of the checkpoint.
3. Temporarily enable the existing create-branch proof only for Cancel → fresh proposal → Approve → independent SHA verification.
4. Return writes to disabled.
5. Fix only real daily-use failures found by that proof.
6. If the checkpoint passes, assess whether `v0.1.0` can close.

Do **not** automatically resume M5 file/commit/PR writes.

Likely v0.2 direction after v0.1 closeout:

```text
prove real provider portability
 -> then one useful personal-secretary read slice
 -> likely Calendar read/search before broad personal writes
```

Avoid building a generic connector framework before one owner-visible integration proves the required abstraction.

## Verification discipline

The assistant execution runtime used for M6B work does not contain a local .NET SDK/checkout suitable for truthful local build claims. GitHub CI supplied the unavailable .NET/Linux/Windows verification. Never claim an unrun local test passed.
