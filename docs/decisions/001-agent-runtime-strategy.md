# ADR-001: Loren-Owned Core and Runtime Boundary

- **Status:** Accepted
- **Date:** 2026-09-03
- **Decision owners:** Loren project

## Context

Loren needs language-model reasoning, tool use, persistent state, and eventually background/event-driven execution. Existing projects such as OpenClaw and Letta solve meaningful parts of the generic agent-runtime problem, but Loren's durable identity, personal state, permission model, and audit history must not become framework-owned state.

The architectural decision is therefore not "which framework is Loren?". The decision is **what Loren owns, and what remains replaceable infrastructure**.

## Decision

Loren will use a **Loren-owned core with replaceable runtime/brain/tool adapters**.

Loren owns the durable and security-critical layer:

```text
Loren Core
├── identity
├── canonical personal/project state
├── memory and provenance
├── world-model relationships
├── permission/policy rules
├── action gateway
├── audit history
└── context assembly
```

Replaceable infrastructure sits behind interfaces:

```text
Brain providers
├── OpenAI
├── future cloud models
└── future local models

Tool/execution adapters
├── native APIs
├── MCP
├── future desktop/computer-use runtimes
└── future external agent runtimes where useful
```

The first v0.1 implementation may use a small Loren-owned agent loop rather than adopting a large external agent framework. That implementation choice is recorded separately in ADR-002.

## Architectural invariants

### 1. Model is compute, not identity

A model receives prepared Loren context and returns reasoning/output/action requests. Switching the model must not redefine Loren's identity or delete canonical state.

### 2. Runtime is not a security boundary

The runtime/model may **request** an action. It may not authorize itself.

```text
Model / Runtime
      |
      | ActionRequest
      v
Loren Action Gateway
      |
      +--> deny / approval
      |
      v
Controlled executor
```

### 3. Privileged credentials remain outside the reasoning loop

Write-capable credentials are resolved only inside controlled executors after policy evaluation. Raw secrets must not be placed in model context, runtime memory, or generic tool metadata.

### 4. Canonical state is Loren-owned

External runtime sessions, provider conversation IDs, model state, or framework memory may be cached as integration metadata, but they must not become the only copy or primary identity of important Loren data.

### 5. External protocols are adapters

MCP is a tool/context protocol, not Loren's brain and not Loren's canonical skill model. Loren may expose or consume MCP, but MCP SDK types must not leak into durable domain objects.

## Rejected alternatives

### OpenClaw as Loren itself

Rejected as the primary identity/state boundary. OpenClaw may still be reused later for channels, device presence, automation, or other infrastructure if a concrete need appears.

### Letta as Loren itself

Rejected as the canonical identity/memory boundary. Letta's stateful-agent and memory concepts remain useful references and may later be used behind an adapter if they solve a real problem better than Loren-native code.

### Fully generic custom agent framework

Rejected. Loren will only build the minimum orchestration required by real Loren flows. It will not attempt to become a general-purpose agent framework.

## Consequences

Positive:

- Loren's continuity survives model/runtime replacement;
- permission enforcement remains deterministic and inspectable;
- credential isolation is possible by construction;
- tests can exercise Loren core with a fake brain/runtime;
- external frameworks can be added selectively instead of defining the whole product.

Costs:

- Loren must define and maintain small internal contracts for brain, actions, runtime, and tool execution;
- some integration code will exist even when an external framework could provide end-to-end behavior;
- replaceability is architectural, not zero-cost migration.

## Required next decision

ADR-002 must choose and validate the concrete v0.1 implementation stack, including:

- language/runtime;
- initial brain provider and API;
- thin agent-loop implementation;
- MCP client strategy;
- canonical storage choice;
- initial web host/UI approach;
- package pinning and adapter boundaries.

## References

- `docs/architecture.md`
- `docs/plans/master-plan.md`
- `docs/plans/v0.1.md`
