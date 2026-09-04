# Loren Project Status

**Last updated:** 2026-09-04  
**Current version phase:** `v0.1 — Trustworthy Core development`  
**Current decision gates:** `Gate A — PASSED`, `Gate B — PASSED`  
**Current milestone:** `M2 — Walking Skeleton`

This file is the authoritative progress ledger for the repository. `README.md` and `README.vi.md` summarize it.

## Current status

Loren has completed `v0.0 — Architecture / Feasibility` and `M1 — Engineering Foundation`. M2 now has the complete owner-facing implementation candidate on top of the already-proven production read vertical slice.

- **Gate A PASSED** through ADR-001: Loren owns canonical identity/state/memory/policy/action authorization/audit.
- **Gate B PASSED** through ADR-002: the provider-neutral v0.1 stack and brain boundary are technically proven.
- **M0 COMPLETE**: provider loop, cancellation, MCP, persistence/recovery, and host proofs passed.
- **M1 COMPLETE**: production solution, deterministic tests, CI, package/version policy, and provider-independent Core are in place.
- **M2 Slice 1 COMPLETE**: read-only ActionGateway, Loren-owned run/action IDs, audit path, and `github.read_repository` executor.
- **M2 Slice 2 COMPLETE**: production `OllamaBrain : IBrain`, typed action-schema translation, observation replay, cancellation, and provider-secret isolation.
- **M2 Slice 3 COMPLETE**: `Loren.Web` composes OllamaBrain, AgentLoop, ActionGateway, read-only GitHub execution, and audit through DI; deterministic end-to-end production-component coverage passes.
- **M2 Slice 4 COMPLETE — TRUSTED LIVE BACKEND PROOF**: exact-main trusted GitHub Actions run completed a real Ollama Cloud -> production Loren host -> real GitHub read -> Ollama final-answer round trip.
- **M2 Slice 5 IMPLEMENTED — OWNER PREVIEW SURFACE**: one-owner cookie authentication, protected `/api/run`, minimal owner console, sign-out/session path, and owner-visible per-run audit presentation are implemented. CI now exercises the fail-closed unauthenticated boundary and authenticated console surface.

M2 remains active until the new exact-main trusted live workflow proves the complete owner-authenticated production path. No GitHub write path is allowed in M2.

## Accepted v0.1 stack

```text
C# 14 / .NET 10 LTS
ASP.NET Core
small Loren-owned bounded agent loop
provider-neutral IBrain
  ├─ Ollama adapter
  ├─ OpenAI adapter
  └─ future providers/local models
MCP C# SDK behind Loren action contracts
SQLite + EF Core
Blazor Web App
xUnit / Microsoft Testing Platform
```

## M2 previously trusted live backend proof

Workflow: `M2 Trusted Live Read Proof`  
Run: `33781183510` / run #1  
Trusted commit: `717fc92b167ce40c2d1652bf66cefce22b123577`  
Trigger branch: `proof/m2-live-read`, required to point exactly at current `main`.

Observed live path:

```text
Development-only localhost proof endpoint
 -> production LorenRunService
 -> production OllamaBrain (gpt-oss:120b)
 -> POST https://ollama.com/api/chat        200
 -> ActionRequest(github.read_repository)
 -> production AgentLoop / ActionGateway
 -> ReadOnlyActionPolicy                    allow
 -> production GitHubReadRepositoryExecutor
 -> GET https://api.github.com/repos/rua-den/loren   200
 -> structured ActionResult
 -> production OllamaBrain second turn
 -> POST https://ollama.com/api/chat        200
 -> final answer
 -> LorenRunResult + correlated audit
```

Live result:

```text
runId:       20c8bdc28b904ac1a92ae244f5346c97
turns:       2
actionCount: 1
final:       Repository name: rua-den/loren
             Default branch: main
```

Audit for the same action ID:

```text
ActionRequested   github.read_repository   requested
PolicyEvaluated   github.read_repository   allow
ActionCompleted   github.read_repository   succeeded
```

That proof established the real provider/tool path. The updated M2 trusted workflow now targets the normal production owner surface instead of the temporary development proof endpoint.

## M2 owner preview implementation

Normal owner path:

```text
Owner browser
 -> /login
 -> one-owner cookie session
 -> protected owner console
 -> POST /api/run
 -> LorenRunService
 -> AgentLoop / configured IBrain
 -> ActionGateway / policy / GitHub read
 -> structured ActionResult
 -> final answer
 -> correlated audit rendered to owner
```

