# Loren Architecture

**Status:** Active baseline. ADR-001, ADR-002, and ADR-003 are accepted.  
**Completed milestone:** M4 Trusted Durable Memory.  
**Next decision gate:** Gate D before M5 external writes.

## Architectural objective

Loren owns stable personal state, context, policy, and action authorization while treating language models, MCP, vendor APIs, UI clients, and execution runtimes as replaceable infrastructure.

> **The model is the reasoning brain. Loren is the stateful system that gives that brain identity, memory, context, tools, and boundaries.**

## Current v0.1 architecture

```text
Owner Web UI
    |
    v
Loren.Web
  auth/session
  request/API surface
  canonical project + memory context preparation
    |
    +-------> IProjectCatalog / IMemoryStore
    |             |
    |             v
    |       Loren.Infrastructure
    |       SQLite + EF Core
    |       canonical Project/Repository/Memory
    |
    v
small prepared BrainContext
    |
    v
Loren.Runtime / AgentLoop
    |
    +-------> IBrain -> Ollama/OpenAI/future provider
    |
    v
ActionRequest
    |
    v
ActionGateway
  -> policy
  -> audit
  -> controlled executor
    |
    v
GitHub / MCP / direct APIs
```

## Boundary 1 — Loren-owned canonical state

State must remain usable if the brain provider, provider session, MCP implementation, UI, or external runtime is replaced.

### Canonical IDs

ADR-003 locks v0.1 durable identity to opaque Loren-owned GUID values.

Rules:

- never derive Loren IDs from GitHub names, provider/session IDs, paths, usernames, or display names;
- canonical IDs are immutable;
- import/restore preserves IDs;
- external IDs/names are integration metadata only.

### Current world model

M3 intentionally implemented only:

```text
Project
  -> aliases
  -> Repository*

Repository
  -> Loren RepositoryId
  -> ProjectId
  -> integration locator (provider / external namespace / name)
```

M4 adds durable `MemoryRecord` state scoped to canonical Project/Repository identity.

Additional `Person`, `Task`, `Decision`, `Preference`, `Device`, or generic graph entities are deferred until a real product flow requires them.

### Canonical persistence

v0.1 uses SQLite + EF Core in `Loren.Infrastructure`.

- explicit checked-in EF migrations;
- production host runs `MigrateAsync` at startup;
- no `EnsureCreated` for production canonical state;
- real SQLite migration/restart tests;
- `Loren.Core` contains no EF/SQLite types;
- portable export versioning is independent from EF schema versioning.

The current database file is `loren.db`, under the OS local application-data `Loren` directory unless `LOREN_DATA_DIRECTORY` overrides the directory.

## Boundary 2 — Context preparation

The application/host layer resolves canonical references and prepares bounded data before the brain runs.

Project path:

```text
owner message + optional projectAlias
    |
IProjectCatalog.FindByAliasAsync
    |
    +--> unknown -> fail closed before brain
    |
ProjectSnapshot
    |
prepared Project/Repository system context
```

Memory path:

```text
canonical ProjectId
    |
IMemoryStore current records
    |
    +--> superseded removed
    +--> trust/provenance eligibility
    +--> deterministic authority ordering
    +--> hard record/content/provenance bounds
    |
prepared durable-memory system context
```

Prepared project context distinguishes configured identity from live external facts. Prepared memory context retains canonical IDs, scope, source class, provenance, and timestamps while framing the payload as inert data.

Runtime and brain adapters never receive `DbContext` or arbitrary database access.

## Boundary 3 — Memory [M4 COMPLETE]

Required source classes:

```text
OWNER_EXPLICIT
OWNER_CORRECTION
VERIFIED_TOOL
OWNER_APPROVED_INFERENCE
MODEL_INFERENCE
EXTERNAL_CONTENT
```

### Durable write/correction behavior

- owner-explicit durable memory survives restart;
- owner correction uses append + supersede rather than destructive rewrite;
- correction requires `OWNER_CORRECTION`, new canonical ID, current target, identical scope, and valid lifecycle ordering;
- model inference/external content cannot use the owner-correction boundary.

### Prepared retrieval behavior

Default trusted prepared context:

- includes `OWNER_CORRECTION`, `OWNER_EXPLICIT`, `OWNER_APPROVED_INFERENCE`, `VERIFIED_TOOL` when provenance semantics are valid;
- excludes `MODEL_INFERENCE` and `EXTERNAL_CONTENT`;
- excludes superseded records;
- bounds record count, content characters, and provenance/source-reference text before brain execution;
- keeps provenance and trust class inspectable.

The full serialized memory payload — content, provenance/source reference, IDs, scope and timestamps — is data, not instruction, permission, policy, or action authorization.

`VERIFIED_TOOL` is authoritative only for the verified external fact at its source/time. It cannot create owner preference/policy/approval and does not automatically represent current mutable state.

