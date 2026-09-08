# M6A.5 Task 1 review

**Spec compliance:** changes requested. **Code quality:** changes requested.

## Findings

- `src/Loren.Tools.GitHub/GitHubRepositoryReadClient.cs:196`: 🔴 bug: `NormalizeBranch` rejects only 40-character SHA strings, so abbreviated SHA input such as `deadbee` is accepted and sent as a branch despite the explicit abbreviated/raw-SHA rejection requirement. Reject plausible abbreviated hexadecimal object IDs as well, and add a deterministic no-HTTP test.
- `src/Loren.Tools.GitHub/GitHubRepositoryReadClient.cs:122`: 🟡 risk: non-caller cancellation/timeouts and response-content transport failures escape as exceptions because only `HttpRequestException` from `SendGetAsync` is converted to a safe failure; `ReadAsStreamAsync` failures at lines 52/137 are outside that catch. Convert transport failures (including `OperationCanceledException` when the caller token is not cancelled) to fixed messages while rethrowing actual caller cancellation.
- `tests/Loren.IntegrationTests/GitHubRepositoryReadClientTests.cs:101`: 🔴 test gap: `PreservesCancellation` never cancels its `CancellationTokenSource`; it proves that an arbitrary handler-thrown `OperationCanceledException` escapes, not that caller cancellation is preserved. Cancel the token before/during the request and add the complementary non-caller timeout/transport-error assertion with a secret-bearing exception message that must not escape.
- `tests/Loren.IntegrationTests/GitHubRepositoryReadClientTests.cs:10`: 🔴 test gap: the required missing-branch/HTTP-failure, malformed-JSON, non-GitHub locator, archived repository, and missing `object` cases are absent. Add distinct tests that assert failure, fixed secret-safe errors, request counts, and no branch GET after metadata rejection.
- `tests/Loren.IntegrationTests/GitHubRepositoryReadClientTests.cs:72`: 🟡 test gap: ref and object-type mismatches occur in the same response, so either validation could be removed without failing the test. Exercise exact-ref mismatch and non-commit type independently; exercise missing `object` independently as required malformed-response coverage.
- `tests/Loren.IntegrationTests/GitHubRepositoryReadClientTests.cs:119`: 🟡 test gap: captured requests omit `HttpMethod`, so the suite does not actually assert the required GET-only/zero-write behavior. Record and assert every request method in both metadata-only and two-request resolution paths.
- `src/Loren.Tools.GitHub/GitHubRepositoryReadClient.cs:43`: 🔵 quality: both `using (response)` bodies are unindented, and `src/Loren.Web/LorenHostServices.cs` currently has mixed line endings (`git ls-files --eol` reports `w/mixed`) against the repository's LF convention. Run the repository formatter/line-ending normalization and include its clean result in the task report.

## Verified compliant behavior

- Resolution uses one canonical metadata GET plus one escaped canonical branch-ref GET; provider-returned URLs are not used.
- Repository identity, exact returned `refs/heads/...`, `object.type == commit`, and a 40-character hexadecimal SHA are validated before success.
- The shared client has no credential resolver/token input, emits no `Authorization` header itself, and DI uses the existing named read `HttpClient`.
- `GitHubReadRepositoryExecutor(HttpClient)` and the existing success data keys remain available; metadata reads remain a single GET.
- Archived repositories are rejected by the implementation, but that behavior needs the missing regression test above.

Review was static as requested. The parent independently reported 125/125 tests passing; no tests were rerun here.
