# ADR-004: Action Approval and Credential Boundary

- **Status:** Accepted
- **Date:** 2026-09-04
- **Gate:** D — Action/Credential Policy
- **Depends on:** ADR-001, ADR-002, ADR-003

## Context

Loren has completed the read-only walking skeleton, canonical Project/Repository state, and trusted durable memory. The next milestone introduces the first real external writes.

The hard invariant remains:

> The brain may request an action. Only Loren may authorize and execute it.

A model-generated tool call, retrieved text, durable memory record, authenticated web session, or possession of a credential is not by itself authorization to perform a consequential write.

Gate D must make the write boundary deterministic before M5 adds GitHub write executors.

## Decision

### 1. Write intent is explicit in the action contract

`IsReadOnly` remains useful for compatibility, but M5 must introduce an explicit Loren-owned action access/risk classification rather than deriving authorization from action names or model text.

The v0.1 policy model must distinguish at least:

- `READ` — no external mutation;
- `REVERSIBLE_WRITE` — remote mutation that is narrowly scoped and reliably reversible, such as creating a non-default branch;
- `EXTERNAL_WRITE` — externally visible mutation/commitment, such as creating a commit or pull request;
- `PRIVILEGED_WRITE` — destructive, security-sensitive, production-critical, administrative, or otherwise high-impact mutation.

Risk class is only one policy dimension. Evaluation must also receive resolved canonical target identity and relevant execution context.

The M5 GitHub write set is strictly limited to:

- create a non-default branch;
- create/update file content through a commit on a non-default branch;
- create a commit/update ref only as required by that controlled file-change path;
- open a pull request.

The following remain forbidden in v0.1 regardless of model request:

- write directly to the repository default branch;
- merge a pull request;
- force-push or rewrite history;
- delete repository/branch/data;
- repository administration/security changes;
- secret management actions;
- production deployment.

### 2. Policy evaluation uses canonical target identity

Before a write can be authorized, Loren must resolve the requested target to canonical Loren-owned state.

For GitHub writes the policy input must include at least:

- action definition and normalized parameters;
- canonical `ProjectId`;
- canonical `RepositoryId`;
- provider and external repository locator snapshot;
- target branch/ref;
- whether the target ref is the current default branch;
- action risk/access class;
- current global read-only state;
- approval evidence, if any.

A free-form repository name supplied by the model is not sufficient authority. Unknown or mismatched Project/Repository scope fails closed before credential resolution.

### 3. v0.1 external writes require owner approval

For the first write-capable v0.1 implementation, every real remote GitHub mutation requires explicit authenticated-owner approval.

This includes `REVERSIBLE_WRITE` actions such as creating a branch. Gate D deliberately chooses the conservative first implementation; standing permissions may be added later only after the approval path is proven and inspectable.

Read actions continue to follow their existing read policy.

### 4. Approval is a Loren-owned artifact, not conversational state

Approval must never be represented as a vague flag such as `user_said_yes=true`, a model message, or a memory claim.

A write approval must be created by Loren from an authenticated owner action and bind to a concrete normalized request.

The approval artifact must include at least:

- opaque Loren-owned `ApprovalId`;
- authenticated owner/session binding or equivalent trusted principal reference;
- action name/version or stable action identity;
- canonical `ProjectId` and `RepositoryId`;
- normalized target/resource;
- digest of the security-relevant normalized parameters;
- issued/approved timestamp;
- expiry timestamp or current-task boundary;
- one-time consumption state;
- optional prerequisite snapshot/digest when a condition is part of the approval.

Approval validation must compare the exact normalized action intent. Material changes to action, repository, branch, path, content digest, PR base/head, or other security-relevant parameters require a new approval.

### 5. Approvals are one-time and non-replayable

A valid approval is consumed atomically before the first consequential executor attempt.

Once consumed, it cannot authorize another action request, another runtime turn, another repository, or a later retry initiated as a new request.

Executor-internal bounded retries are permitted only when the executor can prove they are part of the same single attempt and will not duplicate the external side effect. Otherwise the result is uncertain/failed and a fresh owner approval is required.

Expired, already-consumed, unknown, mismatched, or revoked approvals fail closed.

### 6. Authentication is not write approval

The existing owner cookie/session proves who is interacting with Loren. It does not authorize a privileged write by itself.

A write endpoint or UI flow must therefore require both:

1. authenticated owner identity; and
2. a matching unconsumed Loren approval artifact.

Model text, external content, prepared memory, and tool output can never create or consume owner approval.

### 7. Credentials live behind the executor boundary

Write credentials must never enter:

- `BrainContext`;
- action parameters visible to the model;
- canonical Project/Repository records;
- durable memory payloads;
- owner-visible action result payloads;
- audit fields or logs.

M5 must introduce a credential resolver/secret boundary used only after policy authorization and approval consumption.

