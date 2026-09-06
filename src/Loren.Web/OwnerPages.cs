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
  <title>Loren — v0.1 Owner Console</title>
  <style>
    :root { color-scheme: dark; font-family: Inter, system-ui, sans-serif; }
    body { margin: 0; background: #0f172a; color: #f8fafc; }
    header { display: flex; align-items: center; justify-content: space-between; gap: 16px; padding: 18px 24px; border-bottom: 1px solid #334155; background: #111827; }
    header h1 { margin: 0; font-size: 22px; }
    header button { width: auto; margin: 0; }
    main { width: min(1120px, calc(100% - 32px)); margin: 28px auto 48px; display: grid; gap: 20px; }
    section { border: 1px solid #334155; border-radius: 16px; background: #111827; padding: 20px; }
    h2 { margin-top: 0; font-size: 18px; }
    h3 { margin: 18px 0 8px; font-size: 15px; color: #cbd5e1; }
    label { display: block; margin: 0 0 8px; font-weight: 600; }
    input, textarea, button { box-sizing: border-box; border-radius: 10px; border: 1px solid #475569; padding: 12px 14px; font: inherit; }
    input, textarea { width: 100%; background: #0f172a; color: #f8fafc; }
    input { margin-bottom: 14px; }
    textarea { min-height: 100px; resize: vertical; }
    button { cursor: pointer; background: #f8fafc; color: #0f172a; font-weight: 700; }
    button.danger { background: #f59e0b; color: #111827; }
    button:disabled { opacity: .55; cursor: wait; }
    .grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(240px, 1fr)); gap: 0 14px; }
    .hint { margin: -6px 0 14px; color: #94a3b8; font-size: 13px; line-height: 1.5; }
    .warning { padding: 12px 14px; border: 1px solid #92400e; border-radius: 10px; background: #451a03; color: #fde68a; line-height: 1.5; }
    .actions { display: flex; gap: 10px; align-items: center; margin-top: 12px; flex-wrap: wrap; }
    .status { color: #94a3b8; }
    pre { margin: 0; white-space: pre-wrap; overflow-wrap: anywhere; line-height: 1.55; }
    .result { margin-top: 14px; padding: 12px; min-height: 22px; border-radius: 10px; background: #0f172a; }
    .meta { display: flex; flex-wrap: wrap; gap: 8px 18px; margin-bottom: 14px; color: #cbd5e1; font-size: 14px; }
    table { width: 100%; border-collapse: collapse; font-size: 14px; }
    th, td { text-align: left; vertical-align: top; border-bottom: 1px solid #334155; padding: 10px 8px; overflow-wrap: anywhere; }
    th { color: #cbd5e1; }
    .empty { color: #64748b; }
    .error { color: #fca5a5; }
    code { color: #bfdbfe; }
  </style>
</head>
<body>
  <header>
    <h1>Loren owner console · v0.1 M5</h1>
    <button id="logout" type="button">Sign out</button>
  </header>
  <main>
    <section>
      <h2>1. Bootstrap a canonical GitHub project</h2>
      <p class="hint">Use this once on a fresh Loren database. Existing aliases are never silently rebound.</p>
      <div class="grid">
        <div>
          <label for="bootstrap-project-name">Project name</label>
          <input id="bootstrap-project-name" placeholder="Loren" />
        </div>
        <div>
          <label for="bootstrap-project-alias">Project alias</label>
          <input id="bootstrap-project-alias" placeholder="loren" />
        </div>
        <div>
          <label for="bootstrap-github-owner">GitHub owner</label>
          <input id="bootstrap-github-owner" placeholder="rua-den" />
        </div>
        <div>
          <label for="bootstrap-github-repository">GitHub repository</label>
          <input id="bootstrap-github-repository" placeholder="loren" />
        </div>
      </div>
      <button id="bootstrap" type="button">Save canonical project</button>
      <pre id="bootstrap-result" class="result empty">Not configured in this session.</pre>
    </section>

    <section>
      <h2>2. Approve & create a non-default GitHub branch</h2>
      <p class="warning"><strong>External write.</strong> This button is the explicit owner approval for the exact project/repository, branch name and source SHA shown below. Loren consumes that approval once, resolves <code>GITHUB_WRITE_TOKEN</code> only inside the executor boundary, creates the branch, then independently fetches the ref and verifies the exact SHA.</p>
      <div class="grid">
        <div>
          <label for="write-project-alias">Project alias</label>
          <input id="write-project-alias" placeholder="loren" />
        </div>
        <div>
          <label for="write-repository-id">Repository ID (optional)</label>
          <input id="write-repository-id" placeholder="Leave blank when project has one GitHub repo" />
        </div>
        <div>
          <label for="write-branch">New branch</label>
          <input id="write-branch" placeholder="loren/manual-smoke" />
        </div>
        <div>
          <label for="write-source-sha">Exact source commit SHA</label>
          <input id="write-source-sha" placeholder="40-character Git commit SHA" maxlength="40" />
        </div>
      </div>
      <p class="hint">Host must have <code>LOREN_ENABLE_WRITES=true</code>, <code>GITHUB_WRITE_TOKEN</code> set, and <code>LOREN_GITHUB_WRITE_CREDENTIAL_REVOKED=false</code>. Default-branch creation/replacement is blocked.</p>
      <button id="create-branch" class="danger" type="button">Approve &amp; create branch</button>
      <pre id="write-result" class="result empty">No write attempted.</pre>
    </section>

    <section>
      <h2>3. Run Loren</h2>
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
    const logout = document.getElementById('logout');
    const bootstrap = document.getElementById('bootstrap');
    const bootstrapResult = document.getElementById('bootstrap-result');
    const createBranch = document.getElementById('create-branch');
    const writeResult = document.getElementById('write-result');
    const projectAlias = document.getElementById('project-alias');
    const message = document.getElementById('message');
    const send = document.getElementById('send');
    const status = document.getElementById('status');
    const answer = document.getElementById('answer');
    const meta = document.getElementById('meta');
    const audit = document.getElementById('audit');

    async function postJson(url, payload) {
      const response = await fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      });

      if (response.status === 401) {
        location.assign('/login');
        throw new Error('Owner session expired.');
      }

      const body = await response.json().catch(() => null);
      if (!response.ok) {
        throw new Error(body?.error ?? `HTTP ${response.status}`);
      }

      return body;
    }

    function showJson(element, value) {
      element.className = 'result';
      element.textContent = JSON.stringify(value, null, 2);
    }

    function showError(element, error) {
      element.className = 'result error';
      element.textContent = error instanceof Error ? error.message : String(error);
    }

    bootstrap.addEventListener('click', async () => {
      bootstrap.disabled = true;
      try {
        const result = await postJson('/api/projects/bootstrap', {
          projectName: document.getElementById('bootstrap-project-name').value,
          projectAlias: document.getElementById('bootstrap-project-alias').value,
          gitHubOwner: document.getElementById('bootstrap-github-owner').value,
          gitHubRepository: document.getElementById('bootstrap-github-repository').value
        });
        showJson(bootstrapResult, result);
        document.getElementById('write-project-alias').value = result.aliases?.[0] ?? '';
        document.getElementById('write-repository-id').value = result.repositoryId ?? '';
        projectAlias.value = result.aliases?.[0] ?? '';
      } catch (error) {
        showError(bootstrapResult, error);
      } finally {
        bootstrap.disabled = false;
      }
    });

    createBranch.addEventListener('click', async () => {
      const alias = document.getElementById('write-project-alias').value.trim();
      const repositoryId = document.getElementById('write-repository-id').value.trim();
      const branch = document.getElementById('write-branch').value.trim();
      const sourceSha = document.getElementById('write-source-sha').value.trim();

      if (!alias || !branch || !sourceSha) {
        showError(writeResult, new Error('Project alias, branch and exact source SHA are required.'));
        return;
      }

      if (!confirm(`Approve one-time GitHub branch creation?\n\nProject: ${alias}\nRepository ID: ${repositoryId || '(single GitHub repo)'}\nBranch: ${branch}\nSource SHA: ${sourceSha}`)) {
        return;
      }

      createBranch.disabled = true;
      writeResult.className = 'result';
      writeResult.textContent = 'Executing approved action…';
      try {
        const result = await postJson('/api/github/create-branch', {
          projectAlias: alias,
          repositoryId: repositoryId || null,
          branch,
          sourceSha
        });
        showJson(writeResult, result);
      } catch (error) {
        showError(writeResult, error);
      } finally {
        createBranch.disabled = false;
      }
    });

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
        const result = await postJson('/api/run', {
          message: text,
          projectAlias: alias || null
        });
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
