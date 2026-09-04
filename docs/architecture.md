# Loren Architecture

**Status:** Active baseline. ADR-001, ADR-002, ADR-003, and ADR-004 are accepted.  
**Completed milestone:** M4 Trusted Durable Memory.  
**Current milestone:** M5 Action/Credential Boundary + Narrow GitHub Writes.

## Architectural objective

Loren owns stable personal state, context, policy, approval, and action authorization while treating language models, MCP, vendor APIs, UI clients, secret-store backends, and execution runtimes as replaceable infrastructure.

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
  owner approval surface (M5)
    |
    +-------> IProjectCatalog / IMemoryStore
    |             |
    |             v
    |       Loren.Infrastructure
    |       SQLite + EF Core
    |       canonical Project/Repository/Memory
    |       approval state (M5)
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
  -> canonical target resolution
  -> policy
  -> approval validation / consume
  -> audit
  -> global read-only enforcement
    |
    v
controlled executor boundary
  -> write-specific credential resolver
  -> GitHub / MCP / direct APIs
  -> post-write verification read
    |
    v
structured verified result
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

```text
Project
  -> aliases
  -> Repository*

Repository
  -> Loren RepositoryId
  -> ProjectId
  -> integration locator (provider / external namespace / name)

MemoryRecord
  -> Loren MemoryRecordId
  -> optional Project/Repository scope
  -> source class + provenance
  -> correction/supersession lifecycle
```

M5 adds Loren-owned approval state for exact action intent. Approval is not a generic person/user graph entity and does not make model/session IDs durable canonical identity.

Additional `Person`, `Task`, `Decision`, `Preference`, `Device`, or generic graph entities remain deferred until a real product flow requires them.

### Canonical persistence

v0.1 uses SQLite + EF Core in `Loren.Infrastructure`.

- explicit checked-in EF migrations;
- production host runs `MigrateAsync` at startup;
- no `EnsureCreated` for production canonical state;
- real SQLite migration/restart tests;
- `Loren.Core` contains no EF/SQLite types;
- portable export versioning is independent from EF schema versioning.

The current database file is `loren.db`, under the OS local application-data `Loren` directory unless `LOREN_DATA_DIRECTORY` overrides the directory.

Temp SQLite integration databases disable connection pooling so Windows cleanup semantics are deterministic. Production pooling behavior is unchanged. CI now keeps Ubuntu full gates plus a Windows integration job.

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

The full serialized memory payload — content, provenance/source reference, IDs, scope and timestamps — is data, not instruction, permission, policy, approval, or action authorization.

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

The normal `LorenRunService` path reads prepared durable memory but does not silently perform `AddAsync`, `CorrectAsync`, or `ForgetAsync`. M4 adversarial acceptance locks this behavior before future owner-facing memory mutation UX is introduced.

See [`memory.md`](memory.md) and ADR-003.

## Boundary 4 — Brain

`IBrain` is replaceable compute. It may interpret intent, reason over supplied context, request actions, consume structured results, and return final output.

It may not:

- authorize itself;
- manufacture owner approval;
- directly mutate canonical state outside Loren-owned services;
- receive raw privileged tool credentials as ordinary context;
- define durable identity;
- receive raw database access;
- treat memory/external/tool payload text as self-authorizing instructions;
- disable global read-only mode;
- decide that an unverified write succeeded.

Provider SDK/API types remain outside `Loren.Core`.

## Boundary 5 — Runtime

The runtime remains deliberately small:

```text
prepared BrainContext
-> IBrain
-> final answer OR ActionRequest
-> ActionGateway
-> ActionResult
-> append observation
-> bounded repeat
```

Hard turn/action/cancellation limits belong to Loren and are testable with fake providers. The runtime does not own durable identity, approval semantics, credential values, or persistence.

## Boundary 6 — Action Gateway [GATE D ACCEPTED]

The Action Gateway remains the mandatory security boundary between model reasoning and external side effects.

ADR-004 requires the write-capable path to become:

```text
ActionRequest
 -> action schema/registration validation
 -> resolve canonical Project/Repository target
 -> normalize security-relevant parameters
 -> typed access/risk policy evaluation
 -> deny OR require approval
 -> validate exact Loren approval
 -> atomically consume approval once
 -> enforce global read-only
 -> resolve write-specific credential behind executor boundary
 -> controlled executor
 -> independent post-write read verification
 -> structured verified ActionResult
 -> correlated redacted audit
```

No privileged integration may bypass it.

### Action classification

M5 must distinguish at least:

```text
READ
REVERSIBLE_WRITE
EXTERNAL_WRITE
PRIVILEGED_WRITE
```

`IsReadOnly` may remain for compatibility but is not enough as the long-term authorization contract.

### Canonical target resolution

A free-form model-provided repository string does not grant authority. Before write policy, Loren binds the request to:

```text
ProjectId
RepositoryId
provider/external repository locator snapshot
target branch/ref/path/base/head
default-branch status
normalized security-relevant parameters
```

Unknown or mismatched scope fails before credential resolution.

### v0.1 write allowlist

After M5 foundations are green, only:

```text
create non-default branch
create/update file via controlled commit path on a non-default branch
create commit/update ref only as required by that path
open pull request
```

remain eligible.

The following are not registered as supported v0.1 write capabilities:

```text
direct default-branch write
merge PR
force push/history rewrite
delete repository/branch/data
repository admin/security changes
secret-management actions
production deployment
```

## Boundary 7 — Approval [ADR-004]

Authentication is not approval.

Every first-version real GitHub mutation requires an explicit Loren-owned approval artifact created from an authenticated owner action.

Approval binds at least:

