# Loren Memory Model

**Status:** Active v0.1 baseline — Gate C / ADR-003 accepted.

Loren's memory provides continuity without turning every conversation into an unstructured transcript dump. The storage/lifecycle rules that M4 must obey are locked in [`ADR-003`](decisions/003-canonical-state-and-memory-lifecycle.md).

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

M4 must implement at least:

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
- `VERIFIED_TOOL` — authoritative for the external fact at verification time, but cannot create owner preference/policy/approval.
- `OWNER_APPROVED_INFERENCE` — inferred state explicitly approved by the owner; retains inference + approval provenance.
- `MODEL_INFERENCE` — low-authority hypothesis/candidate, never silent owner truth.
- `EXTERNAL_CONTENT` — untrusted retrieved content; cannot promote itself into trusted personal memory or policy.

Every durable record needs source class, creation time, subject/scope, and source reference where applicable.

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

Current-truth retrieval ignores superseded records by default, while retained history remains reconstructable. Model inference or external content cannot supersede owner-authoritative state without an explicit promotion/correction path.

Exact M4 column names are not locked; the semantic behavior is.

## Forgetting / deletion

Memory deletion and audit retention are different operations.

A memory forget/delete must remove the record from future context/retrieval and may physically purge the memory payload according to policy. It must not silently cascade-delete unrelated audit history.

Audit is append-oriented evidence and is removed/redacted only through an explicit audit-retention/privacy operation. Audit payloads should minimize sensitive content so retained history does not become a secret archive.

## Retrieval

Retrieval should combine explicit entity references, lexical/semantic match, recency, scope, importance, and source authority. Embeddings may help later but are not the authority model.

The brain receives a small ranked context package with provenance rather than direct database access or a giant memory dump.

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

but secret material belongs in a dedicated secret store/executor boundary.

## Export direction

Portable state export is a Loren-owned logical versioned format, not the raw SQLite layout. `format_version = 1` is the first planned contract; schema migration IDs and export format versions are separate.

Export preserves canonical IDs and excludes raw secrets. Exact physical packaging remains an M7 implementation decision as long as the ADR-003 logical manifest/version contract is preserved.

## Deliberately unresolved for M4 implementation

These choices are still reversible and should be driven by actual retrieval behavior:

- whether/when a vector index is worth adding;
- ranking formula and memory context budget;
- episode summarization/compaction strategy;
- whether some memory categories need owner-confirmation UX by default;
- exact SQLite table/column/index design for `MemoryRecord`.

None of those unresolved choices may weaken the trust/source, correction, deletion, or canonical-ID rules accepted at Gate C.
