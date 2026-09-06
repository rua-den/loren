# Loren Architecture

**Status:** Active baseline. ADR-001, ADR-002, ADR-003, and ADR-004 are accepted.  
**Completed product milestone:** `M6A.1 — Conversation primary surface`  
**Current implementation target:** `M6A.2 — Current-information / web read`  
**Paused expansion:** additional GitHub write primitives after the verified create-branch proof.

## Architectural objective

Loren owns stable personal state, context, organization, policy, approval, and action authorization while treating language models, web/search providers, MCP, vendor APIs, UI clients, secret-store backends, and execution runtimes as replaceable infrastructure.

> **The model is replaceable reasoning compute. Loren is the persistent personal secretary that owns identity, memory, context, tools, boundaries, and history.**

## Current v0.1 architecture

```text
Owner
  |
  v
Conversation-first Loren.Web
  auth/session
  bounded conversation history
  friendly project selection / deterministic inference
  secondary activity + audit UI
  |
  +------> Loren-owned canonical state
  |          IProjectCatalog
  |          IMemoryStore
  |          IActionApprovalStore
  |                |
  |                v
  |          Loren.Infrastructure
  |          SQLite + EF Core
  |
  v
Prepared BrainContext
  Loren identity
  optional canonical Project/Repository context
  optional trusted durable memory
  bounded recent user/assistant conversation
  |
  v
Loren.Runtime / AgentLoop
  |
  +------> IBrain -> Ollama / OpenAI / future provider
  |
  +------> READ actions
  |          github.read_repository
  |          web.search                    [M6A.2]
  |          later web.fetch               [M6A.3]
  |
  +------> CONSEQUENCEFUL actions
             model-visible ActionRequest
             + Loren-owned trusted authorization context
             + explicit one-time owner approval
             -> ActionGateway
             -> credential-bound trusted executor
             -> postcondition verification
             -> audit
```

The current production mutation allowlist contains only the already-proven `github.create_branch` executor. File/commit/PR mutation expansion is paused until the M6A owner interaction checkpoint is usable.

---

## Boundary 1 — Loren-owned canonical state

State must remain useful if the brain provider, provider session, MCP implementation, UI, or external runtime is replaced.

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
  -> RepositoryId
  -> ProjectId
  -> integration locator

MemoryRecord
  -> MemoryRecordId
  -> optional Project/Repository scope
  -> source class + provenance
  -> correction/supersession lifecycle

ActionApproval
  -> ApprovalId
  -> owner principal reference
  -> canonical ProjectId + RepositoryId
  -> action identity + exact intent fingerprint
  -> approved / expires / consumed / revoked lifecycle
```

M6A.4 will add Loren-owned `Note`, `Decision`, and `Task` organization entities because a concrete owner-facing flow now requires them.

### Persistence

v0.1 uses SQLite + EF Core in `Loren.Infrastructure`.

Current migrations:

```text
202609040001_InitialCanonicalState
202609040002_AddMemoryRecords
202609040003_AddActionApprovals
```

Production runs checked-in migrations at startup. `EnsureCreated` is not the canonical production path. Migration-drift and real SQLite restart tests remain mandatory, including Windows integration coverage.

---

## Boundary 2 — Conversation and context preparation [M6A.1 COMPLETE]

The application/host prepares bounded trusted context before the brain runs.

Normal path:

```text
owner message
 + optional friendly project alias
 + bounded browser conversation history
        |
        +--> project selected explicitly
        |      OR deterministically inferred from owner message
        |      OR none when ambiguous/not mentioned
        |
        +--> trusted project-scoped memory when project resolved
        |
        v
BrainContext
  1. Loren identity/system guidance
  2. optional canonical project context
  3. optional trusted memory context
  4. bounded user/assistant history only
  5. current owner message
