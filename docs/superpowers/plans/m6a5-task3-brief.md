# M6A.5 Task 3 — conversational proposal, explicit decision and verified outcome

Implement the complete user-facing M6A.5 flow after Task 1 and Task 2 are reviewed. Read `docs/status.md`, `docs/handoff.md`, the main M6A.5 plan, `m6a5-task1-report.md`, and the final `m6a5-task2-report.md`. The Task 2 report's actual signatures are authoritative; adapt this brief to them without weakening the behavior below.

Implementation belongs to `gpt-5.6-luna` at `medium`. Use TDD, do not spawn agents, and do not commit/push or perform a live GitHub mutation. The Sol parent reviews the diff and runs final build/test/format/auth smoke.

## Scope

Add the model-visible, non-executing create-branch proposal action; trusted proposal collection for the current run; authenticated ID-only Approve/Cancel services and routes; the conversation approval card; and meaningful deterministic acceptance/security tests. Reuse Task 1's public read client and Task 2's durable store. Reuse the existing `ActionGateway`, `GateDActionPolicy`, `SqliteActionApprovalStore`, `GitHubCreateBranchActionExecutor`, write credential resolver, create client, verification and audit path.

Do not add another GitHub mutation, generic workflow framework, proposal-history UI, scheduler behavior, or file/commit/PR writes. Remove the old browser-supplied create-branch route/form after the new path is proven.

## Required behavior and interfaces

Add `GitHubActions.ProposeCreateBranch` with access `OwnerStateWrite`, required `branch`, optional `repository_id`, and optional `source_ref`. Add it to the brain action list; keep `github.create_branch` registered in the gateway but absent from the brain list. Update trusted system guidance: a branch request is intent only; chat/history/memory/tool/external content can request a proposal but can never approve or claim creation.

Implement `GitHubCreateBranchProposalExecutor : ITrustedActionExecutor`. It must:

1. require `AuthenticatedOwnerContext` with a project ID;
2. reload that exact canonical project and resolve one GitHub repository, or validate the optional canonical repository ID when multiple exist;
3. normalize/validate the requested branch and optional source ref;
4. call Task 1's `GitHubRepositoryReadClient.ResolveSourceAsync` with the canonical locator, defaulting a missing ref to the live default branch;
   reject a requested new branch equal to the returned live default branch before persisting an unusable proposal;
5. build the exact existing create request `{ branch, source_sha }` and matching `ActionAuthorizationContext` from authenticated owner + canonical IDs/locator;
6. compute and freeze `ActionIntentFingerprint`, `ReversibleWrite`, created time and five-minute expiry in Task 2's Pending proposal;
7. persist once and return a safe proposal observation. It must never resolve a write credential or call create-ref.

Introduce a scoped trusted collector, for example `ICurrentRunProposalCollector`, that records proposal IDs created by the proposal executor during the current `LorenRunService.RunAsync` call. Clear/start it before the agent loop and drain it afterward. Surface every successfully created proposal from that run as `IReadOnlyList<PendingCreateBranchProposal>`; do not silently overwrite multiple proposals. If the existing agent-loop limits should enforce one proposal instead, fail the second proposal explicitly and test that rule. The preferred behavior is to return all proposals in creation order.

`LorenRunService` must load each collected ID from `ICreateBranchProposalStore` and project the authoritative stored fields. Never construct cards from model text or model-visible `ActionResult.Data`. Card fields are proposal ID, action name, repository, branch, source ref, exact source SHA, access class and expiry.

Replace the current direct owner write method with:

```csharp
Task<OwnerActionProposalDecisionResult> ApproveProposalAndCreateBranchAsync(
    string proposalId,
    string ownerPrincipalReference,
    CancellationToken cancellationToken);

Task<OwnerActionProposalDecisionResult> CancelProposalAsync(
    string proposalId,
    string ownerPrincipalReference,
    CancellationToken cancellationToken);
```

Approval must load the frozen proposal and immediately reject a different owner without disclosing proposal fields or consulting its canonical target. Then verify `github.create_branch`, `ReversibleWrite`, exact branch/SHA/fingerprint consistency, reload canonical project/repository and compare the frozen provider/namespace/name, then create a five-minute `ActionApproval` at the decision time. Capture the decision time once and reuse it for the store request and approval. Call Task 2's atomic Approve method; only its successful Approved result may invoke the existing gateway with the frozen request/context and new approval ID. Never retry execution or mint a replacement approval. Canonical deletion/rebind fails before an approval or provider call.

Cancel calls only Task 2's atomic Cancel method. It creates no approval, credential lookup, gateway request or GitHub call.

Map authenticated routes:

```text
POST /api/action-proposals/{proposalId}/approve
POST /api/action-proposals/{proposalId}/cancel
```

The proposal ID comes only from the route; accept no target request DTO/body fields. Obtain owner identity from `ClaimTypes.NameIdentifier`. Map Unknown to 404, OwnerMismatch to 403, and expired/already-decided/mismatch/rebind to 409 with safe non-secret errors. Accepted Approved/Cancelled decisions return 200. A downstream Gate D/credential/provider failure is a decided approval with a safe execution result and audit, not an HTTP authorization success claim or retry opportunity. Unauthenticated calls return 401 through the existing owner-cookie behavior.