### Forget behavior

`ForgetAsync(currentMemoryRecordId)` physically purges the full linear correction chain in one SQLite transaction after validating current state, scope, and expected supersession edges.

For:

```text
A -> B -> C(current)
```

forgetting C deletes A -> B -> C so older corrected claims cannot resurrect. Restart tests prove forgotten claims remain absent while unrelated memories survive.

Memory forgetting remains separate from audit retention.

### Runtime mutation boundary

The normal `LorenRunService` path currently reads prepared durable memory but does not silently perform `AddAsync`, `CorrectAsync`, or `ForgetAsync`. M4 adversarial acceptance locks this behavior before future owner-facing memory mutation UX is introduced.

See [`memory.md`](memory.md) and ADR-003.

## Boundary 4 — Brain

`IBrain` is replaceable compute. It may interpret intent, reason over supplied context, request actions, consume structured results, and return final output.

It may not:

- authorize itself;
- directly mutate canonical state outside Loren-owned services;
- receive raw privileged tool credentials as ordinary context;
- define durable identity;
- receive raw database access;
- treat memory payload text/provenance as self-authorizing instructions.

Provider SDK/API types remain outside `Loren.Core`.

## Boundary 5 — Runtime

The runtime is deliberately small:

```text
prepared BrainContext
-> IBrain
-> final answer OR ActionRequest
-> ActionGateway
-> ActionResult
-> append observation
-> bounded repeat
```

Hard turn/action/cancellation limits belong to Loren and are testable with fake providers. The runtime does not own durable identity or persistence.

## Boundary 6 — Action Gateway

The Action Gateway remains the mandatory security boundary between model reasoning and external side effects.

```text
ActionRequest
 -> schema/registration validation
 -> policy evaluation
 -> deny / approval / allow
 -> controlled executor
 -> postcondition verification for consequential writes
 -> ActionResult + audit
```

No privileged integration may bypass it.

The current production path has trusted GitHub read capability only. **Gate D must pass before the first real write.**

## Boundary 7 — Skills, MCP, and external APIs

Loren owns the internal action contract. MCP/direct APIs/native adapters are execution mechanisms behind it.

MCP is an integration protocol, not Loren's brain or authorization model. Provider-managed MCP must never become a path around ActionGateway or Loren credential boundaries.

## Boundary 8 — Credentials

Secrets live outside canonical memory/context.

```text
brain/runtime
 -> ActionRequest (no secret)
 -> ActionGateway
 -> controlled executor
 -> Secret Resolver
 -> external system
```

Provider credentials are adapter configuration. Future write-capable tool secrets must be readable only by the narrow executor that needs them.

**Gate D is the next architecture decision gate** and must lock write credential storage/resolution, read/write separation, approval binding/replay behavior, redaction, rotation/revocation, global read-only/kill behavior, post-write verification, and write audit expectations.

## Audit and deletion boundary

Audit is append-oriented evidence; memory is owner-controlled knowledge. A memory forget operation does not silently delete audit history.

Audit retention/redaction/purge is an explicit separate operation. Sensitive payloads should be minimized/redacted so retained audit does not become a credential or private-content archive.

## Export/recovery boundary

Portable recovery uses a Loren-owned logical export with its own `format_version`, canonical IDs, and referential integrity. A raw SQLite copy may be a backup but is not the portable canonical contract.

First planned logical format: `format_version = 1`, with manifest + projects/repositories + memory/permission/audit data. Raw secrets are excluded. M7 will implement and prove export -> wipe -> restore.

## Reliability principles

- **Bounded loops:** hard turn/action/cancellation limits.
- **Fail closed:** ambiguous authorization/reference resolution does not execute.
- **Check before act:** fetch current mutable state before important writes.
- **Verify after act:** consequential writes confirm postconditions.
- **Recoverable state:** provider/runtime failure or session deletion does not destroy canonical state.
- **Idempotency:** consequential retries use operation identifiers where practical.
- **Memory provenance:** low-authority content cannot self-promote by spoofing text fields.

## Current accepted decisions

- **ADR-001:** Loren-owned core with replaceable adapters.
- **ADR-002:** .NET 10 / ASP.NET Core / Loren-owned bounded loop / provider-neutral `IBrain` / SQLite+EF Core / Blazor / xUnit baseline.
- **ADR-003:** opaque canonical IDs, EF migration policy, Project/Repository boundary, memory source classes, append/supersede correction, memory-vs-audit deletion distinction, and logical export versioning.

## Next decision gate

**Gate D — Action/Credential Policy** must pass before GitHub writes. It will settle write-action contracts, approval binding/non-replay, write credential resolution/scope/redaction/rotation, global read-only enforcement, post-write verification, and correlated audit.

Background execution, trusted devices/voice, and proactive autonomy remain later gates in `docs/plans/master-plan.md`.
