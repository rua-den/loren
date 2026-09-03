# Loren

**English** · [Tiếng Việt](README.vi.md)

Loren is a long-lived personal intelligence system with persistent memory, explicit permissions, tool use, and eventually proactive behavior across the owner's digital life.

> **The model is replaceable compute. Loren owns identity, memory, context, policy, action boundaries, and history.**

## Core principles

1. **Memory-first** — durable state survives conversations, restarts, and provider changes.
2. **Tool-first** — external facts/actions come from authoritative tools instead of model guessing.
3. **Permission-first** — a model may request an action; Loren authorizes and executes it.
4. **Model-independent** — Ollama, OpenAI, Claude, local models, and future providers are adapters.
5. **Auditable** — consequential actions and important state changes must be reconstructable.
6. **Progressive autonomy** — proactive/background behavior comes only after lower-level trust boundaries are proven.

## Current status

**Last updated:** 2026-09-03  
**Phase:** `v0.1 — Trustworthy Core development`  
**Current milestone:** `M1 — Engineering foundation`

Both architecture gates required to start production are now passed:

- **Gate A / ADR-001:** Loren owns canonical identity/state/policy/action authorization.
- **Gate B / ADR-002:** the provider-neutral v0.1 stack is accepted and M0 is complete.

Trusted M0 run #70 proved the complete real brain path with Ollama Cloud (`gpt-oss:120b`):

```text
real model
 -> get_project_status ActionRequest
 -> Loren ActionGateway
 -> structured ActionResult
 -> real model final answer
 -> PASS
```

The same run proved live provider cancellation and completed MCP, SQLite/EF recovery, and ASP.NET/Blazor regressions successfully. Provider secrets remained masked.

Ollama is the **first provider that closed the brain proof**, not Loren's permanent identity. OpenAI remains an optional adapter; its currently configured API account is blocked by provider credit, which no longer blocks Loren's architecture.

Detailed status: [`docs/status.md`](docs/status.md).

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

## Current work — M1

M1 builds the production engineering foundation:

- production .NET solution/project boundaries;
- pinned SDK/packages;
- deterministic xUnit tests;
- CI restore/build/test/static checks;
- nullable/warnings/analyzers/formatting policy;
- `.env.example` and local setup docs;
- secret/dependency scanning;
- startup/health test;
- `Loren.Core` remains free of provider/MCP/EF/Blazor dependencies.

The `spikes/` directory remains technical evidence, not production architecture.

## First owner-testable preview

The first meaningful owner-facing preview is still **v0.1 M2 — Walking Skeleton**:

```text
"Loren, check repo rua-den/loren."

UI
 -> Loren Runtime
 -> configured IBrain
 -> github.read_repository ActionRequest
 -> Action Gateway
 -> GitHub read executor
 -> structured result
 -> IBrain final response
 -> Audit
```

M1 is engineering foundation. M2 is the first milestone intended to feel like actually using Loren.

## Version path

```text
v0.0  architecture / feasibility        ✓ complete
v0.1  trustworthy core                 <- current development
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
- [`docs/plans/v0.1.md`](docs/plans/v0.1.md) — detailed v0.1 implementation plan
- [`docs/decisions/001-agent-runtime-strategy.md`](docs/decisions/001-agent-runtime-strategy.md) — accepted Loren-owned core/runtime boundary
- [`docs/decisions/002-v0.1-technology-stack.md`](docs/decisions/002-v0.1-technology-stack.md) — accepted provider-neutral v0.1 stack and M0 evidence
- [`docs/memory.md`](docs/memory.md) — memory model
- [`docs/permissions.md`](docs/permissions.md) — permission model
- [`docs/security.md`](docs/security.md) — security baseline
- [`docs/skills.md`](docs/skills.md) — skill/tool model

## Repository role

This repository is the source of truth for Loren's product decisions, architecture, delivery plans, implementation, progress, and release history.
