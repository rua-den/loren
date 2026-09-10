# Loren Thread Handoff

Updated 2026-09-10. Read `docs/status.md`, this file and `docs/plans/2026-09-10-continuity.md`.

## Current checkpoint

- Main baseline: `e9e8165`, PR #35 appsettings fallback merged; 183 tests and CI #263 passed Windows/Ubuntu. UI/theme PR #34 is also merged.
- Active branch: `codex/conversation-continuity`.
- Owner approved: checkpoint cleanup, persistent conversation history, Windows one-click launcher, export/restore, then deterministic reliability coverage. Execute sequentially with Luna medium writing code and parent reviewing/testing.
- Owner has no time for live acceptance now. Defer that work without calling M6A.5 or v0.1 complete.

## Existing capabilities

Authenticated chat-first UI with White, Graphite-Black and Graphite-Cyan themes, safe Markdown, project context, trusted durable memory, web search/fetch and research, durable notes/decisions/tasks, user-triggered catch-up and conversational create-branch proposals.

GitHub creation remains proposal -> explicit owner decision -> one-time approval -> purpose-bound credential -> execute -> independent verification -> redacted audit. Request text and history never grant approval. M5 Slices 4-6 remain paused until owner acceptance; this does not restrict developer Git/PR workflows.

## Setup

Read README and `docs/owner-checkpoint.md`. Standard appsettings.json is supported; optional ignored appsettings.Local.json overrides base JSON, environment overrides JSON, CLI overrides environment. The example file is not loaded automatically. Local JSON requires restart and is excluded from build/publish output. The owner prefers JSON configuration. Do not inspect or print secret values; keep any real-key config untracked. `.env` is not automatically loaded.

Default provider: Ollama gpt-oss:120b at https://ollama.com/api/chat. OLLAMA_API_KEY also supports web search/fetch. Git developer SSH is separate from GITHUB_WRITE_TOKEN used by Loren's HTTP write client.

## Next actions

Follow the approved continuity plan and its ledger. Use temporary databases and deterministic providers for technical verification. Preserve themes, chat-first navigation, canonical identity, approval isolation and credential redaction. Do not add streaming, task board, desktop wrapper, voice or scheduler to this scope.

Later owner acceptance: normal chat/catch-up quality, current sources/research, memory/tasks across restart and M6A.5 Cancel -> fresh proposal -> Approve -> independent GitHub SHA proof. No automatic product mutation during development substitutes for the owner's explicit approval.

## Developer workflow

- Code/refactors: gpt-5.6-luna, medium. Orchestrator reviews and independently tests. No secret values in docs, tests, logs or commits.
- Commit identity: turtle <nhkhuy241@gmail.com>. Personal SSH alias github-personal. Port 443 fallback uses ssh.github.com and HostKeyAlias=github.com with host-key verification.
- Parent may push/create PR/merge after review and green CI, as already authorized.
- RTK was blocked by Windows group policy; direct commands are the documented fallback. Agent tests may hit error1260; parent approved execution has worked.
- Changed C# must be UTF-8 LF. Historical full-format issues were checkout line endings, not approval to ignore new failures.
- Historical branch cleanup removed 40 integrated remote branches. Do not repeat cleanup from stale branch names; inspect current refs. Local recovery bundle and audit remain under .git.

The optional localhost:5093 UI fixture uses simulated data and is not a fresh-clone feature or live-provider evidence.

## WIP checkpoint — continuation required

Owner explicitly requested pushing unfinished work before usage runs out so another AI can continue. This is a development checkpoint, NOT a passing implementation or merge candidate.

Branch: `codex/conversation-continuity`. Last green main: `e9e8165` (PR #35, 183 tests). All work below is based on that baseline.

### Implemented but not fully verified

- Authenticated conversation store/API/UI with SQLite migration `202609100001_AddConversations`, sequence ordering, latest-200 message window, per-conversation overlap gate, selected conversation restoration and HTTP/store tests.
- Windows `Start-Loren.cmd` + `scripts/Start-Loren.ps1`: repo SDK selection, implicit restore/build, direct DLL host process, loopback 5091, readiness/login marker, no-browser/dry-run/smoke modes. Standard appsettings.json and Local.json excluded from Git/build/publish.
- Recovery draft: `LogicalStateRecovery`, audit row/composite sink/migration `202609100002_AddRetainedAudit`, `Loren.Maintenance` CLI project and initial roundtrip/refusal tests.

### Latest verification and known failures

- Parent launcher dry-run from outside repo passed using Windows PowerShell. Real launcher smoke has NOT passed yet; earlier agent attempt was blocked reading NuGet config.
- First parent solution run: 185 total, 66 failed due to Sequence column on the wrong migration table. Luna corrected the migration.
- Next parent run: 192 total, 74 failed, primarily EF PendingModelChangesWarning; drift test reported AlterColumnOperation. This still requires diagnosis and a fresh passing run. Do NOT suppress the warning.
- Latest parent `dotnet restore Loren.slnx` succeeded. Following Release test build failed: `LogicalStateRecovery.cs(133,133): CS0103 ProjectAlias does not exist in current context`. Recovery agent hit usage limit before completing verification.
- Local diagnostic logs: ignored `artifacts/continuity-tests.log`, `continuity-second-tests.log`, `continuity-third-tests.log`, `continuity-restore.log`; these logs are not included in Git, so reproduce using commands below.
- No current feature has been merged or declared complete. No live provider or owner approval acceptance was performed.

### Resume in this order

1. Read this checkpoint, inspect git status, and delegate code to Luna medium. Fix unresolved alias normalization reference using actual domain helper; do not invent incompatible normalization.
2. Run solution build/test. Diagnose EF drift with detailed AlterColumn table/column/nullability output, align explicit model configuration and migrations. Existing dynamic snapshot calls CanonicalStateModel.Configure; do not hide drift with EnsureCreated or warning suppression.
3. Review/test history: refresh restores active conversation; project options load before restoration; list buttons re-enable; post-send refresh does not remove fresh proposal cards; asynchronous selection cannot append a response into another conversation. Existing history stays untrusted and old proposal text is never executable. Check provider failure/cancellation gate release and owner isolation. Last implementation only returns latest 200 messages and lists 100 conversations, no older paging.
4. Review/test recovery: exact format_version=1 validation (missing version must fail), complete field/domain/reference validation, consistent snapshot export, readonly source, absent destination + atomic publish, restore through migrations, restart after restore, audit preservation, revoked approvals and pending-only cancellation preserving terminal proposal status. Audit before durable sink cannot be reconstructed. Archive excludes runtime credential/config stores, but user-authored content remains private. CLI needs tests as well as service tests; docs/recovery.md still missing.
5. Run `powershell.exe -NoProfile -File scripts/Start-Loren.ps1 -SmokeTest -NoPause` with free port 5091. It must use temporary data, no browser and stop its own host; test foreign port conflict/reuse separately. Inspect temp deletion containment checks before running.
6. Run full solution tests and changed-file format; inspect complete diff. Update ledger/docs with evidence, then push PR and merge only after green CI. Keep this checkpoint on a feature branch/draft until then.

User authorizes developer push/PR/merge after review+green CI; this does not authorize product approval actions. User has no time for live acceptance now. Do not spend tokens redoing merged UI/appsettings or expand GitHub writes/desktop/voice/scheduler scope.
