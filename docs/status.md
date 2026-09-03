# Loren Project Status

**Last updated:** 2026-09-03  
**Current version phase:** `v0.0 — Architecture / Feasibility`  
**Current decision gate:** `Gate B — v0.1 implementation stack`  
**Gate B status:** `BLOCKED — OpenAI API credit balance exhausted`  
**Current milestone:** `M0 — ADR-002 technical validation`

This file is the authoritative progress ledger for the repository. The English and Vietnamese READMEs summarize this file.

## Current status

Loren has completed the architecture ownership decision and all no-secret implementation required for Gate B. The trusted live OpenAI path now accepts the repository `OPENAI_API_KEY` successfully and reaches the real OpenAI Responses API.

The current blocker is no longer credential configuration. Trusted live validation rerun attempt #2 reached the provider and received:

```text
HTTP 429
insufficient_quota: credit_balance_exhausted
You have no credits remaining.
```

This is an OpenAI API billing/quota blocker. No new architecture or SDK incompatibility has been observed yet because the request was rejected before model execution.

### Gate A — Core ownership

**PASSED** via ADR-001.

Loren owns canonical identity, state, memory, policy, action authorization, and audit history. Brain providers, MCP, vendor APIs, UI, and runtime details remain replaceable adapters.

### Gate B — v0.1 implementation stack

**BLOCKED** via ADR-002 pending a funded credential-backed OpenAI proof.

Proposed v0.1 stack:

```text
C# 14 / .NET 10
ASP.NET Core
small Loren-owned agent loop
OpenAI Responses API as first brain
MCP C# SDK behind Loren adapter
SQLite + EF Core
Blazor Web App
```

## M0 validation matrix

| Proof | Status | Evidence |
| --- | --- | --- |
| OpenAI brain-loop compile boundary | PASS | OpenAI .NET 2.12.0 compiles on .NET 10; structured function call is intercepted by Loren-owned code; six-turn bound enforced |
| Live OpenAI proof automation | PASS | Normal round trip + explicit cancellation modes are implemented and fail closed without trusted credentials |
| Trusted live trigger boundary | PASS | One-shot branch must equal current `main` SHA before any secret-backed step runs |
| Repository `OPENAI_API_KEY` | PASS | Trusted rerun received the secret; logs mask the value as `***` |
| OpenAI API quota | BLOCKER | Real provider request returned `429 insufficient_quota: credit_balance_exhausted` before model execution |
| OpenAI live provider round trip | OPEN | Retry after API credit is available |
| Provider cancellation path | READY / OPEN | Async provider call, shared cancellation token, timeout, explicit cancel-after mode, and expected-cancellation assertion are implemented; live execution remains open |
| MCP client/gateway | PASS | `ModelContextProtocol` 2.2.0; pinned `server-everything@2026.8.31`; tool enumeration + allow-listed read-only call passed in CI |
| SQLite + EF Core | PASS | EF Core 10.0.11 migration -> persist -> export -> wipe -> migrate -> restore -> reload passed in CI |
| ASP.NET Core + Blazor host | PASS | Host boot, `/health`, Blazor render, DI fake brain, cancellation/logging boundary, and `/brain` smoke test passed in CI |

## Completed implementation evidence

### PR #1 — Brain-loop boundary

Merged into `main`.

Established .NET 10 brain spike, OpenAI Responses function-calling compile boundary, Loren-owned `ActionGateway`, structured fake results, mandatory gateway crossing before PASS, and hard turn bounds.

### PR #2 — MCP, persistence, and host

Merged into `main`.

Established real MCP stdio connection through the Loren gateway, pinned MCP test server, SQLite/EF migration and recovery proof, and ASP.NET Core/Blazor host smoke test.

### PR #3 — Async/cancellation preparation

Merged into `main`.

Established async OpenAI Responses calls, one cancellation token propagated through provider calls, Ctrl+C/wall-clock timeout bounds, and secret isolation from ordinary PR CI.