The action/executor contract passes an opaque credential purpose/reference, never the secret value itself.

For v0.1:

- read and write credentials are logically distinct;
- a write executor requests only the GitHub write credential it needs;
- missing/revoked credential resolution fails closed;
- the write credential is attached to the outbound request inside the executor boundary;
- authorization headers/tokens/cookies are always redacted from exceptions, audit, diagnostics, and structured results.

Environment variables or host secret configuration are acceptable for the first local v0.1 implementation, provided the abstraction remains replaceable by an OS/managed secret store.

### 8. Read/write credential separation is mandatory

Where authentication is required, the normal GitHub read path must not require or automatically inherit the write credential.

A write token must not become a general-purpose credential shared with the brain/runtime.

If only one physical token can be configured during local development, Loren must still expose it through a write-specific secret reference and must not widen the read/action contracts around that token.

### 9. Global read-only is fail-closed

Loren must have a host-controlled global read-only switch before the first write executor ships.

For v0.1 the safe default is read-only when the write-enable setting is absent or invalid.

When global read-only is active:

- no external write approval may result in executor invocation;
- no write credential may be resolved;
- read actions remain available;
- the policy/audit path records that the action was blocked by global read-only mode.

Changing this host-level emergency posture is not something the model may do through an ordinary action.

### 10. Credential revocation takes precedence over approval

An approval grants permission for a specific intent; it does not guarantee that a credential remains usable.

If the write credential is removed, revoked, rotated, or fails resolution after approval, execution stops. Loren must not fall back to another broader credential without an explicit configured mapping.

Credential identifiers/references may be audited; credential values may not.

### 11. External write success requires post-write verification

A successful HTTP/API response is not enough to call a consequential action successful.

After a GitHub write, Loren must verify the intended postcondition through a read path independent of the model's claim.

Examples:

- branch creation -> fetch ref and confirm exact target SHA;
- file/commit write -> fetch resulting commit/file/ref and confirm expected identifier/content digest as appropriate;
- pull request creation -> fetch PR and confirm repository, base, head, state, and returned PR identity.

If verification fails or is ambiguous, the final action outcome must be `failed` or `unverified`, never silently `succeeded`.

The model receives only the structured verified result.

### 12. Audit reconstructs authorization without secrets

Each consequential write must produce a correlated audit trail sufficient to answer “why did Loren do that?”

The retained record must identify at least:

- run/request correlation;
- requester/model proposal;
- normalized action identity;
- canonical Project/Repository target;
- redacted/hashed security-relevant parameter summary;
- policy decision and reason;
- approval ID and approval validation/consumption result;
- credential reference/purpose only, never secret value;
- executor attempt/outcome;
- external identifiers (commit SHA, branch ref, PR number/URL as appropriate);
- verification result/postcondition;
- timestamps.

Sensitive content should be minimized or represented by digests when the raw payload is unnecessary for reconstruction.

### 13. External content cannot grant or alter permission

Repository files, issue/PR text, tool output, model inference, and `EXTERNAL_CONTENT`/`MODEL_INFERENCE` memories remain untrusted with respect to authorization.

They may inform the requested work, but they cannot:

- create approval;
- expand approval scope;
- change global read-only mode;
- select a broader credential;
- bypass canonical target validation;
- mark verification as successful.

### 14. First M5 implementation order

M5 must be implemented in this order:

1. typed action access/risk + resolved policy context;
2. Loren-owned one-time approval artifact/store and exact-request fingerprinting;
3. fail-closed global read-only control;
4. credential resolver boundary and redaction tests;
5. GitHub create-branch executor + post-write verification;
6. controlled file/commit path + verification;
7. open-PR executor + verification;
8. owner-facing approval flow and correlated audit evidence.

No real GitHub mutation may be enabled before items 1–4 are tested.

## Consequences

- M5 starts with more application plumbing before any visible write capability appears.
- The first write experience is intentionally approval-heavy.
- Approval replay and credential leakage have deterministic application boundaries rather than relying on model behavior.
- The implementation remains provider-neutral; GitHub is only the first write adapter.
- Standing permissions and broader autonomy are deferred until the one-time approval path is proven.

## Gate D acceptance

Gate D passes when this ADR is merged and the repository documentation agrees that:

- all first-version real external writes require explicit owner approval;
- approval is exact-request-bound, expiring/task-bounded, one-time, and non-replayable;
- authenticated session is not itself approval;
- canonical target resolution precedes write authorization;
- write credentials resolve only behind the authorized executor boundary;
- read/write credential purpose is separated;
- global read-only defaults fail-closed;
- credential revocation overrides approval;
- post-write verification is mandatory;
- audit reconstructs the decision without storing secrets;
- v0.1 forbidden write classes remain blocked.

Passing Gate D authorizes implementation of M5. It does not by itself add or enable any external write path.
