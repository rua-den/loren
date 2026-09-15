# Loren Thread Handoff

Updated 2026-09-14. Read `docs/status.md`, this file and `docs/plans/2026-09-10-continuity.md` before changing code.

## Current checkpoint

- Repository: `rua-den/loren`.
- Continuity delivery: PR #36 / branch `codex/conversation-continuity`.
- Verified code checkpoint: `11eb38eef3f4973b4d37e538dcbf16f92e8e6e2b`.
- CI #276 / run `34838365005`: Ubuntu full gate PASS; Windows integration PASS.
- The final verification head adds Windows launcher smoke to CI; merge only after that exact head is green.
- Owner direction: CI is the final gate, not the development loop. Diagnose from code first, batch fixes, then verify.

## What the continuity batch delivers

1. Persistent authenticated conversations in SQLite with list/select/new/restart continuity and bounded history.
2. Per-conversation overlap protection without permanent gate retention.
3. Inferred canonical project scope persisted across restart.
4. Windows one-click launcher + contained smoke mode.
5. Versioned logical recovery export/restore + maintenance CLI.
6. Retained durable audit.
7. Reliability/integration coverage for recovery, restart, provider failure, overlap and unsafe archived history.

## 2026-09-14 source-review fixes

Direct source review found and fixed:

- EF migration/model drift for retained-audit `AuditEvents.Id` value generation;
- pending proposal restore lifecycle corruption (`DecidedAt < CreatedAt`);
- recovery tests using `EnsureCreated` instead of checked-in migrations;
- incomplete recovery domain/reference/history validation;
- export source not explicitly read-only;
- inferred project scope returned by a run but not persisted to the conversation;
- one retained semaphore per conversation forever;
- pooled SQLite endpoint-test connections retaining Windows database handles.

The implementation also adds `docs/recovery.md`.

## Verification evidence

Exact code checkpoint `11eb38e` passed CI #276:

```text
Ubuntu: restore, Release build, full solution tests, format,
        secret scan, dependency vulnerability scan, web smoke — PASS
Windows: restore + integration tests — PASS
```

No local .NET build/test claim is made from this assistant runtime because it lacks a suitable Loren checkout/.NET SDK. That limitation is explicit rather than substituted with fake local verification.

The final PR head is expected to run the same gates plus:

```powershell
./scripts/Start-Loren.ps1 -SmokeTest -NoPause
```

inside Windows CI. This closes the last launcher-specific verification gap from the earlier WIP checkpoint.

## Recovery invariants

- Restore uses checked-in migrations into a new empty target.
- Archive format version must be explicit and supported.
- Canonical IDs are preserved.
- Archive references/domain lifecycle validate before rows are written.
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

## Continue from here

If PR #36 is still open: inspect its exact current SHA and required checks, merge only when all are green, then verify post-merge main CI on the exact merge SHA. If PR #36 is already merged: start from current main and post-merge CI evidence. After continuity delivery, return to real-provider/owner acceptance rather than broadening product write scope.
