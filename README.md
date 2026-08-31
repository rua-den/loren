# Loren

Loren is a personal intelligence system: a long-lived assistant with persistent memory, explicit permissions, tool use, and eventually proactive behavior across the owner's digital life.

Loren is not intended to be just another chat UI or an agent-framework clone. The project should own the parts that make Loren *Loren*—identity, memory, personal world model, decisions, permissions, preferences, projects, and experience—while reusing mature infrastructure when that is the better engineering choice.

## Product principles

1. **Personal, not generic** — Loren should become more useful as it learns the owner's stable preferences, projects, people, devices, and decisions.
2. **Memory-first** — important state must survive conversations, model changes, restarts, and infrastructure changes.
3. **Tool-first** — use authoritative tools and APIs for facts and actions instead of guessing through the language model.
4. **Permission-first** — every action has an explicit risk level and approval policy.
5. **Model-independent** — model providers are replaceable reasoning engines, not Loren's identity.
6. **Auditable** — actions, approvals, important memory changes, and automation runs should be inspectable.
7. **Local ownership where practical** — personal state and secrets should remain under the owner's control whenever possible.
8. **Progressive autonomy** — v0.1 is user-driven; later versions may become proactive only after the safety and observability foundations are proven.

## Current phase

**Phase 0: architecture and product planning. No production source code yet.**

The immediate goal is to decide what Loren should own versus what should be delegated to existing agent/runtime projects such as OpenClaw, Letta, Open Interpreter, and Home Assistant.

## Documentation

- [`docs/vision.md`](docs/vision.md) — product vision and target experience
- [`docs/architecture.md`](docs/architecture.md) — proposed system boundaries and major components
- [`docs/research/agent-landscape.md`](docs/research/agent-landscape.md) — existing projects and what Loren can reuse
- [`docs/decisions/001-agent-runtime-strategy.md`](docs/decisions/001-agent-runtime-strategy.md) — first architecture decision under evaluation
- [`docs/plans/v0.1.md`](docs/plans/v0.1.md) — proposed v0.1 scope and implementation gates
- [`docs/roadmap.md`](docs/roadmap.md) — staged path toward an always-on personal agent

## Working definition

> **Loren is a personal intelligence layer that remembers, reasons, and acts within explicit permissions, while remaining independent of any single model provider or execution runtime.**

## Status

This repository is the source of truth for Loren's product decisions, architecture, implementation plans, and eventually its source code.
