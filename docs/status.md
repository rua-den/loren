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
- **M2 Slice 1 COMPLETE**: Loren has a production read-only ActionGateway path, Loren-owned run/action correlation IDs, terminal audit behavior, and a structured `github.read_repository` executor proven with deterministic integration tests.
- **M2 Slice 2 COMPLETE**: Loren has a production `OllamaBrain : IBrain` adapter with provider-neutral action-schema translation, tool-call parsing, observation replay, cancellation propagation, and explicit provider-secret isolation, all covered by deterministic fake-HTTP tests.
- **M2 Slice 3 COMPLETE (deterministic)**: `Loren.Web` now composes the production Ollama brain, AgentLoop, ActionGateway, read-only GitHub executor, and audit sink through DI. A full deterministic production-component round trip passes, and the temporary development run route is absent from the default host surface.

Current work remains **M2 — Walking Skeleton**. The next gate is a trusted live run through the production host path. After that, M2 moves to one-owner authentication/session and the minimal owner UI.

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

Tools/GitHub-specific and larger integration/E2E projects were deferred until a real M2 capability needed them. Project count is intentionally smaller than the conceptual architecture; dependency direction matters more than ceremony.

### Foundation contracts

`Loren.Core` defines provider-neutral contracts including:

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

## M2 progress — Slice 1 read boundary

The first M2 production slice established the security/audit path before adding a live provider or owner UI.

### New Loren-owned contracts

```text
RunId
ActionId
ActionExecutionRequest
PolicyDecision
IActionPolicy
IActionExecutor
AuditEvent
IAuditSink
```

The model still produces only an `ActionRequest`. Loren Runtime creates the trusted `RunId` and `ActionId`; provider output cannot choose the correlation identity used for policy and audit.

### Production read path

```text
fake/deterministic IBrain
 -> ActionRequest(github.read_repository)
 -> AgentLoop assigns RunId + ActionId
 -> ActionGateway
 -> ReadOnlyActionPolicy
 -> GitHubReadRepositoryExecutor
 -> structured ActionResult
 -> BrainActionObservation
 -> final brain response
```

`github.read_repository` performs only an HTTP `GET` to the public GitHub repository endpoint and returns structured fields such as `full_name`, `default_branch`, archive/private state, issue count, push time, and repository URL. This slice has no GitHub write action and no GitHub credential path.

### Fail-closed and audit invariants

- unregistered actions are denied before execution;
- actions not declared read-only are denied before execution;
- policy exceptions fail closed and do not reach the executor;
- executor exceptions are converted to safe structured failures without leaking exception messages;
- cancellation propagates but writes a terminal `cancelled` audit event first;
- successful/denied/failed actions retain the same Loren-owned `RunId`/`ActionId` across audit events;
- normal action audit sequence is `ActionRequested -> PolicyEvaluated -> ActionCompleted`.

Deterministic runtime and integration tests cover the gateway, deny behavior, policy failure, cancellation, structured GitHub result, and fake brain round trip.

## M2 progress — Slice 2 production Ollama brain

The second M2 slice replaced the Ollama marker project with a real provider adapter behind the existing provider-neutral `IBrain` contract.

### Provider-neutral action schema

`ActionDefinition` now carries minimal typed parameter metadata without importing Ollama/JSON SDK types into `Loren.Core`:

```text
ActionParameterDefinition
  name
  description
  type: Text | WholeNumber | DecimalNumber | Flag
  required
```

`github.read_repository` declares `owner` and `repository` as required text parameters. `OllamaBrain` translates these Loren-owned definitions into Ollama function-tool JSON at the provider boundary.

### Production adapter behavior

```text
BrainContext
 -> OllamaBrain
 -> POST /api/chat
 -> one tool_call
 -> Loren ActionRequest

BrainContext + BrainActionObservation
 -> reconstructed assistant tool call + tool result
 -> POST /api/chat
 -> final assistant content
 -> BrainTurnResult.Final
```

The current runtime accepts one action per brain turn, so parallel provider tool calls fail explicitly rather than being executed or silently collapsed.

### Provider-secret boundary

