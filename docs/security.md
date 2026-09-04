# Loren Security Baseline

**Status:** Proposed baseline. Security choices that affect implementation should receive dedicated ADRs.

Loren is unusually sensitive software because it may eventually see personal data, hold credentials, operate computers, control infrastructure, send messages, and act while the owner is not actively watching.

Security is therefore a product requirement, not a later hardening phase.

## Threat model

At minimum Loren should assume the following can happen:

- an LLM produces an unsafe or incorrect tool call;
- external web/email/document content contains prompt injection;
- a connected tool returns malicious or misleading text;
- a token or credential leaks from logs or prompts;
- a background task runs with stale assumptions;
- a compromised integration attempts to expand its scope;
- the owner loses a trusted device;
- a runtime/framework dependency becomes vulnerable;
- repeated retries accidentally duplicate consequential actions.

## Security principles

### 1. The model is not a security boundary

LLM output is untrusted intent. Every consequential tool call must be validated and authorized by deterministic application code.

### 2. Least privilege

Each skill should receive only the credentials and scopes required for the action being executed.

Prefer:

```text
read-only token for inspection
scoped write token for repository work
separate production credential for deploy
```

over one universal credential.

### 3. Secrets never become memory

Secrets must live in a dedicated secret store or operating-system credential facility. Loren memory may store opaque references only.

Never persist raw:

- passwords;
- API keys;
- OAuth refresh/access tokens;
- private keys;
- recovery codes;
- session cookies.

### 4. Treat retrieved content as hostile input

Web pages, emails, documents, repository text, issue comments, and tool output may contain instructions intended to manipulate the agent.

Content-derived instructions never override:

- system policy;
- owner permissions;
- skill boundaries;
- approval requirements.

### 5. Isolate execution

Code execution, browser/computer use, and untrusted file processing should eventually run in explicitly scoped environments.

Possible boundaries:

- repository/workspace directory;
- process sandbox;
- container;
- remote worker;
- dedicated device node.

The correct mechanism may vary by skill.

### 6. Separate environments

Development, staging, and production resources should have separate credentials and permission policies when feasible.

### 7. Verify consequential actions

After external writes, Loren should fetch or inspect the resulting state instead of assuming success.

Examples:

- confirm commit SHA after push;
- fetch created calendar event;
- verify deployment health endpoint;
- confirm message/send identifier;
- inspect target state after configuration change.

## Current M2 owner authentication boundary

M2 introduces the first owner-facing authentication/session boundary for the one-owner preview.

Current behavior:

- owner login is backed by the host-only `LOREN_OWNER_PASSWORD` configuration secret;
- the authentication service immediately derives a SHA-256 digest used for fixed-time equality checks and does not retain a plaintext password field;
- the owner credential is never inserted into Loren brain context, tool input, canonical state, memory, or audit;
- successful login creates a non-persistent ASP.NET Core cookie session;
- the cookie is `HttpOnly`, `SameSite=Strict`, and uses the request transport's secure-cookie policy;
- owner console and `/api/run` require authorization;
- unauthenticated `/api/*` requests fail closed with HTTP `401`;
- `/health` is intentionally public for health checks;
- the temporary `/internal/dev/run` surface remains disabled by default and is not part of the owner flow.

This is deliberately a **one-owner M2 boundary**, not the final remote-access/device identity model. Before exposing Loren beyond localhost or a trusted network boundary:

- terminate TLS with HTTPS;
- store `LOREN_OWNER_PASSWORD` in process environment or a real secret store, never source control;
- add deployment-level request throttling/rate limiting if the login surface is internet reachable;
- avoid treating possession of the cookie as approval for future privileged writes; write approvals still require the dedicated M5 action/approval policy boundary.

Trusted M2 CI checks unauthenticated rejection, wrong-password rejection, authenticated console access, and absence of the development run route. The trusted exact-main live proof is designed to assert that neither the provider credential nor owner credential appears in the owner-visible response.

## Approval security

Approval must not be a vague conversational state such as `user_said_yes=true`.

A privileged approval should bind to:

```text
owner/session identity
action
target/resource
important parameters
expiry/task scope
optional prerequisite conditions
```

Example:

```text
Approve deploy:
  project = wedding-online
  commit = abc123
  environment = production
  condition = tests pass
  expires = end of current task
```

## Audit and redaction

Audit records should contain enough information to reconstruct decisions without leaking secrets.

Logs should redact:

- authorization headers;
- tokens;
- cookies;
- password fields;
- private key material;
- credential-bearing URLs;
- sensitive payloads not needed for diagnosis.

## Background agent controls

Before Loren gains proactive/background execution, it should have:

- global kill switch;
- per-skill enable/disable;
- visible active task list;
- task cancellation;
- maximum runtime/tool-call/model-cost limits;
- notification rate controls;
- failure backoff;
- prevention of unbounded self-created recurring tasks.

## Supply-chain controls

Once source code exists:

- lock dependency versions through the package manager;
- enable automated dependency vulnerability reporting;
- review runtime/plugin upgrades before privileged deployment;
- minimize dependencies in the privileged action gateway;
- avoid dynamically executing downloaded skill code without review/signing policy.

## Data classification

A future implementation should classify data at least roughly as:

```text
public
personal
sensitive
secret
```

This classification can influence:

- which models may receive the data;
- whether cloud processing is allowed;
- retention period;
- logging/redaction;
- whether background use is permitted.

## Network posture

A self-hosted Loren service should not be exposed publicly by default without authentication and transport security.

Remote access should preferably use:

- strong owner authentication;
- TLS or trusted overlay network;
- explicit trusted-device enrollment;
- revocation support;
- rate limiting for public-facing endpoints.

## Incident posture

The system should eventually provide one action to:

1. pause all agent execution;
2. revoke/disable connected write credentials;
3. cancel scheduled privileged jobs;
4. preserve audit logs for review;
5. keep read-only diagnostic access where safe.

## Security gates before increasing autonomy

Loren should not move from user-driven v0.1 to broad proactive execution until all of the following are demonstrated:

- permission gateway cannot be bypassed through normal runtime/tool paths;
- consequential actions are audited;
- privileged approvals are scoped;
- secrets are isolated from prompts/memory;
- background tasks can be inspected and cancelled;
- tool-call and runtime quotas exist;
- prompt-injection tests cover external-content workflows;
- critical actions have verification or rollback strategies.
