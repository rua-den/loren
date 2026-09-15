# Loren Project Status

**Last updated:** 2026-09-14  
**Current version:** `v0.1 — Useful Trustworthy Assistant`  
**Continuity delivery:** PR #36 / `codex/conversation-continuity`  
**Verified code checkpoint:** `11eb38eef3f4973b4d37e538dcbf16f92e8e6e2b`  
**Verification evidence:** CI #276 / run `34838365005` passed Ubuntu full gate + Windows integration on that exact code checkpoint.

This file is the authoritative progress ledger. Read [`handoff.md`](handoff.md) next before continuing work.

## Current checkpoint — conversation continuity / launcher / recovery

The continuity batch adds persisted authenticated conversations, Windows local startup, versioned logical export/restore, retained audit and deterministic reliability hardening without expanding Loren's product write authority.

### Implemented

- SQLite-backed authenticated conversations with list/select/new/restart continuity and latest-200 message reads.
- Per-conversation non-blocking execution gate so overlapping turns return conflict instead of running concurrently; active IDs are removed when leases end.
- Inferred canonical project scope persists with the conversation so restart does not lose auto-detected project context.
- Chat UI restoration keeps historical proposal text non-executable and does not allow an asynchronous response to land in another conversation.
- Windows `Start-Loren.cmd` + `scripts/Start-Loren.ps1` launcher with SDK selection, health/login readiness checks, smoke mode, foreign-port refusal and contained temporary-data cleanup.
- Versioned logical recovery format + `Loren.Maintenance` export/restore CLI.
- Durable retained audit migration/sink.
- [`recovery.md`](recovery.md) documents export/restore and fail-closed security behavior.

### 2026-09-14 direct source review / fixes

The implementation was reviewed directly before using CI as the final gate. The review found and fixed:

1. retained-audit EF migration/model metadata drift on `AuditEvents.Id`;
2. restored pending proposals able to receive a decision timestamp before creation;
3. recovery tests bypassing migrations through `EnsureCreated`;
4. incomplete recovery domain/reference/history validation;
5. maintenance export not explicitly opening SQLite read-only;
6. inferred project scope not being persisted to the conversation;
7. permanent per-conversation semaphore retention in `ConversationExecutionGate`;
8. pooled SQLite test connections retaining Windows file handles.

Recovery now validates domain state through Core constructors, rejects unsafe conversation roles/sequences and bad canonical references, revokes restored approvals, cancels restored pending proposals without violating lifecycle timestamps, preserves terminal proposal state, excludes credentials/configuration, and restores into a fresh migrated target.

### Verification evidence

Code checkpoint `11eb38e` passed CI #276 (`34838365005`):

```text
Ubuntu build-test
  restore                         PASS
  Release build                   PASS
  full solution tests             PASS
  dotnet format --verify          PASS
  basic secret scan               PASS
  dependency vulnerability scan   PASS
  web health/auth/surface smoke   PASS

Windows
  restore                         PASS
  integration tests               PASS
```

The current assistant execution environment did not contain a suitable Loren checkout/.NET SDK, so no local build/test pass is claimed. CI provided the unavailable platform/build verification.

The final PR verification head also adds the existing `Start-Loren.ps1 -SmokeTest -NoPause` path to Windows CI so launcher behavior is continuously verified rather than remaining a manual-only checkpoint. Merge is permitted only after that final exact head is green.

## Proven product baseline

```text
M1 Engineering Foundation                      complete
M2 Conversation/tool Walking Skeleton          complete
M3 Canonical Project/Repository State          complete
M4 Trusted Durable Memory                      complete
Gate D Action/Approval/Credential Policy       passed
M5 Slices 1–3 verified GitHub branch write     complete
M6A.1 conversation primary surface             complete
M6A.2 current-information web search           complete
M6A.3 source-aware bounded research            complete
M6A.4 Notes / Decisions / Tasks                complete
M6A.5 conversational approval                  merged; owner live proof pending
```

Core trust order remains:

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

The model never owns authorization, durable identity, credentials or approval. Broader GitHub file/commit/PR writes remain paused until the v0.1 owner checkpoint is usable. Gate E is still required before background scheduling/reminders.

## Continuation rule

If PR #36 is still open, verify the exact current head is green, then merge it. If PR #36 is already merged, verify main CI against the exact merge SHA before continuing. After continuity delivery, return to the deferred real-provider/owner acceptance checkpoint; do not expand GitHub mutation scope first.
