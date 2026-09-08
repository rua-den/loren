# M6A.5 Conversational Approval Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans task-by-task. All implementation code must be written by `gpt-5.6-luna` at `medium`; the Sol orchestrator reviews every diff and runs final verification.

**Goal:** Let the authenticated owner request a GitHub branch in chat, review an exact server-frozen proposal, explicitly Approve or Cancel it, and receive a verified result through the existing Gate D boundary.

**Architecture:** A public GitHub read client resolves the exact source branch SHA. A durable, owner-bound create-branch proposal freezes the canonical repository locator, branch, source ref/SHA, access class and five-minute expiry. The brain may request proposal creation, but only an ID-only authenticated button decision can atomically create a one-time `ActionApproval`; execution then reuses `ActionGateway`, `GitHubCreateBranchActionExecutor` and `GitHubCreateBranchClient` unchanged.

**Tech stack:** .NET 10, ASP.NET Core minimal APIs/cookie authentication, EF Core + SQLite, xUnit v3, GitHub REST API.

**Spec:** `docs/status.md` and `docs/handoff.md`, `M6A.5 — Conversational approval`

## Global constraints

- Preserve Gate D: canonical target → deterministic policy → explicit owner approval → exact intent fingerprint → atomic consume → write credential → trusted executor → independent verification → redacted audit.
- Chat is intent only. Model/history/memory/tool content cannot approve, create an approval ID, choose credentials, or invoke `github.create_branch`.
- Browser decision requests carry only `proposalId`. Frozen server state supplies repository, branch, ref/SHA, authorization target and fingerprint.
- Proposal and approval lifetimes are five minutes. Approve/Cancel is owner-bound, expiry-checked and atomic; exactly one concurrent decision wins.
- Reject approval if the current canonical repository ID/locator differs from the frozen proposal, including a rebind after proposal creation.
- Proposal/cancel are local owner state. `LOREN_ENABLE_WRITES` and the existing dedicated credential remain mandatory for external execution.
- `github.create_branch` remains the only GitHub mutation primitive and stays out of the brain action list.
- Do not persist/expose secrets. Do not add file/commit/PR writes, scheduler behavior or proposal-history product scope.
- Leave the 39 stale remote branches unchanged.
- Mark local implementation ready only after local green verification. Do not mark M6A.5 complete until exact-head CI and a separately owner-authorized live product proof pass.

## Resolved choices

- Persist proposals in SQLite so the server remains authoritative across refresh/restart and can perform compare-and-set decisions.
- Use a bounded immutable `CreateBranchProposal`; no generic arbitrary action payload framework is needed for v0.1.
- `source_ref` defaults to GitHub's live default branch. Task 1 accepts branch names and normalized `refs/heads/...`; tags and full raw SHA selectors are rejected. Short hexadecimal names are resolved only as exact branch refs, never as abbreviated commit selectors.
- One canonical GitHub repository needs no repository argument; multiple repositories require a canonical `repository_id` belonging to the authenticated project.
- Remove the direct `/api/github/create-branch` form/route when the conversation flow is green, because it accepts browser-supplied target data.
- A successful approval decision persists proposal state and `ActionApproval` in one SQLite transaction. Downstream failure is reported and never retried automatically.
- The approval card is loaded from proposal IDs captured by trusted run-scoped server code, never reconstructed from model `ActionResult` text/data.

---

### Task 1: Trusted live source resolver

**Authoritative brief:** `docs/superpowers/plans/m6a5-task1-brief.md`

**Files:** create `src/Loren.Tools.GitHub/GitHubRepositoryReadClient.cs` and its integration tests; minimally modify `GitHubReadRepositoryExecutor.cs` and host registration.

**Deliverable:** preserve the existing one-GET `github.read_repository` contract and add typed `ResolveSourceAsync(...)` behavior that performs credential-free canonical repository metadata + exact branch-ref reads, returning canonical identity, default branch, normalized source ref and exact lowercase 40-character commit SHA. Accept `main` and `refs/heads/main`; safely escape slash branches; fail closed on unsafe refs, identity/ref/object mismatch, malformed data, HTTP failure and cancellation.

- [ ] Follow the Task 1 brief with TDD and write `docs/superpowers/plans/m6a5-task1-report.md` containing exact final interfaces.
- [ ] Run `dotnet test tests/Loren.IntegrationTests/Loren.IntegrationTests.csproj --configuration Release` and existing production-read regression.
- [ ] Sol reviews the diff before Task 3 consumes the interface. Do not alter policy, proposal or write execution in Task 1.

---

### Task 2: Durable frozen proposal and atomic decision store

**Authoritative brief:** `docs/superpowers/plans/m6a5-task2-brief.md`

