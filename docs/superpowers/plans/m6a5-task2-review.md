# M6A.5 Task 2 review

## Findings

`src/Loren.Core/Actions/CreateBranchProposal.cs:67`: 🔴 bug: any expiry later than creation is accepted, so `AddAsync` can persist a proposal valid for hours or days and the CAS guard will approve it after the required five-minute window. Require `expiresAt == createdAt.AddMinutes(5)` and add boundary tests at exactly five minutes and any shorter/longer duration.

`src/Loren.Infrastructure/CanonicalState/SqliteCreateBranchProposalStore.cs:16`: 🟡 risk: `AddAsync` accepts an already Approved/Cancelled proposal, allowing terminal proposal state to be inserted without the atomic decision path or matching `ActionApproval`. Reject any proposal whose status is not Pending or whose `DecidedAt` is non-null; test both terminal states and verify no row/approval is written.

`src/Loren.Infrastructure/CanonicalState/SqliteCreateBranchProposalStore.cs:79`: 🟡 risk: rollback uses the caller's possibly cancelled token, so cancellation during update/approval insertion can make `RollbackAsync` throw immediately and mask the original error before cleanup completes; `CancelAsync` has no equivalent catch/finally cleanup. Roll back with `CancellationToken.None` in a guarded catch/finally and always clear the change tracker on every commit, rollback, conflict and exception exit.

`tests/Loren.IntegrationTests/CreateBranchProposalStoreTests.cs:173`: 🟡 risk: the Approve/Cancel race asserts only returned statuses; it never checks the persisted terminal state or that an approval exists iff Approve won, so an orphan terminal proposal or stray approval would pass. Reload through a third independent context and assert final status matches the winner plus approval count is exactly one for Approved and zero for Cancelled.

`tests/Loren.IntegrationTests/CreateBranchProposalStoreTests.cs:84`: 🟡 risk: mismatch coverage exercises only fingerprint and expiry, leaving the required owner/action/project/repository, approval-time, five-minute lifetime, consumed and revoked rejection paths unproven. Add one mutation case per `ApprovalMatches` predicate and assert proposal remains Pending and no new approval row exists after each attempt; also add unknown ID, pre-creation decision and foreign project/repository `AddAsync` tests.

## Compliance notes

- The migration contains the bounded proposal columns, primary key, owner/status/expiry index, project/repository indexes, and `Restrict` foreign keys to both canonical tables. The delegated model snapshot is pre-existing repository behavior; no unrelated snapshot rewrite is needed.
- `ApproveAsync` performs the guarded Pending/owner/time update and approval insert in one SQLite transaction. Duplicate approval insertion rolls back the proposal transition, and the existing test checks that effect.
- Zero-row classification checks ownership before returning proposal details, then distinguishes replay, expiry and pre-creation mismatch. Wrong-owner responses do not disclose the proposal.
- The two-context Approve/Approve test uses a barrier, verifies one winner, reloads the final proposal, and counts the two candidate approval IDs. Its synchronization and side-effect assertions are meaningful.
- `ActionName` and `AccessClass` are bounded compile-time constants reconstructed by the domain rather than table columns. This is acceptable for this deliberately create-branch-specific store, but the report should say all variable frozen fields are persisted rather than claiming every exposed property is stored literally.

**Verdict:** changes required before Task 2 is accepted. The five-minute invariant and Pending-only insertion are behavioral requirements; the rollback and race-test fixes close reliability gaps at the atomic decision boundary.
