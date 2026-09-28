# M6B daily-driver live proof and v0.1 owner checkpoint

Updated 2026-09-19.

Deterministic tests and CI prove boundaries; they do not substitute for real-provider owner acceptance. This checklist is the next product decision point after M6B.2 is green on `main`.

## Responsibilities

The owner configures local secrets, logs in, evaluates response quality and explicitly decides any external-write proposal. The coding agent diagnoses failures and records non-secret evidence. Never paste provider/write credentials into chat, docs, commits or issue/PR text.

## 0. Start with readiness

Use the normal local launcher or run:

```powershell
dotnet run --project src/Loren.Web/Loren.Web.csproj --configuration Release --urls http://127.0.0.1:5091
```

Login first, then open/call:

```text
GET /api/readiness
```

Expected read-only starting posture:

```text
status: ready
storage: ready
ownerAuthentication: ready
brain: configured
webResearch: configured
projects: ready | empty
externalWrites: disabled
```

`projects=empty` means bootstrap/select a canonical project before project-scoped tests; it is not a host failure.

Readiness is local/configuration evidence only. It deliberately does not ping Ollama, GitHub or web providers. The steps below prove real reachability and useful behavior.

Public `/health` is only liveness and is not sufficient owner-readiness evidence.

## 1. Read-only daily-use session

Keep:

```text
LOREN_ENABLE_WRITES=false
```

Run and record pass/fail + a short observation for each:

1. **Normal conversation** — ask a stable reasoning/knowledge question; response should be natural and not invoke unnecessary tools.
2. **Current information** — ask something current; Loren should retrieve evidence and show usable sources.
3. **Research** — ask for a multi-source comparison; Loren should synthesize bounded evidence and distinguish fact from inference/uncertainty.
4. **Project context** — select/bootstrap Loren and ask its current state; response should combine canonical project context with a real GitHub read when needed.
5. **Durable memory/decision** — teach or record a durable owner fact/decision, restart with the same data directory, retrieve it correctly.
6. **Organization state** — create/list/complete a task through chat, restart, verify terminal state remains correct.
7. **Conversation continuity** — restart and reopen a prior conversation; history/project scope should remain coherent.
8. **Recovery sanity** — keep a recent logical export available and confirm the documented restore path remains understandable before release closeout.

Any useful-response failure here is higher priority than adding another external write primitive.

## 2. Live conversational approval proof

Loren's GitHub write client uses a dedicated `GITHUB_WRITE_TOKEN`, separate from provider credentials and developer Git/SSH credentials.

Configure locally, restart, then re-check `/api/readiness`:

```powershell
$env:GITHUB_WRITE_TOKEN = Read-Host 'GitHub write token' -MaskInput
$env:LOREN_ENABLE_WRITES = 'true'
$env:LOREN_GITHUB_WRITE_CREDENTIAL_REVOKED = 'false'
```

Before requesting a proposal, expect:

```text
externalWrites.enabled = true
externalWrites.status = ready
```

If readiness says `missing_credential`, `revoked` or `not_configured`, do not attempt the write proof until configuration is corrected.

Then:

1. Ask naturally for a uniquely named disposable non-default branch in the selected Loren repository from `main`.
2. Inspect the proposal card: repository, new branch, source ref, exact frozen source SHA and expiry. Chat text alone must not create the branch.
3. First choose **Cancel**. Independently confirm the branch does not exist.
4. Ask for a fresh proposal. Do not replay the cancelled/expired one.
5. Choose **Approve** only when every field is correct.
6. Independently read back the GitHub ref and verify it equals the proposal's frozen SHA exactly.
7. Inspect the natural completion/audit response. A repeated decision must not cause another mutation.
8. Restore `LOREN_ENABLE_WRITES=false`, restart, and confirm `/api/readiness` returns `externalWrites=disabled` again.

Delete the disposable branch only with owner authorization for cleanup.

## Evidence to record

Record only non-secret evidence:

- app/main commit SHA and time;
- provider/model name, not token;
- canonical repository;
- proposal ID;
- branch + source ref/frozen SHA;
- Cancel result + absence read-back;
- Approve result + exact GitHub SHA read-back;
- redacted audit outcome;
- restart/memory/task observations;
- readiness statuses before and after write proof.

## Exit decision

If this checkpoint passes without material daily-use blockers, assess `v0.1.0` release gates. Do not automatically resume M5 controlled file/commit/open-PR work.

If failures occur, fix the smallest coherent owner-visible problem first and rerun the relevant part of this checklist.

Background scheduling/reminders still require Gate E.
