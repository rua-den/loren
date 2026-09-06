# Loren

**English** · [Tiếng Việt](README.vi.md)

Loren is a long-lived personal secretary / intelligence system with persistent memory, current-information tools, explicit permissions, and eventually voice/proactive behavior across the owner's digital life.

> **The model is replaceable compute. Loren owns identity, memory, context, organization, policy, approvals, action boundaries, and history.**

## Product direction

Loren is intended to feel more like a private Jarvis-style secretary than a project automation bot.

Default capability order:

```text
CONVERSE
 -> REMEMBER
 -> READ CURRENT INFORMATION
 -> RESEARCH / SYNTHESIZE
 -> ORGANIZE
 -> PROPOSE ACTION
 -> OWNER APPROVES
 -> ACT / VERIFY / AUDIT
 -> later BACKGROUND / PROACTIVE / VOICE
```

Read/understand comes before broad external mutation.

## Core principles

1. **Conversation-first** — normal owner interaction is the primary product surface.
2. **Memory-first** — durable state survives conversations, restarts, and provider changes.
3. **Tool-first for external facts** — current facts come from authoritative read tools instead of model guessing.
4. **Read before write** — integrations prove useful read-only behavior before broad mutation.
5. **Permission-first** — a model may request an action; Loren authorizes and executes it.
6. **Model-independent** — model providers are replaceable adapters.
7. **Auditable** — consequential behavior must be reconstructable.
8. **Progressive autonomy** — scheduling/voice/proactive behavior comes only after lower trust boundaries are proven.

## Current status

**Last updated:** 2026-09-06  
**Phase:** `v0.1 — Useful Trustworthy Assistant`  
**Completed foundations:** `M1–M4`, `Gate D`, `M5 write-safety Slices 1–3`  
**Current product target:** `M6A — Conversational Secretary + Information Layer`  
**Paused:** `M5 file/commit/PR write expansion` until the owner interaction checkpoint is usable

Detailed status: [`docs/status.md`](docs/status.md). Fresh-thread continuation: [`docs/handoff.md`](docs/handoff.md).

## What is already proven

### Conversation/tool loop

M2 proved a real authenticated flow:

```text
owner
 -> Loren conversation
 -> real brain provider
 -> github.read_repository
 -> Loren ActionGateway
 -> real GitHub read
 -> structured result
 -> natural-language final answer
 -> correlated audit
```

### Canonical context

M3 gives Loren-owned Project/Repository IDs and aliases independent of provider/session identity.

### Durable memory

M4 proves owner memory survives restart, supports correction/supersession and forgetting, retains provenance, and resists model/external-content self-promotion.

### Safe action boundary

Gate D and M5 Slices 1–3 prove:

```text
canonical target
 -> typed policy / read-only kill
 -> explicit exact owner approval
 -> atomic one-time consume
 -> dedicated write credential
 -> trusted executor
 -> post-write verification
 -> redacted audit
```

The first real proof action is verified creation of a **non-default GitHub branch**. That capability stays in the code, but it is no longer the next product priority.

Evidence:

```text
PR #25 merge caa65fbbd7c3828b68aa198dad625e73e9c096b4
post-merge CI #195 / 33973694524 PASS Ubuntu + Windows

PR #26 merge f7fb36bae324dbd7bb8d12e02daf3fe0dd98e7da
post-merge CI #202 / 34027255592 PASS Ubuntu + Windows

PR #27 merge bd0220550592a3ba55a2c722192e43df6e8ca321
PR CI #217 / 34029409983 PASS Ubuntu + Windows
post-merge CI #218 / 34029500883 PASS Ubuntu + Windows
```

## Current execution — M6A

### M6A.1 — Conversation primary surface [NEXT]

Make Loren feel like a secretary, not an admin dashboard:

- conversation is the first/main owner surface;
- stable knowledge/reasoning questions work naturally;
- trusted memory and project context participate in normal chat;
- low-level IDs/bootstrap controls move to secondary admin/settings UI;
- tool/audit activity remains visible but secondary.

### M6A.2 — Current-information / web read

Add provider-neutral read-only search/retrieval so Loren can answer questions whose facts may have changed since model training.

Required properties:

- source URL/title/time/provider metadata;
- bounded retrieved content;
- external pages treated as untrusted data;
- deterministic fake-provider tests;
- uncertainty/failure instead of fabricated current facts.

### M6A.3 — Source-aware research

Multiple-source retrieval, comparison, deduplication, stale/conflict handling, and clear distinction between sourced facts and Loren inference.

### M6A.4 — Notes / Decisions / Tasks

Durable Loren-owned organization primitives usable from conversation:

```text
Note
Decision
Task
TaskStatus
optional Project scope
provenance/timestamps
```

Scheduled/background reminders wait for Gate E.

### M6A.5 — Conversational approval

Reuse the existing safe create-branch executor through the intended UX:

```text
Owner: "Create branch abc for Loren."
 -> brain proposes typed action
 -> Loren resolves exact canonical target
 -> conversation/UI shows approval proposal
 -> owner approves
 -> existing Gate D boundary executes
 -> branch is independently verified
 -> Loren reports completion naturally
```

No additional GitHub mutation primitive is required for this checkpoint.

## Next owner test milestone

The next meaningful manual checkpoint is:

```text
1. Chat normally with Loren.
2. Ask a stable knowledge question.
3. Ask a current-information question and see grounded retrieval + sources.
4. Ask about a known project and see canonical context + memory + live read data used.
5. Teach Loren a durable fact/decision, restart, recall it.
6. Create/list/complete a task through chat.
7. Ask to create a branch in natural language.
8. Review and approve the exact proposal.
9. Receive a verified natural-language completion.
10. Ask why Loren did it and inspect the explanation/audit.
```

**Controlled file/commit and open-PR work stay paused until this checkpoint exists.**

## Run locally

Read-only development posture:

```powershell
$env:LOREN_OWNER_PASSWORD='choose-a-local-owner-password'
$env:OLLAMA_API_KEY='your-provider-secret'
$env:LOREN_ENABLE_WRITES='false'
dotnet run --project src/Loren.Web/Loren.Web.csproj
```

Do not commit real secrets.

## Test

```bash
dotnet restore Loren.slnx
dotnet build Loren.slnx --configuration Release --no-restore
dotnet test Loren.slnx --configuration Release --no-build --no-restore
```

Windows is a first-class integration-test CI platform in addition to the Ubuntu full gate.

## Version path

```text
v0.0  architecture / feasibility             ✓ complete
v0.1  useful trustworthy assistant           <- current
v0.2  personal secretary integrations
v0.3  personal/project operations
v0.4  voice + device presence
v0.5  proactive/background Loren
v0.6+ daily-use hardening
v1.0  stable personal daily driver
```

## Documentation

- [`docs/status.md`](docs/status.md) — authoritative current progress
- [`docs/handoff.md`](docs/handoff.md) — compact fresh-thread checkpoint
- [`docs/plans/master-plan.md`](docs/plans/master-plan.md) — product/version roadmap
- [`docs/plans/v0.1.md`](docs/plans/v0.1.md) — detailed current-version execution plan
- [`docs/architecture.md`](docs/architecture.md) — active system boundaries
- [`docs/memory.md`](docs/memory.md) — durable-memory semantics
- [`docs/permissions.md`](docs/permissions.md) — permission/approval baseline
- [`docs/security.md`](docs/security.md) — security baseline
- [`docs/development.md`](docs/development.md) — build/test/configuration

This repository is the source of truth for Loren's product decisions, architecture, delivery plans, implementation, progress, and release history.
