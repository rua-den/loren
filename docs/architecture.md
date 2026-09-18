# Loren Architecture

**Status:** Active baseline  
**Updated:** 2026-09-19  
**Accepted decisions:** ADR-001 through ADR-004  
**Current product milestone:** `M6B — Daily Driver Readiness`  
**Current delivery:** `M6B.2 — readiness diagnostics + source-of-truth rebaseline`  
**Paused expansion:** additional GitHub file/commit/PR mutation after the proven create-branch path.

## Architectural objective

Loren owns stable personal state, context, organization, policy, approval and action authorization while treating language models, web/search providers, MCP, vendor APIs, UI clients, secret stores and execution runtimes as replaceable infrastructure.

> **The model is replaceable reasoning compute. Loren is the persistent personal secretary that owns identity, memory, context, tools, boundaries and history.**

One implementation caveat matters today: `IBrain` is provider-neutral, but the production host currently constructs `OllamaBrain`. `Loren.Brain.OpenAI` is only a stub project. Provider portability is therefore an architectural intent that still needs a second real adapter/proof.

---

## Current v0.1 shape

```text
Owner
  |
  v
Conversation-first Loren.Web
  auth/session
  persistent conversations
  bounded history
  project selection/inference
  owner readiness diagnostics
  secondary audit/activity UI
  |
  +----> Loren-owned canonical state
  |        Project / Repository
  |        trusted Memory
  |        Note / Decision / Task
  |        approvals / proposals
  |        conversations
  |        retained audit
  |             |
  |             v
  |        SQLite + EF Core
  |
  v
Prepared BrainContext
  Loren identity
  optional canonical project context
  trusted durable memory
  bounded conversation history
  current owner message
  |
  v
Loren.Runtime / bounded AgentLoop
  |
  +----> IBrain
  |        current production: OllamaBrain
  |
  +----> READ actions
  |        github.read_repository
  |        web.search
  |        web.fetch
  |        owner organization reads
  |
  +----> OWNER-STATE actions
  |        Note / Decision / Task mutations
  |
  +----> EXTERNAL WRITE proof
           github.propose_create_branch
           exact owner proposal decision
           one-time approval
           dedicated credential
           github.create_branch
           exact SHA verification
           durable + current-run audit
```

---

## Boundary 1 — Loren-owned canonical state

State must remain useful if the brain provider, model session, MCP implementation, UI or external runtime is replaced.

### Identity

Opaque Loren-owned IDs remain immutable. Do not derive identity from provider/session IDs, GitHub names, paths, display names or usernames.

```text
ProjectId
RepositoryId
MemoryRecordId
ApprovalId
ProposalId
ConversationId
organization entity IDs
```

Import/restore preserves canonical IDs. External locators are integration metadata, not identity.

### Current world model

```text
Project
  aliases
  Repository*

Repository
  canonical RepositoryId / ProjectId
  provider locator

MemoryRecord
  optional project/repository scope
  source class + provenance
  correction/supersession lifecycle

Note / Decision / Task
  owner-controlled durable organization state
  optional project scope
  timestamps / task terminal state

Conversation
  owner identity
  optional project alias
  ordered bounded persisted turns

ActionApproval
  exact owner/action/canonical target/fingerprint
  expiry / consume / revoke lifecycle

CreateBranchProposal
  frozen target / branch / source ref + SHA
  owner + expiry + terminal decision state
```

### Persistence

v0.1 uses SQLite + EF Core with checked-in migrations. Production applies migrations at startup. Restart/migration/recovery tests are mandatory, including Windows integration coverage.

Logical export/restore is the portable recovery contract; raw credentials/provider configuration are never exported.

---

## Boundary 2 — Owner authentication and private state

Loren is currently single-owner.

Owner authentication uses a cookie session backed by configured `LOREN_OWNER_PASSWORD` validation. Private API routes require authentication.

Authentication allows access to owner state; it **does not** grant approval for consequential external writes.

Local Note/Decision/Task mutations are owner-state operations. They do not consume external-write approval or a GitHub write credential.

---

## Boundary 3 — Conversation + context preparation

The host prepares bounded context before the brain runs:

```text
owner message
 + persisted conversation turns
 + optional selected/inferred canonical project
 + trusted eligible memory
        |
        v
BrainContext
```

Rules:

