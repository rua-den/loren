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

- Baseline before continuity: main `e9e8165`, PR #35 merged; 183 tests and CI #263 passed.
- Conversation persistence: implemented on PR #36, including inferred-project persistence and non-blocking per-conversation overlap protection.
- Windows launcher: implemented on PR #36; source review passed. Final PR CI now includes `Start-Loren.ps1 -SmokeTest -NoPause` on Windows so launcher verification is continuous rather than manual-only.
- Recovery: implemented on PR #36 with migration-backed restore tests, domain/reference/history validation, read-only export, revoked approvals and safe cancellation of restored pending proposals.
- Reliability: coverage includes restart continuity, overlap rejection/release, provider failure release, invalid archived role rejection, restored-domain reload and Windows SQLite non-pooling in endpoint tests.
- EF drift: retained-audit migration/snapshot was aligned with runtime integer-PK value generation without suppressing `PendingModelChangesWarning`.
- Documentation: `docs/recovery.md`, status/handoff and README EN/VI are synchronized with the verified continuity implementation.
- Verified code checkpoint: `11eb38eef3f4973b4d37e538dcbf16f92e8e6e2b` passed CI #276 / `34838365005` — Ubuntu full gate + Windows integration.
- Deferred: real-provider/owner acceptance, streaming, task board, desktop wrapper, voice, scheduler; broader GitHub writes await the owner checkpoint.

## Delivery gate

PR #36 may merge only after its final exact head passes Ubuntu full CI, Windows integration and Windows launcher smoke. After merge, verify CI against the exact main merge SHA before resuming product work.
