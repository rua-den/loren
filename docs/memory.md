# Loren Memory Model

**Status:** M4 implemented under Gate C / ADR-003.  
**Current next gate:** Gate D before M5 external writes.

Loren's memory provides continuity without turning every conversation into an unstructured transcript dump. The storage/lifecycle rules are locked in [`ADR-003`](decisions/003-canonical-state-and-memory-lifecycle.md) and were exercised end to end by M4.

## Memory classes

### Working memory

Short-lived context needed to finish the current conversation/task: current references, task plan, temporary constraints, intermediate results, unresolved questions. It may expire aggressively and does not automatically become durable state.

### Long-term semantic memory

Durable facts, preferences, policies, and stable relationships. It should be scoped to stable Loren entities such as Project/Repository where possible.

### Episodic memory

Significant events over time: failures/fixes, rejected recommendations, one-time exceptions, surprising automation outcomes. Episodes support questions such as "what happened last time?" without pretending every event is current truth.

### Procedural memory

Reusable operating knowledge: release procedure, required checks, owner-defined workflows. Mature procedures may later be promoted into explicit skills/policies rather than remaining vague prose.

## Durable source/trust classes

M4 implements:

```text
OWNER_EXPLICIT
OWNER_CORRECTION
VERIFIED_TOOL
OWNER_APPROVED_INFERENCE
MODEL_INFERENCE
EXTERNAL_CONTENT
```

These are contextual authority classes, not one universal confidence number.

- `OWNER_EXPLICIT` — owner-stated durable truth within a scope.
- `OWNER_CORRECTION` — owner correction; highest authority for the corrected owner-truth subject/scope.
- `VERIFIED_TOOL` — authoritative for the external fact at verification time, but cannot create owner preference/policy/approval and does not automatically remain current.
- `OWNER_APPROVED_INFERENCE` — inferred state explicitly approved by the owner; retains inference + approval provenance.
- `MODEL_INFERENCE` — low-authority hypothesis/candidate, never silent owner truth.
- `EXTERNAL_CONTENT` — untrusted retrieved content; cannot promote itself into trusted personal memory or policy.

Every durable record carries source class, canonical ID, creation/update time, subject/scope, and provenance/source reference when needed for trust semantics.

## Write policy

Loren does not save everything. Candidate memory is evaluated for future usefulness, expected stability, sensitivity, duplication, source authority, and whether the owner explicitly asked to remember it.

Low-authority model/external content does not silently become durable owner truth.

```text
Observation
   |
   v
Candidate
   |
   +--> discard / working memory
   |
   +--> durable record with provenance + authority
```

The current normal `LorenRunService` path is memory-read-only: M4 adversarial acceptance verifies a normal model turn does not silently call `AddAsync`, `CorrectAsync`, or `ForgetAsync`.

## Correction and supersession

Corrections are append/supersede rather than silent destructive rewrites.

```text
MemoryRecord A (current)
    |
owner correction
    v
MemoryRecord B (current)
A -> superseded_by B
```

`CorrectAsync(...)` requires:

- an existing current target;
- a new canonical `MemoryRecordId`;
- `OWNER_CORRECTION` source authority;
- the exact same Project/Repository scope;
- non-regressing lifecycle time;
- no duplicate correction ID.

The replacement insert and old-record supersession occur in one SQLite transaction. Current-truth retrieval ignores superseded records by default, while retained history remains reconstructable. Model inference or external content cannot use the owner-correction boundary.

## Forgetting / deletion

Memory deletion and audit retention are different operations.

M4 implements explicit `ForgetAsync(currentMemoryRecordId)` semantics. For a correction chain:

```text
A -> B -> C(current)
```

forgetting C:

1. requires C to exist and still be current;
2. reverse-walks the same-scope linear correction chain;
3. validates expected supersession edges;
4. physically deletes A, then B, then C inside one SQLite transaction;
5. rolls back on malformed/stale/concurrent history changes.

