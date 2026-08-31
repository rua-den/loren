# Loren Memory Model

**Status:** Proposed.

Loren's memory must provide continuity without turning every conversation into an unstructured transcript dump.

## Memory classes

### 1. Working memory

Short-lived context needed to complete the current conversation or task.

Examples:

- what "that file" refers to;
- current task plan;
- intermediate tool results;
- unresolved questions;
- temporary constraints.

Working memory may expire aggressively and should not automatically become durable memory.

### 2. Long-term semantic memory

Durable facts, preferences, policies, and stable relationships.

Examples:

- preferred communication style;
- project aliases;
- deployment preferences;
- known devices;
- recurring collaborators;
- stable technical decisions.

Semantic memory should be normalized when possible and linked to world-model entities.

### 3. Episodic memory

Records of significant events and experiences over time.

Examples:

- a deployment failed and how it was fixed;
- a project migrated storage backends;
- the owner rejected a recommendation;
- a permission exception was granted once;
- an automation produced an unexpected result.

Episodes are useful for questions like "what happened last time?" and for improving future behavior.

### 4. Procedural memory

Reusable operating knowledge learned or explicitly defined for the owner.

Examples:

- how this project should be released;
- how the owner prefers repositories to be initialized;
- which checks must run before production deployment;
- how to prepare a recurring weekly report.

Procedures should eventually be promotable into skills or explicit policies rather than remaining vague prose.

## Write policy

Loren should not save everything.

A candidate memory should be evaluated for:

- future usefulness;
- expected stability;
- confidence;
- sensitivity;
- duplication;
- source authority;
- whether the owner explicitly requested remembering it.

Low-confidence inferences should not silently become permanent facts.

## Proposed memory lifecycle

```text
Observation
   |
   v
Candidate memory
   |
   +--> discard (temporary/noisy)
   |
   +--> working memory
   |
   +--> durable memory
            |
            +--> semantic entity/fact
            +--> episodic record
            +--> procedural rule
```

## Retrieval

Retrieval should combine multiple signals rather than relying on embeddings alone:

- explicit entity references;
- recent conversation/task context;
- lexical search;
- semantic similarity;
- time relevance;
- project/person scope;
- importance;
- source authority.

The output to the model should be a small, ranked context package with provenance, not a giant memory dump.

## Personal world model

Durable memory should be linkable to stable entities.

Initial entity candidates:

```text
Owner
Person
Project
Repository
Device
Place
Service
Environment
Decision
Preference
Task
Event
Procedure
```

Each entity should have a stable internal ID and may have aliases. Human-readable names are not reliable identifiers.

Example:

```yaml
entity: Project
id: project_wedding_online
name: wedding-online
aliases:
  - wedding project
  - web dam cuoi
relations:
  repository: github:rua-den/wedding-online
```

The exact storage schema is intentionally deferred.

## Provenance

Every durable fact should ideally know:

- where it came from;
- when it was learned;
- confidence or authority;
- when it was last verified;
- whether it was explicitly stated by the owner or inferred.

This allows Loren to distinguish:

> "You told me this preference."

from:

> "I inferred this from three previous actions."

## Correction and forgetting

The owner must be able to:

- inspect important memories;
- correct them;
- delete them;
- mark them temporary;
- prevent specific categories from being stored.

Corrections should preserve audit history while ensuring the superseded value is not treated as current truth.

## Sensitive memory

Secrets, raw credentials, session tokens, private keys, and recovery codes should not be ordinary memory objects.

Memory may store a reference such as:

```text
credential_ref: secret/github/personal-token
```

but secret material belongs in a dedicated secret store.

## Open questions

- relational database plus vector index, or one unified store?
- how aggressively should Loren summarize old episodes?
- how are conflicting memories resolved?
- when should inferred preferences require owner confirmation?
- should memory mutations be git-versioned, database-versioned, or both?
- which subset should remain directly human-editable as Markdown/YAML?