**Files:**
- Create: `src/Loren.Core/Actions/CreateBranchProposal.cs`
- Create: `src/Loren.Infrastructure/CanonicalState/SqliteCreateBranchProposalStore.cs`
- Create: `src/Loren.Infrastructure/CanonicalState/Migrations/202609070002_AddCreateBranchProposals.cs`
- Modify: `src/Loren.Infrastructure/CanonicalState/CanonicalStateDbContext.cs`
- Modify: `src/Loren.Infrastructure/CanonicalState/Migrations/CanonicalStateDbContextModelSnapshot.cs`
- Test: `tests/Loren.Core.Tests/CreateBranchProposalTests.cs`
- Test: `tests/Loren.IntegrationTests/CreateBranchProposalStoreTests.cs`
- Modify test: `tests/Loren.IntegrationTests/CanonicalStateMigrationDriftTests.cs`

**Interfaces:** `CreateBranchProposalId`; Pending/Approved/Cancelled status; immutable owner/project/repository/locator/branch/source-ref/source-SHA/intent-fingerprint/access/timestamp fields; `ICreateBranchProposalStore.AddAsync`, `GetAsync`, atomic `ApproveAsync(decision, ActionApproval)` and `CancelAsync(decision)`; explicit decision statuses Approved, Cancelled, Unknown, OwnerMismatch, Expired, AlreadyDecided and Mismatch.

- [ ] Write domain tests for normalization and invalid IDs, SHA/ref/branch, non-`ReversibleWrite` access, inconsistent status/timestamps and expiry.
- [ ] Write SQLite round-trip/restart tests for every frozen field and migration drift.
- [ ] With independent contexts on one database, test Approve/Approve and Approve/Cancel races: one decision wins and at most one exact `ActionApproval` exists.
- [ ] Test wrong owner, unknown ID, expiry, already-decided proposal and approval owner/action/project/repository/fingerprint mismatch: no transition and no approval insertion. OwnerMismatch must not disclose another owner's proposal.
- [ ] Implement guarded updates matching ID + owner + Pending + `CreatedAt <= DecidedAt < ExpiresAt`. `ApproveAsync` validates the supplied approval and commits approval insert plus Pending→Approved in one transaction; `CancelAsync` performs Pending→Cancelled without an approval.
- [ ] Run `dotnet test tests/Loren.Core.Tests/Loren.Core.Tests.csproj --configuration Release` and `dotnet test tests/Loren.IntegrationTests/Loren.IntegrationTests.csproj --configuration Release`.

Acceptance: exact proposal state survives restart; invalid/tampered decisions fail closed; concurrent decisions produce one terminal state; no credential or provider data enters the schema.

---

### Task 3: Conversation proposal, ID-only owner decision, UI and acceptance

**Files:**
- Create: `src/Loren.Web/GitHubCreateBranchProposalExecutor.cs`
- Modify: `src/Loren.Tools.GitHub/GitHubActions.cs`
- Modify: `src/Loren.Web/LorenHostServices.cs`
- Modify: `src/Loren.Web/LorenProjectContextBuilder.cs`
- Modify: `src/Loren.Web/LorenRunService.cs`
- Modify: `src/Loren.Web/OwnerOperations.cs`
- Modify: `src/Loren.Web/Program.cs`
- Modify: `src/Loren.Web/OwnerPages.cs`
- Test: `tests/Loren.IntegrationTests/ConversationalCreateBranchProposalTests.cs`
- Modify test: `tests/Loren.IntegrationTests/OwnerCreateBranchWorkflowTests.cs`
- Create: `tests/Loren.IntegrationTests/ConversationalApprovalEndpointTests.cs`
- Create: `tests/Loren.IntegrationTests/ConversationalApprovalAcceptanceTests.cs`
- Modify test: `tests/Loren.IntegrationTests/ConversationPrimarySurfaceTests.cs`
- Modify test: `tests/Loren.IntegrationTests/CredentialBoundaryHostCompositionTests.cs`
- Update after green: `docs/status.md`, `docs/handoff.md`, `docs/plans/v0.1.md`

**Interfaces:**
- `GitHubActions.ProposeCreateBranch`: `OwnerStateWrite`, required `branch`, optional `repository_id` and `source_ref`.
- `GitHubCreateBranchProposalExecutor : ITrustedActionExecutor`: resolves authenticated canonical scope and Task 1 live source, then stores Task 2's immutable Pending proposal.
- A scoped proposal collector records proposal IDs created by this run. `LorenRunService` loads those IDs from the trusted store and returns a typed `PendingCreateBranchProposal` card. It never trusts the model-visible action result to populate the card.
- `LorenOwnerGitHubWriteService.ApproveProposalAndCreateBranchAsync(string proposalId, string owner, CancellationToken)` and `CancelProposalAsync(...)`.
- Authenticated `POST /api/action-proposals/{proposalId}/approve` and `/cancel`; no target request DTO.

- [ ] **Write failing proposal-action tests.** Authenticated project + canonical GitHub repo + branch resolves the live default or explicit source ref/SHA and persists one Pending proposal. Missing owner/project, ambiguous or foreign repository, non-GitHub/archived repo, unsafe branch/ref and failed/malformed live read create nothing. Arguments such as `source_sha`, `approval_id`, locator, credential, access class or fingerprint never become authoritative. Assert zero write credentials and zero create-ref calls.

