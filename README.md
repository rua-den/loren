# Loren

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
8. **Progressive autonomy** — Loren starts user-driven and gains background/proactive behavior only after the lower-level trust boundaries are proven.

## Current stage

**v0.0 — Architecture and feasibility. No production source code yet.**

Accepted architecture:

- Loren owns canonical identity/state/memory/policy/audit;
- brain providers, MCP, vendor APIs, UI, and future runtimes are adapters;
- privileged actions must pass the Loren Action Gateway;
- privileged tool credentials stay outside model/runtime context.

Current open gate:

- **ADR-002:** validate the proposed v0.1 stack (`.NET 10 + ASP.NET Core + thin Loren agent loop + OpenAI Responses brain + MCP C# adapter + SQLite/EF Core + Blazor`).

After ADR-002 is accepted, production implementation begins with the v0.1 walking skeleton.

## Version path

```text
v0.0  architecture / feasibility
v0.1  trustworthy core
v0.2  useful project assistant
v0.3  personal operations
v0.4  voice + device presence
v0.5  proactive/background Loren
v0.6+ real-use hardening
v1.0  stable personal daily driver
```

Versions advance by exit gates, not dates or code volume.

## Documentation

- [`docs/vision.md`](docs/vision.md) — product vision and target experience
- [`docs/architecture.md`](docs/architecture.md) — active system boundaries
- [`docs/plans/master-plan.md`](docs/plans/master-plan.md) — authoritative version milestones and transition gates
- [`docs/plans/v0.1.md`](docs/plans/v0.1.md) — detailed trustworthy-core implementation plan
- [`docs/roadmap.md`](docs/roadmap.md) — concise capability roadmap
- [`docs/research/agent-landscape.md`](docs/research/agent-landscape.md) — relevant existing projects and reuse opportunities
- [`docs/decisions/001-agent-runtime-strategy.md`](docs/decisions/001-agent-runtime-strategy.md) — accepted Loren-owned core/runtime boundary
- [`docs/decisions/002-v0.1-technology-stack.md`](docs/decisions/002-v0.1-technology-stack.md) — proposed v0.1 implementation stack and validation gate
- [`docs/memory.md`](docs/memory.md) — memory model
- [`docs/permissions.md`](docs/permissions.md) — permission model
- [`docs/security.md`](docs/security.md) — security baseline
- [`docs/skills.md`](docs/skills.md) — skill/tool model

## Repository role

This repository is the source of truth for Loren's product decisions, architecture, delivery plans, implementation, and release history.
