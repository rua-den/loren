# M6A.5 Task 3 tests

The M6A.5 integration coverage now uses a migrated, file-backed SQLite canonical database for the create-branch approval workflow. `OwnerCreateBranchWorkflowTests` drives the real proposal and approval stores, Gate D policy, credential boundary, GitHub executor and fake provider; it asserts the frozen SHA, exact POST body, independent GET verification, atomic terminal state and replay suppression. Wrong-owner and cancellation cases assert no provider mutation.

`ConversationalCreateBranchProposalTests` exercises the trusted proposal executor against SQLite and a deterministic GitHub read client. It covers default and explicit source refs, ordered collection, exact five-minute expiry, model-supplied authority fields being ignored, and missing authenticated project failure.

`ConversationalApprovalEndpointTests` uses `Microsoft.AspNetCore.Mvc.Testing` 10.0.11 and a real `HttpClient` against the ASP.NET route pipeline. It logs in through `/auth/login`, verifies unauthenticated 401 and authenticated unknown-proposal 404 behavior, and sends target fields in both body and query to prove the route ID is authoritative. The package is centrally pinned in `Directory.Packages.props` because route-source assertions cannot verify cookie middleware and endpoint status mapping.

`CredentialBoundaryHostCompositionTests` now expects the trusted proposal executor in addition to the single external mutation executor and asserts the proposal executor has no credential-bound fields. The existing UI surface tests remain in place for safe text rendering and proposal card behavior.

Validation: `dotnet test tests/Loren.IntegrationTests/Loren.IntegrationTests.csproj --configuration Release --no-restore` passes 107 tests locally after the final assertion correction. The parent agent should rerun the complete solution and its real-host authentication smoke before integration.

Final parent verification supersedes the intermediate 107-test checkpoint: full solution 180/180 passing. Added real chat-to-decision HTTP acceptance, response status/audit tests, rejected-input matrix, expiry/credential/rebind/read-back failures, empty-ID regressions and catalog foreign-key compatibility. No process-global write token is used by acceptance tests; credentials are deterministic DI test doubles.