Authentication behavior:

- `LOREN_OWNER_PASSWORD` is host configuration and is not placed in brain/tool context;
- the authentication service keeps a SHA-256 digest for fixed-time comparison rather than retaining the plaintext password itself;
- cookie is `HttpOnly`, `SameSite=Strict`, non-persistent, and uses `Secure` when the request is secure;
- unauthenticated `/api/*` requests fail with HTTP `401` instead of redirecting into HTML;
- wrong passwords fail closed;
- owner console and `/api/run` require authorization;
- `/health` remains public;
- `/internal/dev/run` remains absent by default and is not part of normal owner use.

Deployment beyond localhost must use HTTPS or an equivalent trusted TLS-terminating reverse-proxy boundary.

## M2 security invariants already proven or deterministically checked

- model output cannot choose trusted Loren `RunId` / `ActionId` values;
- every tool action crosses Loren's ActionGateway;
- unregistered and non-read-only actions fail closed;
- policy/executor failures are converted to safe results and audited;
- cancellation is propagated with terminal audit behavior;
- `github.read_repository` is public GET-only and has no GitHub write credential path;
- Ollama provider JSON/types remain outside `Loren.Core`;
- provider API key is outside serializable options/model context and sent only as Ollama authorization;
- Ollama and GitHub use separate named `HttpClient` instances;
- the default host has no unauthenticated production run API;
- owner console/API are authentication-gated;
- the temporary `/internal/dev/run` route is absent by default and CI verifies HTTP `404`;
- enabling the temporary route outside `Development` fails startup;
- trusted live provider work is guarded by an exact-current-main check before using the repository secret.

## Current milestone — M2 Walking Skeleton

Target end state:

```text
Owner / minimal UI
 -> owner auth/session
 -> Loren Runtime
 -> configured IBrain
 -> github.read_repository ActionRequest
 -> Loren ActionGateway
 -> GitHub read executor
 -> structured ActionResult
 -> IBrain final response
 -> owner-visible Audit
```

Implemented inside M2:

- [x] ActionGateway read path;
- [x] structured GitHub read-only executor;
- [x] Loren-owned run/action IDs;
- [x] append-oriented audit path;
- [x] bounded deterministic AgentLoop;
- [x] production Ollama `IBrain` adapter;
- [x] production host/DI composition;
- [x] deterministic production-component E2E coverage;
- [x] default host does not expose the temporary unauthenticated run route;
- [x] trusted exact-main live Ollama -> ActionGateway -> real GitHub read -> final answer backend proof;
- [x] one-owner authentication/session implementation;
- [x] minimal owner request console and protected `/api/run`;
- [x] owner-visible correlated audit presentation;
- [x] CI auth/default-surface smoke checks defined.

Still required before M2 exits:

- [ ] merge the owner preview implementation with CI green;
- [ ] run the updated exact-main trusted live proof through authenticated `/api/run`;
- [ ] record the trusted result and advance the active milestone to M3.

## Next execution sequence

```text
owner preview PR CI
    |
    v
merge exact tested implementation to main
    |
    v
move proof/m2-live-read exactly to current main
    |
    v
trusted live owner-authenticated Ollama -> GitHub read -> audit proof
    |
    v
M2 COMPLETE / FIRST OWNER-TESTABLE LOREN PREVIEW
    |
    v
M3 — Canonical state
```

## Historical gates

M0's earlier trusted brain proof used Ollama Cloud with `gpt-oss:120b` to validate the provider-neutral brain contract, cancellation, MCP, SQLite/EF recovery, and host feasibility. M1 then rebuilt production code behind Loren-owned interfaces instead of promoting spike code directly.

The production M2 proof is stronger because it runs through actual `Loren.Web` composition, production `OllamaBrain`, production AgentLoop/ActionGateway/policy/audit, and production GitHub executor. The final M2 proof additionally requires the normal owner authentication/session surface.

## Progress-update rule

Any merge that changes capability, milestone completion, ADR status, validated dependencies/providers, or the next execution target must update:

1. `docs/status.md`;
2. `README.md`;
3. `README.vi.md`;
4. the relevant ADR/plan when a decision or milestone changes.

A milestone is not considered closed until implementation/tests and repository documentation agree.