```

Security/quality rules:

- browser history may contain only `user` and `assistant` roles; system-role injection is rejected;
- conversation history is bounded by count and character budget;
- configured project identity is not represented as live external state;
- one-word project names such as `Loren` require project/repo cues for inference;
- ambiguous project matches do not guess;
- owner UI lists friendly project/repository identity without requiring canonical GUID entry for ordinary use.

Runtime and brain adapters never receive `DbContext` or arbitrary database access.

---

## Boundary 3 — Trusted durable memory [M4 COMPLETE]

Source classes:

```text
OWNER_EXPLICIT
OWNER_CORRECTION
VERIFIED_TOOL
OWNER_APPROVED_INFERENCE
MODEL_INFERENCE
EXTERNAL_CONTENT
```

Default prepared memory includes only trusted eligible classes with valid provenance. `MODEL_INFERENCE` and `EXTERNAL_CONTENT` are excluded from default trusted model context.

Rules:

- owner correction is append + supersede, not destructive rewrite;
- forget purges the complete correction chain so old claims cannot resurrect;
- retrieval is deterministically ordered and hard-bounded;
- memory content/provenance is data, never action authorization;
- `VERIFIED_TOOL` is authoritative only for the verified fact at its source/time and is not automatically current forever;
- normal conversation reads trusted memory but does not silently create/correct/forget durable owner memory.

See [`memory.md`](memory.md) and ADR-003.

---

## Boundary 4 — Brain

`IBrain` is replaceable compute. It may:

- answer stable reasoning/knowledge questions;
- interpret owner intent;
- reason over Loren-prepared context;
- request registered actions;
- consume structured action observations;
- synthesize a final answer.

It may not:

- authorize itself;
- manufacture owner approval;
- directly mutate canonical state outside Loren-owned services;
- receive privileged write credentials as ordinary context;
- define durable identity;
- receive raw database access;
- treat memory/web/tool payloads as self-authorizing instructions;
- disable global read-only mode;
- declare an unverified consequential write successful.

Provider SDK/API types stay outside `Loren.Core`.

### Model-visible request vs trusted execution metadata

Brain-facing:

```text
ActionRequest
  name
  arguments
```

Loren-owned trusted execution envelope:

```text
ActionExecutionRequest
  RunId
  ActionId
  ActionRequest
  ActionAuthorizationContext?   <- trusted Loren context
  ApprovalId?                   <- trusted Loren reference
```

Model arguments never become an authorization channel.

---

## Boundary 5 — Runtime

The bounded runtime remains deliberately small:

```text
prepared BrainContext
 -> IBrain
 -> final answer OR ActionRequest
 -> ActionGateway
 -> ActionResult
 -> append BrainActionObservation
 -> bounded repeat
```

Default hard limits remain Loren-owned and testable without a live provider.

M6A.3 research should reuse this bounded loop rather than introducing an autonomous unbounded research runtime.

---

## Boundary 6 — Current-information read tools [M6A.2 ACTIVE]

Current-information capability is a **read boundary**, not a browser authority boundary.

Current action:

```text
web.search(query)
```

Production implementation:

```text
ActionRequest web.search
 -> ActionGateway READ policy
 -> OllamaWebSearchExecutor
 -> POST trusted configured endpoint
 -> Bearer OLLAMA_API_KEY outside BrainContext
 -> bounded response bytes
 -> parse results
 -> validate http/https URLs
 -> reject overlong URLs instead of truncating into broken citations
 -> bound source count/title/content
 -> ActionResult structured evidence
 -> BrainActionObservation
```

Default endpoint:

```text
https://ollama.com/api/web_search
```

Optional trusted configuration:

```text
LOREN_OLLAMA_WEB_SEARCH_ENDPOINT
```

Trust rules:

- search results are untrusted external evidence;
- search content cannot become owner memory/policy/permission/approval by text alone;
- missing `OLLAMA_API_KEY` fails before an external search request;
- provider failure response bodies are not surfaced into action result/audit/brain context;
- credential values are never returned in action result;
- unsafe URL schemes are excluded;
- query/response/source/content size are hard-bounded;
- final current factual claims should be grounded in returned sources and include source URLs.

M6A.3 will add bounded `web.fetch` for selected URLs. Fetch must preserve the same inert-data rule and must not become an arbitrary privileged network tunnel.

---

## Boundary 7 — Action Gateway [GATE D + M5 IMPLEMENTED]

The Action Gateway is mandatory between model reasoning and registered tool execution.

Action classes:

```text
READ
REVERSIBLE_WRITE
EXTERNAL_WRITE
PRIVILEGED_WRITE
```

Read actions such as `github.read_repository` and `web.search` execute under read policy without owner write approval.

Every non-read action requires Loren-owned trusted execution context and exact one-time approval even if a custom policy would otherwise allow it.

Non-read path:

```text
ActionRequest
 -> registered ActionDefinition
 -> trusted ActionAuthorizationContext required
 -> GateDActionPolicy
 -> global read-only check
 -> deny PRIVILEGED_WRITE in v0.1
 -> verify trusted executor registration
 -> exact intent fingerprint
 -> trusted ApprovalId
 -> atomic approval consume
 -> trusted executor
