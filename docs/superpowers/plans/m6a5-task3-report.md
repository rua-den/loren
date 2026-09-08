# M6A.5 Task 3 report

## Status

Local implementation ready. Parent review and verification pass 180/180 tests, build, full-solution format, dependency scan and authentication smoke. Final review findings are fixed, including rejected-input coverage, empty-ID handling and canonical repository updates with proposal foreign keys. CI and separately authorized live GitHub proof remain pending. No live GitHub mutation was performed by the product.

## Interfaces and behavior

- `GitHubActions.ProposeCreateBranch` is an `OwnerStateWrite` brain action with branch, optional canonical repository ID, and optional source ref. `github.create_branch` remains gateway-registered but is absent from the brain action list.
- `GitHubCreateBranchProposalExecutor` requires authenticated owner context and canonical project scope, resolves the canonical GitHub repository and live source SHA through `GitHubRepositoryReadClient`, freezes the create request fingerprint and five-minute expiry, and persists once. It has no write credential or create-ref dependency.
- `ICurrentRunProposalCollector` records proposal IDs in creation order. `LorenRunService` loads authoritative stored proposals and returns `PendingCreateBranchProposal` cards.
- `LorenOwnerGitHubWriteService` exposes ID-only `ApproveProposalAndCreateBranchAsync` and `CancelProposalAsync`. Approval validates owner, canonical rebind, and fingerprint before Task 2 atomic approval; only an approved decision invokes the existing gateway. Cancel creates no approval or provider call.
- `/api/action-proposals/{proposalId}/approve` and `/cancel` use the authenticated `NameIdentifier`; target fields are not accepted from the request body.

The conversation surface renders Vietnamese proposal cards with repository, branch, source ref/SHA, expiry, reversible-write explanation, and separate approve/cancel buttons. Values use `textContent`; both buttons disable before an ID-only request and the card remains terminal.

## Verification

```text
dotnet build Loren.slnx --configuration Release --no-restore  PASS
dotnet test Loren.slnx --configuration Release --no-restore  PASS: 180/180
dotnet format Loren.slnx --verify-no-changes --no-restore  PASS
dotnet package list --project Loren.slnx --vulnerable --include-transitive  PASS: no vulnerabilities reported
Real-host health, owner login, proposal authorization and legacy-route removal smoke  PASS
```

The parent can launch the full suite with the approved execution permissions. Individual Luna processes encountered group policy error 1260; that is not a repository-wide verification blocker. `Microsoft.AspNetCore.Mvc.Testing` 10.0.11 is centrally pinned for actual authenticated HTTP tests against the production route pipeline and a temporary SQLite database.

## Remaining work

Obtain CI evidence for the pushed feature branch. Live-provider proof needs a concrete owner-approved proposal. Broader M5 writes and Gate E remain paused.
