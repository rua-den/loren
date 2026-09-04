namespace Loren.Web;

internal static class OwnerPages
{
    public const string Login = """
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>Loren — Owner login</title>
  <style>
    :root { color-scheme: light dark; font-family: Inter, system-ui, sans-serif; }
    body { margin: 0; min-height: 100vh; display: grid; place-items: center; background: #111827; color: #f9fafb; }
    main { width: min(92vw, 420px); padding: 28px; border: 1px solid #374151; border-radius: 18px; background: #1f2937; box-shadow: 0 18px 60px rgba(0,0,0,.25); }
    h1 { margin: 0 0 8px; font-size: 28px; }
    p { color: #cbd5e1; line-height: 1.5; }
    label { display: block; margin: 20px 0 8px; font-weight: 600; }
    input, button { box-sizing: border-box; width: 100%; border-radius: 10px; border: 1px solid #4b5563; padding: 12px 14px; font: inherit; }
    input { background: #111827; color: #f9fafb; }
    button { margin-top: 12px; cursor: pointer; background: #f9fafb; color: #111827; border: 0; font-weight: 700; }
    #error { min-height: 24px; margin-top: 12px; color: #fca5a5; }
  </style>
</head>
<body>
  <main>
    <h1>Loren</h1>
    <p>Owner-only preview. Sign in to access the Loren request console.</p>
    <form id="login-form">
      <label for="password">Owner password</label>
      <input id="password" name="password" type="password" autocomplete="current-password" required autofocus />
      <button type="submit">Sign in</button>
    </form>
    <div id="error" role="alert"></div>
  </main>
  <script>
    const form = document.getElementById('login-form');
    const password = document.getElementById('password');
    const error = document.getElementById('error');

    form.addEventListener('submit', async (event) => {
      event.preventDefault();
      error.textContent = '';

      const response = await fetch('/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ password: password.value })
      });

      if (response.ok) {
        location.assign('/');
        return;
      }

      if (response.status === 503) {
        error.textContent = 'Owner authentication is not configured on this Loren host.';
        return;
      }

      error.textContent = 'Invalid owner password.';
      password.select();
    });
  </script>
</body>
</html>
""";

