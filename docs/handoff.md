# Loren Thread Handoff

Updated 2026-09-14. Read `docs/status.md`, this file and `docs/plans/2026-09-10-continuity.md` before changing code.

## Current checkpoint

- Repository: `rua-den/loren`.
- Active work: draft PR #36, branch `codex/conversation-continuity`.
- Last pre-fix PR HEAD reviewed directly: `1d31fc297419c617055b953cd159bf30f2a2f966`.
- Last known green main baseline: `e9e8165`.
- Owner direction: finish continuity/recovery correctness first; CI is the final gate, not the development loop.
- Do not merge until the exact current PR HEAD is green.

## What this branch contains

1. Persistent authenticated conversations in SQLite, list/select/new/restart continuity and bounded history.
2. Conversation execution overlap protection.
3. Windows one-click launcher + smoke mode.
4. Logical recovery export/restore + maintenance CLI.
5. Retained durable audit.
6. Reliability/integration coverage for the above.

## 2026-09-14 source-review findings and fixes

A direct code review identified concrete issues independent of CI logs:

- retained-audit migration/snapshot had a SQLite `AUTOINCREMENT` annotation absent from the runtime model;
- pending proposal restore could write `DecidedAt` before `CreatedAt`;
- recovery tests used `EnsureCreated` instead of the production migration path;
- recovery validation allowed malformed domain/reference/history state;
- maintenance export did not explicitly open the source DB read-only;
- inferred project scope was returned by the run but not persisted to the conversation;
- `ConversationExecutionGate` retained a semaphore per conversation forever;
- WebApplicationFactory integration tests used pooled SQLite connections while deleting temp DB directories on Windows.

The prepared fix batch addresses these in one coherent commit and adds `docs/recovery.md`.

## Verification contract for the fix batch

The current environment cannot run the .NET solution locally, so do not claim local build/test/format success from this handoff. After the branch receives the single fix commit:

```text
dotnet restore Loren.slnx
dotnet build Loren.slnx --configuration Release --no-restore
dotnet test Loren.slnx --configuration Release --no-build --no-restore
dotnet format Loren.slnx --verify-no-changes --no-restore
dotnet package list --project Loren.slnx --vulnerable --include-transitive
```

On Windows also run:

```powershell
powershell.exe -NoProfile -File scripts/Start-Loren.ps1 -SmokeTest -NoPause
```

Then verify PR CI Ubuntu + Windows against the exact HEAD SHA. If CI fails, inspect the complete run, batch all necessary fixes, rerun relevant local gates, and make only one follow-up commit/push.

## Recovery invariants

- Restore uses checked-in migrations into a new empty target.
- Archive format version must be explicit and supported.
- Canonical IDs are preserved.
- Archive references/domain lifecycle must validate before rows are written.
- Only `user`/`assistant` conversation roles are restorable.
- Existing approvals restore revoked.
- Pending proposals restore cancelled and cannot gain executable authority.
- Already-terminal proposals remain terminal.
- Credentials/configuration are excluded.
- Audit that was never durably written cannot be reconstructed.

See `docs/recovery.md`.

## Product boundaries that must not regress

- Chat text is intent, never Gate D approval.
- Models never receive write credentials or trusted approval authority.
- Owner state is distinct from external writes.
- Existing verified `github.create_branch` remains the only product mutation primitive for this checkpoint.
- M5 file/commit/PR expansion, streaming, task board, desktop wrapper, voice and scheduler remain out of scope.

## Next action

Finish the single code/docs commit on `codex/conversation-continuity`, move the branch once, then inspect the exact-head CI. Do not merge PR #36 while any required check is red, incomplete, stale or unknown.
