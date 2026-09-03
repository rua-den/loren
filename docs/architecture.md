# Loren Architecture

**Status:** Active baseline. Core ownership is accepted in ADR-001; the v0.1 provider-neutral technology stack is accepted in ADR-002.

## Architectural objective

Loren owns stable personal state, context, policy, and action authorization while treating language models, MCP, vendor APIs, UI clients, and execution runtimes as replaceable infrastructure.

The key distinction is:

> **The model is the reasoning brain. Loren is the stateful system that gives that brain identity, memory, context, tools, and boundaries.**

## Top-level architecture

```text
                        Interfaces
             Web / Mobile / Voice / Messaging
                           |
                           v
                    +-------------+
                    | Loren Host  |
                    +------+------+ 
                           |
                           v
                 +-------------------+
                 |   Loren Runtime   |
                 |-------------------|
                 | context assembly  |
                 | bounded agent loop|
                 | task/run state    |
                 +----+---------+----+
                      |         |
                      |         v
                      |     +--------+
                      |     | IBrain |
                      |     +---+----+
                      |         |
                      |   Ollama / OpenAI /
                      |   future cloud/local
                      |
                      v
                ActionRequest
                      |
                      v
               +--------------+
               |Action Gateway|
               +--+-------+---+
                  |       |
             policy/      | audit
             approval     |
                  |       |
                  v       v
             +----------------+
             | Controlled     |
             | Executors      |
             +-------+--------+
                     |
              secret resolution
                     |
        +------------+-------------+
        |            |             |
        v            v             v
      GitHub        MCP         direct APIs
                                 / systems

         +--------------------------------+
         |       Loren-owned state        |
         |--------------------------------|
         | Identity                       |
         | Projects / world model         |
         | Memory + provenance            |
         | Permission rules               |
         | Audit history                  |
         | Integration metadata           |
         +--------------------------------+
```

## Boundary 1 — Loren-owned state

These concepts must remain usable even if the brain provider, MCP implementation, UI, or external runtime is replaced.

### Identity

Stable Loren behavior, operating rules, owner relationship, communication preferences, and policy defaults.

### Personal world model

Structured anchors for the owner's world. v0.1 intentionally starts small:

```text
Owner
Project
Repository
```

Later versions may add entities such as Person, Device, Place, Service, Environment, Decision, Procedure, Event, or Task only when real workflows justify them.

Example:

```text
Project:wedding-online
  alias -> "web đám cưới"
  repository -> GitHub:rua-den/wedding-online
```

The world model gives Loren stable referents for phrases such as "that project" or "the wedding site". It does not replace memory.

### Memory

Durable knowledge and experience with provenance, trust/source classes, correction, and forgetting. See `memory.md` and the v0.1 plan.

### Permissions

Policies describing which actions are allowed, denied, or approval-gated. Authorization is deterministic application behavior, not an LLM decision. See `permissions.md`.

### Audit history

Append-oriented records of important runs, action requests, policy decisions, approvals, executions, verifications, failures, and memory mutations.

## Boundary 2 — Brain

A brain is a replaceable reasoning provider behind Loren's `IBrain` boundary.

Conceptual contract:

```text
IBrain.Think(context, available_actions) -> BrainTurnResult
```

A brain may:

- interpret user intent;
- reason over supplied context;
- select/request an action;
- use structured tool results;
- produce a final response.

A brain may **not**:

- authorize itself;
- receive raw privileged tool credentials as ordinary context;
- directly mutate Loren canonical state outside controlled services;
- define Loren's durable identity.

v0.1 supports provider adapters rather than one privileged provider. The first real M0 behavior proof passed with native Ollama Cloud (`gpt-oss:120b`), while the OpenAI adapter remains optional. Future adapters may use other cloud or local models without changing Loren-owned state or action contracts.

## Boundary 3 — Loren runtime

The runtime is deliberately small. It coordinates a turn/task but does not own Loren's durable identity.

v0.1 responsibilities:

- build the brain context;
- call configured `IBrain`;
- receive final output or structured action requests;
- route action requests to the Action Gateway;
- append structured action results back to the brain context;
- stop on final output, cancellation, error, or hard loop limit;
- maintain correlation/run IDs.

Conceptual loop:

```text
prepare context
-> brain turn
-> final answer OR ActionRequest
-> Action Gateway
-> ActionResult
-> brain turn
-> ... bounded ...
```

Do not build generic workflow/orchestration features until Loren has a concrete use for them.

