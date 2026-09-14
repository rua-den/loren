# Loren logical recovery

Loren recovery is a versioned logical export/restore path for durable owner state. It is not a raw SQLite file copy and it never exports credentials or provider configuration.

## Scope

Format version `1` exports canonical projects/repositories/aliases, trusted durable memory, Notes/Decisions/Tasks, persisted conversations, action approvals, create-branch proposals, and retained durable audit rows.

The archive is private owner data. Store and transfer it with the same care as `loren.db`.

## Export

```powershell
dotnet run --project src/Loren.Maintenance/Loren.Maintenance.csproj -- \
  export --source C:\path\to\loren.db --output C:\backup\loren-recovery.json
```

Export opens the SQLite source in read-only mode, refuses a source with unapplied migrations, reads a consistent transaction snapshot, and refuses to overwrite an existing archive file.

## Restore

```powershell
dotnet run --project src/Loren.Maintenance/Loren.Maintenance.csproj -- \
  restore --archive C:\backup\loren-recovery.json --target-directory C:\restored-loren
```

Restore always targets a new directory. It creates a fresh database through checked-in EF migrations, validates archive/domain/reference invariants, restores inside one transaction, and only publishes the target directory after restore succeeds.

Security behavior on restore is intentionally conservative:

- existing approvals are restored as revoked unless they were already revoked;
- pending create-branch proposals are restored as cancelled with a decision timestamp no earlier than their creation timestamp;
- already-terminal proposals keep their terminal state;
- conversation history accepts only `user` and `assistant` roles;
- credentials, API keys, owner password configuration, environment values, and provider configuration are not exported.

## Verification after restore

Start Loren against the restored directory and verify projects, trusted memory, Notes/Decisions/Tasks, and conversations through their normal read paths. A restored approval or pending proposal must never become an executable grant.

Recovery does not reconstruct audit events that were never written to the durable audit sink before the archive was created.
