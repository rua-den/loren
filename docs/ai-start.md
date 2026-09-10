# Start here: contributing to Loren

Updated 2026-09-10. Read [status.md](status.md), then [handoff.md](handoff.md). These are the current ledger and continuation instructions; older plans/reports are historical evidence.

## Locate the current work

- Repository: `rua-den/loren`.
- Current development branch: `codex/cyber-ui`. UI implementation through `a4d36de`, preceded by `8cc3cea`; checkpoint docs started at `ab0b6b1`.
- The pre-UI main baseline was M6A.5 merge `1cb4fd7` (PR #33). UI/theme integration is tracked in [PR #34](https://github.com/rua-den/loren/pull/34). After it is merged, fetch and use current main; until then use the PR head.
- Read `git status --short` and preserve others' changes. Give each concurrent coding task its own branch/worktree and clear file ownership. Do not let multiple agents edit `OwnerPages.cs` simultaneously.

## Product direction

Loren is a personal secretary you talk to and catch up with. Conversation comes first. Saved facts, decisions and tasks support continuity. Project selection, tool activity and setup live behind **Chi tiết**. GitHub proposals appear only when relevant and require explicit owner decisions.

Keep the existing web UI and local .NET host for now. A one-click launcher and desktop packaging are possible follow-ups, not implemented or approved architecture changes. No desktop framework has been selected. Do not start a rewrite based on the earlier Jarvis analogy.

## What exists and where

| Area | Starting point |
|---|---|
| Login/chat UI, themes, Markdown, approval cards | `src/Loren.Web/OwnerPages.cs` |
| Authenticated routes and startup | `src/Loren.Web/Program.cs`, `OwnerAuthentication.cs` |
| Conversation orchestration and model context | `LorenRunService.cs`, `LorenProjectContextBuilder.cs`, `LorenMemoryContextBuilder.cs` in `src/Loren.Web/` |
| Local notes/decisions/tasks | `src/Loren.Web/OrganizationActions.cs`, `OrganizationActionExecutor.cs` |
| Frozen proposals and owner decisions | `src/Loren.Web/CreateBranchProposalFlow.cs`, `OwnerOperations.cs` |
| Domain, runtime policy, persistence | `src/Loren.Core/`, `src/Loren.Runtime/`, `src/Loren.Infrastructure/` |
| Provider adapters | `src/Loren.Brain.Ollama/`, `src/Loren.Tools.GitHub/`, `src/Loren.Tools.Web/` |
| Acceptance and boundary tests | `tests/Loren.IntegrationTests/` |

## Current limits and next work

1. Review the UI with the owner; use [owner-checkpoint.md](owner-checkpoint.md) for real-provider acceptance. The optional localhost:5093 fixture is simulated UI data, not acceptance evidence.
2. Exercise chat, retrieval/research, saved memory/tasks and catch-up with real configured providers. `Bắt nhịp hôm nay` is a user-triggered chat request for saved tasks/decisions across projects; it is not an automatic summary or persisted chat history.
3. Complete the live M6A.5 Cancel → fresh proposal → Approve → independent GitHub SHA read-back proof.
4. Fix findings in bounded slices, with code review and relevant verification. Broader product writes stay paused until the owner checkpoint.

Persistent conversation history, a separate task board, streaming, automatic catch-up, desktop packaging and background delivery are **not implemented**. These are candidate future scopes, not concurrent assignments. Background execution requires Gate E; v0.1 release gates remain open.

## Working agreement

- Owner requests Luna (`gpt-5.6-luna`, medium) for code implementation/refactoring, Sol for orchestration/review. Keep expensive orchestration and repeated context small. If required delegation is unavailable, report it rather than silently changing models.
- Preserve authentication, canonical target resolution, frozen proposal SHA, one-time owner approval, credential isolation, verification and audit. Chat/model output never grants approval.
- Use `turtle <nhkhuy241@gmail.com>` for repo commits. Developer SSH alias `github-personal` is unrelated to Loren's `GITHUB_WRITE_TOKEN` API credential. Never put credentials in docs/chat/commits.
- `.env` is not automatically loaded. Runtime setup is documented in [owner-checkpoint.md](owner-checkpoint.md).
- C# files must be UTF-8 with LF. Windows checkout CRLF can cause format failures in untouched files; distinguish those from the actual diff.

Last verified UI head: 182/182 tests; changed-file format passed; browser checked themes/persistence, Markdown safety, draft preservation and responsive context toggling. Inspect PR #34 for current integration CI; live-provider success is not claimed.

## Return a useful handoff

Report branch/commit, changed files, commands and outcomes, remaining issues, and the next concrete action. Update status/handoff when the checkpoint changes. Label simulated evidence. Avoid overwriting another agent's work or claiming an unrun check passed.