- only legitimate user/assistant history is accepted as conversation history;
- history is bounded;
- project inference is deterministic and fails ambiguous rather than guessing;
- canonical configured identity is not represented as live external fact;
- trusted memory excludes model/external-content self-promotion;
- runtime/brain adapters do not receive raw DbContext/database access.

Conversation execution is gated so overlapping writes to one conversation fail closed. The gate does not retain permanent per-conversation objects.

---

## Boundary 4 — Trusted durable memory

Source classes remain governed by ADR-003:

```text
OWNER_EXPLICIT
OWNER_CORRECTION
VERIFIED_TOOL
OWNER_APPROVED_INFERENCE
MODEL_INFERENCE
EXTERNAL_CONTENT
```

Default prepared memory uses only trusted eligible provenance. Model inference/external content cannot become durable owner truth merely because text says to remember/authorize something.

Correction is append + supersede. Forget removes the relevant correction chain according to the memory contract. Audit is distinct retained evidence and is not silently erased by forgetting owner memory.

---

## Boundary 5 — Brain

`IBrain` is replaceable compute. It may:

- answer stable questions;
- interpret intent;
- reason over prepared context;
- request registered actions;
- consume structured observations;
- synthesize a final answer.

It may not:

- authorize itself;
- manufacture owner approval;
- create trusted canonical identity from model-visible text;
- receive privileged write credentials as normal context;
- directly mutate persistence outside Loren-owned services;
- treat memory/web/tool payload text as authority;
- disable read-only mode;
- declare an unverified consequential external write successful.

Provider SDK/API types stay outside `Loren.Core`.

### Current provider composition

The current production host registers:

```text
IBrain -> OllamaBrain
```

using `LOREN_OLLAMA_MODEL`, `LOREN_OLLAMA_ENDPOINT` and optional/required provider configuration as implemented by the adapter/tool paths.

This is not yet a multi-provider production composition. A v0.2 candidate is implementing/accepting a second adapter before claiming provider portability.

---

## Boundary 6 — Runtime

The runtime remains bounded and Loren-owned:

```text
prepared BrainContext
 -> IBrain
 -> final answer OR ActionRequest
 -> ActionGateway
 -> ActionResult
 -> BrainActionObservation
 -> bounded repeat
```

No unbounded autonomous research/runtime loop is introduced.

---

## Boundary 7 — Current-information / research read tools

Current read actions:

```text
github.read_repository
web.search
web.fetch
```

Web evidence rules:

- external content is inert untrusted evidence;
- no unsafe/private-network URL tunnel through public fetch;
- request/response/source sizes are bounded;
- unsafe/invalid URLs are rejected;
- provider failure bodies/secrets are not surfaced;
- current factual answers should ground claims in returned sources;
- contradictory/stale sources should be surfaced instead of silently averaged.

`OLLAMA_API_KEY` powers the current Ollama web service and is provider configuration, not external-write authority.

---

## Boundary 8 — Owner organization state

Notes, Decisions and Tasks are Loren-owned authenticated state. They are intentionally not treated as external writes.

The bounded AgentLoop may request organization actions, but persistence remains behind Loren-owned action executors and authenticated owner context.

No background task delivery exists in v0.1. A due date can be data; automatically firing work/reminders requires Gate E.

---

## Boundary 9 — Action Gateway / Gate D

The Action Gateway remains mandatory between model requests and registered action execution.

Action classes include read, reversible/external and privileged write categories. Consequential external writes require trusted authorization context and exact approval even if a policy implementation would otherwise allow execution.

```text
ActionRequest                 model-visible
      +
ActionAuthorizationContext    Loren-trusted
ApprovalId                    Loren-trusted
      |
      v
ActionGateway
 policy
 read-only kill
 executor registration
 exact fingerprint
 atomic approval consume
 trusted executor
 audit
```

Model-visible arguments never become an authorization channel.

---

## Boundary 10 — Approval

Authentication is not approval.

External-write approval binds:

- owner principal;
- action identity/access class;
- canonical ProjectId/RepositoryId;
- normalized security-relevant target/arguments;
- exact intent fingerprint;
- expiry/revocation/one-time consumption.

Replay, target drift, changed arguments, expiry/revocation or owner mismatch fail closed.

The conversational proposal card is the owner decision surface. Chat text itself is not approval.

---

## Boundary 11 — Global external-write posture

