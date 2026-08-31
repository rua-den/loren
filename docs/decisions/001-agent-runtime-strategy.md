# ADR-001: Agent Runtime Strategy

- **Status:** Proposed
- **Date:** 2026-09-01
- **Decision owners:** Loren project

## Context

Loren requires a runtime capable of driving language-model turns, tool calls, task continuation, and eventually background or event-driven work. Existing projects already solve large portions of this problem.

The project must decide whether Loren should:

1. build directly on OpenClaw;
2. build directly on Letta;
3. own a small core and place one or more runtimes behind adapters;
4. build a fully custom runtime.

The wrong decision could either waste substantial engineering time rebuilding solved infrastructure or couple Loren's personal identity and memory too tightly to a framework that may later change.

## Decision criteria

Any selected strategy should be evaluated against these criteria, ordered roughly by importance:

1. **Personal-state ownership** — Loren's identity, memory, world model, permissions, and audit history remain exportable and under owner control.
2. **Security/permission hooks** — Loren can enforce policy outside the model before consequential tool execution.
3. **Model independence** — switching providers/models does not destroy agent continuity.
4. **Stateful continuity** — tasks and memory survive sessions and process restarts.
5. **Tool/skill extensibility** — GitHub and future domains can be added without core rewrites.
6. **Event/proactive support** — future schedules/webhooks/background work are practical.
7. **Self-hostability** — local/private deployment is possible for sensitive components.
8. **Observability** — runs and tool actions can be logged and inspected.
9. **Implementation speed** — v0.1 can become useful without months of infrastructure work.
10. **Replaceability** — framework-specific concepts do not contaminate Loren-owned state excessively.

## Options

### Option A — OpenClaw as Loren's primary runtime

Use OpenClaw for gateway, model execution, channels, skills/plugins, automation, and runtime lifecycle. Add Loren-owned services for memory/world-model/policy where necessary.

**Pros**

- likely fastest path to a broad personal assistant;
- avoids rebuilding gateway/channel/automation infrastructure;
- strong alignment with always-on assistant use cases.

**Cons**

- risk of architectural coupling;
- Loren may inherit OpenClaw's assumptions about memory and skills;
- replacing the runtime later may require significant adapter work.

### Option B — Letta as Loren's primary runtime

Use Letta for persistent agent identity, memory, sessions, and model execution. Build the rest of Loren around it.

**Pros**

- direct alignment with persistent/stateful agents;
- model-independent identity and memory are first-class concepts;
- promising foundation for long-term continuity.

**Cons**

- Loren may still need to build integration/gateway/event infrastructure;
- world-model and permission semantics may not map directly to Letta's memory abstractions;
- risk of allowing framework state to become Loren's canonical state.

### Option C — Loren-owned core with runtime adapters

Define Loren-owned domain/state/policy boundaries and implement one runtime adapter for v0.1. Additional runtimes may be added only when a real need appears.

**Pros**

- best protection of Loren's identity and durable personal state;
- creates a clean migration path if external runtimes change;
- leaves room for a hybrid architecture later.

**Cons**

- abstraction cost appears before much production experience exists;
- risk of designing an over-general interface;
- slower than adopting a runtime end-to-end.

### Option D — Fully custom runtime

Implement model loop, context management, tools, scheduling, channels, execution, and persistence in Loren itself.

**Pros**

- complete control;
- no third-party architectural constraints.

**Cons**

- highest engineering cost;
- duplicates mature open-source work;
- delays the actual differentiating personal-intelligence layer;
- increases security and reliability burden.

## Current leaning

**Option C is the architectural preference, but it is NOT accepted yet.**

The intended interpretation of Option C is deliberately narrow:

- Loren owns the durable personal domain model and policy boundaries.
- v0.1 should choose exactly **one** practical runtime path.
- the adapter should expose only capabilities Loren actually needs.
- no speculative multi-runtime framework should be built.

Option D is currently disfavored unless code-level evaluation finds that existing runtimes cannot satisfy Loren's security or state-ownership requirements.

## Required spike before decision

Perform implementation-level evaluation of OpenClaw and Letta.

For each candidate, answer:

### State and memory

- Where is canonical agent state stored?
- Can it be exported and restored?
- Can Loren supply or synchronize its own memory/world-model context?
- What happens when switching models?

### Tool execution

- How are tools registered and invoked?
- Can Loren intercept every consequential call before execution?
- Are approvals programmatically controllable?
- Can tool results remain structured?

### Runtime lifecycle

- Can sessions/tasks survive restart?
- How are background tasks represented?
- Are scheduler and event APIs suitable for future proactive behavior?

### Security

- How are credentials scoped?
- What is the sandbox model?
- How are remote devices authenticated?
- Can dangerous capabilities be disabled centrally?

### Integration

- Can Loren use GitHub as the first skill cleanly?
- How much framework-specific code leaks into Loren domain objects?
- Can the runtime operate self-hosted/local for sensitive workflows?

### Operational cost

- complexity to install and maintain;
- dependency footprint;
- upgrade/migration risk;
- testing story;
- likely cost to replace it later.

## Decision rule

Choose the candidate that gets Loren to a useful v0.1 fastest **without surrendering canonical ownership of identity, permissions, and personal state**.

If OpenClaw or Letta is sufficiently close, use it behind a minimal boundary. Build custom runtime components only where they materially protect Loren's product requirements.

## Consequences if Option C is accepted

- Loren must define a small internal runtime contract.
- Canonical identity/world-model/permissions/audit storage remains Loren-owned.
- The first runtime adapter becomes a concrete implementation, not part of durable domain state.
- Tests should be written against Loren's behavioral contracts rather than framework internals.
- Runtime replacement is possible but not promised to be zero-cost.

## Follow-up ADRs

Expected decisions after ADR-001:

- ADR-002: canonical memory/world-model storage
- ADR-003: skill/tool protocol and MCP strategy
- ADR-004: permission policy representation
- ADR-005: secrets and credential storage
- ADR-006: event/scheduler architecture
- ADR-007: owner authentication and trusted devices
