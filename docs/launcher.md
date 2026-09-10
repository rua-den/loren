# Windows launcher

Double-click `Start-Loren.cmd` in a repository checkout to start the Loren web host on
`http://127.0.0.1:5091` and open the browser after `GET /health` reports `{ "status": "ok" }`.
It also checks Loren's login page marker before treating an existing listener as reusable.
The launcher keeps the host in the foreground so its logs remain visible and tracks the host
process it started; press Ctrl+C in that window to stop the host cleanly.

Before the first run, install the .NET SDK version pinned by `global.json`. Configure the
host in `src/Loren.Web/appsettings.json` by copying
`src/Loren.Web/appsettings.Local.example.json`; edit that JSON file with the owner password,
provider key, model/endpoints, and optional data directory. The file is ignored by Git and is
excluded from build and publish output. An optional ignored `appsettings.Local.json` is also
loaded when present. Environment variables still override JSON according to the host's existing
configuration policy.

The launcher is also useful from a terminal:

```text
Start-Loren.cmd -NoBrowser
Start-Loren.cmd -DryRun -NoBrowser
Start-Loren.cmd -DryRun -NoBrowser -NoPause
Start-Loren.cmd -SmokeTest -NoPause
```

`-NoBrowser` leaves browser startup disabled. `-DryRun` validates the checkout and SDK, then
prints the working directory and exact host command without starting the host or opening a
browser. `-NoPause` keeps the wrapper non-interactive when an error occurs. `-SmokeTest` performs a real Release build/restore and host startup using a temporary data directory and fake process-only owner password, verifies health and the Loren login marker, then stops and cleans up the host without opening a browser. A second launch reuses a ready Loren host instead of starting another process. If
port 5091 answers but `/health` is not Loren's ready response, the launcher stops with an
actionable port-conflict message and does not attach to or terminate that process.

If startup fails, read the host output in the launcher window. The first build may restore NuGet
dependencies and therefore requires normal package/network access for this repository. Missing SDK, missing project,
missing configuration, migration, and other host failures are surfaced by the host output. The
launcher diagnostics do not echo configuration values. The `.cmd` wrapper does not bypass PowerShell execution policy; if local policy blocks
the script, use the organization's approved policy or launch it through the normal Windows
PowerShell policy for the checkout.
