# M6A.5 Task 2 — durable frozen proposal store

Implement only the bounded create-branch proposal domain, SQLite persistence, atomic Approve/Cancel decisions and deterministic tests. Do not implement the conversation action, endpoints, executor, UI or GitHub calls in this task.

## Domain contracts

Create `src/Loren.Core/Actions/CreateBranchProposal.cs` with:

- `CreateBranchProposalId` wrapping a non-empty GUID with `New`, `Parse`, and `N`-format `ToString`.
- `CreateBranchProposalStatus`: `Pending`, `Approved`, `Cancelled`.
- Immutable `CreateBranchProposal` fields: ID; owner principal; project ID; repository ID; frozen `RepositoryLocator`; normalized branch; normalized source ref; lowercase exact 40-character source SHA; exact intent fingerprint; `ActionAccessClass.ReversibleWrite`; created/expires timestamps; status; optional decided timestamp.
- `CreateBranchProposal.ActionName` is the bounded constant `github.create_branch`. Task 3 constructs the existing `ActionRequest` and matching `ActionAuthorizationContext` from these fields and verifies their computed fingerprint equals the frozen fingerprint; Core must not reference `Loren.Tools.GitHub`.
- `CreateBranchProposalDecisionRequest(proposalId, ownerPrincipalReference, decidedAt)`.
- `CreateBranchProposalDecisionStatus`: `Approved`, `Cancelled`, `Unknown`, `OwnerMismatch`, `Expired`, `AlreadyDecided`, `Mismatch`.
- `CreateBranchProposalDecisionResult(status, proposal?, reason)`; OwnerMismatch must omit the proposal.
- `ICreateBranchProposalStore.AddAsync`, `GetAsync`, `ApproveAsync(decision, ActionApproval)`, and `CancelAsync(decision)`.

Constructor validation must reject empty values, unsafe/non-normalized branch/ref, invalid SHA, access other than `ReversibleWrite`, expiry not after creation, Pending with decided time, terminal status without decided time, or decision before creation. Keep the record bounded; do not add arbitrary action/evidence dictionaries, credentials or provider response bodies.

## SQLite contract

Create `SqliteCreateBranchProposalStore`, migration `202609070002_AddCreateBranchProposals`, model row/configuration and snapshot updates.

Persist every frozen field. Use Unix milliseconds for lifecycle ordering, `Restrict` foreign keys to canonical project/repository, and indexes supporting ID plus owner/status/expiry and project/repository lookups.

`AddAsync` validates that the repository belongs to the project. `GetAsync` round-trips exact normalized values across a restarted context.

`ApproveAsync` must:

1. validate the supplied `ActionApproval` matches the proposal owner, `github.create_branch`, project, repository, frozen intent fingerprint, approval time equal to decision time, and a five-minute expiry;
2. use a SQLite transaction and guarded compare-and-set for proposal ID + authenticated owner + Pending + `CreatedAt <= DecidedAt < ExpiresAt`;
3. insert the `ActionApproval` and mark Approved with `DecidedAt` in the same transaction;
4. roll back both on conflict; only one concurrent decision may win.

`CancelAsync` uses the same owner/time/Pending guard, changes to Cancelled, inserts no approval, and races atomically with Approve. A zero-row update is classified deterministically as Unknown, OwnerMismatch, Expired, AlreadyDecided or Mismatch. Never include another owner's proposal in the result.

## Tests

- `tests/Loren.Core.Tests/CreateBranchProposalTests.cs`: normalization and all invalid invariant combinations.
- `tests/Loren.IntegrationTests/CreateBranchProposalStoreTests.cs`: add/get restart round-trip; foreign scope; approve transaction; cancel; wrong owner; expired/unknown/already decided; approval mismatch; two-context Approve/Approve and Approve/Cancel races with exactly one terminal decision and at most one approval.
- Update `CanonicalStateMigrationDriftTests` to require migration `202609070002_AddCreateBranchProposals` and zero model differences.
- Assert schema/data contains no credential or secret fields.

Use TDD and capture red before implementation. Run whole projects because the repository uses Microsoft.Testing.Platform:

```powershell
dotnet test tests/Loren.Core.Tests/Loren.Core.Tests.csproj --configuration Release
dotnet test tests/Loren.IntegrationTests/Loren.IntegrationTests.csproj --configuration Release
```

Use .NET SDK 10.0.400. `dotnet test` may need escalation to read machine NuGet configuration. Do not spawn agents, commit or push. Write `docs/superpowers/plans/m6a5-task2-report.md` with changed files, exact public interfaces, red/green commands and results, and concerns. Baseline is 116 tests at `426772d`; current branch is `codex/m6a5-conversational-approval`.
