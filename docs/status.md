# Loren Project Status

**Last updated:** 2026-09-03  
**Current version phase:** `v0.0 — Architecture / Feasibility`  
**Current decision gate:** `Gate B — v0.1 implementation stack`  
**Gate B status:** `OPEN — provider-neutral live brain proof in progress`  
**Current milestone:** `M0 — ADR-002 technical validation`

This file is the authoritative progress ledger for the repository. `README.md` and `README.vi.md` summarize it.

## Current status

Gate A is already **PASSED** through ADR-001: Loren owns canonical identity, state, memory, policy, action authorization, context assembly, and audit. Models and model vendors are replaceable compute adapters.

M0 originally used OpenAI as the first live brain candidate. The credential and provider path was proven, but the OpenAI request was blocked before model execution by `429 insufficient_quota: credit_balance_exhausted`.

That operational billing failure exposed an important architecture point: **Gate B must validate the Loren-owned `IBrain` boundary, not require one specific vendor.** PR #6 therefore adds an independent Ollama Cloud brain spike and changes trusted live validation to select an available provider credential.

Current trusted-provider preference for M0 validation:

```text
OLLAMA_API_KEY present  -> validate Ollama Cloud
else OPENAI_API_KEY     -> validate OpenAI
else                    -> fail closed
```

Ordinary PR CI remains secret-free.

## Proposed v0.1 stack

```text
C# 14 / .NET 10
ASP.NET Core
small Loren-owned agent loop
provider-neutral IBrain boundary
  ├─ Ollama adapter / cloud or local models
  └─ OpenAI adapter
MCP C# SDK behind Loren adapter
SQLite + EF Core
Blazor Web App
xUnit
```

A provider is never Loren's identity or security boundary.

## M0 validation matrix

| Proof | Status | Evidence |
| --- | --- | --- |
| Loren-owned brain/action boundary | PASS | provider action requests are intercepted before execution; hard six-turn bound exists |
| OpenAI adapter compile boundary | PASS | OpenAI .NET 2.12.0 builds on .NET 10; provider SDK types remain in spike/adapter code |
| OpenAI credential/provider reachability | PASS | trusted run received masked secret and reached OpenAI Responses API |
| OpenAI behavioral proof | BLOCKED / OPTIONAL | provider returned `429 credit_balance_exhausted` before model execution; no longer the only path to Gate B |
| Ollama native brain spike compile | PASS | PR #6 builds native `/api/chat` adapter on .NET 10 with no vendor SDK dependency |
| Ollama live tool round trip | OPEN | run after PR #6 merges and trusted branch is synchronized to exact `main` |
| Ollama live cancellation | OPEN | explicit cancel-after mode must terminate at the live provider boundary |
| Trusted live trigger boundary | PASS | secret-backed run only from `main` manual dispatch or exact-main one-shot branch |
| MCP client/gateway | PASS | MCP 2.2.0; pinned reference server; allow-listed read-only action passed in CI |
| SQLite + EF Core | PASS | migration -> persist -> export -> wipe -> migrate -> restore -> reload passed |
| ASP.NET Core + Blazor host | PASS | host boot, health, Blazor render, fake brain DI, cancellation/logging smoke tests passed |

## Provider-neutral brain invariant

The M0 brain proof is now:

```text
real provider
    -> structured ActionRequest
    -> Loren ActionGateway
    -> fake/read-only executor
    -> structured ActionResult
    -> real provider
    -> final answer
```

It must also prove:

- cancellation reaches the provider call;
- hard turn limits remain Loren-owned;
- provider credentials do not enter model-visible action arguments or logs;
- the spike cannot report PASS unless an action actually crossed the Loren gateway.

Passing with Ollama does **not** remove OpenAI support. It proves the architecture can change brain vendors without changing Loren's identity, memory, permissions, or action boundary.

## Completed evidence

### PR #1 — OpenAI compile/tool boundary

Merged. Established the .NET 10 Responses function-calling compile path, fake ActionGateway, mandatory gateway crossing, and hard turn limit.

### PR #2 — MCP, persistence, host

Merged. Established real MCP read-only execution, SQLite/EF migration + recovery, and ASP.NET/Blazor host smoke tests.

### PR #3 — async/cancellation plumbing

Merged. Established async provider calls, shared cancellation token, Ctrl+C/wall-clock bounds, and secret isolation from PR CI.

### PR #4 — live-proof automation

Merged. Added normal live tool round trip plus explicit provider-await cancellation mode.

### PR #5 — trusted connector trigger

Merged. Added the exact-main SHA guard before any repository secret can reach a live validation step.

### OpenAI trusted live evidence

The repository secret was configured successfully and masked in logs. The request reached OpenAI, then failed before model execution with:

```text
HTTP 429
insufficient_quota: credit_balance_exhausted
```

This remains useful evidence for the OpenAI adapter, but it no longer blocks validation of Loren's provider-independent architecture.

### PR #6 — provider-neutral/Ollama validation

**In validation before merge.**

Current no-secret CI evidence:

```text
OpenAI spike build              PASS
Ollama spike build              PASS
trusted-trigger logic           PASS
MCP regression                  PASS
SQLite/EF recovery regression   PASS
ASP.NET/Blazor regression       PASS
secret-backed provider steps    SKIPPED in PR by design
```

The Ollama implementation uses the native cloud endpoint `https://ollama.com/api/chat`, sends the credential only as an Authorization bearer token, exposes `get_project_status` as a tool, routes tool calls through Loren code, returns a structured tool message, and requires a final model response before PASS.

## Current next action

```text
PR #6 self-review + merge
    |
    v
fast-forward spike/adr-002-live-proof-run to exact main HEAD
    |
    v
trusted provider selector chooses Ollama via OLLAMA_API_KEY
    |
    +-- Proof A: Ollama -> ActionGateway -> result -> final answer
    +-- Proof B: provider cancellation
    |
    v
if both PASS
    -> Accept ADR-002
    -> close Gate B / M0
    -> enter v0.1 development / M1
```

If Ollama itself fails for a provider-specific reason, fix or replace only the Ollama adapter/model choice. Do not reopen ADR-001.

## First owner-testable milestone

The first meaningful owner preview remains **v0.1 M2 — Walking Skeleton**:

```text
Owner: "Loren, check repo rua-den/loren."

UI
 -> Loren Runtime
 -> IBrain (configured provider)
 -> github.read_repository ActionRequest
 -> Loren ActionGateway
 -> GitHub read executor
 -> structured ActionResult
 -> IBrain final response
 -> Audit
```

M1 is engineering foundation. M2 is the first milestone intended to feel like actually using Loren.

## Progress-update rule

Any merge that changes capability, milestone completion, ADR status, validated dependencies/providers, or next execution target must update:

1. `docs/status.md`;
2. `README.md`;
3. `README.vi.md`;
4. the relevant ADR/plan when a decision or milestone changes.

A milestone is not considered closed until implementation/tests and repository documentation agree.
