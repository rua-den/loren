# Loren Architecture

**Status:** Active baseline. ADR-001, ADR-002, ADR-003, and ADR-004 are accepted.  
**Completed milestone:** M4 Trusted Durable Memory.  
**Current milestone:** M5 Action/Credential Boundary + Narrow GitHub Writes.  
**Current implementation target:** M5 Slice 2 credential resolver/redaction after Slice 1 merges.

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
    |
    +-------> IProjectCatalog / IMemoryStore / IActionApprovalStore
    |             |
    |             v
    |       Loren.Infrastructure
    |       SQLite + EF Core
    |       Project / Repository / Memory / ActionApproval
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
model-visible ActionRequest
    |
    +---- trusted Loren-owned ActionAuthorizationContext + ApprovalId
    |
    v
ActionGateway
  -> action registration/schema
  -> GateDActionPolicy
  -> global read-only
  -> verify executor registration
  -> exact intent fingerprint
  -> approval validation / atomic consume
  -> audit
    |
    v
controlled executor boundary
  -> M5 Slice 2 write-specific credential resolver
  -> later narrow GitHub writes
  -> independent post-write verification
```

M5 Slice 1 stops before real mutation. The production action set still contains the existing GitHub read path only.

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

ActionApproval
  -> Loren ApprovalId
  -> trusted owner principal reference
  -> canonical ProjectId + RepositoryId
  -> action identity + exact intent fingerprint
  -> approved / expires / consumed / revoked lifecycle
```

Approval state is security state, not model conversation state and not a generic user/person graph. Model/session IDs do not become Loren durable identity.

Additional `Person`, `Task`, `Decision`, `Preference`, `Device`, or generic graph entities remain deferred until a real product flow requires them.

### Canonical persistence

v0.1 uses SQLite + EF Core in `Loren.Infrastructure`.

- explicit checked-in EF migrations;
- production host runs `MigrateAsync` at startup;
- no `EnsureCreated` for production canonical state;
- real SQLite migration/restart tests;
- `Loren.Core` contains no EF/SQLite types;
- portable export versioning is independent from EF schema versioning.

Current migrations include:

```text
202609040001_InitialCanonicalState
202609040002_AddMemoryRecords
202609040003_AddActionApprovals
```

The database file is `loren.db`, under the OS local application-data `Loren` directory unless `LOREN_DATA_DIRECTORY` overrides the directory.

Temporary SQLite integration databases disable connection pooling so Windows file cleanup is deterministic. Production pooling behavior is unchanged. CI keeps the Ubuntu full gate plus a Windows integration job.

A permanent migration-drift test compares the EF migration snapshot with the current design-time model. This prevents canonical schema changes from silently diverging from checked-in migration metadata.

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

The normal `LorenRunService` path reads prepared durable memory but does not silently perform `AddAsync`, `CorrectAsync`, or `ForgetAsync`.

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

### Model-visible action request is intentionally untrusted

`ActionRequest` remains the brain-facing proposal:

```text
name
arguments
```

It does **not** carry trusted canonical authority or trusted approval. If model output includes a field named `approvalId`, that string remains ordinary untrusted action data.

Trusted execution metadata is attached separately by Loren through `ActionExecutionRequest`:

```text
RunId
ActionId
ActionRequest
ActionAuthorizationContext?   <- Loren-owned trusted context
ApprovalId?                   <- Loren-owned trusted reference
```

This separation prevents model arguments from becoming an authorization channel.

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

Hard turn/action/cancellation limits belong to Loren and are testable with fake providers. Runtime/provider code does not own canonical identity, approval authority, credential values, or persistence.

## Boundary 6 — Action Gateway [M5 SLICE 1 IMPLEMENTED]

The Action Gateway remains the mandatory security boundary between model reasoning and external side effects.

Model-visible arguments and trusted normalized-target data are frozen into defensive immutable snapshots before policy/approval evaluation, so the intent approved and fingerprinted cannot be changed before executor invocation.

Current Slice 1 non-read path:

```text
ActionRequest
 -> registered ActionDefinition
 -> trusted ActionAuthorizationContext required
 -> GateDActionPolicy
 -> deny if global read-only
 -> deny PRIVILEGED_WRITE
 -> verify executor registration
 -> require approval for every eligible non-read action
 -> ActionIntentFingerprint recomputed from frozen trusted + proposed data
 -> trusted ApprovalId required
 -> IActionApprovalStore.ConsumeAsync
 -> first consequential executor attempt only after successful consume
```

The executor-registration check deliberately occurs before approval consumption so host misconfiguration cannot burn a valid one-time approval. Approval consumption remains immediately before the first consequential executor attempt.

Defense in depth: ActionGateway independently requires approval for **every non-read `ActionAccessClass`**, even if a custom/permissive `IActionPolicy` mistakenly returns `Allow`.

### Action classification

Implemented classifications:

```text
READ
REVERSIBLE_WRITE
EXTERNAL_WRITE
PRIVILEGED_WRITE
```

Legacy `IsReadOnly` remains for compatibility and maps to the typed classification; it is no longer the sole authorization semantic.

### Trusted canonical authorization context

`ActionAuthorizationContext` carries:

```text
ProjectId
RepositoryId
RepositoryLocator
OwnerPrincipalReference
NormalizedTarget
```

