# Loren

**English** · [Tiếng Việt](README.vi.md)

Loren is a personal intelligence system: a long-lived assistant with persistent memory, explicit permissions, tool use, and eventually proactive behavior across the owner's digital life.

Loren is not intended to be just another chat UI or an agent-framework clone. The project owns the parts that make Loren *Loren*—identity, memory, personal world model, permissions, project context, action boundaries, and experience—while treating models and execution infrastructure as replaceable components.

## Working definition

> **Loren is a stateful personal intelligence system. The model is its reasoning brain; Loren owns the identity, memory, context, policy, action boundary, and history around that brain.**

## Product principles

1. **Personal, not generic** — Loren should become more useful as it learns the owner's stable preferences, projects, people, devices, and decisions.
2. **Memory-first** — important state must survive conversations, model changes, restarts, and infrastructure changes.
3. **Tool-first** — use authoritative tools and APIs for facts and actions instead of guessing through the language model.
4. **Permission-first** — the model may request an action; Loren authorizes and executes it through deterministic policy.
5. **Model-independent** — model providers are replaceable reasoning engines, not Loren's identity.
6. **Auditable** — actions, approvals, important memory changes, and background work should be reconstructable.
7. **Local ownership where practical** — personal state and secrets should remain under the owner's control whenever possible.
8. **Progressive autonomy** — Loren starts user-driven and gains background/proactive behavior only after lower-level trust boundaries are proven.

## Current status

**Last updated: 2026-09-03**  
**Phase:** `v0.0 — Architecture / Feasibility`  
**Current gate:** `Gate B — v0.1 implementation stack`  
**Current milestone:** `M0 — ADR-002 technical validation`

Gate A is complete: ADR-001 establishes a Loren-owned core with models/runtimes/MCP as replaceable adapters.

Gate B has completed all no-secret implementation work. Current M0 evidence:

| Area | Status |
| --- | --- |
| OpenAI brain-loop compile boundary | ✅ PASS |
| Live OpenAI proof automation | ✅ PASS |
| Trusted connector-safe live trigger | ✅ PASS / validating |
| Live OpenAI Responses round trip | ⏳ OPEN |
| Live provider cancellation execution | ⏳ OPEN |
| MCP client + Loren gateway | ✅ PASS |
| SQLite + EF Core migration/recovery | ✅ PASS |
| ASP.NET Core + Blazor host | ✅ PASS |

The final Gate B evidence is one trusted secret-backed OpenAI run. Manual `workflow_dispatch` from `main` remains supported. Because the connected GitHub tooling does not expose workflow dispatch, Loren also has a one-shot branch trigger: `spike/adr-002-live-proof-run`. Before any secret-backed step runs, CI fetches `main` and requires the live branch SHA to equal the current `main` SHA exactly.

The live run proves both:

```text
normal path:
OpenAI brain
  -> ActionRequest
  -> Loren ActionGateway
  -> structured fake result
  -> OpenAI brain
  -> final response
  -> PASS

cancellation path:
OpenAI provider call
  -> Loren cancellation token
  -> provider call cancelled
  -> PASS
```

ADR-002 remains **Proposed** until both live checks pass. Production v0.1 scaffolding starts only after the gate is accepted.

For the detailed, authoritative progress ledger, see [`docs/status.md`](docs/status.md).

## First testable Loren preview

The first owner-facing preview is planned for **v0.1 M2 — Walking Skeleton**:

```text
"Loren, check repo rua-den/loren."

UI
 -> Loren Runtime
 -> Brain
 -> github.read_repository ActionRequest
 -> Action Gateway
 -> GitHub read executor
 -> structured result
 -> Brain final response
 -> Audit
```

M1 will establish the engineering foundation; M2 is the first milestone intended to feel like actually using Loren.

## Proposed v0.1 stack

Pending final ADR-002 acceptance:

```text
C# 14 / .NET 10
ASP.NET Core
small Loren-owned agent loop
OpenAI Responses API as first brain
MCP C# SDK behind Loren adapters
SQLite + EF Core
Blazor Web App
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

Repository progress must stay synchronized with implementation.

Any merge that changes capability, milestone completion, ADR status, validated dependency versions, or the next execution target must update:

- [`docs/status.md`](docs/status.md) — authoritative detailed status;
- `README.md` — English summary;
- [`README.vi.md`](README.vi.md) — Vietnamese summary;
- the relevant ADR/plan when a decision or milestone changes.

A milestone is not considered fully closed until code/tests and repository documentation agree.

## Documentation

- [`docs/status.md`](docs/status.md) — authoritative current progress and next target
- [`docs/vision.md`](docs/vision.md) — product vision and target experience
- [`docs/architecture.md`](docs/architecture.md) — active system boundaries
- [`docs/plans/master-plan.md`](docs/plans/master-plan.md) — authoritative version milestones and transition gates
- [`docs/plans/v0.1.md`](docs/plans/v0.1.md) — detailed trustworthy-core implementation plan
- [`docs/roadmap.md`](docs/roadmap.md) — concise capability roadmap
- [`docs/research/agent-landscape.md`](docs/research/agent-landscape.md) — relevant existing projects and reuse opportunities
- [`docs/decisions/001-agent-runtime-strategy.md`](docs/decisions/001-agent-runtime-strategy.md) — accepted Loren-owned core/runtime boundary
- [`docs/decisions/002-v0.1-technology-stack.md`](docs/decisions/002-v0.1-technology-stack.md) — proposed v0.1 implementation stack and validation evidence
- [`docs/memory.md`](docs/memory.md) — memory model
- [`docs/permissions.md`](docs/permissions.md) — permission model
- [`docs/security.md`](docs/security.md) — security baseline
- [`docs/skills.md`](docs/skills.md) — skill/tool model

## Repository role

This repository is the source of truth for Loren's product decisions, architecture, delivery plans, implementation, current progress, and release history.
