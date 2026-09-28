# Start here: contributing to Loren

Updated 2026-09-19.

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

PR #37 merge `ba60246a410b257097ecd537f4d977b402a37b35` passed post-merge CI #280 / `35373858830` across Ubuntu full gate, Windows integration and Windows launcher smoke.

That baseline includes persistent conversations, logical recovery, retained audit and the M6B.1 transient-audit lifetime fix.

M6B.2 adds a safe authenticated readiness surface and rebaselines stale source-of-truth docs. Its merge gate remains exact-head CI + post-merge main CI.

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

Finish M6B.2 and then execute [`owner-checkpoint.md`](owner-checkpoint.md) against real configured providers.

Do not add a new product write primitive merely because the infrastructure makes it easy. Real daily-use findings decide the next slice.
