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

**Last updated:** 2026-09-04  
**Phase:** `v0.1 — Trustworthy Core development`  
**Current milestone:** `M2 — Walking Skeleton`

Completed or implemented:

- **Gate A / ADR-001:** Loren owns canonical identity/state/policy/action authorization.
- **Gate B / ADR-002:** provider-neutral v0.1 stack accepted; M0 complete.
- **M1:** production engineering foundation complete.
- **M2 Slice 1:** read-only ActionGateway + structured `github.read_repository` + Loren-owned run/action IDs + audit.
- **M2 Slice 2:** production `OllamaBrain : IBrain` with typed action schemas, observation replay, cancellation, and provider-secret isolation.
- **M2 Slice 3:** production ASP.NET host composition and deterministic production-component E2E.
- **M2 Slice 4:** trusted exact-main live backend proof passed through real Ollama Cloud and real GitHub read.
- **M2 Slice 5:** one-owner cookie auth, protected `/api/run`, minimal owner console, and owner-visible correlated audit are implemented.

M2's implementation is now owner-testable in shape. Its exit gate is one final exact-main trusted live proof through the **authenticated production owner path**, after which the active milestone advances to M3.

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

## Current M2 production path

```text
Owner browser
        |
        v
/login -> one-owner cookie session
        |
        v
protected owner console
        |
        v
POST /api/run
        |
        v
LorenRunService
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
        |
        v
structured result -> final answer -> owner-visible audit
```

Important invariants already implemented/proven:

- every action crosses Loren's ActionGateway;
- model output cannot choose trusted Loren run/action IDs;
- non-read-only/unregistered actions fail closed;
- owner and provider credentials stay outside model-visible tool context;
- owner console and `/api/run` require authentication;
- unauthenticated `/api/*` requests fail with HTTP `401`;
- owner session cookie is `HttpOnly` and `SameSite=Strict`;
- Ollama and GitHub transports are separated;
- `github.read_repository` has no GitHub write credential path;
- the temporary `/internal/dev/run` route is absent by default and is not the normal owner path;
- trusted live-secret validation requires an exact-current-main guard.

No GitHub write path is allowed in M2.

## Run the owner preview locally

Set local secrets in the process environment:

```bash
export LOREN_OWNER_PASSWORD='choose-a-local-owner-password'
export OLLAMA_API_KEY='your-provider-secret'
dotnet run --project src/Loren.Web/Loren.Web.csproj
```

Then open the root URL printed by ASP.NET Core, sign in, and run the prefilled request:

```text
Loren, check repo rua-den/loren.
```

Do not commit real values. Use HTTPS or a trusted TLS-terminating reverse proxy when exposing the host beyond localhost. More details: [`docs/development.md`](docs/development.md).

## Next

```text
owner preview PR CI
 -> merge to main
 -> trusted exact-main owner-authenticated live proof
 -> M2 COMPLETE / FIRST OWNER-TESTABLE LOREN PREVIEW
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
