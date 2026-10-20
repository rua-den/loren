# Run Loren with Ollama on the same Windows PC

This is the owner setup for the **M6B.3 live daily-driver checkpoint**.
It does not replace the real-provider quality and approval checks in
[owner-checkpoint.md](owner-checkpoint.md).

## 1. Confirm that the local model actually exists

Install/start Ollama, then in PowerShell:

```powershell
ollama list
Invoke-RestMethod http://127.0.0.1:11434/api/tags
```

Choose the **exact** model tag shown by `ollama list`. The model must be
capable of Ollama tool/function calling for Loren's read/organization actions;
a model that can only chat will not complete the full owner checkpoint.
If the Ollama desktop app already runs its server, do not start a second
`ollama serve` on the same port.

## 2. Configure the local brain, separately from cloud web research

Copy `src/Loren.Web/appsettings.Local.example.json` to the ignored
`src/Loren.Web/appsettings.Local.json` and set at least:

```json
{
  "LOREN_OWNER_PASSWORD": "choose-a-strong-local-password",
  "LOREN_OLLAMA_MODEL": "replace-with-the-exact-tag-from-ollama-list",
  "LOREN_OLLAMA_ENDPOINT": "http://127.0.0.1:11434/api/chat",
  "LOREN_OLLAMA_WEB_SEARCH_ENDPOINT": "https://ollama.com/api/web_search",
  "LOREN_OLLAMA_WEB_FETCH_ENDPOINT": "https://ollama.com/api/web_fetch",
  "OLLAMA_API_KEY": "",
  "LOREN_ENABLE_WRITES": "false"
}
```

This JSON is an *illustration of the relevant fields*; it is not an
Ollama model download command. Put the real installed model name in
`LOREN_OLLAMA_MODEL`. The local config is gitignored. Environment variables
override local JSON, so check for stale `LOREN_OLLAMA_ENDPOINT` or
`LOREN_OLLAMA_MODEL` values if an edit seems ineffective. Restart Loren after
changing configuration.

**Authentication separation:** Local Ollama chat does not need
`OLLAMA_API_KEY`. The web search/fetch actions use Ollama's *cloud* endpoints
and require a valid Ollama Cloud API key in `OLLAMA_API_KEY`.
For the full M6B.3 research checkpoint, place that key in the ignored local
configuration or trusted process environment. The brain adapter deliberately
does **not** send that bearer credential to loopback addresses or any
plaintext HTTP chat endpoint; it continues sending it to configured
non-loopback HTTPS chat endpoints for cloud use.

Without the cloud key, ordinary local conversation can work, but
`/api/readiness` reports `webResearch=not_configured` and overall
`needs_setup`; current-information web actions will fail closed.
Do not interpret a working offline chat as a fully passed readiness gate.
This endpoint checks configuration, **not** network/provider reachability.

Keep Ollama bound to localhost for this flow. Never expose local
`/api/chat` or Loren's owner HTTP port directly to an untrusted network.

## 3. Run Loren

The repository pins .NET SDK `10.0.400` via `global.json`. In a
Windows checkout, double-click `Start-Loren.cmd`, or run:

```powershell
.\Start-Loren.cmd -NoBrowser
```

Open `http://127.0.0.1:5091` and log in. The public `/health` checks
liveness; authenticated `/api/readiness` should report
`brain=configured` and `externalWrites=disabled`. With valid cloud web
configuration, `webResearch=configured` and overall readiness can be
`ready`. Prove real Ollama response quality with a normal chat message,
then follow [M6B.3 owner acceptance](owner-checkpoint.md).

Start with all external writes disabled; only temporarily enable the existing
create-branch proof during the explicit owner approval steps. Never paste
owner passwords, Ollama keys, or GitHub write tokens into issue/PR reports.

## 4. If the session fails

- `ollama list`/tags fails: fix the local server first.
- Chat returns model-not-found: use exactly the installed Ollama tag.
- Chat works but web search/fetch fails: verify the cloud key and separate
  HTTPS web endpoints; do not point web-search/fetch to local `/api/chat`.
- `/api/readiness` says `brain=configured` but chat fails: this is expected
  because readiness does not ping providers; inspect Loren/Ollama local logs
  without exposing tokens.
- Restart loses context: confirm the same `LOREN_DATA_DIRECTORY` is used
  before filing a persistence regression.