Purging the whole chain prevents an older corrected claim from resurrecting as current truth. Restart acceptance proves forgotten records stay absent while unrelated memory remains.

A memory forget does **not** silently cascade-delete audit history. Audit retention/redaction/purge is a separate future privacy/retention operation. Audit payloads should minimize sensitive content so retained evidence does not become a secret archive.

## Retrieval and prepared context

The current v0.1 memory path is deterministic and intentionally small:

```text
canonical Project
 -> IMemoryStore.ListCurrentForProjectAsync
 -> source/provenance eligibility
 -> deterministic authority ordering
 -> hard record/content/provenance bounds
 -> Loren-owned prepared memory package
 -> BrainContext
```

Default prepared-context eligibility:

```text
included when provenance semantics are valid:
  OWNER_CORRECTION
  OWNER_EXPLICIT
  OWNER_APPROVED_INFERENCE
  VERIFIED_TOOL

excluded by default:
  MODEL_INFERENCE
  EXTERNAL_CONTENT
```

Superseded records are already removed by current-record retrieval. Runtime and brain receive prepared application data, not EF/DbContext access.

Current ordering is used only to spend the context budget deterministically:

```text
OWNER_CORRECTION
OWNER_EXPLICIT
OWNER_APPROVED_INFERENCE
VERIFIED_TOOL
```

It is **not** a universal conflict-resolution score across unrelated fact types.

## Poisoning boundary

M4 treats the entire serialized memory payload as inert data:

```text
content
source/provenance reference
canonical IDs
scope
timestamps
```

None of those fields can act as instructions, permission, policy, or action authorization merely because they appear in prepared memory.

Adversarial acceptance proves:

- `MODEL_INFERENCE` / `EXTERNAL_CONTENT` remain excluded even when their provenance text looks owner-like;
- `OWNER_APPROVED_INFERENCE` and `VERIFIED_TOOL` require provenance to enter default trusted prepared context;
- malicious provenance/source-reference text is bounded before serialization;
- owner correction remains current owner truth while superseded and conflicting low-authority claims stay out;
- `VERIFIED_TOOL` remains source/time-scoped fact and cannot grant owner permission.

## Personal world model boundary

M3 deliberately implemented only the identity needed now:

```text
Project
Repository
```

Project aliases are canonical configured referents, not ordinary inferred memory. Repository locators identify where to fetch current external state; they do not make cached external facts current.

Do not add broad `Person`, `Task`, `Decision`, `Preference`, `Device`, or generic graph schemas until a real workflow requires them.

## Provenance

Durable memory should answer:

- where did this come from?
- when was it learned/verified?
- who/what had authority to assert it?
- what subject/scope does it apply to?
- has it been superseded?
- was an inference owner-approved or merely model-generated?

This supports clear distinctions such as:

> "You told me this."

versus:

> "I inferred this and have not treated it as owner truth."

## Sensitive memory

Secrets, raw credentials, session tokens, private keys, recovery codes, and session cookies are never ordinary memory objects.

Memory may eventually hold opaque references such as:

```text
credential_ref: secret/github/personal-token
```

but secret material belongs in a dedicated secret store/executor boundary. Gate D owns the next credential decisions.

## Export direction

Portable state export is a Loren-owned logical versioned format, not the raw SQLite layout. `format_version = 1` is the first planned contract; schema migration IDs and export format versions are separate.

Export preserves canonical IDs and excludes raw secrets. Exact physical packaging remains an M7 implementation decision as long as the ADR-003 logical manifest/version contract is preserved.

## Deliberately unresolved after M4

These choices remain reversible and should be driven by actual product behavior:

- whether/when a vector index is worth adding;
- richer lexical/semantic ranking beyond the current deterministic M4 package;
- episode summarization/compaction strategy;
- whether some memory categories need owner-confirmation UX by default;
- memory-management UI details in M6.

None of those unresolved choices may weaken the source authority, correction, forgetting, canonical-ID, provenance, or poisoning rules proven by M4.
