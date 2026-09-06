# Loren

**English** · [Tiếng Việt](README.vi.md)

Loren is a long-lived personal secretary / intelligence system with persistent memory, current-information tools, explicit permissions, and eventually voice/proactive behavior across the owner's digital life.

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
3. **Tool-first for external facts** — current facts come from authoritative read tools instead of model guessing.
4. **Read before write** — integrations prove useful read-only behavior before broad mutation.
5. **Permission-first** — a model may request an action; Loren authorizes and executes it.
6. **Model-independent** — model providers are replaceable adapters.
7. **Auditable** — consequential behavior must be reconstructable.
8. **Progressive autonomy** — scheduling/voice/proactive behavior comes only after lower trust boundaries are proven.

## Current status

**Last updated:** 2026-09-06  
**Phase:** `v0.1 — Useful Trustworthy Assistant`  
**Completed:** `M1–M4`, `Gate D`, `M5 write-safety Slices 1–3`, `M6A.1 conversation primary surface`  
**Active:** `M6A.2 — Current-information / web read`  
**Paused:** `M5 file/commit/PR write expansion` until the owner interaction checkpoint is usable

Detailed status: [`docs/status.md`](docs/status.md). Fresh-thread continuation: [`docs/handoff.md`](docs/handoff.md).

## What is already proven

### Conversation-first surface — M6A.1

PR #29 moved the owner experience back to Loren itself:

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

Low-level bootstrap and create-branch proof forms now live under **Advanced / safety harness**.

Evidence:

```text
PR #29 merge a1652b2451fe2e706aa83373932b210178f63ebe
PR CI #224 / 34042192552 PASS Ubuntu + Windows
post-merge CI #225 / 34042352724 PASS Ubuntu + Windows
```

### Canonical context + durable memory

M3 gives Loren-owned Project/Repository IDs and aliases independent of provider/session identity. M4 proves owner memory survives restart, supports correction/supersession and forgetting, retains provenance, and resists model/external-content self-promotion.

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

The first write proof is verified creation of a **non-default GitHub branch**. That capability remains available only as a narrow proof; expanding GitHub writes is paused.

## Current execution — M6A

### M6A.1 — Conversation primary surface [COMPLETE]

- conversation is the default owner surface;
- friendly project selection and deterministic project inference;
- bounded user/assistant history;
- trusted project memory participates in normal chat;
- system-role injection from browser history is rejected;
- tool/audit activity is secondary.

### M6A.2 — Current-information / web read [ACTIVE — PR #30]

Adds read-only `web.search` using Ollama Web Search and the existing `OLLAMA_API_KEY`.

```text
current question
 -> brain chooses web.search
 -> ActionGateway READ policy
 -> bounded Ollama web search
 -> validated source URLs + bounded evidence
 -> evidence marked untrusted external data
 -> Loren synthesizes a sourced answer
```

The implementation rejects unsafe/overlong source URLs, bounds query/result/content size, fails closed when the search credential is missing, and never surfaces provider failure bodies or secrets.

### M6A.3 — Source-aware research [NEXT]

Multiple searches/sources, deeper page fetch where useful, comparison/deduplication, stale/conflict handling, and clear distinction between sourced facts and Loren inference.

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
 -> Loren resolves exact target
 -> conversation shows exact proposal
 -> owner approves
 -> existing Gate D boundary executes
 -> branch is independently verified
 -> Loren reports completion naturally
```

No additional GitHub mutation primitive is required for this checkpoint.

## v0.1 owner test milestone

The next pull specifically for product testing happens when Loren can:

```text
1. Chat normally.
2. Answer stable knowledge/reasoning questions.
3. Retrieve current information with sources.
4. Do bounded source-aware research.
5. Combine project context + memory + live read data.
6. Store/recall a durable fact or decision across restart.
7. Create/list/complete tasks through chat.
8. Propose a branch action in natural language.
9. Show exact approval, then execute + verify after approval.
10. Explain what happened with audit context.
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

`OLLAMA_API_KEY` powers both the Ollama brain cloud endpoint and the current-information web-search endpoint. `LOREN_OLLAMA_WEB_SEARCH_ENDPOINT` is optional and defaults to `https://ollama.com/api/web_search`.

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
