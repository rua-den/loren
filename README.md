# Loren

**English** · [Tiếng Việt](README.vi.md)

Loren is a long-lived personal intelligence system with persistent memory, explicit permissions, tool use, and eventually proactive behavior across the owner's digital life.

> **The model is replaceable compute. Loren owns identity, memory, context, policy, action boundaries, and history.**

## Core principles

1. **Memory-first** — durable state survives conversations, restarts, and provider changes.
2. **Tool-first** — external facts/actions come from authoritative tools instead of model guessing.
3. **Permission-first** — a model may request an action; Loren authorizes and executes it.
4. **Model-independent** — Ollama, OpenAI, Claude, local models, and future providers are adapters.
5. **Auditable** — consequential actions and important state changes must be reconstructable.
6. **Progressive autonomy** — proactive/background behavior comes only after lower-level trust boundaries are proven.

## Current status

**Last updated:** 2026-09-03  
**Phase:** `v0.1 — Trustworthy Core development`  
**Current milestone:** `M2 — Walking Skeleton`

Completed:

- **Gate A / ADR-001:** Loren owns canonical identity/state/policy/action authorization.
- **Gate B / ADR-002:** provider-neutral v0.1 stack accepted; M0 complete.
- **M1:** production engineering foundation complete.
- **M2 Slice 1:** read-only ActionGateway + structured `github.read_repository` + Loren-owned run/action IDs + audit.
- **M2 Slice 2:** production `OllamaBrain : IBrain` with typed action schemas, observation replay, cancellation, and provider-secret isolation.
- **M2 Slice 3:** production ASP.NET host composes OllamaBrain, AgentLoop, ActionGateway, read-only GitHub execution, and audit through DI; deterministic production-component E2E coverage passes.
- **M2 Slice 4:** **trusted live production proof passed** on exact `main`.

Trusted run `33781183510` proved:

```text
real Ollama Cloud (gpt-oss:120b)
 -> production Loren.Web
 -> production OllamaBrain
 -> ActionRequest(github.read_repository)
 -> production AgentLoop / ActionGateway / ReadOnlyActionPolicy
 -> real GET https://api.github.com/repos/rua-den/loren
 -> structured ActionResult
 -> real Ollama second turn
 -> final answer: rua-den/loren / main
 -> correlated audit
```

Observed result: `turns=2`, `actionCount=1`, audit sequence `ActionRequested -> PolicyEvaluated -> ActionCompleted`, final action outcome `succeeded`.

M2 now has a real model-to-tool vertical path. Remaining work before M2 exits is **one-owner auth/session, minimal owner UI/endpoint, and owner-visible audit presentation**.

Detailed status: [`docs/status.md`](docs/status.md).

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

## Current production read architecture

```text
Owner (next: authenticated UI)
        |
        v
Loren.Web
        |
        v
AgentLoop -> IBrain -> Ollama
        |
        v
ActionRequest
        |
        v
ActionGateway
  -> ReadOnlyActionPolicy
  -> Audit
        |
        v
GitHubReadRepositoryExecutor
        |
        v
real public GitHub GET
```

Important invariants already proven:

- every action crosses Loren's ActionGateway;
- model output cannot choose trusted Loren run/action IDs;
- non-read-only/unregistered actions fail closed;
- provider API key stays outside model-visible request JSON and owner-visible live response;
- Ollama and GitHub transports are separated;
- `github.read_repository` has no GitHub write credential path;
- the temporary `/internal/dev/run` route is absent by default and may only exist in Development with an explicit flag;
- trusted live-secret validation requires an exact-current-main guard.

No GitHub write path is allowed in M2.

## Next

```text
one-owner authentication/session
 -> minimal owner request UI
 -> owner-visible audit
 -> "Loren, check repo rua-den/loren."
 -> FIRST OWNER-TESTABLE LOREN PREVIEW
 -> M3 Canonical State
```

## Version path

```text
v0.0  architecture / feasibility        ✓ complete
v0.1  trustworthy core                 <- current / M2
v0.2  useful project assistant
v0.3  personal operations
v0.4  voice + device presence
v0.5  proactive/background Loren
v0.6+ real-use hardening
v1.0  stable personal daily driver
```

Versions advance by exit gates, not dates or code volume.

## Documentation

- [`docs/status.md`](docs/status.md) — authoritative current progress
- [`docs/development.md`](docs/development.md) — build/test/configuration/dependency guidance
- [`docs/vision.md`](docs/vision.md) — product vision
- [`docs/architecture.md`](docs/architecture.md) — active system boundaries
- [`docs/plans/master-plan.md`](docs/plans/master-plan.md) — version milestones and gates
- [`docs/plans/v0.1.md`](docs/plans/v0.1.md) — detailed v0.1 implementation plan
- [`docs/decisions/001-agent-runtime-strategy.md`](docs/decisions/001-agent-runtime-strategy.md) — accepted Loren-owned core/runtime boundary
- [`docs/decisions/002-v0.1-technology-stack.md`](docs/decisions/002-v0.1-technology-stack.md) — accepted provider-neutral v0.1 stack and M0 evidence
- [`docs/memory.md`](docs/memory.md) — memory model
- [`docs/permissions.md`](docs/permissions.md) — permission model
- [`docs/security.md`](docs/security.md) — security baseline
- [`docs/skills.md`](docs/skills.md) — skill/tool model

This repository is the source of truth for Loren's product decisions, architecture, delivery plans, implementation, progress, and release history.