### PR #4 — Final live-proof automation

Merged into `main`.

Established normal provider round-trip automation, dedicated provider-await cancellation mode, fail-closed missing-secret behavior, and secret-free ordinary PR/push CI.

### PR #5 — Connector-safe trusted live trigger

Merged into `main`.

Established manual `workflow_dispatch` only from `main` plus connector-triggered live validation only from `spike/adr-002-live-proof-run`, with exact-current-main SHA verification before secret access.

## Live validation evidence

### Run #53 — missing secret proof

```text
brain build                         PASS
trusted SHA guard                   PASS
OPENAI_API_KEY present              NO
fail-closed secret requirement      EXPECTED FAILURE
```

### Run #54 / rerun attempt #2 — real provider reached

After the repository secret was configured:

```text
brain build                         PASS
trusted SHA guard                   PASS
OPENAI_API_KEY present              YES
secret value exposed in logs        NO (masked)
OpenAI Responses request            REACHED PROVIDER
provider response                   HTTP 429
provider error                      insufficient_quota: credit_balance_exhausted
normal round trip                   BLOCKED BEFORE MODEL EXECUTION
cancellation proof                  NOT RUN (previous step failed)
```

This proves the secret path and provider network/API path work. It does not yet satisfy the M0 brain behavior proof because no model response was produced.

## Current blocker

Add API credit / enable API billing for the OpenAI organization/project associated with the configured `OPENAI_API_KEY`.

Do not change or expose the key unless the billing account/project association itself is wrong.

After credit is available, re-run the trusted validation. The rerun must prove:

```text
Proof A — normal path
OpenAI model
    -> requests get_project_status
    -> Loren ActionGateway intercepts
    -> fake structured result
    -> result returned to OpenAI
    -> final model response
    -> PASS

Proof B — cancellation path
OpenAI provider call
    -> Loren cancellation token fires
    -> provider call terminates through that token
    -> expected cancellation observed
    -> PASS
```

Until both pass, ADR-002 remains **Proposed** and production v0.1 scaffolding does not begin.

## Next execution sequence

```text
NOW
v0.0 / Gate B / M0
    |
    +-- add/enable OpenAI API credit for the key's project/org
    |
    +-- fast-forward trusted live branch to current main
    |
    +-- rerun trusted live validation
    |      normal provider round trip
    |      cancellation proof
    |
    v
Accept ADR-002
    |
    v
M1 — Engineering foundation
    |
    v
M2 — Walking skeleton
    |   first owner-testable Loren preview
    |   UI -> Brain -> ActionGateway -> GitHub READ -> Audit
    v
M3 — Canonical state
    v
M4 — Trusted memory
    v
M5 — Permission/credential boundary + GitHub writes
    v
M6 — Minimal daily-use UI
    v
M7 — Export/restore
    v
M8 — Security/reliability E2E
    v
v0.1.0
```

## First owner-testable milestone

The first meaningful user test is **M2 — Walking Skeleton**.

Expected flow:

```text
Owner: "Loren, check repo rua-den/loren."

UI
 -> Loren Runtime
 -> OpenAI brain
 -> github.read_repository ActionRequest
 -> Loren ActionGateway
 -> GitHub read executor
 -> structured ActionResult
 -> brain final response
 -> audit
```

M1 is engineering foundation and will be runnable, but M2 is the first milestone intended to feel like using Loren rather than testing infrastructure.

## Progress-update rule

Any merge that changes project capability, milestone completion, an ADR gate, validated dependency versions, or the next execution target **must update repository progress in the same change or immediately following documentation commit**.

Required synchronized files:

1. `docs/status.md` — authoritative detailed status;
2. `README.md` — English summary;
3. `README.vi.md` — Vietnamese summary.

When a decision changes, update the relevant ADR as well. When milestone scope/order changes, update the master/version plan as well.

A milestone is not considered fully closed in the repository until its implementation/tests and status documentation agree.