The exact intent fingerprint also covers model-provided action arguments, sorted deterministically. Changes in canonical target, target parameters, action arguments, action identity, access class, or trusted owner principal change the fingerprint and invalidate an old approval.

## Boundary 7 — Approval [M5 SLICE 1 IMPLEMENTED]

Authentication is not approval.

Every eventual first-version real GitHub mutation requires an explicit Loren-owned approval artifact created by an authenticated-owner path.

Current approval state includes:

```text
ApprovalId
owner principal reference
action name
ProjectId + RepositoryId
intent fingerprint
ApprovedAt
ExpiresAt
ConsumedAt?
RevokedAt?
```

`IActionApprovalStore` is a Core contract. `SqliteActionApprovalStore` is the current Infrastructure implementation.

### One-time consume and replay resistance

Approval is atomically consumed before the consequential executor attempt. Consumption checks owner/action/canonical scope/fingerprint/expiry/revocation and only updates a still-unconsumed, still-unrevoked record.

Consequences:

- exact first use can proceed;
- replay is denied;
- changed target/arguments are denied;
- expired/revoked approval is denied;
- concurrent attempts have exactly one successful consumer;
- an independent retry after executor failure/ambiguity requires a fresh approval.

This last rule intentionally favors non-replay over transparent retries until M5 write executors have explicit duplicate-safe/idempotent semantics.

## Boundary 8 — Global read-only [M5 SLICE 1 IMPLEMENTED]

The host now has a fail-closed write posture configured by:

```text
LOREN_ENABLE_WRITES
```

Semantics:

```text
missing -> read-only
false -> read-only
malformed/other value -> read-only
true -> policy may evaluate eligible writes
```

The model cannot toggle this through an action. Read-only denial occurs before approval consumption/executor invocation.

Slice 1 still registers no mutation action/executor, so `LOREN_ENABLE_WRITES=true` alone does not create an external write capability.

## Boundary 9 — Credentials [M5 SLICE 2 NEXT]

Secrets remain outside canonical memory/context.

Target path:

```text
brain/runtime
 -> ActionRequest (no secret) + trusted canonical target
 -> policy + global read-only
 -> verify executor registration
 -> exact owner approval validate/consume
 -> controlled executor boundary
 -> write-specific Secret Resolver
 -> external system
```

M5 Slice 2 must prove:

- opaque write credential purpose/reference outside secret implementation;
- secret value materialized only within controlled executor boundary;
- read/write purpose separation;
- missing/revoked credentials fail closed;
- revocation overrides prior approval;
- no fallback to broader credentials;
- secret redaction across logs/exceptions/audit/action results/brain context.

No real GitHub mutation executor is enabled before this boundary is green on `main`.

## Boundary 10 — Post-write verification [PLANNED M5 WRITE SLICES]

A successful API response will not be enough to report a consequential write as successful.

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

### v0.1 mutation allowlist

After M5 Slice 2 and each action-specific acceptance gate, only:

```text
create non-default branch
controlled file/commit path on a non-default branch
open pull request
```

may be registered as v0.1 mutations.

Not supported:

```text
direct default-branch write
merge PR
force push/history rewrite
delete repository/branch/data
repository admin/security changes
secret-management actions
production deployment
```

## Audit and deletion boundary

Audit is append-oriented evidence; memory is owner-controlled knowledge. A memory forget operation does not silently delete audit history.

M5 Slice 1 adds `ApprovalEvaluated` to action audit sequencing. Approval audit records outcome/reference semantics, not credential values.

Future consequential write audit must reconstruct:

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
- **Global read-only:** writes can be stopped before approval consumption/executor/credential resolution.
- **Check before act:** fetch current mutable state before important writes.
- **Verify after act:** consequential writes confirm postconditions.
- **Recoverable state:** provider/runtime failure or session deletion does not destroy canonical state.
- **Idempotency:** executor retries are bounded and duplicate-safe or require new approval.
- **Memory provenance:** low-authority content cannot self-promote by spoofing text fields.
- **Credential revocation:** stale approval never resurrects a revoked secret.
- **Migration fidelity:** canonical EF model and checked-in migration snapshot must remain drift-free.

## Current accepted decisions

- **ADR-001:** Loren-owned core with replaceable adapters.
- **ADR-002:** .NET 10 / ASP.NET Core / Loren-owned bounded loop / provider-neutral `IBrain` / SQLite+EF Core / Blazor / xUnit baseline.
- **ADR-003:** opaque canonical IDs, EF migration policy, Project/Repository boundary, memory source classes, append/supersede correction, memory-vs-audit deletion distinction, and logical export versioning.
- **ADR-004:** typed write intent, canonical target authorization, explicit exact owner approval, non-replay, global read-only, credential isolation/revocation, post-write verification, and redacted correlated audit.

## Current milestone

**M5 — Action/Credential Boundary + Narrow GitHub Writes** is active.

Execution order:

```text
Slice 1 typed action policy + one-time approval + global read-only   ✓ implemented / PR #25 merge gate
Slice 2 write credential resolver + redaction/revocation             <- next
Slice 3 create non-default branch + verify
Slice 4 controlled file/commit path + verify
Slice 5 open PR + verify
Slice 6 adversarial replay/revocation/injection/audit E2E
```

No real GitHub mutation is enabled until the policy/approval/read-only/credential foundations are green on `main`.

Background execution, trusted devices/voice, and proactive autonomy remain later gates in `docs/plans/master-plan.md`.
