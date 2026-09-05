# Loren Thread Handoff

**Updated:** 2026-09-05  
**Repository:** `rua-den/loren`  
**Source of truth:** GitHub repository state and `docs/status.md`  
**Current phase:** `v0.1 — Trustworthy Core development`  
**Current milestone:** `M5 — Action/Credential Boundary + Narrow GitHub Writes`

This file is a compact continuation checkpoint for a fresh ChatGPT thread. It does not replace `docs/status.md`; it points to the exact work that is in flight and the next execution step.

## Current pull request

```text
PR: #25 — feat: add M5 one-time write approval foundation
branch: feat/m5-policy-approval-slice1
base: main
base commit: b8649cb563e30af845a0b383103797632bed79a4
state: OPEN / mergeable / not merged
```

PR #25 is M5 Slice 1. It deliberately adds **no real GitHub mutation executor**.

## Last validated code state

The last code-changing self-review hardening head is:

```text
5ed9049eeedf3210f1df13a0c8735b67d7e4766e
```

CI evidence:

```text
CI #186
run: 33900018499
Ubuntu full gate: PASS
Windows integration: PASS
```

The Ubuntu full gate includes build, all tests, format verification, secret scan, dependency vulnerability scan, and web/auth smoke tests.

Earlier important implementation validation:

```text
base implementation head: 15a2b2c4c853324a546a55d13da22d94d4ac5765
CI #172 / run 33898878125: PASS on Ubuntu full gate + Windows integration
```

CI #172 also proved the fix for the EF migration/model drift that had caused the integration suite to fail before behavior tests could run.

## What M5 Slice 1 currently delivers

- typed `ActionAccessClass`: `READ`, `REVERSIBLE_WRITE`, `EXTERNAL_WRITE`, `PRIVILEGED_WRITE`;
- trusted `ActionAuthorizationContext` carrying canonical Project/Repository target and owner principal outside model-visible action arguments;
- Loren-owned `ApprovalId`, `ActionApproval`, lifecycle/status types, and EF-neutral `IActionApprovalStore`;
- deterministic SHA-256 exact-intent fingerprint;
- SQLite `ActionApprovals` persistence via migration `202609040003_AddActionApprovals`;
- atomic one-time consume with replay, expiry, revocation, mismatch, and concurrent-consume rejection;
- `GateDActionPolicy` with fail-closed global read-only behavior;
- ActionGateway defense-in-depth requiring approval for every non-read action even if policy accidentally returns `Allow`;
- model-visible `approvalId` text cannot become trusted approval;
- `LOREN_ENABLE_WRITES` defaults fail-closed;
- permanent EF migration-drift regression test;
- production still registers only the existing GitHub read executor.

## Self-review hardening already completed

Two additional trust-boundary issues were found and fixed after the base implementation was green:

1. **Do not burn an approval when no executor exists.**
   - executor registration is confirmed before approval consumption;
   - approval is still consumed immediately before the first real executor attempt.

2. **Freeze approved intent against TOCTOU mutation.**
   - model-visible `ActionRequest.Arguments` are defensively copied/frozen;
   - trusted normalized target data is defensively copied/frozen;
   - approved/fingerprinted intent cannot later be mutated so the executor sees a different request.

Both hardenings are covered by regression tests and passed CI #186 on Ubuntu and Windows.

## Migration failure that was fixed

A prior PR CI failure showed 22+ integration tests failing with EF `PendingModelChangesWarning`.

A dedicated migration-drift diagnostic reduced the mismatch to:

```text
AddColumn ActionApprovals.RevokedAtUnixMs (Int64)
```

The EF model metadata was corrected. The warning was **not suppressed**. A permanent snapshot-vs-design-time-model regression test remains in the suite.

## Current documentation state

The M5 Slice 1 status and Slice 2 next target are already synchronized across:

- `docs/status.md` — authoritative progress ledger;
- `README.md`;
- `README.vi.md`;
- `docs/roadmap.md`;
- `docs/plans/v0.1.md`;
- `docs/plans/master-plan.md`;
- `docs/development.md`;
- `.env.example`.

`docs/architecture.md` already reflects M5 Slice 1 and Slice 2 direction. Before merging PR #25, do one final review that the execution-order wording matches the hardened implementation:

```text
freeze trusted/proposed intent
-> policy/read-only
-> verify executor registration
-> recompute exact fingerprint
-> validate + atomically consume approval
-> first consequential executor attempt
```

## Exact next action in a fresh thread

Do **not** start Slice 2 yet.

Continue PR #25 in this order:

```text
1. Read docs/status.md and this handoff.
2. Review docs/architecture.md against the hardened ActionGateway order.
3. Make only documentation corrections needed for consistency.
4. Treat the resulting PR head as frozen.
5. Run/fetch final exact-head PR CI.
6. Require Ubuntu full gate + Windows integration PASS.
7. Self-review the final PR diff for accidental mutation registration, secret leakage, scope/lifetime issues, replay/TOCTOU regressions, and docs drift.
8. If clean and green, squash-merge PR #25 to main using the exact expected head SHA.
9. Verify post-merge main CI.
10. Update status/README EN+VI/roadmap/plans with the merged commit and main CI evidence.
11. Only then create the M5 Slice 2 branch.
```

## M5 Slice 2 next target

After PR #25 is on green `main`, implement the credential boundary:

- write-specific credential resolver abstraction;
- host/env-backed local v0.1 secret implementation;
- secret value materialized only inside the controlled executor boundary;
- logical separation of read/write credential purpose;
- missing/revoked credential fails closed;
- no silent fallback to a broader token;
- credential revocation overrides prior approval;
- redaction acceptance across request, logs, exceptions, audit, action results, and brain context;
- still no broad GitHub write surface until this boundary is green.

After Slice 2, proceed to the first verified real write: create a non-default branch, then controlled file/commit, then open PR.

## Hard security invariant

> The brain may request an action. Only Loren can authorize and execute it.

Authentication is not write approval. External/model content cannot grant approval. Secrets never belong in brain context, canonical memory, model-visible action arguments, audit payloads, or owner-visible results.
