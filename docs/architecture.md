# Loren Architecture

**Status:** Proposed baseline for planning. Runtime choice remains unresolved in ADR-001.

## Architectural objective

Loren should own stable personal state and policy while treating language models, agent runtimes, communication channels, and execution environments as replaceable infrastructure.

```text
                       Interfaces
          Web / Mobile / Voice / Messaging
                         |
                         v
                  +--------------+
                  |  Loren API   |
                  +------+-------+
                         |
                         v
              +---------------------+
              | Personal Agent Core |
              |---------------------|
              | Context assembly    |
              | Intent/task state   |
              | Planning/orchestration
              | Tool routing        |
              +----+----------+-----+
                   |          |
          +--------+          +----------------+
          v                                    v
 +------------------+                  +------------------+
 | Loren-owned state|                  | Runtime adapters |
 |------------------|                  |------------------|
 | Identity         |                  | Model providers  |
 | Memory           |                  | OpenClaw?        |
 | World model      |                  | Letta?           |
 | Projects         |                  | Custom runtime?  |
 | Permissions      |                  +---------+--------+
 | Audit history    |                            |
 +--------+---------+                            v
          |                              +---------------+
          |                              | Skills / Tools|
          |                              +-------+-------+
          |                                      |
          +------------------+-------------------+
                             v
       GitHub / Web / Gmail / Calendar / Server / Files /
       Home Assistant / Browser / Desktop / future systems
```

## Boundary 1: Loren-owned state

These concepts must remain portable even if every external runtime is replaced.

### Identity

Stable system behavior, operating rules, name, communication preferences, and policy defaults.

### Personal world model

Structured entities and relationships such as:

```text
Person
Project
Device
Place
Service
CredentialReference
Decision
Preference
Event
Task
```

Example relationship:

```text
Project:wedding-online
  repository -> GitHub:rua-den/wedding-online
  environment -> production VPS
  related_people -> [...]
  decisions -> [...]
```

The world model is not intended to replace raw memory. It gives Loren stable anchors for resolving phrases such as "that project", "my server", or "the wedding site".

### Memory

Durable knowledge and episodic experience. See `memory.md`.

### Permissions

Policies describing what Loren may read, draft, write, execute, publish, delete, or spend. See `permissions.md`.

### Audit history

Append-oriented records of significant actions, approvals, failures, memory mutations, automation runs, and external side effects.

## Boundary 2: Agent/runtime layer

This layer is responsible for executing an agent turn or task. Its implementation is intentionally undecided.

Candidate responsibilities:

- model invocation and provider routing;
- context-window management;
- tool-call loop;
- task continuation;
- background/scheduled execution;
- sandbox or execution host integration;
- streaming responses;
- retries and provider failover.

Possible implementations include OpenClaw, Letta, a custom runtime, or a hybrid. Loren should expose an internal interface that prevents runtime-specific types from leaking into durable state.

Conceptual interface:

```text
AgentRuntime.run(task, context, tools, policy) -> RunResult
AgentRuntime.resume(run_id, input) -> RunResult
AgentRuntime.schedule(task, trigger) -> ScheduledHandle
```

This is conceptual only; no language or framework is chosen yet.

## Boundary 3: Skills and tools

A skill packages one capability domain and describes:

- actions it exposes;
- required credentials;
- input/output schemas;
- read/write/destructive classification;
- approval defaults;
- audit metadata;
- optional event subscriptions.

Example:

```text
GitHub skill
  inspect_repository      read
  search_code             read
  create_branch           safe-write
  push_commit             external-write
  merge_pull_request      privileged-write
  delete_repository       destructive
```

Skills should return structured results rather than prose whenever practical. The reasoning layer can then summarize results for the owner.

## Boundary 4: Events and proactivity

Loren v0.1 is primarily user-driven, but the architecture must not prevent future event-driven operation.

Potential event sources:

```text
cron/schedule
GitHub webhook
calendar
email
server monitoring
Home Assistant
manual task
```

All events should normalize to a small internal envelope such as:

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

An event does **not** automatically authorize an action. It merely wakes evaluation. Permission policy remains authoritative.

## Boundary 5: Interfaces

Interfaces should be thin clients over the same Loren core.

Expected evolution:

1. web chat / development console;
2. installable PWA or mobile client;
3. messaging channels;
4. voice push-to-talk;
5. wake word / ambient interfaces.

Voice must not own business logic. Switching from text to voice should not change Loren's memory, permissions, or tools.

## Data ownership rules

1. Durable Loren state must have a canonical store controlled by the project owner.
2. External runtime memory may be used as a cache or execution aid, but must not silently become the only copy of important personal state.
3. Secrets must be referenced, not embedded in memories or prompts.
4. Sensitive tool outputs should have retention and redaction policies.
5. Export should be possible without dependence on one model vendor.

## Reliability principles

### Idempotency

Actions with side effects should carry operation IDs where supported so retries do not duplicate emails, deployments, payments, or other writes.

### Check before act

For mutable external systems, Loren should fetch current state immediately before important writes when stale context could be dangerous.

### Verify after act

Important operations should have explicit postconditions: deployment health checks, commit SHA confirmation, calendar event retrieval, etc.

### Bounded autonomy

Agent loops need hard limits for tool calls, time, spend, and recursive task creation.

### Fail closed for privilege escalation

If Loren cannot determine whether an action is authorized, it must not perform the action.

## Architecture questions still open

- Which agent runtime should power v0.1?
- What database or storage split best fits structured world state versus semantic/episodic memory?
- Which skill protocol should be native: custom schemas, MCP-compatible tools, OpenClaw skills, or adapters across them?
- How should authentication work across owner devices?
- Which tasks deserve isolated execution sandboxes?
- How should proactive events be prioritized to avoid notification spam?

These questions should be resolved through ADRs before their implementation becomes difficult to reverse.
