# Continuity and local readiness

Approved by owner on 2026-09-10. Real-provider acceptance remains deferred until the owner has time.

## Sequence

1. Refresh status/handoff/AI onboarding around merged main and this sequence.
2. Persist authenticated conversations in SQLite; list/select/new conversation; reload and restart continuity. Keep history untrusted and approval authority server-owned. Bound history and handle concurrent submissions.
3. Add a Windows one-click launcher for the existing web host, browser opening and actionable missing-configuration errors. No desktop rewrite or per-launch environment setup.
4. Implement versioned logical export/restore for canonical state, memory, organization, conversation history and retained security records. Exclude credentials; restore into an empty isolated target, never overwrite owner data. Restored approvals/proposals must not become executable grants.
5. Close deterministic reliability coverage: restart, provider/tool failure, cancellation, read-only/revocation, replay and recovery. Record remaining live-provider/owner checks honestly.

## Verification

Each slice requires diff review, relevant tests and formatting. Final integration CI is the merge gate. Use temporary databases and deterministic providers; do not consume real provider credentials or create external product mutations.

## Ledger

- Baseline: main `e9e8165`, PR #35 merged; 183 tests and CI #263 passed before continuity work began.
- Conversation persistence: implemented on draft PR #36.
- Windows launcher: implemented on draft PR #36; source review found no additional launcher defect. Real smoke remains pending on a suitable Windows checkout.
- Recovery: implementation present on draft PR #36; 2026-09-14 source review found lifecycle/validation/migration-path defects and prepared a corrective batch.
- Reliability: overlap/provider-failure coverage exists; the corrective batch adds migration-backed recovery, invalid-history rejection, inferred-project persistence and SQLite non-pooling for endpoint tests.
- EF drift: corrective batch removes the retained-audit `AUTOINCREMENT` annotation that existed only in the WIP migration/snapshot and not in the runtime model.
- Documentation: `docs/recovery.md` added; status/handoff synchronized to the source-reviewed, not-yet-CI-verified state.
- Deferred: streaming, task board, desktop wrapper, voice, scheduler; broader GitHub writes await the owner checkpoint.

## Current gate

The fix batch must land as one coherent commit on `codex/conversation-continuity`. After that exact HEAD exists, run restore/build/tests/format/dependency scan + Windows launcher smoke where available, then verify Ubuntu + Windows CI on that SHA. Do not merge PR #36 before all required checks are green.
