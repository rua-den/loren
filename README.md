# Loren

**English** · [Tiếng Việt](README.vi.md)

Loren is a long-lived personal intelligence system with persistent memory, explicit permissions, tool use, and eventually proactive behavior across the owner's digital life.

> **The model is replaceable compute. Loren owns identity, memory, context, policy, action boundaries, and history.**

## Core principles

1. **Memory-first** — durable state survives conversations, restarts, and provider changes.
2. **Tool-first** — external facts/actions come from authoritative tools instead of model guessing.
3. **Permission-first** — a model may request an action; Loren authorizes and executes it.
4. **Model-independent** — OpenAI, Ollama, Claude, local models, and future providers are adapters.
5. **Auditable** — consequential actions and important state changes must be reconstructable.
6. **Progressive autonomy** — proactive/background behavior comes only after lower-level trust boundaries are proven.

## Current status

**Last updated:** 2026-09-03  
**Phase:** `v0.0 — Architecture / Feasibility`  
**Gate:** `Gate B — v0.1 implementation stack`  
**Milestone:** `M0 — ADR-002 technical validation`

Gate A is **PASSED**. Loren owns canonical state and the action/security boundary.

Gate B is now being completed as a **provider-neutral brain proof**. OpenAI credential/provider reachability was proven, but model execution was blocked by `429 credit_balance_exhausted`. That vendor billing issue no longer defines the architecture gate.

PR #6 adds a native Ollama Cloud brain path and updates trusted validation to choose an available provider:

```text
OLLAMA_API_KEY present  -> Ollama
else OPENAI_API_KEY     -> OpenAI
else                    -> fail closed
```

Current M0 evidence:

| Area | Status |
| --- | --- |
| Loren-owned ActionGateway / bounded loop | ✅ PASS |
| OpenAI adapter compile + provider reachability | ✅ PASS |
| OpenAI behavioral proof | ⚠️ blocked by provider credit |
| Ollama brain spike compile | ✅ PASS |
| Ollama live tool round trip | ⏳ OPEN after PR #6 merge |
| Ollama live cancellation | ⏳ OPEN after PR #6 merge |
| MCP client + Loren gateway | ✅ PASS |
| SQLite + EF Core migration/recovery | ✅ PASS |
| ASP.NET Core + Blazor host | ✅ PASS |

Detailed status: [`docs/status.md`](docs/status.md).

## Brain architecture

```text
                Loren Core
                    │
                  IBrain
                    │
        ┌───────────┼───────────┐
        │           │           │
     Ollama       OpenAI      future
        │           │           │
        └──── ActionRequest ─────┘
                    │
             Loren ActionGateway
                    │
            Policy / Executor / Audit
```

Changing providers must not require migrating Loren's identity, memory, permissions, projects, or audit history.

## First owner-testable preview

The first meaningful owner-facing preview remains **v0.1 M2 — Walking Skeleton**:

```text
"Loren, check repo rua-den/loren."

UI
 -> Loren Runtime
 -> IBrain
 -> github.read_repository ActionRequest
 -> Action Gateway
 -> GitHub read executor
 -> structured result
 -> IBrain final response
 -> Audit
```

M1 establishes the engineering foundation. M2 is the first milestone intended to feel like actually using Loren.

## Proposed v0.1 stack

Pending final ADR-002 acceptance:

```text
C# 14 / .NET 10
ASP.NET Core
small Loren-owned agent loop
provider-neutral IBrain
Ollama and OpenAI adapters
MCP C# SDK behind Loren adapters
SQLite + EF Core
Blazor Web App
xUnit
```

## Version path

```text
v0.0  architecture / feasibility        <- current
v0.1  trustworthy core
v0.2  useful project assistant
v0.3  personal operations
v0.4  voice + device presence
v0.5  proactive/background Loren
v0.6+ real-use hardening
v1.0  stable personal daily driver
```

Versions advance by exit gates, not dates or code volume.

## Progress discipline

Any merge that changes capability, milestone completion, ADR status, validated providers/dependencies, or the next execution target must update:

- [`docs/status.md`](docs/status.md)
- `README.md`
- [`README.vi.md`](README.vi.md)
- the relevant ADR/plan when needed

A milestone is not closed until code/tests and repository documentation agree.

## Documentation

- [`docs/status.md`](docs/status.md) — authoritative current progress
- [`docs/vision.md`](docs/vision.md) — product vision
- [`docs/architecture.md`](docs/architecture.md) — active system boundaries
- [`docs/plans/master-plan.md`](docs/plans/master-plan.md) — version milestones and gates
- [`docs/plans/v0.1.md`](docs/plans/v0.1.md) — detailed v0.1 plan
- [`docs/decisions/001-agent-runtime-strategy.md`](docs/decisions/001-agent-runtime-strategy.md) — accepted Loren-owned core/runtime boundary
- [`docs/decisions/002-v0.1-technology-stack.md`](docs/decisions/002-v0.1-technology-stack.md) — proposed v0.1 stack and M0 evidence
- [`docs/memory.md`](docs/memory.md) — memory model
- [`docs/permissions.md`](docs/permissions.md) — permission model
- [`docs/security.md`](docs/security.md) — security baseline
- [`docs/skills.md`](docs/skills.md) — skill/tool model

## Repository role

This repository is the source of truth for Loren's product decisions, architecture, delivery plans, implementation, progress, and release history.
