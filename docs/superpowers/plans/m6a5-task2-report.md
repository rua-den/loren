# M6A.5 Task 2 report

Implemented the bounded create-branch proposal domain and durable SQLite store.

Changed files:

- `src/Loren.Core/Actions/CreateBranchProposal.cs`
- `src/Loren.Infrastructure/CanonicalState/CanonicalStateDbContext.cs`
- `src/Loren.Infrastructure/CanonicalState/SqliteCreateBranchProposalStore.cs`
- `src/Loren.Infrastructure/CanonicalState/Migrations/202609070002_AddCreateBranchProposals.cs`
- `tests/Loren.Core.Tests/CreateBranchProposalTests.cs`
- `tests/Loren.IntegrationTests/CreateBranchProposalStoreTests.cs`
- `tests/Loren.IntegrationTests/CanonicalStateMigrationDriftTests.cs`

Public interfaces:

```text
CreateBranchProposalId(Guid), New(), Parse(string), N-format ToString()
CreateBranchProposalStatus = Pending | Approved | Cancelled
CreateBranchProposal(...): immutable frozen owner, canonical project/repository, RepositoryLocator,
  branch, refs/heads source ref, lowercase 40-character SHA, fingerprint, ReversibleWrite access,
  lifecycle timestamps, status and optional decision timestamp
CreateBranchProposalDecisionRequest(ProposalId, OwnerPrincipalReference, DecidedAt)
CreateBranchProposalDecisionStatus = Approved | Cancelled | Unknown | OwnerMismatch | Expired |
  AlreadyDecided | Mismatch
CreateBranchProposalDecisionResult(Status, Proposal?, Reason)
ICreateBranchProposalStore.AddAsync
ICreateBranchProposalStore.GetAsync
ICreateBranchProposalStore.ApproveAsync(decision, ActionApproval)
ICreateBranchProposalStore.CancelAsync(decision)
```

`ApproveAsync` verifies the owner, bounded action name, canonical scope, frozen intent fingerprint,
approval time, and exactly five-minute approval expiry. It updates a Pending proposal and inserts the
approval in one SQLite transaction. The approval remains pending later Gate D consumption; this store
does not consume it. `CancelAsync` uses the same guarded compare-and-set and inserts no approval.
Conflict classification is deterministic and never returns another owner's proposal. Tests cover
approval mismatch, expiry, duplicate-approval rollback, and independent approval races.

The migration stores all frozen fields and lifecycle timestamps as Unix milliseconds, uses Restrict
foreign keys to canonical Projects and Repositories, and adds ID, owner/status/expiry, repository and
project/repository indexes. No credential, secret, or provider-response fields are present.

TDD verification:

```text
dotnet test tests/Loren.Core.Tests/Loren.Core.Tests.csproj --configuration Release --filter FullyQualifiedName~CreateBranchProposalTests
  passed: 8

dotnet test tests/Loren.IntegrationTests/Loren.IntegrationTests.csproj --configuration Release --filter FullyQualifiedName~CreateBranchProposalStoreTests
  passed: 6

dotnet test tests/Loren.IntegrationTests/Loren.IntegrationTests.csproj --configuration Release --filter FullyQualifiedName~CanonicalStateMigrationDriftTests
  passed: 2
```

The initial red command failed because the new domain types were absent. The first executable test
attempt was blocked by Windows group policy; rerunning the focused commands with the repository's
required elevated test permission passed. Full required project runs passed after the race-test
expansion: Core 22 tests and Integration 88 tests; the focused store suite passes 6 tests.
The snapshot intentionally delegates to `CanonicalStateModel.Configure`, so the new table and indexes
are represented there and the drift test remains zero. The integration project builds cleanly in Release.

Concern: concurrent SQLite writers depend on the provider's normal transaction locking behavior; the
guarded update and approval insert are intentionally in one transaction so a conflict cannot leave a
terminal proposal without its matching approval.

## Review fix round 1

Applied the five review findings:

- Proposal expiry is now exactly `CreatedAt + 5 minutes`; core tests cover the exact boundary and four/six-minute rejection.
- `AddAsync` accepts only undecided `Pending` proposals; terminal insertion tests verify no proposal row is written.
- Approve and Cancel rollback paths use `CancellationToken.None` and clear the EF change tracker on conflict, commit, and exception cleanup.
- Approve/Cancel race tests reload through a third context and assert final status and approval existence correlate with the winner.
- Binding mismatch tests cover owner, action, project, repository, fingerprint, approval time, expiry, consumed, and revoked fields; each leaves the proposal Pending and writes no approval. Unknown, pre-creation, and foreign-scope cases are also covered.

Final verification after the fixes:

```text
dotnet test tests/Loren.Core.Tests/Loren.Core.Tests.csproj --configuration Release
  passed: 22

dotnet test tests/Loren.IntegrationTests/Loren.IntegrationTests.csproj --configuration Release
  passed: 104

dotnet test tests/Loren.Core.Tests/Loren.Core.Tests.csproj --configuration Release --filter FullyQualifiedName~CreateBranchProposalTests
  passed: 8

dotnet test tests/Loren.IntegrationTests/Loren.IntegrationTests.csproj --configuration Release --filter FullyQualifiedName~CreateBranchProposalStoreTests
  passed: 9
```

## Review fix round 2

Corrected the test fixtures so the exact field under test is reached: all non-expiry invalid-domain
cases use a valid five-minute lifetime, while only four and six minutes exercise expiry rejection.
The pre-creation approval now uses the matching pre-creation timestamp, and the scope test seeds a
second canonical project/repository pair to distinguish missing-project rejection from a repository
belonging to another existing project. Assertions also verify the classifier reason and that no row or
approval side effect was produced.

Final round-two verification:

```text
dotnet test tests/Loren.Core.Tests/Loren.Core.Tests.csproj --configuration Release
  passed: 22

dotnet test tests/Loren.IntegrationTests/Loren.IntegrationTests.csproj --configuration Release
  passed: 95

dotnet test tests/Loren.Core.Tests/Loren.Core.Tests.csproj --configuration Release --filter FullyQualifiedName~CreateBranchProposalTests
  passed: 8

dotnet test tests/Loren.IntegrationTests/Loren.IntegrationTests.csproj --configuration Release --filter FullyQualifiedName~CreateBranchProposalStoreTests
  passed: 9
```

Scoped formatting verification after the review follow-up:

```text
dotnet format whitespace Loren.slnx --no-restore --include src/Loren.Infrastructure/CanonicalState/SqliteCreateBranchProposalStore.cs
  passed

dotnet format Loren.slnx --verify-no-changes --no-restore --include src/Loren.Infrastructure/CanonicalState/SqliteCreateBranchProposalStore.cs
  passed with no output and exit code 0
```
