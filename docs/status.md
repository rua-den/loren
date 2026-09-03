# Loren Project Status

**Last updated:** 2026-09-03  
**Current version phase:** `v0.1 — Trustworthy Core development`  
**Current decision gate:** `Gate B — PASSED`  
**Current milestone:** `M1 — Engineering foundation`

This file is the authoritative progress ledger for the repository. `README.md` and `README.vi.md` summarize it.

## Current status

Loren has completed `v0.0 — Architecture / Feasibility`.

- **Gate A PASSED** through ADR-001: Loren owns canonical identity/state/memory/policy/action authorization/audit.
- **Gate B PASSED** through ADR-002: the v0.1 implementation stack and provider-neutral brain boundary are technically proven.
- **M0 COMPLETE**: real provider loop, cancellation, MCP, persistence/recovery, and web-host proofs passed.

Production implementation may now begin. Current work is **M1 — Engineering foundation**.

## Accepted v0.1 stack

```text
C# 14 / .NET 10 LTS
ASP.NET Core
small Loren-owned bounded agent loop
provider-neutral IBrain
  ├─ Ollama adapter
  ├─ OpenAI adapter
  └─ future providers/local models
MCP C# SDK behind Loren action contracts
SQLite + EF Core
Blazor Web App
xUnit
```

The first live provider to close Gate B was **Ollama Cloud**, using native `/api/chat` with `gpt-oss:120b`. This does not make Ollama Loren's identity or permanent provider; it is simply the first real provider that proved the contract.

## M0 final evidence

Trusted run #70 (`33730812646`) executed from exact `main` commit `801c5d24d4cec720af65b2d1b8a74e2adcbf9f5a` and completed successfully.

```text
OpenAI spike build              PASS
Ollama spike build              PASS
trusted exact-main SHA guard    PASS
provider selector -> Ollama     PASS
Ollama live tool round trip     PASS
Ollama live cancellation        PASS
MCP regression                  PASS
SQLite/EF recovery              PASS
ASP.NET/Blazor smoke test       PASS
```

### Live brain proof

Observed:

```text
Ollama gpt-oss:120b
 -> ActionRequest: get_project_status
 -> Loren ActionGateway
 -> { project: Loren, repository: rua-den/loren, branch: main, status: planning }
 -> Ollama final answer
 -> PASS
```

### Live cancellation proof

Cancellation was armed immediately before the live provider request and observed at the provider await after 100 ms.

### Secret isolation

`OLLAMA_API_KEY` and `OPENAI_API_KEY` appeared in GitHub Actions logs only as masked `***`. No provider secret entered action arguments or tool results.

### OpenAI adapter note

The OpenAI adapter compile/function-call boundary and provider reachability are proven. The currently configured OpenAI account/project returned `429 credit_balance_exhausted` before model execution. That provider-specific billing state no longer blocks Loren because `IBrain` is provider-neutral.

## Current milestone — M1 Engineering foundation

M1 now owns production scaffolding. Required deliverables:

- production solution/project structure;
- pin .NET SDK and package versions;
- nullable/warnings/analyzers/formatting policy;
- xUnit deterministic tests;
- CI restore/build/test/static checks;
- `.env.example` without secrets;
- local development/setup docs;
- dependency update/lock strategy;
- basic secret/dependency scanning;
- health/startup test;
- guarantee `Loren.Core` has no Ollama/OpenAI/MCP/EF/Blazor dependency.

M0 spike code remains disposable evidence under `spikes/adr-002/`; production code must not simply promote spike implementation without proper boundaries/tests.

## Next execution sequence

```text
NOW
v0.1 / M1
    |
    +-- scaffold production solution
    +-- pin SDK/packages
    +-- establish dependency direction
    +-- add deterministic tests + CI
    +-- setup/development docs
    |
    v
M1 exit gate
    |
    v
M2 — Walking Skeleton
    |   FIRST OWNER-TESTABLE LOREN PREVIEW
    |
    |   UI
    |    -> Loren Runtime
    |    -> IBrain
    |    -> github.read_repository ActionRequest
    |    -> Action Gateway
    |    -> GitHub read executor
    |    -> structured result
    |    -> final answer
    |    -> Audit
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

The first meaningful user test remains **M2 — Walking Skeleton**.

Expected flow:

```text
Owner: "Loren, check repo rua-den/loren."

UI
 -> Loren Runtime
 -> configured IBrain provider
 -> github.read_repository ActionRequest
 -> Loren ActionGateway
 -> GitHub read executor
 -> structured ActionResult
 -> IBrain final response
 -> Audit
```

M1 will be runnable for engineering validation, but M2 is the first milestone intended to feel like actually using Loren.

## Progress-update rule

Any merge that changes capability, milestone completion, ADR status, validated dependencies/providers, or the next execution target must update:

1. `docs/status.md`;
2. `README.md`;
3. `README.vi.md`;
4. the relevant ADR/plan when a decision or milestone changes.

A milestone is not considered closed until implementation/tests and repository documentation agree.