## Boundary 4 — Action Gateway

The Action Gateway is the security-critical boundary between reasoning and side effects.

Flow:

```text
ActionRequest
    |
    v
schema validation
    |
    v
policy evaluation
    |
    +--> deny
    |
    +--> request owner approval
    |
    v
controlled executor
    |
    v
postcondition verification
    |
    v
ActionResult + audit
```

No privileged integration may have an alternate route that bypasses this gateway.

## Boundary 5 — Skills, tools, MCP, and APIs

Loren has an internal action model independent of any one tool protocol.

Each action should describe at least:

- stable action name;
- human-readable description;
- typed input/output contract;
- external target/resource;
- side-effect characteristics;
- policy metadata;
- verification strategy when consequential.

Execution may come from:

```text
Loren-native adapter
direct vendor API
MCP client/server
future desktop/computer-use adapter
future external runtime/service
```

### MCP rule

MCP is an integration protocol, not Loren's brain and not Loren's authorization model.

MCP tool definitions/results should be normalized into Loren action contracts. Provider-managed remote MCP must not be used for privileged actions if doing so would bypass Loren's Action Gateway or credential boundary.

## Boundary 6 — Credentials and secrets

Canonical memory stores opaque credential references only.

Preferred execution flow:

```text
brain/runtime
    |
ActionRequest
    |
Action Gateway
    |
authorized executor
    |
Secret Resolver
    |
external system
```

Write-capable secrets should be readable only by the narrow executor boundary that needs them.

Provider credentials are also adapter configuration, not canonical Loren memory. M0 demonstrated that provider keys can stay masked/outside action payloads while live tool calling still works.

## Boundary 7 — Events and proactivity

v0.1 is user-driven. Later versions may ingest events such as:

```text
schedule
GitHub webhook
calendar
email
server monitoring
Home Assistant
manual task
```

Events eventually normalize into an internal envelope, but an event never grants authorization by itself.

```text
Event {
  id
  source
  type
  occurred_at
  entity_refs
  payload_ref
  sensitivity
}
```

Background/proactive execution requires the dedicated gates in `docs/plans/master-plan.md`.

## Boundary 8 — Interfaces

Interfaces are thin clients over the same Loren core.

Expected progression:

1. v0.1 web interface;
2. mobile-friendly/PWA;
3. optional messaging channels;
4. push-to-talk voice;
5. device nodes / ambient interfaces.

Switching interface must not create separate memory, policy, or identity systems.

## Data ownership rules

1. Durable Loren state has a canonical owner-controlled store.
2. Provider/runtime session state is integration metadata/cache, not canonical personal state.
3. Secrets are referenced, not embedded in memories or prompts.
4. Sensitive tool outputs need retention/redaction rules.
5. Canonical state must have a versioned export/restore path.
6. External/provider-specific identifiers never become Loren primary identity.

## Reliability principles

### Bounded loops

Every agent run has hard cancellation/tool-call/turn/runtime limits.

### Idempotency

Consequential actions should use operation/idempotency identifiers where practical so retry does not duplicate effects.

### Check before act

Fetch current mutable state before important writes when stale context could make the action unsafe.

### Verify after act

Consequential writes must confirm explicit postconditions instead of assuming success.

### Fail closed

If Loren cannot determine authorization, it does not execute the action.

### Recoverable state

A model/provider/runtime failure must not destroy canonical Loren state. Export/wipe/restore is a v0.1 release requirement.

## Current decisions

- **ADR-001 — Accepted:** Loren-owned core with replaceable brain/runtime/tool adapters.
- **ADR-002 — Accepted:** .NET 10 / ASP.NET Core / thin Loren agent loop / provider-neutral `IBrain` / MCP C# adapter / SQLite+EF Core / Blazor / xUnit for v0.1.
- **M0 brain proof:** Ollama Cloud native `/api/chat`, `gpt-oss:120b`, live ActionGateway round trip and cancellation passed.

## Remaining major decisions

Resolve them only when they are about to become expensive to reverse:

- exact credential store/auth method for GitHub writes;
- canonical schema/deletion/export rules before memory/write workflows stabilize;
- long-term canonical storage if SQLite stops being sufficient;
- background scheduler/event model before private proactive work;
- trusted-device model before mobile/voice;
- standing-permission model before proactive autonomy;
- additional provider routing when real usage justifies it.

See `docs/plans/master-plan.md` for the version gates that control these decisions.
