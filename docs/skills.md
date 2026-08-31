# Loren Skill Model

**Status:** Proposed.

Skills are Loren's controlled interfaces to external systems. They should be modular, typed, permission-aware, and independently testable.

## Goals

A skill should make it possible to add a new capability without changing the agent's core reasoning loop.

Examples:

```text
GitHub
Web research
Gmail
Calendar
Filesystem
Server/VPS
Browser/computer use
Home Assistant
Spotify/media
Notifications
```

## Skill contract

Each skill should declare:

```yaml
name: github
version: 1
capabilities:
  - inspect_repository
  - create_branch
  - push_commit
  - merge_pull_request
credentials:
  - github_account
events:
  - pull_request.changed
  - workflow.completed
```

Each action should additionally declare:

- human-readable description;
- typed input schema;
- typed output schema;
- risk/permission classification;
- whether the action is idempotent;
- expected external side effects;
- audit fields;
- optional verification/postcondition method.

## Tool execution flow

```text
Task
  |
  v
Agent chooses capability
  |
  v
Resolve skill + action
  |
  v
Validate input schema
  |
  v
Evaluate permission policy
  |
  +--> approval required --> owner
  |
  v
Execute skill action
  |
  v
Verify postcondition when applicable
  |
  v
Structured result + audit record
```

## Structured results

Skills should avoid returning prose-only responses.

Preferred:

```json
{
  "repository": "rua-den/loren",
  "branch": "main",
  "commit": "abc123",
  "checks": {
    "tests": "passed",
    "build": "passed"
  }
}
```

The model may then convert this to concise natural language.

## Native skills versus adapters

Loren should not assume that every capability must be implemented from scratch.

Potential sources:

- Loren-native skill;
- MCP server/tool;
- OpenClaw plugin/skill;
- Letta tool;
- direct vendor API client;
- local command/execution adapter;
- Home Assistant service/action.

The preferred architecture is an adapter layer that normalizes these into Loren's action + permission model.

## Credentials

Skills request credential references, not raw secrets.

Example:

```text
skill: github
credential_ref: github/owner-account
```

The secret manager resolves the credential at execution time according to the action's scope.

## Events

Some skills may publish external changes into Loren's event layer.

Example:

```text
GitHub webhook
  -> github.workflow.failed
  -> normalized Event
  -> proactive evaluator
```

An event subscription is not authority to act. The event may trigger analysis or a notification while all consequential actions still pass through permission policy.

## Initial v0.1 skill candidates

1. **GitHub** — first-class because Loren itself lives in GitHub and project work is a primary use case.
2. **Web research** — public information retrieval with source provenance.
3. **Tasks/reminders** — internal scheduled task capability.
4. **Memory/world model** — internal capabilities exposed carefully to the agent.

Email, calendar, server, computer-use, and Home Assistant should follow after the core boundaries prove stable.

## Open questions

- adopt MCP as a first-class transport or only as an adapter?
- how much of OpenClaw's plugin model can be reused directly?
- should skills execute in-process, worker processes, containers, or remote services?
- what is the minimum manifest needed without over-engineering v0.1?
- how are skill versions migrated when stored tasks reference old schemas?