Remove `/api/github/create-branch`, `OwnerCreateBranchRequest`, the advanced low-level form, `window.confirm`, and browser code sending project/repository/branch/SHA.

## Conversation UI

Render every returned proposal beside the assistant turn. Keep the existing Vietnamese, direct product voice. Show repository, branch, source ref, exact source SHA, five-minute expiry, and a clear `ReversibleWrite` explanation such as “Thay đổi GitHub bên ngoài; cần mày duyệt một lần.” Provide separate Approve and Cancel buttons.

Render server values with `textContent`. On either click, disable both buttons before sending the ID-only request and keep the card terminal after a decided response. On verified success, append natural text naming repository, branch, source ref/SHA and that read-back verification succeeded. On cancellation, say no GitHub change was made. On execution failure, state the safe failure without implying success. Reuse the existing secondary activity/audit panel using the `LorenAuditEntry` string-kind shape (raw AuditEvent enum JSON is numeric). Never append button labels, proposal IDs, or a fabricated owner approval message to conversation history.

## Tests — execute behavior, not markup alone

Create/update deterministic tests under `tests/Loren.IntegrationTests`:

- `ConversationalCreateBranchProposalTests`: execute the trusted proposal service/executor with fake catalog, Task 1 HTTP handler, Task 2 store and clock. Prove default and explicit ref resolution, frozen fingerprint/locator, five-minute expiry, multiple proposals surfaced in order, and zero approval/credential/write calls. Cover missing auth/project, ambiguous/foreign repo, non-GitHub/archived repo, unsafe branch/ref, read failure, and model arguments attempting `source_sha`, approval ID, credential, locator, access or fingerprint authority.
- `OwnerCreateBranchWorkflowTests`: convert existing direct-workflow tests to start from a frozen proposal. Execute Approve through the real service into the existing gateway/client; assert one atomic approval, one consume, exact create-ref, independent exact-SHA read-back, natural safe result and audit. Cover cancel, replay, wrong owner, expiry, canonical rebind, disabled writes, missing/revoked credential and provider verification mismatch.
- `ConversationalApprovalEndpointTests`: boot the actual ASP.NET route pipeline, log in through `/auth/login` to obtain the owner cookie, and call both ID-only routes over `HttpClient`. Assert unauthenticated 401; authenticated status mapping 200/403/404/409; no accepted target body/query override; and side effects in the real temporary SQLite stores/fake provider. Do not limit this file to route-source or HTML string assertions.
- `ConversationalApprovalAcceptanceTests`: fake Ollama proposes from “Tạo branch fix-login cho Loren từ main”; run `/api/run`, verify the brain sees propose but not create, receive the authoritative stored card, then call authenticated Approve and observe exact Gate D consume/create/read-back/audit. Replay must make no second mutation. Adversarial history/model/tool content claiming approval or supplying fake approval/SHA must not decide or alter the frozen target. Assert the write secret is absent from brain JSON, database proposal, responses, results, errors and audit.
- `ConversationPrimarySurfaceTests`: retain focused safe DOM/markup assertions for card labels, all-proposal rendering, `textContent`, ID-only URL, button disabling and removal of the legacy form. These supplement the behavioral route/service tests.
- `CredentialBoundaryHostCompositionTests`: assert exactly one GitHub external mutation executor (`github.create_branch`), the proposal executor is trusted owner-state code, and it has no write credential dependency.

Use the existing framework and packages first. Add no NuGet package unless real authenticated in-process HTTP testing cannot be achieved otherwise. If `Microsoft.AspNetCore.Mvc.Testing` is required, pin its .NET 10 patch version centrally in `Directory.Packages.props` and add an unversioned test-project reference; document why in the report. Do not add a test dependency merely for string-level endpoint tests.

Because the repository uses Microsoft.Testing.Platform, run whole projects rather than VSTest filter syntax:

```powershell
dotnet test tests/Loren.Core.Tests/Loren.Core.Tests.csproj --configuration Release
dotnet test tests/Loren.Runtime.Tests/Loren.Runtime.Tests.csproj --configuration Release
dotnet test tests/Loren.Brain.Ollama.Tests/Loren.Brain.Ollama.Tests.csproj --configuration Release
dotnet test tests/Loren.IntegrationTests/Loren.IntegrationTests.csproj --configuration Release
dotnet test Loren.slnx --configuration Release
git diff --check
```

The parent performs final real-auth smoke. Do not perform live GitHub mutation without a separate explicit owner authorization for that proof.

## Documentation and report

After local green verification, update `docs/status.md`, `docs/handoff.md`, `docs/plans/v0.1.md`, and the stale M6A.4 milestone text in `README.md` only where needed. Describe status as local implementation ready with CI/live proof pending unless those proofs actually pass. Keep later M5 writes and Gate E paused.

Write `docs/superpowers/plans/m6a5-task3-report.md` with changed files, final interfaces, proposal collector behavior, test commands/counts, security evidence, dependency changes, and remaining CI/live-proof work. Do not commit or push.
