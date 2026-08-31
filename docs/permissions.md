# Loren Permission Model

**Status:** Proposed.

Loren should never treat "the model decided to do it" as authorization. Authorization belongs to Loren's policy layer.

## Action classes

### Level 0 — Read

Examples:

- inspect repository state;
- search documentation;
- read calendar events;
- query server health;
- read files explicitly available to Loren.

Default: allowed when the relevant account/tool is connected and data scope permits it.

### Level 1 — Reversible or private write

Examples:

- create a local note;
- create a draft;
- create a git branch;
- update Loren-owned internal task state;
- stage a proposed configuration change.

Default: may be allowed automatically if the action is reliably reversible and does not create an external commitment.

### Level 2 — External write / consequential action

Examples:

- push commits;
- open or merge pull requests;
- send email or messages;
- create/update calendar events;
- modify a remote service;
- trigger a deployment to a non-critical environment.

Default: policy-specific. Some actions may become pre-approved after the owner explicitly grants a standing rule.

### Level 3 — Destructive, privileged, financial, or production-critical

Examples:

- delete repositories or data;
- change authentication/security configuration;
- production deployment with material impact;
- infrastructure deletion;
- purchases or financial transfers;
- secret rotation;
- actions that may lock the owner out.

Default: explicit approval immediately before execution. Some actions may remain permanently non-delegable.

## Policy dimensions

Risk level alone is insufficient. Permission evaluation should consider:

```text
action
tool/skill
resource
project/environment
recipient or external party
reversibility
financial impact
current task authorization
standing rules
freshness of owner approval
```

Example rules:

```text
GitHub.create_branch(*)                  -> allow
GitHub.push(repo=loren, branch!=main)    -> allow
GitHub.push(repo=loren, branch=main)     -> approval or project policy
Deploy(env=staging)                      -> allow after tests
Deploy(env=production)                   -> require approval
Email.send(*)                            -> require approval
Repository.delete(*)                     -> always require approval
```

These are examples, not committed defaults.

## Approval semantics

Approval must bind to a concrete intended action.

Bad:

> "Can I make changes?"

Better:

> "Approve deploying commit abc123 to production after the current test suite passes?"

Where possible approval should include:

- action;
- target/resource;
- important parameters;
- expected side effect;
- expiry or task scope.

Approval should not silently authorize unrelated follow-up actions.

## Standing permissions

The owner may create durable policies such as:

```text
Allow Loren to create branches in personal repositories.
Allow Loren to merge dependency-update PRs when all required checks pass.
Never allow Loren to delete a repository without confirmation.
```

Standing permissions must be inspectable and revocable.

## Tool-side enforcement

The reasoning model must not be the only enforcement point.

Preferred flow:

```text
Model proposes tool call
        |
        v
Policy engine evaluates
        |
    +---+---+
    |       |
  allow   deny/approval
    |       |
    v       v
 Tool     Owner
```

A compromised or confused model should therefore still be constrained by the action gateway.

## Audit record

Every consequential action should record at least:

```text
who/what requested it
resolved action
resource/target
policy decision
approval reference if any
execution time
result
external identifier
verification/postcondition
```

Sensitive values should be redacted.

## Security boundaries

- Secrets are not prompt memory.
- Credentials should be scoped to the minimum capabilities practical.
- Read and write credentials should be separable when possible.
- Production and development credentials should be distinct.
- Tool output must be treated as untrusted input for prompt-injection purposes.
- External content cannot grant itself permission by instructing Loren to run tools.
- Permission changes are themselves privileged actions.

## Emergency controls

A future always-on Loren should have:

- a global pause/kill switch;
- per-skill disable controls;
- revocation of background jobs;
- visibility into active tasks;
- maximum spend/tool-call/runtime quotas;
- a way to invalidate all active privileged approvals.

## Open questions

- exact representation of policy rules;
- whether to adopt a capability-token model internally;
- how approvals are cryptographically/session-bound across devices;
- how long standing permissions remain valid;
- which production actions should be categorically forbidden from autonomy.
