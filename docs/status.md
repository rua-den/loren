# Loren Project Status

**Last updated:** 2026-09-03  
**Current version phase:** `v0.1 — Trustworthy Core development`  
**Current decision gates:** `Gate A — PASSED`, `Gate B — PASSED`  
**Current milestone:** `M2 — Walking Skeleton`

This file is the authoritative progress ledger for the repository. `README.md` and `README.vi.md` summarize it.

## Current status

Loren has completed `v0.0 — Architecture / Feasibility` and the first production milestone of v0.1.

- **Gate A PASSED** through ADR-001: Loren owns canonical identity/state/memory/policy/action authorization/audit.
- **Gate B PASSED** through ADR-002: the v0.1 implementation stack and provider-neutral brain boundary are technically proven.
- **M0 COMPLETE**: real provider loop, cancellation, MCP, persistence/recovery, and web-host proofs passed.
- **M1 COMPLETE**: the production engineering foundation is scaffolded, deterministic tests and CI are green, and `Loren.Core` remains provider/framework independent.

Current work is **M2 — Walking Skeleton**, the first owner-testable vertical slice.

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
xUnit / Microsoft Testing Platform
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

## M1 final evidence — Engineering foundation

PR #7 established the first production source tree instead of promoting spike code directly.

### Production boundaries

```text
src/
├── Loren.Core/
├── Loren.Runtime/
├── Loren.Brain.Ollama/
├── Loren.Brain.OpenAI/
├── Loren.Infrastructure/
└── Loren.Web/

tests/
├── Loren.Core.Tests/
└── Loren.Runtime.Tests/
```

Tools/GitHub-specific and larger integration/E2E projects remain deferred until a real M2/M3 capability needs them. Project count is intentionally smaller than the conceptual architecture; dependency direction matters more than ceremony.

### Foundation contracts

`Loren.Core` now defines provider-neutral contracts including:

```text
IBrain
BrainContext
BrainTurnResult
ActionDefinition
ActionRequest
ActionResult
IActionGateway
```

`Loren.Runtime` contains a deterministic bounded `AgentLoop` with hard turn/action limits and cancellation propagation. Tests prove that an action must cross `IActionGateway` before its result is returned to the brain context.

### Toolchain and CI

M1 pins:

```text
.NET SDK                  10.0.400
Target framework          net10.0
C#                        14
nullable                  enabled
warnings as errors        enabled
central package versions  enabled
xUnit / MTP               enabled for .NET 10
```

PR #7 CI passes:

```text
restore                         PASS
build (0 warnings / 0 errors)   PASS
deterministic tests             PASS
format verification             PASS
basic secret scan               PASS
dependency vulnerability check  PASS
web /health smoke test          PASS
```

`Loren.Core` has no Ollama/OpenAI/MCP/EF Core/ASP.NET Core/Blazor package dependency. Provider adapter, persistence, and UI dependencies remain outside the core boundary.

Development/setup guidance lives in `docs/development.md`, and `.env.example` contains names only—no real credentials.

## Current milestone — M2 Walking Skeleton

M2 builds the first complete user-to-tool vertical slice:

```text
Owner / minimal UI
 -> Loren Runtime
 -> configured IBrain
 -> github.read_repository ActionRequest
 -> Loren ActionGateway
 -> GitHub read executor
 -> structured ActionResult
 -> IBrain final response
 -> Audit
```

Required M2 work:

- one-owner authentication/session;
- at least one production `IBrain` implementation plus deterministic fake brain;
- Action Gateway for reads as well as future writes;
- GitHub read-only executor returning structured repository state;
- correlation/run/action IDs;
- minimal append-oriented audit across the round trip;
- runtime receives prepared context rather than direct database access;
- provider and GitHub credentials remain outside brain-visible context.

## Next execution sequence

```text
NOW
v0.1 / M2 Walking Skeleton
    |
    +-- production brain adapter
    +-- github.read_repository action
    +-- Action Gateway read path
    +-- minimal owner auth/UI
    +-- correlation + audit
    +-- deterministic + integration tests
    |
    v
FIRST OWNER-TESTABLE LOREN PREVIEW
    |
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

M2 is now active and remains the first meaningful user test.

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

## Progress-update rule

Any merge that changes capability, milestone completion, ADR status, validated dependencies/providers, or the next execution target must update:

1. `docs/status.md`;
2. `README.md`;
3. `README.vi.md`;
4. the relevant ADR/plan when a decision or milestone changes.

A milestone is not considered closed until implementation/tests and repository documentation agree.
