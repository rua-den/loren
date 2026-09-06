# Loren Thread Handoff

**Updated:** 2026-09-06  
**Repository:** `rua-den/loren`  
**Source of truth:** GitHub repository state and `docs/status.md`  
**Current phase:** `v0.1 — Trustworthy Core development`  
**Current milestone:** `M5 — Action/Credential Boundary + Narrow GitHub Writes`

This file is the compact continuation checkpoint for a fresh ChatGPT thread. It does not replace `docs/status.md`.

## Green main baseline

M5 Slice 2 is merged and green on `main`.

```text
PR #26 — feat: add M5 write credential boundary
merge: f7fb36bae324dbd7bb8d12e02daf3fe0dd98e7da
frozen PR head: e9e2b07378e1435e62e6090829619603ac7df42b
PR CI #201 / 34027113298: Ubuntu full gate PASS + Windows integration PASS
post-merge main CI #202 / 34027255592: Ubuntu full gate PASS + Windows integration PASS
```

Slices complete on main:

```text
Slice 1 — typed write policy + exact one-time approval + fail-closed global read-only
Slice 2 — write credential purpose/reference + revocation + redaction boundary
```

## Current pull request

```text
PR: #27 — feat: add verified GitHub create-branch slice
branch: feat/m5-github-create-branch-slice3
base: main
base commit: f7fb36bae324dbd7bb8d12e02daf3fe0dd98e7da
state: OPEN
```

PR #27 is M5 Slice 3 and contains the first narrow real mutation capability: explicit-owner create of a **non-default GitHub branch** from an exact existing commit SHA, with independent ref/SHA verification before success.

## Slice 3 capability contract

```text
authenticated owner
 -> owner reviews exact project/repository/branch/source SHA
 -> presses “Approve & create branch”
 -> canonical Project/Repository resolution
 -> exact 5-minute ActionApproval creation
 -> ActionGateway policy/read-only check
 -> trusted-executor check
 -> exact fingerprint validation + atomic one-time consume
 -> write credential resolution
 -> GET repository/default branch preflight
 -> reject unsafe/default branch
 -> POST git/refs
 -> GET exact created ref
 -> verified SHA must equal approved source SHA
 -> redacted result + correlated audit
```

Important: authentication still does not authorize the write by itself. The explicit owner create-branch request is the approval event for that exact branch/SHA intent.

## Trust-boundary hardening added in Slice 3

- `ITrustedActionExecutor` was introduced because legacy `IActionExecutor` only receives model-visible `ActionRequest`.
- Every non-read executor must implement the trusted contract and receive the full Loren-owned `ActionExecutionRequest`.
- `ActionGateway` rejects a legacy non-read executor **before approval consumption**.
- `CredentialBoundActionExecutor` refuses direct untrusted invocation.
- Canonical repository owner/name comes only from `ActionAuthorizationContext.RepositoryLocator`, never from model-visible GitHub owner/repository arguments.
- exact branch + source SHA are frozen in trusted normalized target and compared again at executor boundary.
- create-branch client validates safe Git ref names and exact 40-character hexadecimal source SHA.
- creating/replacing the repository default branch is forbidden before mutation.
- POST success alone is insufficient; exact GET ref/SHA readback is required.

## Owner-testable vertical slice

PR #27 also adds an authenticated local owner path so a fresh database can actually exercise the feature:

```text
Owner console
 -> bootstrap canonical GitHub Project/Repository if database is empty
 -> enter exact existing source commit SHA
 -> enter new non-default branch name
 -> Approve & create branch
 -> inspect result + audit
```

Required local environment for the write checkpoint:

```text
LOREN_OWNER_PASSWORD=<local password>
LOREN_ENABLE_WRITES=true
GITHUB_WRITE_TOKEN=<GitHub token allowed to create refs in the target repository>
LOREN_GITHUB_WRITE_CREDENTIAL_REVOKED=false
```

`OLLAMA_API_KEY` is still separate and is never used as the GitHub write credential.

## Deterministic acceptance coverage in PR #27

- GitHub request order: GET preflight -> POST create ref -> GET verify ref.
- Authorization header uses the write credential only inside the client call.
- secret never appears in owner-visible operation result.
- default branch is rejected before POST.
- unsafe Git ref names are rejected before any HTTP call.
- verification SHA mismatch is never reported as success.
- owner workflow covers project resolution -> approval creation -> approval consume -> credential boundary -> verified write.
- revoked write credential overrides a fresh approval and produces **zero GitHub HTTP calls**.
- approval is still consumed before a credential/executor attempt; retry requires a fresh approval by design.

## CI history while hardening PR #27

Early CI failures were compiler/analyzer feedback, not permission relaxations:

```text
CI #207: invalid StartsWith/EndsWith overloads in branch-name validation
CI #208: CA1865 single-character overload analyzer
CI #210: nullable-flow warning at trusted branch/SHA handoff
```

All fixes preserve the same fail-closed behavior. Do not merge until the latest exact PR head passes both Ubuntu full gate and Windows integration.

## Exact next action in a fresh thread

```text
1. Read docs/status.md and this handoff.
2. Fetch PR #27 current head and exact-head CI.
3. Fix any remaining compiler/analyzer/test failures without weakening trust invariants.
4. Synchronize README.md and README.vi.md with Slice 2 complete + Slice 3 checkpoint.
5. Review final PR diff for:
   - no direct default-branch write path;
   - no merge/delete/admin capability;
   - no model-created approval;
   - no owner/repo selection from model-visible write arguments;
   - no credential fallback or leakage;
   - mandatory post-write SHA verification;
   - no approval replay.
6. Freeze exact PR head and require Ubuntu full gate + Windows integration PASS.
7. Merge PR #27 using expected head SHA.
8. Verify post-merge main CI.
9. Report the exact pull/test instructions to the owner.
10. Only after that move to the controlled file/commit-on-non-default-branch slice.
```

## Next M5 target after Slice 3

```text
controlled file/commit path on the approved non-default branch
 -> exact path/content/branch intent binding
 -> no default-branch write
 -> verify commit SHA + branch ref + content identity
```

Open PR comes only after the controlled file/commit slice is green.

## Hard security invariant

> The brain may request an action. Only Loren can authorize and execute it.

Authentication is not write approval. External/model content cannot grant approval. Secrets never belong in brain context, canonical memory, model-visible action arguments, audit payloads, or owner-visible results.
