# Loren Project Status

**Last updated:** 2026-09-03  
**Current version phase:** `v0.0 — Architecture / Feasibility`  
**Current decision gate:** `Gate B — v0.1 implementation stack`  
**Gate B status:** OPEN  
**Current milestone:** `M0 — ADR-002 technical validation`

This file is the authoritative progress ledger for the repository. The English and Vietnamese READMEs summarize this file.

## Current status

Loren has completed the architecture ownership decision and most of the v0.1 stack validation.

### Gate A — Core ownership

**PASSED** via ADR-001.

Loren owns canonical identity, state, memory, policy, action authorization, and audit history. Brain providers, MCP, vendor APIs, UI, and runtime details remain replaceable adapters.

### Gate B — v0.1 implementation stack

**OPEN** via ADR-002.

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
| OpenAI live provider round trip | OPEN | Requires `OPENAI_API_KEY` outside source control; must prove real model -> ActionGateway -> structured result -> final model response |
| Provider cancellation path | PARTIAL | Async Responses call and `CancellationToken` propagation compile; live cancellation against provider still needs execution evidence |
| MCP client/gateway | PASS | `ModelContextProtocol` 2.2.0; pinned `server-everything@2026.8.31`; tool enumeration + allow-listed read-only call passed in CI |
| SQLite + EF Core | PASS | EF Core 10.0.11 migration -> persist -> export -> wipe -> migrate -> restore -> reload passed in CI |
| ASP.NET Core + Blazor host | PASS | Host boot, `/health`, Blazor render, DI fake brain, cancellation/logging boundary, and `/brain` smoke test passed in CI |

## Completed implementation evidence

### PR #1 — Brain-loop boundary

Merged into `main`.

Established:

- .NET 10 brain spike;
- OpenAI Responses function-calling compile boundary;
- Loren-owned `ActionGateway` interception;
- structured fake action result;
- mandatory gateway crossing before spike can report PASS;
- hard turn bound.

### PR #2 — MCP, persistence, and host

Merged into `main`.

Established:

- real MCP stdio connection and read-only call through Loren gateway;
- pinned MCP test server version;
- SQLite/EF migration and recovery proof;
- ASP.NET Core/Blazor host smoke test.

### PR #3 — Async/cancellation preparation

Merged into `main`.

Established:

- async OpenAI Responses path;
- one cancellation token propagated through provider calls;
- Ctrl+C and wall-clock timeout bounds;
- live OpenAI workflow can only receive repository secret through manual `workflow_dispatch`, never through PR CI.

## Current blocker

The only remaining Gate B blocker is the live OpenAI proof:

```text
OpenAI model
    -> requests get_project_status
    -> Loren ActionGateway intercepts
    -> fake structured result
    -> result returned to OpenAI
    -> final model response
    -> PASS
```

The live cancellation path must also be exercised and logs checked for secret leakage.

Until this passes, ADR-002 remains **Proposed** and production v0.1 scaffolding does not begin.

## Next milestones

```text
NOW
v0.0 / Gate B / M0
    |
    +-- live OpenAI round trip + cancellation evidence
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
