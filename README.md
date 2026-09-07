# Loren

**English** · [Tiếng Việt](README.vi.md)

Loren is a long-lived personal secretary / intelligence system with persistent memory, current-information and research tools, durable organization state, explicit permissions, and eventually voice/proactive behavior across the owner's digital life.

> **The model is replaceable compute. Loren owns identity, memory, context, organization, policy, approvals, action boundaries, and history.**

## Product direction

Loren should feel like a private Jarvis-style secretary, not a GitHub automation bot.

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
3. **Tool-first for current facts** — use read tools instead of stale model guessing.
4. **Read before write** — useful read behavior comes before broad mutation.
5. **Permission-first** — a model may request an action; Loren authorizes and executes it.
6. **Owner-state is distinct from external writes** — local notes/tasks require authenticated owner context but do not consume external-write approvals or credentials.
7. **Model-independent** — brain/search/tool providers are replaceable adapters.
8. **Auditable** — consequential behavior must be reconstructable.
9. **Progressive autonomy** — scheduling/voice/proactive behavior comes only after lower trust boundaries are proven.

## Current status

**Last updated:** 2026-09-07  
**Phase:** `v0.1 — Useful Trustworthy Assistant`  
**Completed:** `M1–M4`, `Gate D`, `M5 write-safety Slices 1–3`, `M6A.1`, `M6A.2`, `M6A.3`  
**Ready to merge:** `M6A.4 — Notes / Decisions / Tasks` via PR #32  
**Next:** `M6A.5 — Conversational approval`  
**Paused:** `M5 file/commit/PR write expansion` until the v0.1 owner checkpoint is usable

Detailed status: [`docs/status.md`](docs/status.md). Fresh-thread continuation: [`docs/handoff.md`](docs/handoff.md).

## What is already proven

### Conversation-first surface — M6A.1

```text
owner login
 -> conversation-first UI
 -> Loren identity
 -> bounded multi-turn history
 -> optional/inferred canonical project context
 -> trusted durable memory
 -> read tools
 -> natural answer
 -> secondary activity/audit
```

PR #29 merge `a1652b2451fe2e706aa83373932b210178f63ebe`; exact-head CI #224 and main CI #225 passed Ubuntu + Windows.

### Current information — M6A.2

Read-only `web.search` is available through the normal ActionGateway path and uses the existing `OLLAMA_API_KEY`. Search evidence is bounded, source URLs are validated, provider failure bodies/secrets are suppressed, and current claims can be grounded in returned sources.

PR #30 merge `a8d3e7bbc94c9a468ebc234deb1fe87dcb7d23e9`; exact-head CI #237 and main CI #238 passed Ubuntu + Windows.

### Source-aware research — M6A.3

```text
web.search
 -> selected sources
 -> web.fetch
 -> bounded page evidence
 -> compare / synthesize in bounded AgentLoop
 -> sourced facts + explicit inference
```

`PublicWebUrlPolicy` rejects unsafe schemes, credentials, localhost/private literal addresses and non-standard ports. External evidence remains inert data.

PR #31 merge `d789ccc7f7540cb802b14f677d317db3e571a7d3`; exact-head CI #240 / `34045079047` and main CI #241 / `34052513007` passed Ubuntu + Windows.

### Canonical context + durable memory

M3 gives Loren-owned Project/Repository IDs and aliases independent of provider/session identity. M4 proves owner memory survives restart, supports correction/supersession and forgetting, retains provenance, and resists model/external-content self-promotion.

### Safe external action boundary

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

The first real write proof is verified creation of a **non-default GitHub branch**. Broader GitHub writes remain paused.

## Current execution — M6A.4

PR #32 adds durable Loren-owned organization state:

```text
Note
Decision
Task
TaskStatus = Open | Completed
optional Project scope
provenance/timestamps
```

Conversation actions:

```text
organization.create_note
organization.record_decision
organization.create_task
organization.list
organization.complete_task
organization.reopen_task
```

The new `OwnerStateRead` / `OwnerStateWrite` classes are intentionally separate from public reads and external mutations. Owner state requires authenticated trusted owner context and trusted executors. It does not use `GITHUB_WRITE_TOKEN` or external-write one-time approval. ActionGateway independently rejects owner-state access when owner context is missing.

State is stored in SQLite through migration `202609070001_AddOrganizationItems`; task lifecycle and note/decision/task data survive restart. CI #247 / `34053503945` passed the Ubuntu full gate and Windows integration before this final documentation sync.

No background scheduling/reminder delivery is introduced; Gate E remains required for background execution.

## Next — M6A.5 conversational approval

Reuse the already-safe `github.create_branch` executor through the intended UX:

```text
Owner: "Create branch abc for Loren from main."
 -> Loren resolves exact canonical target + source SHA
 -> conversation shows exact proposal + risk
 -> owner explicitly clicks Approve
 -> exact one-time approval is created
 -> existing credential-bound executor runs
 -> branch state is independently verified
 -> Loren reports completion naturally + audit context
```

The chat message is intent, **not** Gate D approval. No new GitHub mutation primitive is required for this checkpoint.

## v0.1 owner test milestone

The next pull specifically for product testing happens only when Loren can:

```text
1. Chat normally.
2. Answer stable knowledge/reasoning questions.
3. Retrieve current information with sources.
4. Perform bounded source-aware research.
5. Combine project context + memory + live read data.
6. Store/retrieve durable notes or decisions across restart.
7. Create/list/complete tasks through chat.
8. Propose branch creation in natural language.
9. Show exact approval, then execute + verify only after approval.
10. Explain what happened with audit context.
```

**Controlled file/commit and open-PR work stay paused until this checkpoint exists.**

## Run locally

Read-only external-write posture:

```powershell
$env:LOREN_OWNER_PASSWORD='choose-a-local-owner-password'
$env:OLLAMA_API_KEY='your-provider-secret'
$env:LOREN_ENABLE_WRITES='false'
dotnet run --project src/Loren.Web/Loren.Web.csproj
```

`OLLAMA_API_KEY` powers the Ollama brain plus web search/fetch read paths. Optional trusted endpoint overrides:

```text
LOREN_OLLAMA_WEB_SEARCH_ENDPOINT
LOREN_OLLAMA_WEB_FETCH_ENDPOINT
```

`LOREN_ENABLE_WRITES=false` blocks external mutations; it does not disable authenticated local Notes / Decisions / Tasks.

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
