# Loren Vision

## What Loren should become

Loren should feel less like opening a chatbot and more like consulting a persistent personal operator that already understands the owner's world.

A mature Loren should be able to:

- understand ambiguous references using project/person/device context;
- remember durable preferences and past decisions;
- inspect authoritative systems before answering operational questions;
- perform actions through tools within explicit permission policies;
- explain what it did and why;
- continue scheduled or event-driven work when appropriate;
- notify the owner when something material changes;
- preserve identity and memory even when the underlying language model or runtime changes.

## Target interaction

The long-term target is conversation such as:

> **Owner:** Loren, how is the wedding project doing?
>
> **Loren:** Main is healthy. The latest build passed. Two open items remain, one of which affects mobile layout. I can investigate that one now.
>
> **Owner:** Do it. Test it and deploy if everything passes.
>
> **Loren:** Understood. I can implement and test autonomously; production deployment remains approval-gated under the current policy.

The important properties are not the wording. They are:

1. Loren resolved "the wedding project" to the correct project object.
2. Loren inspected real systems instead of inventing status.
3. Loren knew the current permission policy.
4. Loren could perform a multi-step task.
5. Loren retained an audit trail.

## Loren is not

Loren is not intended to be:

- a thin wrapper around one LLM API;
- another general-purpose chat product;
- an agent framework built for arbitrary third-party users;
- a collection of hard-coded API integrations;
- an autonomous system with unrestricted shell or infrastructure access;
- dependent on a single vendor for identity or memory.

## Product moat

Existing projects already solve large parts of the infrastructure problem. Loren should therefore concentrate engineering effort on the personal layer:

- **Identity** — stable behavior and system rules independent of a model.
- **Personal world model** — structured knowledge of people, projects, devices, places, services, and relationships.
- **Memory** — durable preferences, decisions, facts, and episodic history.
- **Policy** — action-specific permission rules and approval behavior.
- **Experience** — history of actions, outcomes, corrections, and learned operating patterns.
- **Proactivity** — deciding what changes are important enough to surface to the owner.

## Core principles

### Memory is owned by Loren

Models may come and go. Loren's durable memory must not disappear when a provider changes.

### Tools are authoritative

For repository state, Loren should inspect GitHub. For schedule state, inspect the calendar. For server health, inspect monitoring or the server. The model interprets facts; it should not fabricate them.

### Autonomy is earned

The architecture should support proactive agents from the beginning, but autonomy should be introduced gradually based on proven permissions, logging, rollback, and reliability.

### Human-readable state matters

Important memories, decisions, policies, and architecture choices should have inspectable representations. Loren should never become a black box whose behavior cannot be explained from its stored state and logs.

### Infrastructure should be replaceable

OpenClaw, Letta, a custom runtime, or future systems may provide useful infrastructure. Loren should place stable interfaces around external runtimes so that replacing them does not require replacing Loren's identity and personal data.

## Success criteria

The project becomes meaningfully successful when the owner can use Loren daily and observe that it:

1. remembers relevant context without repeatedly being re-taught;
2. uses the correct tools automatically;
3. safely completes useful multi-step work;
4. knows when it must request approval;
5. gives concise, trustworthy status based on real data;
6. can change models or runtime components without losing personal continuity.

Voice, avatars, wake words, and smart-home integrations are useful interfaces, but none of them are prerequisites for achieving the core product.
