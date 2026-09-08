# M6A.5 Task 2 fix-round-1 re-review

## Findings

`tests/Loren.Core.Tests/CreateBranchProposalTests.cs:58`: 🔴 bug: every invalid owner/branch/ref/SHA theory case now passes `created.AddMinutes(10)`, so the new exact-five-minute guard throws before the field under test; lines 65–79 similarly mask the Pending/terminal/decision-time lifecycle assertions with the ten-minute default. Use `created.AddMinutes(5)` for all non-expiry cases and reserve four/six-minute values for the two explicit expiry assertions.

`tests/Loren.IntegrationTests/CreateBranchProposalStoreTests.cs:251`: 🟡 risk: the “precreation” decision reuses an approval timestamped one minute after creation, so `ApprovalMatches` rejects it before the transactional `CreatedAt <= DecidedAt` guard/classifier runs; the “foreign scope” proposal uses a nonexistent project and therefore never tests an existing project paired with a repository belonging elsewhere. Build the approval at the pre-creation decision time, and seed a second project/repository pair to test both missing project and cross-project repository rejection.

## Finding disposition

- Five-minute implementation: fixed in `CreateBranchProposal` line 67; test masking above remains.
- Pending-only insertion: fixed in `SqliteCreateBranchProposalStore.AddAsync` lines 19–22; both terminal states verify no row.
- Rollback/tracker cleanup: fixed for Approve and Cancel using uncancelled rollback plus cleanup on conflict, commit and exception.
- Approve/Cancel race effects: fixed; a third context now correlates persisted terminal state and approval existence with the winner.
- Approval binding coverage: owner/action/project/repository/fingerprint/time/lifetime/consumed/revoked cases now verify Pending state and no candidate approval; the two branch-coverage gaps above remain.

**Verdict:** changes required in tests only. Store/domain implementation satisfies the five reviewed behaviors, but the masked core tests and non-exercised pre-creation/cross-project paths must be corrected before Task 2 passes review.

## Fix round 2

- `CreateBranchProposalTests.cs:58,65`: resolved. Non-expiry fixtures now use the valid five-minute lifetime, so invalid owner/branch/ref/SHA and lifecycle assertions reach their intended guards; four/six-minute fixtures isolate expiry rejection.
- `CreateBranchProposalStoreTests.cs:251-272`: resolved. The approval timestamp now matches the pre-creation decision, so the store reaches its CAS/classification path and asserts the `predates` reason with Pending state. A separately seeded existing project now pairs with the first project's repository, reaching the cross-project repository rejection and verifying no proposal row.
- No production changes or new scoped regressions appear in the round-two patch.

**Final verdict:** accepted. Both remaining test findings are resolved; Task 2 satisfies the reviewed domain/store requirements and is ready for the parent’s independent full verification.
