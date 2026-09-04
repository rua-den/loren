# Loren Roadmap

This is the concise product roadmap. The authoritative delivery sequence, milestones, and version gates live in [`docs/plans/master-plan.md`](plans/master-plan.md).

Loren advances by **proven capability and trust**, not by calendar dates.

## Current status

**Current stage:** `v0.1 — Trustworthy Core development`  
**Passed:** `Gate A — Core ownership`, `Gate B — v0.1 implementation stack`, `Gate C — canonical state/memory lifecycle`, `Gate D — action/approval/credential boundary`  
**Completed milestone:** `M4 — Trusted Durable Memory`  
**Current milestone:** `M5 — Action/Credential Boundary + Narrow GitHub Writes`  
**Now:** `M5 Slice 2 — write credential resolver + secret redaction/revocation after Slice 1 merges`

M2 proved the first authenticated owner-to-real-tool path. M3 made Project/Repository identity provider-independent and restart-safe. M4 made durable memory restart-safe, correctable, forgettable, bounded before brain use, and resistant to model/external-content poisoning. Gate D/ADR-004 froze the first write-capable security contract. M5 Slice 1 now implements the deterministic policy/approval/read-only foundation without enabling any real GitHub mutation.

M4 path:

```text
OWNER_EXPLICIT durable memory
-> restart-safe SQLite persistence
-> OWNER_CORRECTION append/supersede
-> current-memory retrieval
-> source/provenance trust filtering
-> bounded prepared memory context
-> owner forget / full correction-chain purge
-> adversarial poisoning boundary
-> Windows local + CI integration hardening
```

M4 evidence:

- PR #18 — OWNER_EXPLICIT persistence;
- PR #19 — correction/supersession;
- PR #20 — authority-aware prepared context;
- PR #21 — forget/delete without resurrection;
- PR #22 — provenance and poisoning/trust-boundary acceptance;
- PR #23 — Windows-safe SQLite temp integration tests + permanent Windows CI.

PR #23 post-merge main CI #163 / `33894104116` passed Ubuntu full gates and Windows integration; the owner local Windows integration suite also passed.

Gate D evidence:

- PR #24 merged at `b8649cb563e30af845a0b383103797632bed79a4`;
- exact-head CI #164 / `33896004193` — Ubuntu full gate + Windows integration **PASS**.

Gate D write path contract:

```text
brain requests write
-> canonical target resolution
-> deterministic policy
-> exact owner approval artifact
-> one-time atomic consume / replay rejection
-> fail-closed global read-only
-> write-specific credential resolver
-> executor
-> independent post-write verification
-> correlated redacted audit
```

### M5 Slice 1 implemented in PR #25

```text
typed ActionAccessClass
-> trusted ActionAuthorizationContext
-> exact SHA-256 ActionIntentFingerprint
-> Loren-owned ApprovalId / ActionApproval
-> persistent IActionApprovalStore
-> GateDActionPolicy
-> gateway-enforced approval for every non-read action
-> atomic one-time consume
-> replay / expiry / revoke / mismatch rejection
-> host-controlled LOREN_ENABLE_WRITES read-only default
-> migration-drift regression coverage
```

Implementation head `15a2b2c4c853324a546a55d13da22d94d4ac5765` passed CI #172 / `33898878125` across zero-warning Ubuntu build, all tests, format/security/web gates, and Windows integration.

Slice 1 deliberately registers no GitHub mutation executor. Approval is consumed before the first consequential executor attempt; independent retry after failure/ambiguity needs fresh approval.

---

## v0.0 — Architecture and feasibility [COMPLETE]

Goal: establish the ownership boundary and prove the concrete v0.1 stack is implementable.

Exit evidence:

- ADR-001 Accepted;
- ADR-002 Accepted after technical spikes;
- v0.1 implementation plan finalized;
- brain/action/MCP/persistence/host boundaries proven enough to scaffold production code.

---

## v0.1 — Trustworthy Core [ACTIVE]