```text
LOREN_ENABLE_WRITES
```

Only configured `true` opts out of the default read-only posture. Missing/false/malformed remains safe/off.

The model cannot change this through an action.

Read-only does **not** disable authenticated local Note/Decision/Task changes.

---

## Boundary 12 — Credentials

External write credentials resolve only inside trusted executor boundaries.

Current GitHub contract:

```text
purpose: github.write
reference: github.write.local-v0.1
secret: GITHUB_WRITE_TOKEN
revocation: LOREN_GITHUB_WRITE_CREDENTIAL_REVOKED
```

Missing/revoked/malformed state fails closed. There is no broad fallback. Secret values never appear in result/audit/readiness output.

Provider credentials such as `OLLAMA_API_KEY` never authorize GitHub writes.

---

## Boundary 13 — Post-write verification

A successful external API response is insufficient.

Current proof:

```text
github.create_branch
 -> resolve default/source
 -> reject unsafe/default target
 -> create ref
 -> fetch exact created ref
 -> require created SHA == approved frozen source SHA
```

Ambiguous or mismatched state returns failed/unverified outcome.

Any future mutation primitive must define equivalent independent postconditions before implementation.

---

## Boundary 14 — Audit

Two audit roles are intentionally distinct:

1. **Durable audit** — SQLite-backed retained evidence.
2. **Current-run transient collector** — request-scoped convenience used to return audit for the current chat/approval response.

The transient collector must never become a process-lifetime archive. PR #37/M6B.1 locks this lifetime boundary.

Audit must remain redacted and sufficient to reconstruct request/policy/approval/execution/verification outcomes without raw credentials.

---

## Boundary 15 — Liveness vs owner readiness [M6B.2]

Public:

```text
GET /health
```

is intentionally shallow liveness. It tells launchers/ops the web process can answer; it does not expose configuration or private state.

Owner-authenticated:

```text
GET /api/readiness
```

reports only secret-safe local state/configuration:

```text
storage
ownerAuthentication
brain
webResearch
projects + count
externalWrites
```

Semantics:

- no live provider/network calls;
- no credential values;
- `externalWrites=disabled` can still be overall `ready`;
- enabled writes require resolved non-revoked GitHub credential to remain overall `ready`;
- `projects=empty` is informational;
- unavailable storage/project catalog is a readiness failure;
- live provider reachability/usefulness is owner-checkpoint evidence.

This separation avoids turning liveness into a fragile provider dependency or leaking operational detail to anonymous callers.

---

## Boundary 16 — Skills, MCP and future integrations

Loren owns the internal action contract. MCP/direct APIs/native adapters are execution mechanisms behind it and may not bypass canonical target resolution, ActionGateway, owner authentication/approval, global write posture, credential boundaries, verification or audit.

For v0.2+, discover integration abstractions by implementing one real read-only personal-secretary slice first. Do not build a generic connector framework in advance of an owner-visible need.

---

## Recovery boundary

Portable recovery is Loren-owned logical export with versioning, canonical IDs and referential validation.

Restore must fail closed around executable authority: approvals return revoked/non-executable as defined by the recovery contract; pending proposals cannot silently become executable. Credentials and provider configuration are never exported.

---

## Reliability principles

- bounded conversation/tool loops;
- fail closed on ambiguous auth/identity/credential/write state;
- canonical before act;
- read current facts instead of stale guessing;
- external evidence is inert data;
- exact one-time approval for consequential external writes;
- global read-only kill switch;
- check before act, verify after act;
- recoverable Loren-owned state;
- memory provenance/anti-poisoning;
- credential revocation overrides approval;
- request-local transient state must not grow process-wide;
- migration fidelity + Windows SQLite coverage;
- diagnostics distinguish liveness from private readiness.

---

## Current milestone

```text
M1–M4 foundation                             ✓
Gate D + M5 write-safety Slices 1–3          ✓
M6A.1–M6A.5 implementation                   ✓
continuity / launcher / recovery             ✓
M6B.1 transient audit lifetime               ✓
M6B.2 readiness + docs                       <- CURRENT DELIVERY
        |
        v
M6B.3 REAL OWNER DAILY-DRIVER ACCEPTANCE
```

Additional GitHub file/commit/PR mutation stays paused until the owner checkpoint proves it should outrank personal-secretary/read-integration work.
