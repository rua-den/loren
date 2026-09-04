# Loren Security Baseline

**Status:** Active v0.1 baseline. ADR-004 now controls the first write-capable action/approval/credential boundary.

Loren is unusually sensitive software because it may eventually see personal data, hold credentials, operate computers, control infrastructure, send messages, and act while the owner is not actively watching.

Security is therefore a product requirement, not a later hardening phase.

## Threat model

At minimum Loren assumes the following can happen:

- an LLM produces an unsafe or incorrect tool call;
- external web/email/document/repository content contains prompt injection;
- a connected tool returns malicious or misleading text;
- a token or credential leaks from logs or prompts;
- a background task runs with stale assumptions;
- a compromised integration attempts to expand its scope;
- the owner loses a trusted device;
- a runtime/framework dependency becomes vulnerable;
- repeated retries accidentally duplicate consequential actions;
- an old approval is replayed for another request;
- authenticated-session possession is mistaken for write authorization;
- a revoked write credential is silently replaced by a broader credential.

## Security principles

### 1. The model is not a security boundary

LLM output is untrusted intent. Every consequential tool call must be validated and authorized by deterministic application code.

### 2. Least privilege

Each skill receives only the credentials/scopes required for the action being executed.

Prefer:

```text
read-only credential for inspection
scoped write credential for repository work
separate production credential for deploy
```

over one universal credential.

Read/write credential purposes must remain logically separated in Loren even if local development temporarily maps them to one physical token.

### 3. Secrets never become memory

Secrets live in a dedicated secret boundary or host/OS credential facility. Loren memory may store opaque references only.

Never persist raw:

- passwords;
- API keys;
- OAuth refresh/access tokens;
- private keys;
- recovery codes;
- session cookies.

### 4. Treat retrieved content as hostile input

Web pages, emails, documents, repository text, issue/PR comments, tool output, and model inference may contain instructions intended to manipulate the agent.

Content-derived instructions never override:

- system policy;
- owner permissions;
- skill boundaries;
- approval requirements;
- global read-only mode;
- credential selection;
- post-write verification.

### 5. Isolate execution

Code execution, browser/computer use, and untrusted file processing should eventually run in explicitly scoped environments.

Possible boundaries:

- repository/workspace directory;
- process sandbox;
- container;
- remote worker;
- dedicated device node.

### 6. Separate environments

Development, staging, and production resources should use separate credentials and permission policies when feasible.

### 7. Verify consequential actions

After an external write, Loren fetches/inspects the resulting state instead of trusting the write response or model claim.

Examples:

- confirm branch ref SHA after branch creation;
- confirm commit/ref/file identity after a commit path;
- fetch a created pull request and verify repository/base/head/state;
- fetch created calendar event in future integrations;
- verify deployment health endpoint in future deployment integrations.

A write is not `succeeded` until the required postcondition is verified. Ambiguous verification is `unverified`/`failed`.

## Current one-owner authentication boundary

The owner-facing authentication/session boundary remains intentionally small.

Current behavior:

- owner login is backed by host-only `LOREN_OWNER_PASSWORD` configuration;
- the authentication service derives a SHA-256 digest used for fixed-time equality checks and does not retain a plaintext password field;
- the owner credential is never inserted into brain context, tool input, canonical state, memory, or audit;
- successful login creates a non-persistent ASP.NET Core cookie session;
- the cookie is `HttpOnly`, `SameSite=Strict`, and follows the request transport's secure-cookie policy;
- owner console and `/api/run` require authorization;
- unauthenticated `/api/*` requests fail closed with HTTP `401`;
- `/health` is intentionally public;
- the temporary `/internal/dev/run` surface remains disabled by default.

This authentication proves owner/session identity. **It is not write approval.** Gate D/ADR-004 explicitly forbids treating possession of the authenticated cookie as permission for a consequential write.

Before exposing Loren beyond localhost or a trusted network boundary:

- terminate TLS with HTTPS;
- store `LOREN_OWNER_PASSWORD` in process environment or a real secret store, never source control;
- add deployment-level throttling/rate limiting if internet reachable;
- introduce trusted-device/session hardening before cross-device privileged approval.

## Gate D write security boundary

ADR-004 is accepted for the first M5 write-capable implementation.

### Exact-request approval

Every real v0.1 GitHub mutation requires explicit authenticated-owner approval.

Approval must be a Loren-owned artifact binding:

