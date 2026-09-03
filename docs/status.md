# Loren Project Status

**Last updated:** 2026-09-03  
**Current version phase:** `v0.0 — Architecture / Feasibility`  
**Current decision gate:** `Gate B — v0.1 implementation stack`  
**Gate B status:** `BLOCKED — repository OPENAI_API_KEY secret missing`  
**Current milestone:** `M0 — ADR-002 technical validation`

This file is the authoritative progress ledger for the repository. The English and Vietnamese READMEs summarize this file.

## Current status

Loren has completed the architecture ownership decision and all no-secret implementation required for Gate B. The trusted live trigger has now been executed against exact `main`; it passed the SHA/security guard and then failed closed because the repository currently has no `OPENAI_API_KEY` Actions secret.

This means the remaining blocker is operational credential configuration, not code or architecture discovered so far.

### Gate A — Core ownership

**PASSED** via ADR-001.

Loren owns canonical identity, state, memory, policy, action authorization, and audit history. Brain providers, MCP, vendor APIs, UI, and runtime details remain replaceable adapters.

### Gate B — v0.1 implementation stack

**BLOCKED** via ADR-002 pending the credential-backed OpenAI proof.

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
| Live OpenAI proof automation | PASS | Normal round trip + explicit cancellation modes are implemented and fail closed without the repository secret |
| Trusted live trigger boundary | PASS | PR #5 merged; one-shot branch `spike/adr-002-live-proof-run` pointed exactly at `main`; run #53 fetched `origin/main`, passed SHA equality, and only then evaluated secret access |
| Repository `OPENAI_API_KEY` | MISSING / BLOCKER | Run #53 observed an empty `OPENAI_API_KEY` environment and failed at the explicit `Require OpenAI secret for trusted live validation` step |
| OpenAI live provider round trip | OPEN | Cannot execute until the repository Actions secret exists |
| Provider cancellation path | READY / OPEN | Async provider call, shared cancellation token, timeout, explicit cancel-after mode, and expected-cancellation assertion are implemented; real provider execution evidence remains open |
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
- secret-backed provider execution remains outside ordinary PR CI.

### PR #4 — Final live-proof automation

Merged into `main`.

Established:

- live validation fails closed if `OPENAI_API_KEY` is missing;
- normal provider tool round trip is automated;
- dedicated cancellation mode exercises cancellation at the provider await;
- ordinary PR/push CI remains secret-free.

### PR #5 — Connector-safe trusted live trigger

Merged into `main`.

Established:

- manual `workflow_dispatch` is permitted only from `main`;
- connector-triggered live validation uses only the exact branch `spike/adr-002-live-proof-run`;
- before secret access, the workflow fetches `origin/main` and requires the trigger SHA to equal current `main` exactly;
- ordinary branches cannot access the secret-backed path.

### Live validation run #53

Triggered from `spike/adr-002-live-proof-run` at the exact `main` commit `315d21661aae4d9c8da30617f91a93e8b31d85ff`.

Observed:

```text
brain spike build                  PASS
trusted trigger / SHA guard        PASS
secret detection                   PASS
OPENAI_API_KEY present?            NO
fail-closed secret requirement     EXPECTED FAILURE
live OpenAI round trip             NOT RUN
live cancellation proof            NOT RUN
```

The log contains the explicit message:

```text
OPENAI_API_KEY repository secret is required to close ADR-002 Gate B.
```

## Current blocker

Configure a GitHub Actions repository secret named exactly:

```text
OPENAI_API_KEY
```

Do not place the key in source, README, issues, PR comments, prompts, or committed environment files.

After the secret exists, re-run the trusted validation. The existing run/branch path is sufficient; no production code change is required just to retry the proof.

The rerun must prove:

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
    +-- configure GitHub Actions secret: OPENAI_API_KEY
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