- [ ] **Implement proposal action and trusted run projection.** Register the Task 1 client, Task 2 store, scoped collector and trusted proposal executor. Add only `github.propose_create_branch` to the brain actions; keep `github.create_branch` gateway-only. Build the frozen create request strictly from server-resolved `{ branch, source_sha }`, authenticated owner/project, canonical repository ID/locator, `ReversibleWrite`, and five-minute expiry. Add identity guidance that branch chat is intent and cannot imply approval. Return card data only by loading collector IDs from the store.

- [ ] **Write failing service/endpoint tests.** Chat alone produces no `ActionApproval` or write. ID-only Approve recomputes the exact fingerprint and the existing gateway consumes it once. Cancel creates no approval/call. Wrong owner, expiry, replay and crossed decisions fail closed. Browser query/body attempts to override branch/SHA/repository/access/fingerprint are rejected or ignored. Canonical repository rebind after proposal yields conflict before approval/provider calls.

- [ ] **Implement explicit decisions.** Approval loads the proposal, verifies action/access and exact frozen fields, reloads project/repository and compares provider/namespace/name, computes `ActionIntentFingerprint`, constructs the five-minute `ActionApproval`, and calls atomic `ApproveAsync`. Only status Approved may call existing `IActionGateway.ExecuteAsync` with frozen request/context and new approval ID. Cancel only calls `CancelAsync`. Map Unknown→404, OwnerMismatch→403, Expired/AlreadyDecided/Mismatch/rebind→409, accepted decisions→200; report downstream execution failure safely without retry.

- [ ] **Remove the direct target route.** Delete `/api/github/create-branch`, `OwnerCreateBranchRequest`, the low-level form and `window.confirm` path after the ID-only flow is tested.

- [ ] **Write UI tests and implement the card.** Show Repository, Branch, Source ref, Exact source SHA, Access/risk, expiry and separate Approve/Cancel. Render server strings with `textContent`. Disable both buttons before a decision request and keep them terminal after a decided response. Append a natural verified-completion or cancellation message and reuse the audit panel. Do not append button labels, proposal IDs or fabricated approval speech to conversation history.

- [ ] **Write deterministic end-to-end security acceptance.** With fake Ollama/GitHub transports and a temporary migrated SQLite database, prove Vietnamese intent exposes propose but not create; live canonical state is frozen; no write precedes click; ID-only Approve creates/consumes one exact approval; the existing credential client creates and reads back the exact SHA; audit records request/policy/approval-consumed/completion; replay performs no second mutation. Adversarial model/history/tool text claiming approval or fake approval/SHA cannot decide or alter the proposal. Assert the write secret is absent from brain JSON, proposal rows, responses, results, errors and audit.

- [ ] **Verify failure paths.** With `LOREN_ENABLE_WRITES=false`, proposal/cancel still work but Gate D denies execution before credentials/provider. Missing/revoked credential remains fail-closed after one-time consumption. Existing default-branch guard and post-write mismatch tests remain green.

- [ ] **Run full local verification.** Because this repository uses Microsoft.Testing.Platform, run whole test projects instead of VSTest `--filter`:

```powershell
dotnet test tests/Loren.Core.Tests/Loren.Core.Tests.csproj --configuration Release
dotnet test tests/Loren.Runtime.Tests/Loren.Runtime.Tests.csproj --configuration Release
dotnet test tests/Loren.Brain.Ollama.Tests/Loren.Brain.Ollama.Tests.csproj --configuration Release
dotnet test tests/Loren.IntegrationTests/Loren.IntegrationTests.csproj --configuration Release
dotnet test Loren.slnx --configuration Release
git diff --check
git status --short
```

- [ ] **Review and document local readiness.** Sol reviews the full diff and reruns verification. Update status/handoff/v0.1 as “M6A.5 implementation ready; CI/live proof pending” unless both proofs have actually passed. Keep later mutations and Gate E paused.

- [ ] **Complete external proof only with separate owner authorization.** Do not perform a live GitHub mutation merely because this plan is approved. After the owner separately authorizes the product proof, use one unique non-default branch through the real browser flow and record only safe repository/branch/ref/SHA, decision, consumed/verified audit outcomes and CI links. Exact-head Ubuntu + Windows CI and this owner-approved live proof are required before marking M6A.5 complete.

## Final review checklist

- [ ] Brain sees proposal action and never create action.
- [ ] Card comes from trusted collector/store state, not model result data.
- [ ] Chat/model/history/memory/tool content cannot approve.
- [ ] Proposal uses canonical scope plus credential-free live reads.
- [ ] Approve/Cancel are ID-only, owner-bound, expiry-checked and atomic.
- [ ] Rebind, tampering, replay and races fail closed.
- [ ] Fingerprint binds owner/action/project/repository locator/branch/SHA.
- [ ] Existing approval consume, kill switch, credential isolation, default-branch guard and read-back verification remain active.
- [ ] Exactly one GitHub mutation primitive exists: `github.create_branch`.
- [ ] Secrets remain within the credential lease/provider request boundary.
- [ ] Full local suite passes; exact-head CI and separately authorized live proof pass before milestone completion.