```

Executor registration is checked before approval consumption so host misconfiguration cannot burn a valid approval.

---

## Boundary 8 — Approval

Authentication is not approval.

Approval binds:

```text
owner principal
action name + access class
canonical ProjectId + RepositoryId
normalized target
model-visible security-relevant arguments
expiry / revocation / one-time consumption
```

Replay, changed target/arguments, expired/revoked approvals, or owner mismatch fail closed.

Executor failure after approval consumption requires a fresh approval unless an explicit future idempotency contract says otherwise.

---

## Boundary 9 — Global read-only

```text
LOREN_ENABLE_WRITES
```

Only exact configured `true` opts the host out of read-only. Missing/false/malformed values remain read-only.

The model cannot alter this state through an action.

---

## Boundary 10 — Credentials [M5 SLICE 2 COMPLETE]

Write-specific credentials are resolved only inside controlled trusted executor boundaries.

Current GitHub write credential contract:

```text
purpose: github.write
reference: github.write.local-v0.1
secret env: GITHUB_WRITE_TOKEN
revocation env: LOREN_GITHUB_WRITE_CREDENTIAL_REVOKED
```

Credential behavior:

- exact purpose/reference binding;
- missing/revoked/malformed revocation state fails closed;
- no broad credential fallback;
- raw secret is not a public property;
- executor results/exceptions are redacted before returning to runtime/brain/audit.

`OLLAMA_API_KEY` used for brain/web read services is provider configuration, not a GitHub write credential and never authorizes a consequential action.

---

## Boundary 11 — Post-write verification [M5 SLICE 3 PROVEN]

A successful external API response is not sufficient for consequential write success.

Current proof:

```text
github.create_branch
 -> GET repository/default branch
 -> reject default/unsafe branch
 -> POST git/refs
 -> GET exact created ref
 -> verified_sha must equal exact approved source_sha
```

Failure/ambiguity produces failed/unverified outcome, never silent success.

Future write primitives must define equivalent postconditions before implementation.

---

## Boundary 12 — Skills, MCP, and external APIs

Loren owns the internal action contract. MCP/direct APIs/native adapters are execution mechanisms behind it.

MCP is an integration protocol, not Loren's brain or authorization model. No provider-managed execution path may bypass ActionGateway, canonical target resolution, approval, global read-only, credential boundaries, or verification.

Current registered production capabilities are intentionally narrow:

```text
READ
  github.read_repository
  web.search

MUTATION PROOF
  github.create_branch
```

Not currently supported:

```text
direct default-branch write
file/commit mutation
open/merge PR
force push/history rewrite
delete repository/branch/data
repository admin/security changes
secret-management actions
production deployment
```

---

## Audit and deletion boundary

Audit is append-oriented evidence; owner memory/organization state is owner-controlled knowledge. Forgetting memory does not silently erase retained audit.

Action audit correlates request, policy, approval evaluation, executor result, and verification outcome without retaining raw secrets.

---

## Export/recovery boundary

Portable recovery remains a Loren-owned logical export with its own `format_version`, canonical IDs, and referential integrity. Raw SQLite copy may be a backup but is not the portable contract.

Raw credentials are never exported.

---

## Reliability principles

- **Bounded conversation/tool loops.**
- **Fail closed** on ambiguous authorization/reference/credential state.
- **Canonical before act.**
- **Read tools for current facts instead of stale guessing.**
- **External evidence is inert data.**
- **One-time approval for consequential actions.**
- **Global read-only kill.**
- **Check before act, verify after act.**
- **Recoverable Loren-owned state.**
- **Memory provenance and anti-poisoning.**
- **Credential revocation overrides approval.**
- **Migration fidelity and Windows SQLite coverage.**

---

## Current accepted decisions

- **ADR-001:** Loren-owned core with replaceable adapters.
- **ADR-002:** .NET 10 / ASP.NET Core / Loren-owned bounded loop / provider-neutral `IBrain` / SQLite+EF Core / owner web UI / xUnit baseline.
- **ADR-003:** opaque canonical IDs, EF migration policy, Project/Repository boundary, memory source classes, append/supersede correction, memory-vs-audit deletion distinction, logical export versioning.
- **ADR-004:** typed write intent, canonical authorization, explicit exact owner approval, non-replay, global read-only, credential isolation/revocation, post-write verification, redacted correlated audit.

---

## Current milestone

```text
M1–M4 foundation                              ✓
Gate D + M5 write-safety Slices 1–3           ✓
M6A.1 conversation primary surface            ✓
M6A.2 current-information / web search         <- ACTIVE
M6A.3 source-aware research / web fetch
M6A.4 Notes / Decisions / Tasks
M6A.5 conversational approval
        |
        v
OWNER v0.1 TEST CHECKPOINT
```

Additional file/commit/PR mutation work stays paused until that owner checkpoint is usable.
