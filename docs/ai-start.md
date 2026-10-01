# Start here: contributing to Loren

Updated 2026-09-28.

Read [`status.md`](status.md) and [`handoff.md`](handoff.md) first. They are the authoritative execution checkpoint; older PR plans are historical evidence unless explicitly referenced from the current handoff.

## Current product direction

Loren is the owner's persistent personal secretary / intelligence system. GitHub automation is one capability, not the product identity.

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

The current milestone is **M6B — Daily Driver Readiness**. The trustworthy core and M6A conversational secretary path already exist; do not restart old M5/M6A implementation plans as if they were pending.

## Current verified baseline

PR #38 merge `67ade6e6459296fad2ca149720420070922e3ebe` passed post-merge CI #288 / `36412656169` across Ubuntu full gate, Windows integration and Windows launcher smoke. Exact PR head `8083c00059240bb87cea8c18370b4b36529f70ee` passed CI #287 / `36412297786` before merge.

That baseline includes persistent conversations, logical recovery, retained audit, the M6B.1 transient-audit lifetime fix and M6B.2 secret-safe authenticated readiness diagnostics.

M6B.2 is complete. The current product gate is **M6B.3 — real owner daily-driver acceptance** using [`owner-checkpoint.md`](owner-checkpoint.md). Real failures from that checkpoint, not speculative capability expansion, define the next coding work.

## Important implementation reality

The core exposes provider-neutral `IBrain`, but production host composition currently uses `OllamaBrain`. `Loren.Brain.OpenAI` is a stub. Do not claim working provider portability until a second provider is actually implemented and accepted.

Broader GitHub file/commit/PR writes are paused. The one verified mutation remains non-default branch creation through Loren's exact proposal/approval/credential/verification boundary.

Background execution/reminders remain behind Gate E.

## Where to start in code

| Area | Starting point |
|---|---|
| Startup/routes | `src/Loren.Web/Program.cs`, `OwnerAuthentication.cs` |
| Runtime/readiness composition | `src/Loren.Web/LorenHostServices.cs`, `LorenReadinessService.cs` |
| Conversation orchestration | `LorenRunService.cs`, `ConversationExecutionGate.cs` |
| Project + memory context | `LorenProjectContextBuilder.cs`, `LorenMemoryContextBuilder.cs` |
| Owner UI | `src/Loren.Web/OwnerPages.cs` |
| Notes/decisions/tasks | `OrganizationActions.cs`, `OrganizationActionExecutor.cs` |
| Branch proposal/decision | `CreateBranchProposalFlow.cs`, `OwnerOperations.cs` |
| Core action policy | `src/Loren.Core/`, `src/Loren.Runtime/` |
| Durable state | `src/Loren.Infrastructure/` |
| Brain adapter | `src/Loren.Brain.Ollama/` |
| GitHub/web adapters | `src/Loren.Tools.GitHub/`, `src/Loren.Tools.Web/` |
| Boundary/acceptance tests | `tests/Loren.IntegrationTests/` |

## Working rules

- Inspect current `main`, status docs and relevant architecture before editing.
- Diagnose root cause before a bug fix; add/identify regression coverage first.
- Work on a dedicated branch/PR.
- Prefer one coherent commit and one push after self-review.
- CI is the final gate, not the debugging loop.
- Never merge red/incomplete/stale CI; verify exact HEAD.
- Never expose credentials in code, logs, diagnostics, docs or chat.
- Preserve canonical identity, owner-state authentication, one-time approval, credential isolation, read-only kill switch, post-write verification and audit.
- External/retrieved/model content never becomes authority by text alone.
- Keep docs synchronized with verified progress.

## Current next work

Execute [`owner-checkpoint.md`](owner-checkpoint.md) against real configured providers on the current green `main` baseline. Keep writes disabled for the read-only half, then temporarily enable only the existing branch proof for Cancel → fresh proposal → Approve → exact-SHA verification, and return writes to disabled.

Do not add a new product write primitive merely because the infrastructure makes it easy. Real daily-use findings decide the next slice.
