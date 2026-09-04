# Loren Permission Model

**Status:** Active v0.1 baseline — Gate D / ADR-004.

Loren never treats "the model decided to do it" as authorization. Authorization belongs to Loren's deterministic policy layer.

The controlling decision for the first write-capable v0.1 boundary is [`ADR-004`](decisions/004-action-approval-and-credential-boundary.md).

## Action classes

M5 must represent write intent explicitly in Loren-owned contracts. The minimum v0.1 access/risk classes are:

### `READ`

No external mutation.

Examples:

- inspect repository state;
- search documentation;
- query server health;
- read explicitly available files.

Default: allowed only when resource/tool scope permits it.

### `REVERSIBLE_WRITE`

Narrow remote mutation that is reliably reversible.

Example:

- create a non-default Git branch.

For the first v0.1 write path this still requires explicit owner approval. Gate D deliberately starts conservative; standing permissions come later.

### `EXTERNAL_WRITE`

Externally visible mutation/commitment.

Examples:

- create/update file through a remote commit path;
- move/update a remote non-default ref as part of the controlled commit path;
- open a pull request;
- send a message or create/update an external object in future integrations.

Default in v0.1: exact-request-bound owner approval required.

### `PRIVILEGED_WRITE`

Destructive, administrative, security-sensitive, financial, or production-critical action.

Examples:

- delete repositories/data;
- force-push/rewrite history;
- change authentication/security configuration;
- production deployment;
- secret rotation;
- repository administration.

The v0.1 GitHub write implementation does not expose these actions at all.

## v0.1 GitHub write allowlist

Only after M5 policy/approval/credential foundations are tested may Loren implement:

```text
create non-default branch
create/update file via controlled commit path on a non-default branch
create commit/update ref only as required by that path
open pull request
```

Categorically outside the v0.1 write surface:

```text
write directly to default branch
merge pull request
force push / rewrite history
delete repository/branch/data
repository admin/security changes
secret-management actions
production deployment
```

## Policy dimensions

Risk level alone is insufficient. Permission evaluation must consider at least:

```text
action identity + version/access class
canonical ProjectId
canonical RepositoryId
provider/external repository locator snapshot
target branch/ref/path/base/head
normalized security-relevant parameters
whether target is current default branch
global read-only state
approval evidence
credential purpose
```

A free-form model-provided repository name is not authority. Unknown/mismatched canonical scope fails closed before credential resolution.

## Approval semantics

### Authentication is not approval

The owner web session proves who is interacting with Loren. It does not authorize a remote write by itself.

### Approval is a Loren-owned artifact

Approval is never:

```text
user_said_yes = true
model message text
memory content
external/tool content
```

A v0.1 write approval must bind to a concrete normalized action intent and include at least:

```text
ApprovalId
authenticated owner/session binding
action identity
ProjectId + RepositoryId
normalized target/resource
security-relevant parameter digest
approved/issued timestamp
expiry or task boundary
one-time consumption state
optional prerequisite snapshot/digest
```

Material changes to repo, branch, path, content digest, PR base/head, action identity, or other security-relevant parameters require a new approval.

### One-time / non-replay

Approval is atomically consumed before the first consequential executor attempt.

Consumed, expired, mismatched, unknown, or revoked approvals cannot authorize another request, turn, repository, or later independent retry.

Executor-internal retry is allowed only when proven to be the same bounded attempt without duplicate side effect. Ambiguous failures require fresh approval.

## Standing permissions

Standing permissions are intentionally deferred from the first M5 write implementation.

Later Loren may support inspectable/revocable rules such as branch creation in a bounded repo set, but only after one-time approval semantics are proven end to end.

External/model content can never create or modify standing permission.

## Global read-only control

Before any write executor ships Loren must have a host-controlled fail-closed global read-only state.

Safe v0.1 behavior:

- write-enable setting absent/invalid -> read-only;
- read-only active -> no write executor invocation;
- read-only active -> no write credential resolution;
- read actions remain usable;
- denial reason is auditable;
- the model cannot toggle the host emergency posture through an ordinary action.

## Credential boundary

Secrets are not permission and are not prompt context.

Write credentials:

- resolve only after policy authorization + matching approval consumption;
- live behind the controlled executor boundary;
- never appear in model-visible action parameters;
- never enter memory/canonical state/audit payloads/owner-visible results;
- use write-specific credential purpose/reference;
- fail closed if missing/revoked;
- never silently fall back to a broader credential.

Read/write credential purposes remain logically separate even if local development temporarily maps them to one physical token.

## Post-write verification

Remote API success is not enough.

Every consequential write must verify its intended postcondition through a read path independent of the model's claim.

Examples:

```text
create branch -> fetch ref and confirm exact SHA
commit/file write -> fetch commit/ref/file identity and verify expected state
open PR -> fetch PR and verify repository/base/head/state/identifier
```

Ambiguous or failed verification yields `failed`/`unverified`, never silent success.

## Tool-side enforcement

The model is not an enforcement point.

```text
Model proposes action
        |
        v
Canonical target resolution
        |
        v
Policy evaluation
        |
   require approval
        |
        v
Owner creates exact Loren approval
        |
        v
Approval validate + consume
        |
        v
Credential resolver
        |
        v
Executor
        |
        v
Post-write verifier
        |
        v
Structured result + audit
```

A compromised/confused model remains constrained by Loren's gateway.

## Audit record

Every consequential write must reconstruct at least:

```text
run/request correlation
requester/model proposal
normalized action identity
canonical Project/Repository target
redacted/hashed parameter summary
policy decision + reason
approval ID + validation/consumption outcome
credential purpose/reference only
execution outcome
external identifiers
verification/postcondition
timestamps
```

Raw secrets are forbidden in audit.

## Security boundaries

- Secrets are not prompt memory.
- Credentials use least practical privilege.
- Read/write credential purpose is separated.
- External/tool content is untrusted for authorization.
- Memory is data, not permission.
- Permission changes are privileged operations.
- Credential revocation overrides prior approval.
- External content cannot create approval, expand scope, disable read-only mode, select credentials, or mark verification successful.

## Emergency controls

Current Gate D requirement: global read-only before first write.

Future always-on Loren should additionally have:

- per-skill disable controls;
- revocation of background jobs;
- visible active tasks;
- maximum spend/tool-call/runtime quotas;
- one control to invalidate active privileged approvals.

## Deferred questions

These are intentionally not required for first M5 write capability:

- durable standing-permission rule representation;
- cross-device cryptographic approval binding;
- trusted-device enrollment;
- background-task approval inheritance;
- which future production actions remain permanently non-delegable.

Any expansion beyond ADR-004's one-time approval boundary requires explicit design before implementation.