```text
ApprovalId
owner/session principal
action identity
canonical ProjectId + RepositoryId
normalized target/resource
security-relevant parameter digest
approved timestamp
expiry/task boundary
one-time consumption state
optional prerequisite snapshot/digest
```

Model text, memory, external content, and tool output cannot create approval.

### Non-replay

Approval is atomically consumed before the first consequential executor attempt.

Consumed, expired, mismatched, unknown, or revoked approval fails closed. A later independent retry requires fresh approval unless the executor proves a bounded retry is the same single attempt and cannot duplicate the side effect.

### Canonical target binding

A write request must resolve to canonical Loren Project/Repository identity before authorization. Free-form model-provided repo names do not create authority. Scope mismatch fails before secret resolution.

### Global read-only

Before the first write executor ships, Loren must have a host-controlled fail-closed read-only posture.

Safe default:

```text
write-enable absent/invalid -> read-only
read-only -> no write executor invocation
read-only -> no write credential resolution
read actions remain available
```

The model cannot toggle this posture through an ordinary action.

### Credential resolver boundary

Write credentials resolve only after policy authorization and matching approval consumption.

Credential values never enter:

- `BrainContext`;
- model-visible action parameters;
- canonical state;
- durable memory;
- audit payloads;
- owner-visible structured results.

Only an opaque credential purpose/reference may cross application boundaries. Missing/revoked credentials fail closed and Loren must not silently fall back to a broader credential.

### Revocation precedence

Approval grants permission for intent; it does not keep a secret valid. Credential removal/revocation/rotation overrides any prior approval.

## Audit and redaction

Audit must reconstruct consequential decisions without leaking secrets.

Write audit records should identify:

```text
run/request correlation
request/model proposal
normalized action identity
canonical target
redacted/hashed parameter summary
policy decision
approval ID + validation/consumption outcome
credential purpose/reference only
execution result
external identifiers
verification result
timestamps
```

Logs must redact:

- authorization headers;
- tokens;
- cookies;
- password fields;
- private key material;
- credential-bearing URLs;
- sensitive payloads not needed for diagnosis.

## M5 v0.1 write allowlist

After the policy/approval/credential foundations are tested, M5 may expose only:

```text
create non-default GitHub branch
create/update file via controlled commit path on a non-default branch
create commit/update ref only as required by that path
open pull request
```

Still forbidden in v0.1:

```text
write directly to default branch
merge pull request
force push / history rewrite
delete repository/branch/data
repository admin/security changes
secret-management actions
production deployment
```

## Background agent controls

Before Loren gains proactive/background execution, it must additionally have:

- per-skill enable/disable;
- visible active task list;
- task cancellation;
- maximum runtime/tool-call/model-cost limits;
- notification rate controls;
- failure backoff;
- prevention of unbounded self-created recurring tasks;
- approval invalidation appropriate to background work.

Global read-only arrives earlier in M5 because it is required before any real write.

## Supply-chain controls

- lock dependency versions through the package manager;
- keep automated dependency vulnerability reporting;
- review runtime/plugin upgrades before privileged deployment;
- minimize dependencies in the privileged action gateway;
- avoid dynamically executing downloaded skill code without review/signing policy.

Current CI runs full gates on Ubuntu and the integration suite on Windows to catch platform-specific persistence/file-lock behavior.

## Data classification

A future implementation should classify data at least roughly as:

```text
public
personal
sensitive
secret
```

This can influence model/provider routing, retention, logging/redaction, and background-use policy.

## Network posture

A self-hosted Loren service should not be exposed publicly by default without authentication and transport security.

Remote access should preferably use strong owner authentication, TLS/trusted overlay network, trusted-device enrollment/revocation, and rate limiting for public-facing endpoints.

## Incident posture

The system should eventually provide one action to:

1. enable global read-only / pause privileged execution;
2. revoke/disable connected write credentials;
3. cancel scheduled privileged jobs;
4. invalidate active privileged approvals;
5. preserve audit logs for review;
6. keep read-only diagnostic access where safe.

## Security gates before increasing autonomy

Loren should not move from user-driven v0.1 to broad proactive execution until all of the following are demonstrated:

- permission gateway cannot be bypassed through normal runtime/tool paths;
- consequential actions are audited;
- privileged approvals are exact-request-bound and non-replayable;
- secrets are isolated from prompts/memory;
- global privileged execution can be stopped;
- background tasks can later be inspected/cancelled;
- tool-call/runtime quotas exist before broad background autonomy;
- prompt-injection tests cover external-content workflows;
- critical actions have verification or rollback strategies.
