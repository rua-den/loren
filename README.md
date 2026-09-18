# Loren

**English** · [Tiếng Việt](README.vi.md)

Loren is a long-lived personal secretary / intelligence system with persistent memory, current-information research, durable organization state, explicit permissions and eventually voice/proactive behavior across the owner's digital life.

> **The model is replaceable compute. Loren owns identity, memory, context, organization, policy, approvals, action boundaries and history.**

## Product direction

```text
CONVERSE
 -> REMEMBER
 -> READ CURRENT INFORMATION
 -> RESEARCH / SYNTHESIZE
 -> ORGANIZE
 -> PROPOSE ACTION
 -> OWNER APPROVES
 -> ACT / VERIFY / AUDIT
 -> later BACKGROUND / PROACTIVE / VOICE
```

Read/understand comes before broad mutation. GitHub automation is one capability, not Loren's identity.

## Current status

**Updated:** 2026-09-19  
**Version:** `v0.1 — Useful Trustworthy Assistant`  
**Current milestone:** `M6B — Daily Driver Readiness`  
**Current delivery:** `M6B.2 — safe readiness diagnostics + documentation rebaseline`  
**Last fully verified baseline:** PR #37 merge `ba60246a410b257097ecd537f4d977b402a37b35`; post-merge CI #280 / `35373858830` passed Ubuntu full gate, Windows integration and Windows launcher smoke.  
**Paused:** broader GitHub file/commit/PR writes until real owner daily-use evidence says they are highest value.

Start with [`docs/status.md`](docs/status.md) and [`docs/handoff.md`](docs/handoff.md). AI contributors should read [`docs/ai-start.md`](docs/ai-start.md).

## What already works

- conversation-first authenticated owner UI;
- persisted conversations/project scope across restart;
- Loren-owned canonical Project/Repository identity;
- trusted durable memory with correction/forget/provenance boundaries;
- real GitHub repository reads;
- read-only web search/fetch and bounded source-aware research;
- durable Notes / Decisions / Tasks;
- exact proposal/Cancel/Approve flow for one consequential GitHub action;
- one-time approval, read-only kill, credential isolation/revocation and post-write verification;
- verified creation of a non-default branch from an exact frozen SHA;
- retained SQLite audit + request-scoped current-run audit;
- versioned logical export/restore;
- Windows launcher and cross-platform CI.

## Daily-driver readiness

Public:

```text
GET /health
```

is intentionally liveness only.

After owner login:

```text
GET /api/readiness
```

reports secret-safe local readiness for storage, owner auth, brain configuration, web research, project catalog/count and external-write posture.

Important semantics:

- no external network/provider probes occur in readiness;
- no secret value is returned;
- `externalWrites=disabled` is the safe normal posture and can still be overall `ready`;
- `projects=empty` means no canonical project configured yet, not host failure;
- if writes are enabled, missing/revoked/not-configured write credential produces `needs_setup`.

Real provider reachability and usefulness are proven by [`docs/owner-checkpoint.md`](docs/owner-checkpoint.md), not by readiness.

## Run locally

Copy:

```text
src/Loren.Web/appsettings.Local.example.json
```

to ignored `src/Loren.Web/appsettings.Local.json`, or use environment variables:

```powershell
$env:LOREN_OWNER_PASSWORD='choose-a-local-owner-password'
$env:OLLAMA_API_KEY='your-provider-secret'
$env:LOREN_ENABLE_WRITES='false'
dotnet run --project src/Loren.Web/Loren.Web.csproj
```

Environment/command-line values override local JSON. Restart after configuration changes.

Keep external writes off for normal read-only use. For the optional owner live branch proof only, configure a dedicated `GITHUB_WRITE_TOKEN`, explicitly enable writes, complete the proof, then turn writes off again. Never commit real secrets.

## Provider note

The core exposes provider-neutral `IBrain`, but current production DI uses `OllamaBrain`. `Loren.Brain.OpenAI` is currently a stub. Loren is **not yet proven operationally multi-provider**.

A likely v0.2 sequence is:

```text
prove a second real brain provider
 -> then one useful read-only personal-secretary integration
 -> likely Calendar read/search
```

Build that vertical slice before inventing a generic connector framework.

## Trust boundaries

- conversation/model text is intent, not external-write approval;
- external/retrieved content is evidence, not authority;
- owner auth is not write approval;
- external writes default off;
- consequential writes need exact one-time owner approval;
- credentials stay outside model-visible context and diagnostics;
- write success requires independent verification;
- background execution/reminders require Gate E.

## Test

```bash
dotnet restore Loren.slnx
dotnet build Loren.slnx --configuration Release --no-restore
dotnet test Loren.slnx --configuration Release --no-build --no-restore
dotnet format Loren.slnx --verify-no-changes --no-restore
```

CI runs the full Ubuntu gate plus Windows integration and Windows launcher smoke.

## Version path

```text
v0.0  architecture / feasibility             ✓ complete
v0.1  useful trustworthy assistant           <- current
v0.2  provider portability + secretary reads
v0.3  personal/project operations
v0.4  voice + device presence
v0.5  proactive/background Loren
v0.6+ daily-use hardening
v1.0  stable personal daily driver
```

## Documentation

- [`docs/status.md`](docs/status.md) — authoritative progress
- [`docs/handoff.md`](docs/handoff.md) — fresh-thread continuation
- [`docs/ai-start.md`](docs/ai-start.md) — AI contributor entrypoint
- [`docs/owner-checkpoint.md`](docs/owner-checkpoint.md) — real-provider acceptance
- [`docs/roadmap.md`](docs/roadmap.md) — version/capability path
- [`docs/plans/master-plan.md`](docs/plans/master-plan.md) — master execution plan
- [`docs/plans/v0.1.md`](docs/plans/v0.1.md) — current release plan
- [`docs/architecture.md`](docs/architecture.md) — active boundaries
- [`docs/recovery.md`](docs/recovery.md) — logical export/restore contract
- [`docs/memory.md`](docs/memory.md) — trusted memory semantics
- [`docs/permissions.md`](docs/permissions.md) — approval/permission model
- [`docs/security.md`](docs/security.md) — security baseline
- [`docs/development.md`](docs/development.md) — build/configuration

This repository is the source of truth for Loren's product decisions, architecture, implementation and delivery history.