The Ollama API key is deliberately **not** part of `OllamaBrainOptions`. It is supplied separately to `OllamaBrain`, retained in a private field, and written only to the outbound `Authorization: Bearer ...` header. This avoids accidental record/options serialization or `ToString()` leakage and keeps the credential out of model-visible request JSON.

Provider HTTP failures return a safe status-only `OllamaBrainException`; raw provider response bodies are not copied into exception messages.

Deterministic fake-HTTP tests prove:

- Loren action schema -> Ollama function-tool translation;
- API key is in the authorization header and absent from the request body;
- one provider tool call -> Loren `ActionRequest`;
- `BrainActionObservation` -> assistant tool-call + tool-result replay;
- final response parsing;
- parallel tool-call rejection;
- cancellation at the provider await;
- provider failure does not expose the raw response body.

## M2 progress — Slice 3 production host wiring

The third M2 slice composes the already-proven boundaries into the actual ASP.NET host without exposing an unauthenticated production chat/tool endpoint.

### Host composition

```text
Loren.Web DI
 -> OllamaBrain
 -> AgentLoop
 -> ActionGateway
 -> ReadOnlyActionPolicy
 -> GitHubReadRepositoryExecutor
 -> InMemoryAuditSink
```

`LorenRunService` owns the M2 host-level run composition and exposes only `github.read_repository` to the agent loop.

### HTTP/credential separation

Ollama and GitHub use separate named `HttpClient` instances. The deterministic production-component test proves:

```text
Ollama request
  Authorization: Bearer <test provider key>
        |
        v
ActionRequest(github.read_repository)
        |
        v
GitHub GET
  Authorization: <absent>
        |
        v
structured ActionResult
        |
        v
Ollama final response
```

The provider key remains absent from model-visible request JSON and is not propagated to the GitHub executor.

### Development-only host proof surface

The temporary M2 route `/internal/dev/run` is **not mapped by default**. It exists only when both conditions hold:

- `ASPNETCORE_ENVIRONMENT=Development`;
- `LOREN_ENABLE_DEVELOPMENT_RUN_ENDPOINT=true`.

If the flag is enabled outside Development, startup fails closed. Regular CI starts the normal host and explicitly verifies `/internal/dev/run` returns HTTP `404`.

Deterministic integration coverage now proves the production components can complete:

```text
user message
 -> production OllamaBrain (fake HTTP provider)
 -> ActionRequest
 -> production AgentLoop / ActionGateway / policy
 -> production GitHubReadRepositoryExecutor (fake HTTP GitHub)
 -> structured ActionResult
 -> production OllamaBrain final turn
 -> LorenRunResult + correlated audit
```

The next proof must replace the fake provider/GitHub HTTP with the real trusted Ollama provider and real public GitHub read while using this same host composition.

## Current milestone — M2 Walking Skeleton

Target end state:

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

Completed inside M2:

- [x] Action Gateway read path;
- [x] GitHub read-only executor returning structured repository state;
- [x] Loren-owned correlation/run/action IDs;
- [x] minimal append-oriented in-memory audit contract/path;
- [x] deterministic fake-brain integration test;
- [x] production Ollama `IBrain` adapter with deterministic provider-boundary tests;
- [x] production host/DI composition with deterministic end-to-end read-path test;
- [x] default host surface keeps the unauthenticated development run route disabled.

Still required before M2 exits:

- [ ] trusted live provider proof through the production M2 host/read path;
- [ ] one-owner authentication/session;
- [ ] minimal owner request UI/endpoint;
- [ ] owner-visible audit for the round trip;
- [ ] provider/GitHub credential isolation verified in the trusted live host run.

## Next execution sequence

```text
NOW
trusted live Ollama -> production host -> ActionGateway -> real GitHub read proof
    |
    v
one-owner auth/session + minimal owner UI
    |
    v
"Loren, check repo rua-den/loren."
    |
    v
FIRST OWNER-TESTABLE LOREN PREVIEW
    |
    v
M3 — Canonical state
```

## Progress-update rule

Any merge that changes capability, milestone completion, ADR status, validated dependencies/providers, or the next execution target must update:

1. `docs/status.md`;
2. `README.md`;
3. `README.vi.md`;
4. the relevant ADR/plan when a decision or milestone changes.

A milestone is not considered closed until implementation/tests and repository documentation agree.
