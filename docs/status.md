# Loren Project Status

**Last updated:** 2026-09-03  
**Current version phase:** `v0.1 — Trustworthy Core development`  
**Current decision gates:** `Gate A — PASSED`, `Gate B — PASSED`  
**Current milestone:** `M2 — Walking Skeleton`

This file is the authoritative progress ledger for the repository. `README.md` and `README.vi.md` summarize it.

## Current status

Loren has completed `v0.0 — Architecture / Feasibility` and `M1 — Engineering Foundation`. M2 now has a real production read vertical slice, including a trusted live provider/tool proof.

- **Gate A PASSED** through ADR-001: Loren owns canonical identity/state/memory/policy/action authorization/audit.
- **Gate B PASSED** through ADR-002: the provider-neutral v0.1 stack and brain boundary are technically proven.
- **M0 COMPLETE**: provider loop, cancellation, MCP, persistence/recovery, and host proofs passed.
- **M1 COMPLETE**: production solution, deterministic tests, CI, package/version policy, and provider-independent Core are in place.
- **M2 Slice 1 COMPLETE**: read-only ActionGateway, Loren-owned run/action IDs, audit path, and `github.read_repository` executor.
- **M2 Slice 2 COMPLETE**: production `OllamaBrain : IBrain`, typed action-schema translation, observation replay, cancellation, and provider-secret isolation.
- **M2 Slice 3 COMPLETE**: `Loren.Web` composes OllamaBrain, AgentLoop, ActionGateway, read-only GitHub execution, and audit through DI; deterministic end-to-end production-component coverage passes.
- **M2 Slice 4 COMPLETE — TRUSTED LIVE PROOF**: exact-main trusted GitHub Actions run completed a real Ollama Cloud -> production Loren host -> real GitHub read -> Ollama final-answer round trip.

M2 remains active. The remaining owner-facing work is one-owner authentication/session, a minimal request UI/endpoint, and owner-visible audit presentation.

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

## M2 trusted live production proof

Workflow: `M2 Trusted Live Read Proof`  
Run: `33781183510` / run #1  
Trusted commit: `717fc92b167ce40c2d1652bf66cefce22b123577`  
Trigger branch: `proof/m2-live-read`, required to point exactly at current `main`.

The exact-main guard passed before provider secrets were made useful to the host proof. `OLLAMA_API_KEY` was present only as a masked GitHub Actions secret.

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

The workflow also asserts that the provider secret is absent from the owner-visible JSON response. Deterministic host tests separately prove the Ollama bearer token is not propagated to the GitHub request; the live run exercised the same separated named `HttpClient` composition and performed both real external calls successfully.

## M2 security invariants already proven

- model output cannot choose trusted Loren `RunId` / `ActionId` values;
- every tool action crosses Loren's ActionGateway;
- unregistered and non-read-only actions fail closed;
- policy/executor failures are converted to safe results and audited;
- cancellation is propagated with terminal audit behavior;
- `github.read_repository` is public GET-only and has no GitHub write credential path;
- Ollama provider JSON/types remain outside `Loren.Core`;
- provider API key is outside serializable options/model context and sent only as Ollama authorization;
- Ollama and GitHub use separate named `HttpClient` instances;
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

Completed inside M2:

- [x] ActionGateway read path;
- [x] structured GitHub read-only executor;
- [x] Loren-owned run/action IDs;
- [x] append-oriented audit path;
- [x] bounded deterministic AgentLoop;
- [x] production Ollama `IBrain` adapter;
- [x] production host/DI composition;
- [x] deterministic production-component E2E coverage;
- [x] default host does not expose the temporary unauthenticated run route;
- [x] trusted exact-main live Ollama -> ActionGateway -> real GitHub read -> final answer proof;
- [x] provider secret absent from owner-visible live response and separated from GitHub transport by the tested host composition.

Still required before M2 exits:

- [ ] one-owner authentication/session;
- [ ] minimal owner request UI/endpoint;
- [ ] owner-visible audit presentation for the request round trip.

No GitHub write path is allowed in M2.

## Next execution sequence

```text
NOW
one-owner authentication/session
    |
    v
minimal owner request UI + owner-visible audit
    |
    v
"Loren, check repo rua-den/loren."
    |
    v
FIRST OWNER-TESTABLE LOREN PREVIEW
    |
    v
M3 — Canonical state
```

## Historical gates

M0's earlier trusted brain proof used Ollama Cloud with `gpt-oss:120b` to validate the provider-neutral brain contract, cancellation, MCP, SQLite/EF recovery, and host feasibility. M1 then rebuilt production code behind Loren-owned interfaces instead of promoting spike code directly.

The production M2 proof above is stronger than the M0 spike because it runs through the actual `Loren.Web` composition, production `OllamaBrain`, production AgentLoop/ActionGateway/policy/audit, and production GitHub executor.

## Progress-update rule

Any merge that changes capability, milestone completion, ADR status, validated dependencies/providers, or the next execution target must update:

1. `docs/status.md`;
2. `README.md`;
3. `README.vi.md`;
4. the relevant ADR/plan when a decision or milestone changes.

A milestone is not considered closed until implementation/tests and repository documentation agree.