Goal: build the smallest trustworthy Loren.

Required flows:

```text
"Loren, repo wedding hiện sao rồi?"
"Nhớ rằng project này production deploy phải hỏi tao."
"Tạo branch và chuẩn bị thay đổi X."
"Tại sao mày vừa làm việc đó?"
```

Milestones:

```text
M1 Engineering Foundation              ✓ complete
M2 Walking Skeleton                    ✓ complete
M3 Canonical Project/Repository State  ✓ complete
M4 Trusted Durable Memory              ✓ complete
Gate D Action/Credential Policy        ✓ passed / ADR-004
M5 Action/Credential Boundary + Writes <- current
M6 Minimal Daily-use UI
M7 Export/Restore + Recovery
M8 Security/Reliability E2E
```

Core capabilities across v0.1:

- one-owner web interface;
- provider-neutral brain boundary;
- bounded Loren-owned agent loop;
- canonical Project/Repository state;
- trusted durable memory with provenance, correction and forgetting;
- ActionGateway + exact one-time approvals;
- credential-isolated GitHub read/narrow writes;
- fail-closed global read-only control;
- post-write verification;
- audit trail;
- export/wipe/restore proof;
- adversarial security/reliability tests.

### M5 implementation order

```text
Slice 1 typed policy + approval + read-only        ✓ implemented / PR #25 merge gate
Slice 2 credential resolver + redaction            <- next
Slice 3 create non-default branch + verify
Slice 4 controlled file/commit path + verify
Slice 5 open pull request + verify
Slice 6 replay/revocation/injection/audit E2E
```

No real GitHub mutation is enabled until the policy/approval/read-only/credential foundations are green on `main`.

Allowed v0.1 writes remain limited to non-default branch creation, controlled file/commit changes on a non-default branch, and opening a pull request. Direct default-branch writes, merge, force-push/history rewrite, deletion/admin/security changes, secret-management actions, and production deployment remain forbidden.

Detailed plan: [`docs/plans/v0.1.md`](plans/v0.1.md)

---

## v0.2 — Useful Project Assistant

Goal: make the trusted v0.1 core useful for richer daily project work.

Candidate capabilities:

- public web research with source provenance;
- explicit research-to-memory/decision promotion;
- persistent reminders/light scheduler;
- project decisions/procedures;
- richer GitHub project health summaries;
- improved memory retrieval/conflict handling;
- model/run cost visibility.

Before private/background personal operations, Gate E must pass.

---

## v0.3 — Personal Operations

Candidate capabilities include Gmail, Google Calendar, server/VPS health and constrained actions, filesystem integrations, notifications, cross-tool context, daily brief, and stronger data/credential scopes.

---

## v0.4 — Voice and Device Presence

Candidate capabilities include mobile/PWA, trusted devices, push-to-talk, STT/TTS, notification actions, and optional device nodes. Gate F must pass first.

---

## v0.5 — Proactive Loren

Candidate capabilities include event ingestion, GitHub/webhook watchers, proactive notifications, recurring/background work, bounded standing permissions, quotas, active-task visibility, and global pause. Gate G must pass first.

---

## v0.6+ — Real-use hardening

Let actual usage determine priorities: memory consolidation, more providers/local models, Home Assistant, computer use, offline/private execution, performance/cost, packaging, and UX.

---

## v1.0 — Stable Personal Daily Driver

v1.0 means Loren's core can be trusted as a long-lived daily assistant: stable workflows, tested recovery/migration, continuity across upgrades/providers, maintainable action/skill boundaries, reliable background/device controls, secret rotation, reconstructable audit, and documented privacy/security defaults.

Gate H must pass before release.

---

## Ongoing rule

At every version ask:

> Does this capability strengthen Loren's personal intelligence, or are we rebuilding infrastructure a mature project already solves better?

Reuse infrastructure where it is safe and replaceable. Spend custom engineering on Loren's identity, memory, policy, personal semantics, and trustworthy behavior.
