# Agent Landscape Research

**Snapshot date:** 2026-09-01

This document records the projects most relevant to Loren's architecture. It is not intended to be a permanent ranking; capabilities will change and should be re-validated before major implementation decisions.

## Research question

If Loren aims to become a persistent personal assistant with memory, tools, permissions, automation, voice, and eventually proactive behavior, which existing systems already solve meaningful parts of that problem?

## Summary

The market is converging toward the same broad direction as Loren: stateful agents, tool use, replaceable model providers, skills/integrations, local execution, approvals, and long-lived processes.

Therefore Loren should **not** begin by rebuilding generic agent infrastructure. Its differentiated layer should be the owner's persistent personal intelligence: identity, world model, memory policy, permissions, project context, action history, and proactive prioritization.

## OpenClaw

Official site: https://openclaw.ai/

### Relevant strengths

OpenClaw positions itself as an open-source assistant that runs on the user's machine, works through existing chat channels, and can act on systems such as inbox and calendar. Its current product direction explicitly emphasizes the user's memories, skills, models, machines, and data.

This makes OpenClaw the closest candidate for an **always-on personal-assistant runtime/integration layer**.

Potentially reusable areas:

- long-lived gateway/runtime behavior;
- chat/channel connectivity;
- model/provider routing;
- skills/plugins;
- automation/scheduling;
- device/runtime integration;
- operational infrastructure that Loren should avoid reinventing without reason.

### Loren-specific concern

Loren should not allow an external runtime's memory representation to become the canonical personal data model by accident. Even if OpenClaw powers execution, Loren-owned identity, world model, permissions, and durable memory must remain exportable and independently understandable.

### Current hypothesis

Strong candidate for runtime/integration infrastructure, but requires deeper code-level evaluation before adoption.

## Letta

Official SDK: https://www.letta.com/agent-sdk/

Announcement: https://www.letta.com/blog/introducing-the-letta-agent-sdk/

### Relevant strengths

Letta's current Agents SDK is explicitly designed for stateful, persistent agents whose identity, experience, and memory survive across sessions, processes, machines, and model choices.

Its architecture is particularly relevant to Loren because memory is treated as part of the agent rather than merely application chat history. Current Letta material also describes memory as a versioned repository owned by the agent.

Potentially reusable areas:

- stateful agent lifecycle;
- persistent memory management;
- model-independent agent identity;
- session/resume semantics;
- long-running agent abstractions;
- memory versioning and consolidation ideas.

### Loren-specific concern

Loren's personal world model and permission policy are broader than agent memory alone. If Letta is adopted, it should likely be a cognition/memory runtime behind a Loren-owned boundary rather than the sole definition of Loren's durable state.

### Current hypothesis

Strong candidate for the **stateful cognition/memory runtime** or a source of architectural patterns.

## Open Interpreter

Official desktop docs: https://www.openinterpreter.com/docs/desktop

### Relevant strengths

Open Interpreter focuses on real computer work: operating apps, files, browser workflows, documents, and desktop interfaces. Current desktop behavior includes scoped workspaces and pauses for review before consequential operations such as submit/send/delete/overwrite.

Potentially reusable areas:

- computer-use execution;
- local workspace boundaries;
- desktop automation;
- sandbox/approval patterns;
- MCP-connected tools;
- voice as an interaction mode.

### Loren-specific concern

Computer use is an execution capability, not Loren's identity. It should be integrated as a skill/runtime adapter rather than becoming the architecture center.

### Current hypothesis

Useful future **hands-and-eyes** layer for desktop workflows.

## Home Assistant Assist

Official voice page: https://www.home-assistant.io/voice_control/

### Relevant strengths

Home Assistant Assist supports natural-language control of the smart home and can run fully locally. Home Assistant also supports AI/LLM integrations for more conversational behavior.

Potentially reusable areas:

- smart-home device graph;
- device control;
- event source;
- local voice pipeline;
- wake-word/voice hardware ecosystem.

### Loren-specific concern

Loren should not reimplement smart-home protocols. Home Assistant should remain the authority for devices while Loren supplies higher-level personal reasoning and policy.

### Current hypothesis

Preferred future **smart-home skill** rather than native device implementation.

## What Loren should probably NOT reinvent

Unless deeper evaluation reveals a blocker, Loren should avoid spending early project time on:

- generic multi-provider LLM clients;
- messaging-channel gateways;
- raw desktop automation engines;
- smart-home protocol/device support;
- generic cron implementations;
- generic plugin packaging solely for third-party ecosystem breadth;
- another general-purpose agent framework.

## What Loren should own

The project should deliberately own:

1. **Identity** — stable Loren behavior independent of the runtime/model.
2. **Personal world model** — structured entities and relationships in the owner's life/work.
3. **Memory policy** — what is remembered, why, provenance, correction, forgetting, and retrieval.
4. **Permission policy** — what may be done automatically, what requires approval, and what is forbidden.
5. **Audit/experience history** — actions, outcomes, corrections, and learned operating procedures.
6. **Personal project semantics** — aliases, environments, decision history, working conventions.
7. **Proactive prioritization** — which events matter enough to interrupt the owner.
8. **Adapter boundaries** — making runtimes and external systems replaceable.

## Architecture candidates

### A — Loren on OpenClaw

Use OpenClaw as the primary runtime/gateway. Add Loren-owned memory/world-model/policy services around or above it.

**Advantages**

- fastest path to an always-on assistant;
- large amount of integration/runtime infrastructure already solved;
- likely shortest path to channels, devices, and automation.

**Risks**

- runtime concepts may leak into Loren's identity/state;
- deep customization may require following OpenClaw's architecture closely;
- replacing the runtime later may be expensive if boundaries are weak.

### B — Loren on Letta

Use Letta as the persistent agent runtime and build Loren interfaces, tools, policies, and integrations around it.

**Advantages**

- strongest alignment with persistent agent identity/memory;
- model independence is native to the concept;
- attractive for continuity across sessions/machines.

**Risks**

- more surrounding integration/gateway work may remain for Loren;
- Loren's world model and authorization layer could be awkward if forced into agent memory;
- runtime lifecycle decisions could become Letta-specific.

### C — Loren core with runtime adapters

Build a small Loren-owned core and support one runtime first through an adapter, with the architecture explicitly allowing replacement later.

**Advantages**

- cleanest long-term ownership boundary;
- protects personal state from framework churn;
- allows future hybrid use of multiple runtimes.

**Risks**

- easiest option to over-engineer;
- abstraction may be designed before enough real usage exists;
- more v0.1 code than simply adopting one framework end-to-end.

## Current recommendation

Do **not** choose A/B/C based only on feature lists.

Before implementation, perform a code-level spike on OpenClaw and Letta covering:

- lifecycle/state ownership;
- self-hosting and local operation;
- tool/skill APIs;
- approval hooks;
- event/scheduler behavior;
- memory storage/export;
- model switching;
- extension points;
- security boundaries;
- how difficult it is to put a Loren-owned policy and world-model layer in front of them.

The result should resolve ADR-001.
