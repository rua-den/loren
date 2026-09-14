# Loren Project Status

**Last updated:** 2026-09-14  
**Current version:** `v0.1 — Useful Trustworthy Assistant`  
**Current branch:** `codex/conversation-continuity` / draft PR #36  
**Last known green main baseline:** `e9e8165`  
**Current verification state:** source-reviewed fix batch prepared; build/test/format/CI still pending on the new exact HEAD.

This file is the authoritative progress ledger. Read [`handoff.md`](handoff.md) next before continuing work.

## Current checkpoint — conversation continuity / launcher / recovery

PR #36 carries the approved continuity sequence: persisted authenticated conversations, Windows launcher, logical export/restore, then deterministic reliability hardening. It remains a draft and must not merge until the exact PR HEAD passes the repository gates.

### Implemented on the branch

- SQLite-backed authenticated conversations with list/select/new/restart continuity and latest-200 message reads.
- Per-conversation non-blocking execution gate so overlapping turns return conflict instead of running concurrently.
- Chat UI restore behavior that keeps historical proposal text non-executable.
- Windows `Start-Loren.cmd` + `scripts/Start-Loren.ps1` launcher with SDK selection, health/login readiness checks, smoke mode, foreign-port refusal and contained temp cleanup.
- Versioned logical recovery format + `Loren.Maintenance` export/restore CLI.
- Durable retained audit migration/sink.

### 2026-09-14 direct source review / fix batch

The code was reviewed directly instead of waiting on CI logs. The following concrete issues were identified and fixed in the prepared patch:

1. **Retained-audit EF drift** — the WIP migration/snapshot carried a SQLite `AUTOINCREMENT` annotation that the runtime model did not. The patch removes that extra annotation so the checked-in snapshot follows `CanonicalStateModel.Configure` without provider-only drift.
2. **Recovery proposal lifecycle** — restoring a pending proposal with a restore timestamp earlier than proposal creation could create `Cancelled` state with `DecidedAt < CreatedAt`. Restore now clamps the decision time to at least proposal creation.
3. **Recovery validation** — archive validation now reuses Core domain constructors for Project/Repository/Memory/Approval/Proposal/Organization invariants and rejects bad references, untrusted memory source classes, invalid conversation roles/sequences, non-normalized aliases and malformed retained audit rows.
4. **Recovery path fidelity** — recovery tests now create schemas through checked-in migrations rather than `EnsureCreated`, restart the restored database, and reload state through normal domain stores.
5. **Read-only export** — maintenance export opens the source SQLite database read-only with pooling disabled.
6. **Inferred project continuity** — when the context builder infers a project during a conversation turn, `/api/run` now persists that canonical alias with the conversation. Selecting automatic/no explicit project no longer discards inferred scope after restart.
7. **Execution-gate retention** — the old gate retained one `SemaphoreSlim` per conversation forever. It now tracks only currently active conversation IDs and removes them when the lease is disposed.
8. **Windows test SQLite handles** — authenticated endpoint tests re-register their test DbContext with `Pooling=False` so disposing the host releases the temporary `loren.db` before directory cleanup.
9. **Recovery runbook** — [`recovery.md`](recovery.md) documents export/restore semantics and the fail-closed security behavior.

### Verification status

Do not mark this batch complete yet.

- The current execution environment does not have a Loren checkout/.NET SDK suitable for running the solution locally.
- Static/source review has been performed against PR #36 HEAD `1d31fc2` before creating the fix commit.
- Existing regression coverage is being extended for migration-backed recovery, invalid conversation roles and inferred project persistence.
- Required next gates after the single branch update: restore, Release build, full tests, `dotnet format --verify-no-changes`, secret/dependency scans, launcher smoke, Ubuntu CI and Windows integration CI on the exact new HEAD.

## Proven product baseline

The green main baseline already includes:

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

## Required continuation

1. Inspect the exact new PR #36 HEAD after the single fix commit lands.
2. Run the full local gates when a suitable checkout is available.
3. Run launcher smoke on Windows using temporary data and no browser.
4. Let CI verify Ubuntu + Windows on that exact SHA.
5. Investigate all failures from that run before any follow-up push; batch fixes together.
6. Only after exact-head green verification, update evidence, mark the PR ready and merge.
7. After merge, verify main CI on the exact merge SHA.

No live provider credential or owner product approval is required for this technical continuity batch. Real-provider/owner acceptance remains a separate deferred checkpoint.
