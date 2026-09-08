# M6A.5 Task 3 consolidated production review

`src/Loren.Web/CreateBranchProposalFlow.cs:49`: 🔴 bug: proposal validation compares the new branch with `source.SourceRef`, so `branch=main` sourced from `feature/x` is persisted even though the existing executor will later reject writing the live default branch. Compare the normalized new branch with `source.DefaultBranch` before persistence; retain the source-ref equality guard separately if desired.

`src/Loren.Web/Program.cs:128`: 🔴 bug: an atomically accepted approval whose Gate D/credential/provider execution fails falls through to HTTP 409 with only `{ error }`, discarding `Execution` and `Audit`; the owner cannot inspect the consumed approval or failure audit and the response misrepresents a decided proposal as a decision conflict. Return the structured `OwnerActionProposalDecisionResult` with 200 for every `status == "approved"`, using `Success`/`Execution` to express execution outcome; reserve 409 for proposal-decision conflicts.

`src/Loren.Web/OwnerPages.cs:358`: 🟡 bug: decision responses only replace text inside the card; verified completion/cancellation is never appended as a Loren message and `response.audit` is never rendered in the activity panel. Append the safe server `message` as an assistant message without fabricating owner approval history, and render the returned `LorenAuditEntry` string-kind rows (including accepted execution failures).

**Verdict:** changes required. Frozen request/authorization reconstruction, fingerprint verification, wrong-owner-before-canonical-read ordering, atomic proposal→approval transition, one-time gateway consumption, scoped ordered collector projection, ID-only routes, and legacy route removal are otherwise sound in this production snapshot. Completion remains subject to the parent’s independent full test and authentication verification.

## Production fix disposition

- `CreateBranchProposalFlow.cs:49`: resolved. The proposed branch is now compared with `source.DefaultBranch` before persistence, so a non-default source cannot make the default branch appear approvable.
- `Program.cs:125-130`: resolved. Every accepted `approved` decision returns the structured result with HTTP 200, preserving `Success`, execution detail and audit for downstream failures; decision conflicts remain 409.
- `OwnerPages.cs:358-361`: resolved. The safe server outcome is appended as a Loren message and returned `LorenAuditEntry` rows are rendered through the existing activity view without adding fabricated owner approval history.

**Production verdict:** accepted for these three findings. This closes only the production fix review; Task 3 completion still depends on the in-progress acceptance/security tests and the parent’s independent final gates.

## Final acceptance/security test review

`tests/Loren.IntegrationTests/OwnerCreateBranchWorkflowTests.cs:93`: 🔴 the expiry test checks `GetAsync(ApprovalId.New())`, which will be null regardless of whether approval incorrectly created another ID. Query/assert the real `ActionApprovals` row count is zero (as the acceptance test does elsewhere), so the named invariant “expired proposal cannot create approval” is actually proven.

`tests/Loren.IntegrationTests/ConversationalCreateBranchProposalTests.cs:80`: 🔴 the required executor rejection matrix is mostly absent. This test covers missing project context and branch-equals-live-default only; it does not execute ambiguous/foreign repository selection, non-GitHub or archived targets, unsafe branch/ref, or read failure. Add a compact theory/set of cases asserting failure, no persisted proposal/collector ID, and no write/approval side effect. These are proposal-boundary cases and are not established by the separate Task 1 read-client tests.

The three host acceptance tests otherwise provide meaningful evidence through the real `/api/run` and authenticated decision routes, real SQLite proposal/approval state, Gate D consumption, exact create/read-back, replay prevention, ordered multi-proposal collection, body/query tamper resistance, safe audit projection, and credential/secret isolation. The endpoint and UI tests usefully supplement that behavioral coverage.

**Test verdict:** changes required for the two evidence gaps above. Production remains accepted. Final completion still requires the parent’s clean full-suite and smoke verification after the test fixes.

## Final test-fix disposition

- `OwnerCreateBranchWorkflowTests.cs:93-98`: resolved. The expiry case now queries the real SQLite `ActionApprovals` table and asserts its row count remains zero, so it can detect an approval created under any ID.
- `ConversationalCreateBranchProposalTests.cs:20-71`: resolved. The SQLite-backed theory executes the trusted gateway/proposal path for ambiguous and foreign repository selection, non-GitHub and archived targets, unsafe branch and source ref, read failure, and missing project context. Every case asserts failure, an empty run collector, zero persisted proposals, and no provider POST.

**Final reviewer verdict:** accepted. The two test findings are closed; production and acceptance/security evidence are review-ready. Completion remains contingent on the parent’s in-progress full-suite and formatting gates.

## Parent self-review requested by owner

- Empty GUID IDs pass Guid.TryParse but throw in CreateBranchProposalId; both authenticated decision routes need safe unknown/404 handling and route regressions.
- SqliteProjectCatalog.SaveAsync deletes all repository rows before insertion. Proposal foreign keys make even unchanged project saves fail. Update existing repository rows in place, preserve restricted deletion, and test metadata save plus locator rebind through the actual catalog.
- Removed stale remote-branch deletion instructions from status/handoff; owner explicitly retained all 39 branches.

Luna fixes and parent verification pending.
- Parent disposition: accepted after reviewing both fixes and independently passing 180/180 tests. Empty/malformed IDs map safely to 404; canonical saves update matching repository rows in place, retaining proposal foreign keys and rejecting rebound targets before approval. No known blocking finding remains.