    public const string Console = """
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>Loren — M3 Owner Console</title>
  <style>
    :root { color-scheme: light dark; font-family: Inter, system-ui, sans-serif; }
    body { margin: 0; background: #0f172a; color: #f8fafc; }
    header { display: flex; align-items: center; justify-content: space-between; gap: 16px; padding: 18px 24px; border-bottom: 1px solid #334155; background: #111827; }
    header h1 { margin: 0; font-size: 22px; }
    header button { width: auto; margin: 0; }
    main { width: min(1100px, calc(100% - 32px)); margin: 28px auto 48px; display: grid; gap: 20px; }
    section { border: 1px solid #334155; border-radius: 16px; background: #111827; padding: 20px; }
    h2 { margin-top: 0; font-size: 18px; }
    label { display: block; margin: 0 0 8px; font-weight: 600; }
    input, textarea, button { box-sizing: border-box; border-radius: 10px; border: 1px solid #475569; padding: 12px 14px; font: inherit; }
    input, textarea { width: 100%; background: #0f172a; color: #f8fafc; }
    input { margin-bottom: 14px; }
    textarea { min-height: 100px; resize: vertical; }
    button { cursor: pointer; background: #f8fafc; color: #0f172a; font-weight: 700; }
    button:disabled { opacity: .55; cursor: wait; }
    .hint { margin: -6px 0 14px; color: #94a3b8; font-size: 13px; }
    .actions { display: flex; gap: 10px; align-items: center; margin-top: 12px; }
    .status { color: #94a3b8; }
    pre { margin: 0; white-space: pre-wrap; overflow-wrap: anywhere; line-height: 1.55; }
    .meta { display: flex; flex-wrap: wrap; gap: 8px 18px; margin-bottom: 14px; color: #cbd5e1; font-size: 14px; }
    table { width: 100%; border-collapse: collapse; font-size: 14px; }
    th, td { text-align: left; vertical-align: top; border-bottom: 1px solid #334155; padding: 10px 8px; overflow-wrap: anywhere; }
    th { color: #cbd5e1; }
    .empty { color: #64748b; }
    .error { color: #fca5a5; }
  </style>
</head>
<body>
  <header>
    <h1>Loren owner console · v0.1 M3</h1>
    <button id="logout" type="button">Sign out</button>
  </header>
  <main>
    <section>
      <h2>Request</h2>
      <label for="project-alias">Project alias</label>
      <input id="project-alias" placeholder="Optional exact configured alias, e.g. wedding-online" />
      <p class="hint">When set, Loren resolves this alias to canonical Project/Repository state before the model runs.</p>
      <label for="message">Message</label>
      <textarea id="message">Loren, check the configured project's repository.</textarea>
      <div class="actions">
        <button id="send" type="button">Run Loren</button>
        <span id="status" class="status">Ready</span>
      </div>
    </section>

    <section>
      <h2>Answer</h2>
      <div id="meta" class="meta"></div>
      <pre id="answer" class="empty">No run yet.</pre>
    </section>

    <section>
      <h2>Audit</h2>
      <table>
        <thead>
          <tr>
            <th>Kind</th>
            <th>Action</th>
            <th>Outcome</th>
            <th>Action ID</th>
            <th>Detail</th>
          </tr>
        </thead>
        <tbody id="audit">
          <tr><td colspan="5" class="empty">No audit events yet.</td></tr>
        </tbody>
      </table>
    </section>
  </main>

  <script>
    const projectAlias = document.getElementById('project-alias');
    const message = document.getElementById('message');
    const send = document.getElementById('send');
    const logout = document.getElementById('logout');
    const status = document.getElementById('status');
    const answer = document.getElementById('answer');
    const meta = document.getElementById('meta');
    const audit = document.getElementById('audit');

    function addMeta(label, value) {
      const item = document.createElement('span');
      item.textContent = `${label}: ${value}`;
      meta.appendChild(item);
    }

    function addAuditCell(row, value) {
      const cell = document.createElement('td');
      cell.textContent = value ?? '';
      row.appendChild(cell);
    }

    send.addEventListener('click', async () => {
      const text = message.value;
      if (!text.trim()) return;

      send.disabled = true;
      status.textContent = 'Running…';
      answer.className = '';
      answer.textContent = '';
      meta.replaceChildren();
      audit.replaceChildren();

      try {
        const alias = projectAlias.value.trim();
        const response = await fetch('/api/run', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            message: text,
            projectAlias: alias || null
          })
        });

        if (response.status === 401) {
          location.assign('/login');
          return;
        }

        if (!response.ok) {
          const body = await response.text();
          throw new Error(`HTTP ${response.status}: ${body}`);
        }

        const result = await response.json();
        answer.textContent = result.finalOutput;
        addMeta('runId', result.runId);
        addMeta('turns', result.turns);
        addMeta('actions', result.actionCount);

        if (result.project) {
          addMeta('project', `${result.project.name} (${result.project.projectId})`);
          const repositories = result.project.repositories
            .map(repository => `${repository.provider}:${repository.externalFullName}`)
            .join(', ');
          addMeta('repositories', repositories || 'none');
        }

        if (!result.audit.length) {
          const row = document.createElement('tr');
          const cell = document.createElement('td');
          cell.colSpan = 5;
          cell.className = 'empty';
          cell.textContent = 'No audit events returned for this run.';
          row.appendChild(cell);
          audit.appendChild(row);
        } else {
          for (const event of result.audit) {
            const row = document.createElement('tr');
            addAuditCell(row, event.kind);
            addAuditCell(row, event.actionName);
            addAuditCell(row, event.outcome);
            addAuditCell(row, event.actionId);
            addAuditCell(row, event.detail);
            audit.appendChild(row);
          }
        }

        status.textContent = 'Complete';
      } catch (error) {
        answer.className = 'error';
        answer.textContent = error instanceof Error ? error.message : String(error);
        status.textContent = 'Failed';
      } finally {
        send.disabled = false;
      }
    });

    logout.addEventListener('click', async () => {
      await fetch('/auth/logout', { method: 'POST' });
      location.assign('/login');
    });
  </script>
</body>
</html>
""";
}
