# M6A.5 live proof and v0.1 owner checkpoint

Updated 2026-09-08. Code is merged in PR #33; CI #259 is green. This checklist is **not yet executed with real providers**. Deterministic tests and authentication smoke do not substitute for owner acceptance.

## Responsibilities

The owner configures secrets locally, logs in, evaluates useful responses and explicitly approves the intended proposal card. The coding agent prepares the host, diagnoses failures, checks stored state and GitHub read-back, records evidence and delegates code fixes to Luna. No token needs to be sent in chat.

## 1. Read-only session

In PowerShell, set the local password and Ollama key. The configured adapter does not require an OpenAI key. Copying `.env.example` to `.env` alone does not load settings.

```powershell
$env:LOREN_OWNER_PASSWORD = Read-Host 'Local Loren password' -MaskInput
$env:OLLAMA_API_KEY = Read-Host 'Ollama API key' -MaskInput
$env:LOREN_ENABLE_WRITES = 'false'
dotnet run --project src/Loren.Web/Loren.Web.csproj --configuration Release --urls http://127.0.0.1:5091
```

Open `http://127.0.0.1:5091` and log in. Keep the same data directory across restarts; the default is the OS local application-data directory under `Loren/`. Select or bootstrap the canonical Loren project/repository through the UI if needed. Do not rebind an existing alias to another repository.

Record pass/fail and a short observation:

- Normal conversation answers a stable question naturally.
- A current-information question invokes retrieval and includes usable sources.
- Research compares multiple sources and explains uncertainty.
- A project question uses canonical project context and a real GitHub read.
- Teach a durable fact/decision, restart, and retrieve it from the same database.
- Create/list/complete a task through chat; confirm its status after restart.

These steps require no external write. Local Notes/Decisions/Tasks work with writes disabled.

## 2. Live conversational approval

The `github-personal` SSH key supports developer Git operations. Loren's create-branch client uses a separate `GITHUB_WRITE_TOKEN` for GitHub HTTP API calls. Configure a write-specific credential for the intended repository locally; it must not be reused as a model credential.

Stop and restart the host in the same shell, preserving the password, provider settings and data directory:

```powershell
$env:GITHUB_WRITE_TOKEN = Read-Host 'GitHub write token' -MaskInput
$env:LOREN_ENABLE_WRITES = 'true'
$env:LOREN_GITHUB_WRITE_CREDENTIAL_REVOKED = 'false'
dotnet run --project src/Loren.Web/Loren.Web.csproj --configuration Release --urls http://127.0.0.1:5091
```

1. Ask in chat for a uniquely named disposable non-default branch, e.g. `proof/m6a5-owner-<unique-suffix>`, in the selected Loren repository from `main`.
2. Inspect the card: repository, new branch, source ref/exact live SHA and five-minute expiry. Chat alone must not create a branch.
3. First test **Cancel**. Confirm no branch creation occurred.
4. Request a fresh proposal and click **Approve** only when every field is correct. An expired or terminal decision needs a fresh proposal, not a replay.
5. Verify the real GitHub branch ref equals the card's frozen SHA. Inspect natural outcome/audit. A repeated decision must not perform another mutation.
6. Restore `LOREN_ENABLE_WRITES=false` and restart after the proof. Delete the disposable branch only after owner authorization for its cleanup.

## Evidence and exit criteria

Record app commit/time, provider/model, canonical repository, proposal ID, branch, source ref/SHA, decision, independently read-back SHA, redacted outcome/audit, owner observations and restart results. Never record credentials.

Live proof closes M6A.5's remaining gap. The full owner checklist is the decision point for resuming M5 Slices 4–6. Recovery, security/reliability and release gates in `plans/v0.1.md` still precede v0.1.0. Scheduler work remains behind Gate E.
