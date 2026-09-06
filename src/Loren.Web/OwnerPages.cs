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
    :root { color-scheme: dark; font-family: Inter, ui-sans-serif, system-ui, sans-serif; }
    * { box-sizing: border-box; }
    body { margin: 0; min-height: 100vh; display: grid; place-items: center; background: #0b1020; color: #f8fafc; }
    main { width: min(92vw, 420px); padding: 30px; border: 1px solid #25324a; border-radius: 20px; background: #111827; box-shadow: 0 22px 70px rgba(0,0,0,.35); }
    .brand { display: flex; align-items: center; gap: 12px; margin-bottom: 18px; }
    .mark { width: 42px; height: 42px; display: grid; place-items: center; border-radius: 14px; background: #e2e8f0; color: #0f172a; font-weight: 900; }
    h1 { margin: 0; font-size: 28px; }
    p { color: #94a3b8; line-height: 1.55; }
    label { display: block; margin: 20px 0 8px; font-weight: 650; }
    input, button { width: 100%; border-radius: 11px; border: 1px solid #334155; padding: 12px 14px; font: inherit; }
    input { background: #0b1020; color: #f8fafc; }
    button { margin-top: 12px; cursor: pointer; background: #f8fafc; color: #0f172a; border: 0; font-weight: 750; }
    #error { min-height: 24px; margin-top: 12px; color: #fca5a5; }
  </style>
</head>
<body>
  <main>
    <div class="brand"><div class="mark">L</div><div><h1>Loren</h1><p style="margin:4px 0 0">Your persistent personal assistant.</p></div></div>
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
  <title>Loren — v0.1 Conversation</title>
  <style>
    :root { color-scheme: dark; font-family: Inter, ui-sans-serif, system-ui, sans-serif; }
    * { box-sizing: border-box; }
    body { margin: 0; min-height: 100vh; background: #0b1020; color: #e5edf8; }
    button, input, textarea, select { font: inherit; }
    button { cursor: pointer; }
    header { position: sticky; top: 0; z-index: 5; display: flex; align-items: center; justify-content: space-between; gap: 14px; padding: 14px 20px; border-bottom: 1px solid #243047; background: rgba(11,16,32,.94); backdrop-filter: blur(12px); }
    .brand { display: flex; align-items: center; gap: 11px; min-width: 0; }
    .mark { width: 36px; height: 36px; display: grid; place-items: center; border-radius: 12px; background: #e2e8f0; color: #0f172a; font-weight: 900; }
    .brand h1 { margin: 0; font-size: 18px; }
    .brand small { display: block; margin-top: 2px; color: #7f8da4; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .header-actions { display: flex; align-items: center; gap: 8px; }
    .ghost { border: 1px solid #334155; border-radius: 10px; padding: 8px 11px; background: #111827; color: #cbd5e1; }
    .shell { width: min(920px, calc(100% - 24px)); margin: 0 auto; }
    .context-bar { display: flex; align-items: center; gap: 10px; padding: 14px 4px 8px; color: #94a3b8; font-size: 13px; }
    .context-bar select { min-width: 180px; max-width: 340px; border: 1px solid #334155; border-radius: 9px; padding: 7px 10px; background: #111827; color: #dbeafe; }
    #chat { min-height: calc(100vh - 270px); padding: 20px 0 150px; display: flex; flex-direction: column; gap: 18px; }
    .message { display: flex; gap: 11px; max-width: 86%; }
    .message.user { align-self: flex-end; flex-direction: row-reverse; }
    .avatar { flex: 0 0 32px; width: 32px; height: 32px; display: grid; place-items: center; border-radius: 10px; background: #1e293b; color: #cbd5e1; font-size: 12px; font-weight: 800; }
    .message.user .avatar { background: #334155; }
    .bubble { border: 1px solid #27344a; border-radius: 16px; padding: 12px 14px; background: #111827; white-space: pre-wrap; overflow-wrap: anywhere; line-height: 1.58; }
    .message.user .bubble { background: #1d2a42; border-color: #344967; }
    .message.pending .bubble { color: #94a3b8; }
    .composer-wrap { position: fixed; left: 0; right: 0; bottom: 0; padding: 16px 12px 20px; background: linear-gradient(transparent, #0b1020 28%); z-index: 4; }
    .composer { width: min(920px, 100%); margin: 0 auto; border: 1px solid #334155; border-radius: 17px; padding: 10px; background: #111827; box-shadow: 0 15px 50px rgba(0,0,0,.32); }
    textarea { width: 100%; min-height: 52px; max-height: 180px; resize: vertical; border: 0; outline: 0; padding: 9px 10px; background: transparent; color: #f8fafc; line-height: 1.45; }
    .composer-actions { display: flex; align-items: center; justify-content: space-between; gap: 10px; padding: 4px 4px 0; }
    .status { color: #728198; font-size: 12px; }
    .send { border: 0; border-radius: 11px; padding: 9px 15px; background: #f8fafc; color: #0f172a; font-weight: 800; }
    .send:disabled { opacity: .55; cursor: wait; }
    details.panel { margin: 8px 0 18px; border: 1px solid #25324a; border-radius: 14px; background: #0f172a; overflow: hidden; }
    details.panel > summary { cursor: pointer; padding: 12px 14px; color: #9fb0c7; font-weight: 650; }
    .panel-body { border-top: 1px solid #25324a; padding: 14px; }
    .meta { display: flex; flex-wrap: wrap; gap: 7px 14px; margin-bottom: 12px; color: #94a3b8; font-size: 12px; }
    table { width: 100%; border-collapse: collapse; font-size: 12px; }
    th, td { padding: 8px 7px; text-align: left; vertical-align: top; border-bottom: 1px solid #25324a; overflow-wrap: anywhere; }
    th { color: #94a3b8; }
    .empty { color: #64748b; }
    .error { color: #fca5a5; }
    .advanced { margin: 18px 0 180px; }
    .advanced section + section { margin-top: 20px; padding-top: 20px; border-top: 1px solid #25324a; }
    .advanced h2 { margin: 0 0 8px; font-size: 15px; }
    .advanced p { color: #8190a7; font-size: 13px; line-height: 1.5; }
    .grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(210px, 1fr)); gap: 0 12px; }
    label { display: block; margin: 8px 0 6px; color: #b8c5d7; font-size: 13px; font-weight: 650; }
    input { width: 100%; margin-bottom: 10px; border: 1px solid #334155; border-radius: 9px; padding: 9px 10px; background: #0b1020; color: #f8fafc; }
    .advanced button { border: 1px solid #475569; border-radius: 9px; padding: 9px 12px; background: #e2e8f0; color: #0f172a; font-weight: 750; }
    .advanced button.danger { background: #f59e0b; border-color: #f59e0b; }
    pre.result { margin: 10px 0 0; border-radius: 9px; padding: 10px; background: #090e1b; white-space: pre-wrap; overflow-wrap: anywhere; font-size: 12px; }
    .warning { padding: 10px 12px; border: 1px solid #854d0e; border-radius: 9px; background: #3c1f08; color: #fde68a !important; }
    @media (max-width: 640px) {
      header { padding: 12px; }
      .brand small { display: none; }
      .message { max-width: 95%; }
      .context-bar { align-items: stretch; flex-direction: column; }
      .context-bar select { max-width: none; width: 100%; }
      .header-actions .ghost:first-child { display: none; }
    }
  </style>
</head>
<body>
  <header>
    <div class="brand">
      <div class="mark">L</div>
      <div><h1>Loren</h1><small>Personal secretary · v0.1 · Loren owner console</small></div>
    </div>
    <div class="header-actions">
      <button id="new-chat" class="ghost" type="button">New conversation</button>
      <button id="logout" class="ghost" type="button">Sign out</button>
    </div>
  </header>

  <main class="shell">
    <div class="context-bar">
      <span>Project context</span>
      <select id="project-context" aria-label="Project context">
        <option value="">Auto-detect from conversation</option>
      </select>
      <span id="project-status">Loading projects…</span>
    </div>

    <div id="chat" aria-live="polite"></div>

    <details class="panel" id="activity-panel">
      <summary>Activity &amp; audit for the latest turn</summary>
      <div class="panel-body">
        <div id="meta" class="meta"><span class="empty">No run yet.</span></div>
        <table>
          <thead><tr><th>Kind</th><th>Action</th><th>Outcome</th><th>Detail</th></tr></thead>
          <tbody id="audit"><tr><td colspan="4" class="empty">No tool activity yet.</td></tr></tbody>
        </table>
      </div>
    </details>

    <details class="panel advanced" id="advanced-panel">
      <summary>Advanced / setup / safety test harness</summary>
      <div class="panel-body">
        <p>These controls exist for local setup and trust-boundary testing. Normal Loren use should happen through conversation above.</p>

        <section>
          <h2>Bootstrap a canonical GitHub project</h2>
          <p>Use once on a fresh database. Existing aliases are never silently rebound.</p>
          <div class="grid">
            <div><label for="bootstrap-project-name">Project name</label><input id="bootstrap-project-name" placeholder="Loren" /></div>
            <div><label for="bootstrap-project-alias">Project alias</label><input id="bootstrap-project-alias" placeholder="loren" /></div>
            <div><label for="bootstrap-github-owner">GitHub owner</label><input id="bootstrap-github-owner" placeholder="rua-den" /></div>
            <div><label for="bootstrap-github-repository">GitHub repository</label><input id="bootstrap-github-repository" placeholder="loren" /></div>
          </div>
          <button id="bootstrap" type="button">Save canonical project</button>
          <pre id="bootstrap-result" class="result empty">Not configured in this session.</pre>
        </section>

        <section>
          <h2>Create-branch safety harness</h2>
          <p class="warning"><strong>External write.</strong> This remains the explicit low-level M5 test harness. M6A.5 will replace normal use with a conversational approval card.</p>
          <div class="grid">
            <div><label for="write-project-alias">Project alias</label><input id="write-project-alias" placeholder="loren" /></div>
            <div><label for="write-repository-id">Repository ID (optional)</label><input id="write-repository-id" placeholder="Only for disambiguation" /></div>
            <div><label for="write-branch">New branch</label><input id="write-branch" placeholder="loren/manual-smoke" /></div>
            <div><label for="write-source-sha">Exact source commit SHA</label><input id="write-source-sha" placeholder="40-character Git commit SHA" maxlength="40" /></div>
          </div>
          <button id="create-branch" class="danger" type="button">Approve &amp; create branch</button>
          <pre id="write-result" class="result empty">No write attempted.</pre>
        </section>
      </div>
    </details>
  </main>

  <div class="composer-wrap">
    <div class="composer">
      <textarea id="message" aria-label="Message Loren" placeholder="Message Loren…" autofocus></textarea>
      <div class="composer-actions">
        <span id="status" class="status">Ready · Enter to send · Shift+Enter for newline</span>
        <button id="send" class="send" type="button">Send</button>
      </div>
    </div>
  </div>

  <script>
    const chat = document.getElementById('chat');
    const message = document.getElementById('message');
    const send = document.getElementById('send');
    const status = document.getElementById('status');
    const projectContext = document.getElementById('project-context');
    const projectStatus = document.getElementById('project-status');
    const meta = document.getElementById('meta');
    const audit = document.getElementById('audit');
    const newChat = document.getElementById('new-chat');
    const logout = document.getElementById('logout');
    const bootstrap = document.getElementById('bootstrap');
    const bootstrapResult = document.getElementById('bootstrap-result');
    const createBranch = document.getElementById('create-branch');
    const writeResult = document.getElementById('write-result');

    let history = [];
    let projects = [];

    function addMessage(role, text, extraClass = '') {
      const row = document.createElement('div');
      row.className = `message ${role} ${extraClass}`.trim();
      const avatar = document.createElement('div');
      avatar.className = 'avatar';
      avatar.textContent = role === 'user' ? 'YOU' : 'L';
      const bubble = document.createElement('div');
      bubble.className = 'bubble';
      bubble.textContent = text;
      row.appendChild(avatar);
      row.appendChild(bubble);
      chat.appendChild(row);
      row.scrollIntoView({ behavior: 'smooth', block: 'end' });
      return row;
    }

    function resetConversation() {
      history = [];
      chat.replaceChildren();
      addMessage('assistant', 'Tao là Loren. Cứ hỏi bình thường; nếu câu hỏi cần project, tao sẽ dùng project mày chọn hoặc thử nhận ra từ câu nói. Tool activity nằm ở phần phụ bên dưới.');
      meta.innerHTML = '<span class="empty">No run yet.</span>';
      audit.innerHTML = '<tr><td colspan="4" class="empty">No tool activity yet.</td></tr>';
      status.textContent = 'Ready · Enter to send · Shift+Enter for newline';
      message.focus();
    }

    async function readJson(response) {
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

    async function postJson(url, payload) {
      const response = await fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      });
      return readJson(response);
    }

    async function loadProjects(preferredAlias = null) {
      try {
        const response = await fetch('/api/projects');
        projects = await readJson(response);
        const previous = preferredAlias ?? projectContext.value;
        projectContext.replaceChildren();
        const auto = document.createElement('option');
        auto.value = '';
        auto.textContent = 'Auto-detect from conversation';
        projectContext.appendChild(auto);

        for (const project of projects) {
          const alias = project.aliases?.[0] ?? '';
          if (!alias) continue;
          const option = document.createElement('option');
          option.value = alias;
          option.textContent = `${project.name} · ${alias}`;
          projectContext.appendChild(option);
        }

        if (previous && [...projectContext.options].some(option => option.value === previous)) {
          projectContext.value = previous;
        }
        projectStatus.textContent = projects.length ? `${projects.length} configured` : 'No project configured';
      } catch (error) {
        projectStatus.textContent = error instanceof Error ? error.message : String(error);
        projectStatus.className = 'error';
      }
    }

    function renderActivity(result) {
      meta.replaceChildren();
      const values = [
        ['run', result.runId],
        ['turns', result.turns],
        ['actions', result.actionCount],
      ];
      if (result.project) {
        values.push(['project', result.project.name]);
        values.push(['repositories', result.project.repositories.map(repo => `${repo.provider}:${repo.externalFullName}`).join(', ') || 'none']);
      }
      for (const [label, value] of values) {
        const span = document.createElement('span');
        span.textContent = `${label}: ${value}`;
        meta.appendChild(span);
      }

      audit.replaceChildren();
      if (!result.audit?.length) {
        const row = document.createElement('tr');
        const cell = document.createElement('td');
        cell.colSpan = 4;
        cell.className = 'empty';
        cell.textContent = 'No tool activity in this turn.';
        row.appendChild(cell);
        audit.appendChild(row);
        return;
      }

      for (const event of result.audit) {
        const row = document.createElement('tr');
        for (const value of [event.kind, event.actionName, event.outcome, event.detail ?? '']) {
          const cell = document.createElement('td');
          cell.textContent = value;
          row.appendChild(cell);
        }
        audit.appendChild(row);
      }
    }

    async function sendMessage() {
      const text = message.value.trim();
      if (!text || send.disabled) return;

      const priorHistory = history.slice();
      const selectedAlias = projectContext.value || null;
      addMessage('user', text);
      message.value = '';
      send.disabled = true;
      status.textContent = 'Loren is thinking…';
      const pending = addMessage('assistant', 'Thinking…', 'pending');

      try {
        const result = await postJson('/api/run', {
          message: text,
          projectAlias: selectedAlias,
          history: priorHistory
        });
        pending.remove();
        addMessage('assistant', result.finalOutput);
        history.push({ role: 'user', content: text });
        history.push({ role: 'assistant', content: result.finalOutput });
        renderActivity(result);

        if (!selectedAlias && result.project?.aliases?.length) {
          const inferredAlias = result.project.aliases[0];
          if ([...projectContext.options].some(option => option.value === inferredAlias)) {
            projectContext.value = inferredAlias;
            projectStatus.textContent = `Using inferred project: ${result.project.name}`;
          }
        }

        status.textContent = result.actionCount
          ? `Complete · ${result.actionCount} tool action${result.actionCount === 1 ? '' : 's'}`
          : 'Complete';
      } catch (error) {
        pending.remove();
        const errorText = error instanceof Error ? error.message : String(error);
        addMessage('assistant', `I couldn't complete that turn: ${errorText}`);
        status.textContent = 'Failed';
      } finally {
        send.disabled = false;
        message.focus();
      }
    }

    send.addEventListener('click', sendMessage);
    message.addEventListener('keydown', (event) => {
      if (event.key === 'Enter' && !event.shiftKey) {
        event.preventDefault();
        void sendMessage();
      }
    });

    newChat.addEventListener('click', resetConversation);

    bootstrap.addEventListener('click', async () => {
      bootstrap.disabled = true;
      try {
        const result = await postJson('/api/projects/bootstrap', {
          projectName: document.getElementById('bootstrap-project-name').value,
          projectAlias: document.getElementById('bootstrap-project-alias').value,
          gitHubOwner: document.getElementById('bootstrap-github-owner').value,
          gitHubRepository: document.getElementById('bootstrap-github-repository').value
        });
        bootstrapResult.className = 'result';
        bootstrapResult.textContent = JSON.stringify(result, null, 2);
        const alias = result.aliases?.[0] ?? '';
        document.getElementById('write-project-alias').value = alias;
        document.getElementById('write-repository-id').value = result.repositoryId ?? '';
        await loadProjects(alias);
      } catch (error) {
        bootstrapResult.className = 'result error';
        bootstrapResult.textContent = error instanceof Error ? error.message : String(error);
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
        writeResult.className = 'result error';
        writeResult.textContent = 'Project alias, branch and exact source SHA are required.';
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
        writeResult.textContent = JSON.stringify(result, null, 2);
      } catch (error) {
        writeResult.className = 'result error';
        writeResult.textContent = error instanceof Error ? error.message : String(error);
      } finally {
        createBranch.disabled = false;
      }
    });

    logout.addEventListener('click', async () => {
      await fetch('/auth/logout', { method: 'POST' });
      location.assign('/login');
    });

    resetConversation();
    void loadProjects();
  </script>
</body>
</html>
""";
}
