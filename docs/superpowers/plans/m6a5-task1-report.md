# M6A.5 Task 1 — trusted live source resolution report

Implemented the shared GitHub read client and deterministic tests.

Changed files:

- `src/Loren.Tools.GitHub/GitHubRepositoryReadClient.cs` — shared unauthenticated GET client and typed metadata/source resolution results.
- `src/Loren.Tools.GitHub/GitHubReadRepositoryExecutor.cs` — preserves the existing `HttpClient` constructor, action name and `ActionResult` fields while delegating metadata reads.
- `src/Loren.Web/LorenHostServices.cs` — registers one singleton read client backed by the named `loren-github-read` `HttpClient`; no write credential is involved.
- `tests/Loren.IntegrationTests/GitHubRepositoryReadClientTests.cs` — thirteen deterministic tests covering default and explicit refs, slash escaping, invalid refs, identity/ref/object/SHA validation, no authorization, cancellation and transport failures.

Public interfaces:

```csharp
public sealed class GitHubRepositoryReadClient
{
    public GitHubRepositoryReadClient(HttpClient httpClient);
    public Task<GitHubRepositoryMetadataResult> ReadRepositoryAsync(
        RepositoryLocator repository,
        CancellationToken cancellationToken);
    public Task<GitHubRepositoryResolutionResult> ResolveSourceAsync(
        RepositoryLocator repository,
        string? sourceBranchOrRef,
        CancellationToken cancellationToken);
}

public sealed record GitHubRepositoryMetadataResult(
    bool Success,
    RepositoryLocator Repository,
    string? DefaultBranch,
    IReadOnlyDictionary<string, string> Data,
    string? Error);

public sealed record GitHubRepositoryResolutionResult(
    bool Success,
    RepositoryLocator Repository,
    string? DefaultBranch,
    string? SourceRef,
    string? SourceSha,
    string? Error);
```

`ResolveSourceAsync` accepts a branch name or `refs/heads/` form; null resolves the current default branch. It constructs both URLs from the canonical locator and escaped branch, validates the returned canonical identity, exact branch ref, commit object type and 40-character SHA, rejects tags/raw SHA/unsafe refs, rejects archived repositories, suppresses HTTP failure details, and preserves cancellation. All requests are GETs without an `Authorization` header.

TDD evidence and verification:

- Red: focused test run initially failed to compile because the new client/result types were absent.
- Green: `dotnet build Loren.slnx --no-restore` passed with 0 warnings and 0 errors.
- `dotnet test tests/Loren.IntegrationTests/Loren.IntegrationTests.csproj --no-build --filter FullyQualifiedName~GitHubRepositoryReadClientTests`: 13 passed.
- Production regressions (`ProductionReadPathTests`, `WalkingSkeletonReadTests`, `CredentialBoundaryHostCompositionTests`): 3 passed.
- Pre-review integration project: 81 passed, 0 failed.

No commits or pushes were made. HTTP test execution required the approved escalated .NET runner because local group policy blocks the sandbox runner.

Review fixes applied:

- Kept short hexadecimal names such as `deadbee` valid as branch names; added a test proving lookup stays on the escaped `git/ref/heads/deadbee` endpoint and never falls back to commit selection.
- Preserved caller cancellation while converting non-caller timeout cancellation, `HttpRequestException`, and `IOException` during request/content handling into fixed secret-safe failures.
- Added distinct tests for metadata HTTP failure, branch HTTP failure, malformed metadata JSON, missing branch ref object, non-GitHub locators, archived repositories, exact ref mismatch, object type mismatch, invalid SHA, and transport exception redaction.
- Request capture now records `HttpMethod`; all resolution requests are asserted to be GET and no authorization header is present.
- Scoped whitespace formatting was applied to the four changed C# files and `dotnet format Loren.slnx whitespace ... --verify-no-changes` passed.

Final review verification:

- Focused client suite: 13 passed.
- Prior review integration run: 92 passed, 0 failed.

Round-two re-review evidence:

- Added throwing response-body stream coverage after successful `SendAsync` for metadata and branch-ref bodies, independently covering `HttpRequestException`, `IOException`, non-caller timeout cancellation, and actual caller cancellation propagation.
- Added a direct metadata-only assertion for exactly one unauthenticated `GET` request.
- Focused client suite after round-two additions: 22 passed.
- `dotnet test tests/Loren.IntegrationTests/Loren.IntegrationTests.csproj -c Release --no-restore`: 104 passed, 0 failed.
- `dotnet format Loren.slnx whitespace --include tests/Loren.IntegrationTests/GitHubRepositoryReadClientTests.cs --no-restore --verify-no-changes`: passed.
