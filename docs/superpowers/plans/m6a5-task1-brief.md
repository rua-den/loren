# M6A.5 Task 1 — trusted live source resolution

Implement only the shared GitHub read client and its deterministic tests. Read `docs/status.md` and `docs/handoff.md` for product boundaries.

## Requirements

- Extract the HTTP repository metadata read from `src/Loren.Tools.GitHub/GitHubReadRepositoryExecutor.cs` into `GitHubRepositoryReadClient` in the same project. Preserve the existing executor constructor and result fields so current callers/tests remain valid.
- Provide a typed method that takes a canonical `RepositoryLocator` and optional source branch/ref, reads current repository metadata and resolves the exact live branch commit SHA. Missing source branch means current default branch. Accept branch names and `refs/heads/` form; reject tags, unsafe Git refs, full raw SHA selectors, malformed/mismatched responses and non-GitHub locators. Short hexadecimal names such as `deadbee` remain valid branch names and must be looked up only under `refs/heads/`; never interpret them through a commits endpoint or fall back to abbreviated commit selection.
- GET only. Use the existing public GitHub read transport; never request or use the GitHub write credential. Build request URLs from escaped canonical owner/repository/branch, never from provider-returned URLs.
- Validate a 40-character hexadecimal commit SHA and exact returned branch ref/object type. Confirm repository identity matches the canonical target. Suppress HTTP failure bodies and exception messages; preserve caller cancellation.
- Existing read_repository remains a single metadata GET and retains its result contract. Source resolution adds one exact branch-ref GET and returns typed canonical repository identity, default branch, normalized source ref and source SHA.
- Expose public signatures/types clearly in the completion report for the next task. Register the shared client using the current named read HttpClient in `LorenHostServices.cs`, with correct DI lifetimes. Do not alter write execution, policy, proposals or UI in this task.

## Files and tests

- Create `src/Loren.Tools.GitHub/GitHubRepositoryReadClient.cs`.
- Modify `src/Loren.Tools.GitHub/GitHubReadRepositoryExecutor.cs` and `src/Loren.Web/LorenHostServices.cs` only as needed for reuse/composition.
- Create `tests/Loren.IntegrationTests/GitHubRepositoryReadClientTests.cs`.
- Test default/explicit branch resolution, slash-containing branch escaping, canonical identity/ref/type mismatch, invalid refs/SHA, missing branch/HTTP failures, malformed JSON and cancellation. Assert actual requests/results, including zero writes and no Authorization header.
- Run existing production read path regression and the integration project with .NET 10.0.400.

## Workflow

Use TDD; record a red failure before implementation and green results afterward. Implementation belongs to Luna medium. Do not spawn agents or commit/push. Report in `docs/superpowers/plans/m6a5-task1-report.md` with changed files, exact interfaces, commands/results, and any concerns. RTK is blocked by system policy; use direct commands. `dotnet test` needs escalation to read the machine NuGet configuration. Baseline at `426772d`: 116 tests passed. Current branch is `codex/m6a5-conversational-approval`.