```text
ApprovalId
trusted owner/session principal binding
action identity
ProjectId + RepositoryId
normalized target/resource
security-relevant parameter digest
issued/approved timestamp
expiry or task boundary
one-time consumption state
optional prerequisite snapshot/digest
```

Material changes to repository, branch, path, content digest, PR base/head, or other security-relevant parameters require a new approval.

Approval is atomically consumed before the first consequential executor attempt. Consumed, expired, mismatched, unknown, revoked, or replayed approval fails closed.

Independent retry after ambiguous failure requires fresh approval unless a bounded executor retry is provably the same single attempt and cannot duplicate the external side effect.

Model output, memory, repository content, issue/PR text, or tool output cannot create, consume, expand, or revoke owner approval.

## Boundary 8 — Global read-only

Before any real write executor ships, Loren must have a host-controlled fail-closed global read-only posture.

```text
write-enable absent/invalid -> read-only
read-only -> no write executor invocation
read-only -> no write credential resolution
read actions remain available
```

The model cannot toggle this posture through an ordinary action.

The policy/audit path records when a write is denied by read-only mode.

## Boundary 9 — Credentials [ADR-004]

Secrets live outside canonical memory/context.

```text
brain/runtime
 -> ActionRequest (no secret)
 -> canonical target + policy
 -> owner approval validate/consume
 -> global read-only check
 -> controlled executor
 -> write-specific Secret Resolver
 -> external system
```

Write credential values never enter:

- `BrainContext`;
- model-visible action parameters;
- canonical Project/Repository/Memory state;
- audit payloads;
- owner-visible structured results.

Only an opaque credential purpose/reference may cross application boundaries. Missing/revoked credential resolution fails closed. Loren never silently falls back to a broader credential.

Read/write credential purposes remain logically separated even if local development temporarily maps them to one physical token.

Credential revocation takes precedence over prior approval.

## Boundary 10 — Post-write verification

A successful API response is not enough to report a consequential write as successful.

Examples:

```text
branch create
 -> fetch branch/ref
 -> confirm exact expected source SHA

file/commit write
 -> fetch resulting commit/ref/file identity
 -> confirm approved state/digest as appropriate

PR create
 -> fetch PR
 -> confirm repository + base + head + state + PR identity
```

If verification fails or remains ambiguous, action outcome is `failed` or `unverified`, never silently `succeeded`.

The brain consumes only structured verified results.

## Boundary 11 — Skills, MCP, and external APIs

Loren owns the internal action contract. MCP/direct APIs/native adapters are execution mechanisms behind it.

MCP is an integration protocol, not Loren's brain or authorization model. Provider-managed MCP must never become a path around ActionGateway, approval, global read-only, canonical target resolution, or Loren credential boundaries.

## Audit and deletion boundary

Audit is append-oriented evidence; memory is owner-controlled knowledge. A memory forget operation does not silently delete audit history.

For consequential writes, audit must reconstruct:

```text
run/request correlation
request/model proposal
normalized action identity
canonical Project/Repository target
redacted/hashed security-relevant parameter summary
policy decision + reason
approval ID + validation/consumption result
credential purpose/reference only
executor outcome
external identifiers
verification/postcondition
timestamps
```

Raw secrets are forbidden in audit. Sensitive payloads should be minimized/redacted so retained audit does not become a credential or private-content archive.

## Export/recovery boundary

Portable recovery uses a Loren-owned logical export with its own `format_version`, canonical IDs, and referential integrity. A raw SQLite copy may be a backup but is not the portable canonical contract.

First planned logical format: `format_version = 1`, with manifest + projects/repositories + memory/permission/approval-state-as-needed + retained audit data. Raw secrets are excluded. M7 will implement and prove export -> wipe -> restore.

## Reliability principles

- **Bounded loops:** hard turn/action/cancellation limits.
- **Fail closed:** ambiguous authorization/reference/approval/credential resolution does not execute.
- **Canonical before act:** resolve Loren-owned target identity before policy.
- **One-time approval:** consumed approvals do not replay.
- **Global read-only:** privileged writes can be stopped before credential resolution.
- **Check before act:** fetch current mutable state before important writes.
- **Verify after act:** consequential writes confirm postconditions.
- **Recoverable state:** provider/runtime failure or session deletion does not destroy canonical state.
- **Idempotency:** executor retries are bounded and duplicate-safe or require new approval.
- **Memory provenance:** low-authority content cannot self-promote by spoofing text fields.
- **Credential revocation:** stale approval never resurrects a revoked secret.

## Current accepted decisions

- **ADR-001:** Loren-owned core with replaceable adapters.
- **ADR-002:** .NET 10 / ASP.NET Core / Loren-owned bounded loop / provider-neutral `IBrain` / SQLite+EF Core / Blazor / xUnit baseline.
- **ADR-003:** opaque canonical IDs, EF migration policy, Project/Repository boundary, memory source classes, append/supersede correction, memory-vs-audit deletion distinction, and logical export versioning.
- **ADR-004:** typed write intent, canonical target authorization, explicit exact owner approval, non-replay, global read-only, credential isolation/revocation, post-write verification, and redacted correlated audit.

## Current milestone

**M5 — Action/Credential Boundary + Narrow GitHub Writes** is active.

Execution order:

```text
Slice 1 typed action policy + one-time approval + global read-only
Slice 2 write credential resolver + redaction/revocation
Slice 3 create non-default branch + verify
Slice 4 controlled file/commit path + verify
Slice 5 open PR + verify
Slice 6 adversarial replay/revocation/injection/audit E2E
```

No real GitHub mutation is enabled until the policy/approval/read-only/credential foundations are green.

Background execution, trusted devices/voice, and proactive autonomy remain later gates in `docs/plans/master-plan.md`.
