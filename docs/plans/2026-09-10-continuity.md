# Continuity and local readiness

Approved by owner on 2026-09-10. Code implementation goes to Luna medium; parent reviews and verifies each slice. Real-provider acceptance is deferred until the owner has time.

## Sequence

1. Refresh status/handoff/AI onboarding around merged main and this sequence.
2. Persist authenticated conversations in SQLite; list/select/new conversation; reload and restart continuity. Keep history untrusted and approval authority server-owned. Bound history and handle concurrent submissions.
3. Add a Windows one-click launcher for the existing web host, browser opening and actionable missing-configuration errors. No desktop rewrite or per-launch environment setup.
4. Implement versioned logical export/restore for canonical state, memory, organization, conversation history and retained security records. Exclude credentials; restore into an empty isolated target, never overwrite owner data. Restored approvals/proposals must not become executable grants.
5. Close deterministic reliability coverage: restart, provider/tool failure, cancellation, read-only/revocation, replay and recovery. Record remaining live-provider/owner checks honestly.

## Verification

Each slice: parent diff review, relevant tests, changed-file format. Final integration CI before merge. Use temporary databases and deterministic providers; do not consume real provider credentials or create external product mutations.

## Ledger

- Baseline: main e9e8165, PR #35 merged; 183 tests and CI #263 passed. UI PR #34 merged previously.
- Checkpoint refresh: in progress.
- Conversation persistence: delegated to Luna; implementation in progress.
- Launcher, recovery, reliability: pending in the order above.
- Deferred: streaming, task board, desktop wrapper, voice, scheduler; broader GitHub writes await owner checkpoint.

- WIP push requested by owner: history/launcher/recovery drafts exist; latest build fails unresolved ProjectAlias, previous EF drift unverified. See docs/handoff.md WIP section. No feature completion or merge claim.
