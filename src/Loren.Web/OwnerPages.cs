namespace Loren.Web;

internal static class OwnerPages
{
    public const string Login = """
<!doctype html>
<html lang="vi">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <script>try { const t = localStorage.getItem('loren-theme'); if (['white','graphite-black','graphite-cyan'].includes(t)) document.documentElement.dataset.theme = t; } catch {}</script>
  <title>Loren — Owner login</title>
  <style>
    :root { color-scheme: dark; --mono: "JetBrains Mono", "Cascadia Code", "Cascadia Mono", "Fira Code", Consolas, ui-monospace, "SFMono-Regular", Menlo, monospace; --bg:#0b0f14; --pane-2:#0f151c; --line:#22303c; --line-strong:#38505f; --text:#d3dee5; --muted:#6f8796; --accent:#5fd7d7; --ok:#87d787; --warn:#d7af5f; --err:#ff8787; --on-accent:#061014; }
    :root[data-theme="graphite-black"] { --bg:#000000; --pane-2:#0a0a0a; --line:#262626; --line-strong:#444444; --text:#d0d0d0; --muted:#7a7a7a; --accent:#eeeeee; --ok:#a8d08d; --warn:#e0c070; --err:#ff8a8a; --on-accent:#000000; }
    :root[data-theme="white"] { color-scheme: light; --bg:#f6f5ef; --pane-2:#eceae1; --line:#cfccbf; --line-strong:#9e9a8a; --text:#262622; --muted:#75736a; --accent:#00747d; --ok:#3a7a22; --warn:#996600; --err:#b3261e; --on-accent:#ffffff; }
    * { box-sizing: border-box; }
    html, body { height: 100%; }
    body { margin: 0; display: grid; place-items: center; padding: 16px; background: var(--bg); color: var(--text); font: 14px/1.55 var(--mono); }
    ::selection { background: var(--accent); color: var(--on-accent); }
    main { position: relative; width: min(100%, 440px); border: 1px solid var(--line-strong); padding: 26px 22px 18px; }
    .pane-title { position: absolute; top: -.8em; left: 12px; padding: 0 6px; background: var(--bg); color: var(--accent); font-size: 12px; }
    .theme { position: absolute; top: -.95em; right: 12px; padding: 0 4px; background: var(--bg); }
    .logo { margin: 0 0 4px; color: var(--accent); font-size: 12px; line-height: 1.2; white-space: pre; overflow: hidden; }
    p { margin: 0 0 18px; color: var(--muted); }
    label { display: block; margin-bottom: 6px; color: var(--muted); }
    label::before { content: '$ '; color: var(--accent); }
    .field { display: flex; align-items: center; gap: 8px; border: 1px solid var(--line-strong); padding: 0 10px; }
    .field:focus-within { border-color: var(--accent); }
    .field span { color: var(--accent); }
    input { flex: 1; min-width: 0; border: 0; outline: 0; padding: 9px 0; background: transparent; color: var(--text); font: inherit; caret-color: var(--accent); }
    button, select { font: inherit; }
    button[type="submit"] { width: 100%; margin-top: 14px; border: 1px solid var(--accent); padding: 8px 12px; background: var(--accent); color: var(--on-accent); font-weight: 700; cursor: pointer; }
    button[type="submit"]:hover { filter: brightness(1.1); }
    select { border: 1px solid var(--line); padding: 1px 4px; background: var(--bg); color: var(--muted); font-size: 12px; }
    :focus-visible { outline: 1px solid var(--accent); outline-offset: 2px; }
    .field input:focus-visible { outline: 0; }
    #error { min-height: 22px; margin-top: 12px; color: var(--err); }
    #error:not(:empty)::before { content: '✗ '; }
    .hint { margin-top: 8px; color: var(--muted); font-size: 12px; text-align: center; }
    .sr-only { position:absolute; width:1px; height:1px; overflow:hidden; clip:rect(0,0,0,0); white-space:nowrap; }
  </style>
</head>
<body>
  <main>
    <span class="pane-title">loren · owner login</span>
    <span class="theme"><label class="sr-only" for="theme-select">Theme</label><select id="theme-select" aria-label="Theme"><option value="white">White</option><option value="graphite-black">Graphite–Black</option><option value="graphite-cyan">Graphite–Cyan</option></select></span>
    <pre class="logo" aria-hidden="true"> _
| |    ___  _ __ ___ _ __
| |   / _ \| '__/ _ \ '_ \
| |__| (_) | | |  __/ | | |
|_____\___/|_|  \___|_| |_|</pre>
    <p>Your persistent personal assistant.</p>
    <form id="login-form">
      <label for="password">Mật khẩu chủ tài khoản</label>
      <div class="field"><span aria-hidden="true">❯</span><input id="password" name="password" type="password" autocomplete="current-password" required autofocus /></div>
      <button type="submit">Đăng nhập ⏎</button>
    </form>
    <div id="error" role="alert"></div>
    <div class="hint">local · owner-only · v0.1</div>
  </main>
  <script>
    const form = document.getElementById('login-form');
    const password = document.getElementById('password');
    const error = document.getElementById('error');
    const themeSelect = document.getElementById('theme-select');
    const allowedThemes = ['white', 'graphite-black', 'graphite-cyan'];
    const applyTheme = (theme, persist = true) => { if (!allowedThemes.includes(theme)) theme = 'graphite-cyan'; document.documentElement.dataset.theme = theme; themeSelect.value = theme; if (persist) { try { localStorage.setItem('loren-theme', theme); } catch {} } };
    applyTheme(document.documentElement.dataset.theme || 'graphite-cyan', false);
    themeSelect.addEventListener('change', () => applyTheme(themeSelect.value));

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
        error.textContent = 'Máy chủ Loren chưa cấu hình xác thực chủ tài khoản.';
        return;
      }

      error.textContent = 'Mật khẩu chủ tài khoản không đúng.';
      password.select();
    });
  </script>
</body>
</html>
""";

    public const string Console = """
<!doctype html>
<html lang="vi">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <script>try { const t = localStorage.getItem('loren-theme'); if (['white','graphite-black','graphite-cyan'].includes(t)) document.documentElement.dataset.theme = t; } catch {}</script>
  <title>Loren — v0.1 Conversation</title>
  <style>
    :root { color-scheme: dark; --mono: "JetBrains Mono", "Cascadia Code", "Cascadia Mono", "Fira Code", Consolas, ui-monospace, "SFMono-Regular", Menlo, monospace;
      --bg:#0b0f14; --pane-2:#0f151c; --hover:#131c25; --line:#22303c; --line-strong:#38505f; --text:#d3dee5; --muted:#6f8796; --dim:#4a5d6a;
      --accent:#5fd7d7; --user:#d7afff; --ok:#87d787; --warn:#d7af5f; --err:#ff8787; --code:#ffd787; --code-bg:#111a22; --on-accent:#061014; }
    :root[data-theme="graphite-black"] { --bg:#000000; --pane-2:#0a0a0a; --hover:#141414; --line:#262626; --line-strong:#444444; --text:#d0d0d0; --muted:#7a7a7a; --dim:#555555;
      --accent:#eeeeee; --user:#bdbdbd; --ok:#a8d08d; --warn:#e0c070; --err:#ff8a8a; --code:#f0f0f0; --code-bg:#0e0e0e; --on-accent:#000000; }
    :root[data-theme="white"] { color-scheme: light; --bg:#f6f5ef; --pane-2:#eceae1; --hover:#ebe9df; --line:#cfccbf; --line-strong:#9e9a8a; --text:#262622; --muted:#75736a; --dim:#a29f92;
      --accent:#00747d; --user:#7a3fa8; --ok:#3a7a22; --warn:#996600; --err:#b3261e; --code:#8a4b00; --code-bg:#ecebe3; --on-accent:#ffffff; }

    * { box-sizing: border-box; }
    html, body { height: 100%; }
    body { margin: 0; overflow: hidden; background: var(--bg); color: var(--text); font: 14px/1.6 var(--mono); scrollbar-color: var(--line-strong) transparent; }
    ::selection { background: var(--accent); color: var(--on-accent); }
    button, input, textarea, select { font: inherit; color: inherit; }
    button { cursor: pointer; border: 1px solid var(--line-strong); border-radius: 0; padding: 2px 10px; background: transparent; color: var(--text); }
    button:hover:not(:disabled) { border-color: var(--accent); background: var(--accent); color: var(--on-accent); }
    button:disabled { opacity: .45; cursor: wait; }
    :focus-visible { outline: 1px solid var(--accent); outline-offset: 1px; }
    kbd { border: 1px solid var(--line-strong); padding: 0 4px; font: inherit; font-size: 11px; color: var(--muted); }
    .sr-only { position:absolute; width:1px; height:1px; padding:0; margin:-1px; overflow:hidden; clip:rect(0,0,0,0); white-space:nowrap; border:0; }
    .empty { color: var(--muted); }
    .error { color: var(--err); }

    .app { height: 100vh; height: 100dvh; display: grid; grid-template-columns: minmax(0, 1fr); grid-template-rows: auto minmax(0, 1fr) auto; }

    /* top bar */
    .topbar { display: flex; align-items: center; justify-content: space-between; gap: 12px; padding: 5px 12px; border-bottom: 1px solid var(--line); background: var(--pane-2); }
    .brand { display: flex; align-items: baseline; gap: 10px; min-width: 0; white-space: nowrap; overflow: hidden; }
    .brand h1 { margin: 0; font-size: 14px; color: var(--accent); }
    .brand h1::before { content: '◆ '; }
    .brand small { color: var(--muted); overflow: hidden; text-overflow: ellipsis; }
    .header-actions { display: flex; align-items: center; gap: 6px; }
    .theme-select { border: 1px solid var(--line-strong); padding: 1px 4px; background: var(--bg); color: var(--muted); font-size: 12px; }
    .header-actions button { font-size: 12px; }

    /* workspace */
    .workspace { display: grid; grid-template-columns: 300px minmax(0, 1fr); gap: 12px; padding: 14px 12px 10px; min-height: 0; }
    body.panel-collapsed .workspace { grid-template-columns: minmax(0, 1fr); }
    body.panel-collapsed .side-panel { display: none; }
    .pane { position: relative; border: 1px solid var(--line); padding: 14px 12px 10px; }
    .pane:focus-within { border-color: var(--line-strong); }
    .pane-title { position: absolute; top: -.8em; left: 10px; z-index: 1; padding: 0 6px; background: var(--bg); color: var(--muted); font-size: 12px; line-height: 1.5; white-space: nowrap; }
    .pane-title b { color: var(--accent); font-weight: 700; }
    .pane:focus-within > .pane-title { color: var(--accent); }

    /* sidebar */
    .side-panel { display: flex; flex-direction: column; gap: 18px; min-height: 0; overflow: auto; padding: 8px 2px 2px; }
    .conversation-list { display: flex; flex-direction: column; max-height: 34vh; overflow: auto; }
    .conversation-list button { width: 100%; border: 0; padding: 1px 4px; text-align: left; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; color: var(--text); }
    .conversation-list button::before { content: '  '; white-space: pre; color: var(--accent); }
    .conversation-list button.active { background: var(--hover); color: var(--accent); }
    .conversation-list button.active::before { content: '▸ '; }
    .conversation-list button:hover:not(:disabled) { background: var(--hover); color: var(--accent); }
    .context-bar { display: flex; flex-direction: column; gap: 6px; color: var(--muted); font-size: 12px; }
    .context-bar select { width: 100%; border: 1px solid var(--line-strong); padding: 3px 6px; background: var(--bg); color: var(--text); font-size: 13px; }
    #project-status::before { content: '└ '; color: var(--dim); }
    .meta { display: flex; flex-wrap: wrap; gap: 2px 12px; margin-bottom: 8px; color: var(--muted); font-size: 12px; }
    table { width: 100%; border-collapse: collapse; font-size: 11px; }
    th, td { padding: 3px 4px; text-align: left; vertical-align: top; border-bottom: 1px dashed var(--line); overflow-wrap: anywhere; }
    th { color: var(--dim); font-weight: 400; text-transform: uppercase; letter-spacing: .05em; }
    .audit-table thead { position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0,0,0,0); }
    .audit-table, .audit-table tbody { display: block; }
    #audit tr { display: grid; grid-template-columns: minmax(0, 1fr) auto; grid-template-areas: "kind outcome" "action action" "detail detail"; padding: 5px 0; border-bottom: 1px dashed var(--line); }
    #audit td { padding: 0; border: 0; }
    #audit td:nth-child(1) { grid-area: kind; color: var(--dim); text-transform: uppercase; letter-spacing: .05em; }
    #audit td:nth-child(2) { grid-area: action; color: var(--text); }
    #audit td:nth-child(3) { grid-area: outcome; text-align: right; }
    #audit td:nth-child(4) { grid-area: detail; color: var(--muted); }
    #audit td:nth-child(4):empty { display: none; }
    #audit td[colspan] { grid-column: 1 / -1; grid-area: auto; text-transform: none; letter-spacing: 0; color: var(--muted); }
    td.ok { color: var(--ok); } td.err { color: var(--err); }
    details.advanced > summary { cursor: pointer; color: var(--muted); font-size: 12px; list-style: none; }
    details.advanced > summary::-webkit-details-marker { display: none; }
    details.advanced > summary::before { content: '▸ '; color: var(--accent); }
    details.advanced[open] > summary::before { content: '▾ '; }
    .advanced h2 { margin: 12px 0 2px; font-size: 13px; color: var(--accent); }
    .advanced p { margin: 0 0 8px; color: var(--muted); font-size: 12px; }
    .grid { display: grid; grid-template-columns: 1fr 1fr; gap: 0 8px; }
    label { display: block; margin: 6px 0 2px; color: var(--muted); font-size: 11px; }
    input { width: 100%; border: 1px solid var(--line); padding: 3px 6px; background: transparent; color: var(--text); font-size: 12px; }
    input:focus { border-color: var(--accent); outline: 0; }
    #bootstrap { margin-top: 10px; font-size: 12px; }
    pre.result { margin: 8px 0 0; padding: 6px 8px; border-left: 2px solid var(--line-strong); background: var(--code-bg); white-space: pre-wrap; overflow-wrap: anywhere; font-size: 11px; }

    /* conversation */
    .chat-pane { display: grid; grid-template-rows: minmax(0, 1fr) auto; min-height: 0; margin-top: 8px; padding: 0; }
    #chat { overflow-y: auto; padding: 18px 16px 12px; display: flex; flex-direction: column; gap: 14px; overscroll-behavior: contain; }
    .logo { margin: 0 0 4px; color: var(--accent); font-size: 12px; line-height: 1.2; white-space: pre; opacity: .9; }
    .message { display: grid; grid-template-columns: 6ch minmax(0, 1fr); gap: 0 12px; max-width: 980px; }
    .avatar { color: var(--accent); font-weight: 700; text-align: right; user-select: none; }
    .message.user .avatar { color: var(--user); }
    .bubble { min-width: 0; padding-left: 12px; border-left: 1px solid var(--line); overflow-wrap: anywhere; }
    .message.assistant .bubble { border-left-color: var(--line-strong); }
    .message.user .bubble { white-space: pre-wrap; color: var(--text); }
    .message.pending .bubble { color: var(--muted); }
    .message.pending .bubble::before { content: '⠋'; display: inline-block; width: 2ch; color: var(--accent); animation: spin .8s steps(1) infinite; }
    @keyframes spin { 0%{content:'⠋'} 12%{content:'⠙'} 25%{content:'⠹'} 37%{content:'⠸'} 50%{content:'⠼'} 62%{content:'⠴'} 75%{content:'⠦'} 87%{content:'⠧'} }
    .bubble h1, .bubble h2, .bubble h3 { margin: 4px 0 6px; font-size: 14px; line-height: 1.4; color: var(--accent); }
    .bubble h1::before { content: '# '; color: var(--dim); }
    .bubble h2::before { content: '## '; color: var(--dim); }
    .bubble h3::before { content: '### '; color: var(--dim); }
    .bubble p { margin: 0 0 8px; } .bubble p:last-child { margin-bottom: 0; }
    .bubble ul, .bubble ol { margin: 2px 0 8px; padding-left: 3ch; }
    .bubble ul { list-style-type: '• '; }
    .bubble li::marker { color: var(--accent); }
    .bubble strong { color: var(--accent); font-weight: 700; }
    .bubble code { padding: 0 4px; background: var(--code-bg); color: var(--code); }
    .bubble pre { position: relative; margin: 6px 0 10px; padding: 26px 12px 10px; border: 1px dashed var(--line-strong); overflow: auto; background: var(--code-bg); }
    .bubble pre::before { content: 'code'; position: absolute; top: 3px; left: 10px; color: var(--dim); font-size: 11px; }
    .bubble pre code { padding: 0; color: var(--text); white-space: pre; background: transparent; }
    .bubble a { color: var(--accent); text-decoration: underline; text-decoration-style: dotted; text-underline-offset: 3px; }
    .bubble a::after { content: ' ↗'; font-size: .85em; }
    .copy-code { position: absolute; top: 2px; right: 4px; border: 0; padding: 0 6px; font-size: 11px; color: var(--muted); }
    .copy-code::before { content: '['; } .copy-code::after { content: ']'; }
    .welcome-actions { display: flex; flex-wrap: wrap; gap: 8px; margin-left: calc(6ch + 24px); }
    .welcome-actions button { font-size: 13px; }
    .welcome-actions button::before { content: '› '; color: var(--accent); }
    .welcome-actions button:hover:not(:disabled)::before { color: inherit; }

    /* proposal */
    .proposal-card { position: relative; max-width: 720px; margin: 10px 0 4px calc(6ch + 24px); padding: 16px 14px 12px; border: 1px solid var(--warn); }
    .proposal-card h2 { position: absolute; top: -.8em; left: 10px; margin: 0; padding: 0 6px; background: var(--bg); color: var(--warn); font-size: 12px; font-weight: 700; }
    .proposal-card h2::before { content: '⚠ '; }
    .proposal-card p { margin: 8px 0 0; color: var(--muted); font-size: 13px; }
    .proposal-facts { display: grid; gap: 1px 0; }
    .proposal-facts .fact { display: grid; grid-template-columns: 12ch minmax(0, 1fr); gap: 12px; }
    .proposal-facts .fact small { color: var(--muted); font-size: 13px; }
    .proposal-facts .fact small::after { content: ':'; }
    .proposal-facts .fact strong { font-weight: 400; overflow-wrap: anywhere; }
    .proposal-actions { display: flex; flex-wrap: wrap; gap: 8px; margin-top: 12px; }
    .proposal-actions button { padding: 3px 14px; font-weight: 700; }
    .proposal-actions button:first-child { border-color: var(--ok); color: var(--ok); }
    .proposal-actions button:first-child:hover:not(:disabled) { background: var(--ok); color: var(--on-accent); }
    .proposal-status { display: inline-block; margin-top: 10px; color: var(--warn); font-size: 12px; }
    .proposal-status::before { content: '[ '; } .proposal-status::after { content: ' ]'; }

    /* composer */
    .composer { display: flex; align-items: flex-start; gap: 10px; padding: 8px 12px; border-top: 1px solid var(--line); }
    .prompt { padding-top: 4px; color: var(--accent); font-weight: 700; user-select: none; }
    textarea { flex: 1; min-width: 0; height: 32px; min-height: 32px; max-height: 200px; resize: none; border: 0; outline: 0; padding: 4px 0; background: transparent; color: var(--text); line-height: 1.6; caret-color: var(--accent); }
    textarea::placeholder { color: var(--dim); }
    .send { margin-top: 2px; border-color: var(--accent); color: var(--accent); font-weight: 700; }

    /* status bar */
    .statusbar { display: flex; align-items: stretch; min-width: 0; border-top: 1px solid var(--line); background: var(--pane-2); font-size: 12px; white-space: nowrap; overflow: hidden; }
    .statusbar .mode { padding: 1px 10px; background: var(--accent); color: var(--on-accent); font-weight: 700; }
    .statusbar .seg { padding: 1px 10px; border-right: 1px solid var(--line); color: var(--muted); }
    .statusbar .seg:empty { display: none; }
    .statusbar .seg b { font-weight: 400; color: var(--text); }
    .statusbar .dot { font-size: 9px; vertical-align: 1px; }
    .ok { color: var(--ok); } .warn { color: var(--warn); } .err { color: var(--err); } .dim { color: var(--dim); }
    .statusbar b.ok { color: var(--ok); } .statusbar b.warn { color: var(--warn); } .statusbar b.err { color: var(--err); } .statusbar b.dim { color: var(--muted); }
    .status { flex: 1; min-width: 0; padding: 1px 10px; overflow: hidden; text-overflow: ellipsis; color: var(--text); }
    .keys { padding: 1px 10px; color: var(--muted); }
    .keys kbd { border: 0; padding: 0; color: var(--accent); }

    @media (max-width: 900px) {
      .workspace { grid-template-columns: minmax(0, 1fr); padding: 12px 8px 8px; }
      .side-panel { display: none; }
      body.panel-open .side-panel { display: flex; position: fixed; z-index: 20; top: 38px; bottom: 22px; left: 0; right: 0; padding: 18px 12px 12px; background: var(--bg); border-bottom: 1px solid var(--line); }
      .conversation-list { max-height: 40vh; }
      .topbar { padding: 5px 8px; gap: 8px; }
      .brand small { display: none; }
      .header-actions { gap: 4px; min-width: 0; }
      .header-actions button { padding: 2px 6px; }
      .theme-select { max-width: 96px; min-width: 0; }
      #chat { padding: 14px 10px 10px; }
      .message { grid-template-columns: minmax(0, 1fr); gap: 2px; }
      .avatar { text-align: left; }
      .bubble { padding-left: 10px; }
      .welcome-actions, .proposal-card { margin-left: 0; }
      .statusbar .opt, .keys { display: none; }
      .proposal-facts .fact { grid-template-columns: minmax(0, 1fr); gap: 0; }
    }
    @media (prefers-reduced-motion: reduce) { *, *::before, *::after { scroll-behavior: auto !important; animation: none !important; transition-duration: .01ms !important; } }
  </style>
</head>
<body>
 <div class="app">
  <header class="topbar">
    <div class="brand">
      <h1>loren</h1><small>v0.1 · trợ lý cá nhân · owner console</small>
    </div>
    <div class="header-actions">
      <label class="sr-only" for="theme-select">Theme</label><select id="theme-select" class="theme-select" aria-label="Theme"><option value="white">White</option><option value="graphite-black">Graphite–Black</option><option value="graphite-cyan">Graphite–Cyan</option></select>
      <button id="context-toggle" type="button" aria-expanded="true" aria-controls="context-panel" title="Bật/tắt bảng ngữ cảnh (Ctrl+B)">Chi tiết</button>
      <button id="new-chat" type="button" aria-label="Cuộc trò chuyện mới" title="Cuộc trò chuyện mới (Alt+N)">+ Mới</button>
      <button id="logout" type="button">Thoát</button>
    </div>
  </header>

  <main class="workspace">
    <aside class="side-panel" id="context-panel" aria-label="Ngữ cảnh và hoạt động">
      <section class="pane">
        <span class="pane-title"><b>1</b> cuộc trò chuyện</span>
        <div id="conversation-list" class="conversation-list"><span class="empty">Đang tải…</span></div>
      </section>
      <section class="pane" id="project-context-panel">
        <span class="pane-title"><b>2</b> project</span>
        <div class="context-bar">
          <label class="sr-only" for="project-context">Project đang dùng</label>
          <select id="project-context" aria-label="Project context">
            <option value="">Tự nhận diện từ cuộc trò chuyện</option>
          </select>
          <span id="project-status">Đang tải project…</span>
        </div>
      </section>
      <section class="pane" id="activity-panel">
        <span class="pane-title"><b>3</b> hoạt động · audit</span>
        <div id="meta" class="meta"><span class="empty">Chưa có lượt chạy.</span></div>
        <table class="audit-table">
          <thead><tr><th>Loại</th><th>Hành động</th><th>Kết quả</th><th>Chi tiết</th></tr></thead>
          <tbody id="audit"><tr><td colspan="4" class="empty">Chưa có hoạt động tool.</td></tr></tbody>
        </table>
      </section>
      <section class="pane">
        <span class="pane-title"><b>4</b> thiết lập</span>
        <details class="advanced" id="advanced-panel">
          <summary>Thiết lập project &amp; kiểm tra an toàn</summary>
          <h2>Khởi tạo project GitHub chuẩn</h2>
          <p>Dùng một lần trên database mới. Alias hiện có không bị đổi ngầm.</p>
          <div class="grid">
            <div><label for="bootstrap-project-name">Tên project</label><input id="bootstrap-project-name" placeholder="Loren" /></div>
            <div><label for="bootstrap-project-alias">Alias</label><input id="bootstrap-project-alias" placeholder="loren" /></div>
            <div><label for="bootstrap-github-owner">Owner GitHub</label><input id="bootstrap-github-owner" placeholder="owner" /></div>
            <div><label for="bootstrap-github-repository">Repository</label><input id="bootstrap-github-repository" placeholder="loren" /></div>
          </div>
          <button id="bootstrap" type="button">Lưu project chuẩn</button>
          <pre id="bootstrap-result" class="result empty">Chưa cấu hình trong phiên này.</pre>
        </details>
      </section>
    </aside>

    <section class="pane chat-pane" aria-label="Cuộc trò chuyện">
      <span class="pane-title"><b>0</b> hội thoại</span>
      <div id="chat" aria-live="polite"></div>
      <div class="composer">
        <span class="prompt" aria-hidden="true">❯</span>
        <textarea id="message" aria-label="Tin nhắn cho Loren" placeholder="Nhắn Loren…  (/ để focus)" rows="1" autofocus></textarea>
        <button id="send" class="send" type="button">Gửi ⏎</button>
      </div>
    </section>
  </main>

  <footer class="statusbar" aria-label="Trạng thái">
    <span class="mode">LOREN</span>
    <span class="seg" id="seg-sys"><span class="dot dim">●</span> sys:<b class="dim">…</b></span>
    <span class="seg opt" id="seg-brain"></span>
    <span class="seg opt" id="seg-web"></span>
    <span class="seg" id="seg-writes"></span>
    <span class="seg opt" id="seg-projects"></span>
    <span id="status" class="status" role="status">Sẵn sàng · Enter để gửi · Shift+Enter xuống dòng</span>
    <span class="keys"><kbd>^B</kbd> panel · <kbd>Alt+N</kbd> mới · <kbd>/</kbd> nhập · <kbd>Esc</kbd></span>
  </footer>
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
    const contextToggle = document.getElementById('context-toggle');
    const themeSelect = document.getElementById('theme-select');
    const logout = document.getElementById('logout');
    const bootstrap = document.getElementById('bootstrap');
    const bootstrapResult = document.getElementById('bootstrap-result');
    const conversationList = document.getElementById('conversation-list');
    const LOGO = String.raw` _
| |    ___  _ __ ___ _ __
| |   / _ \| '__/ _ \ '_ \
| |__| (_) | | |  __/ | | |
|_____\___/|_|  \___|_| |_|`;

    let history = [];
    let conversationId = null;
    let conversationLoadVersion = 0;
    let conversationLoading = false;
    let initializing = true;
    let projects = [];
    let welcomeActions = null;
    const allowedThemes = ['white', 'graphite-black', 'graphite-cyan'];
    function applyTheme(theme, persist = true) {
      const nextTheme = allowedThemes.includes(theme) ? theme : 'graphite-cyan';
      document.documentElement.dataset.theme = nextTheme;
      themeSelect.value = nextTheme;
      if (persist) { try { localStorage.setItem('loren-theme', nextTheme); } catch {} }
    }
    applyTheme(document.documentElement.dataset.theme || 'graphite-cyan', false);
    themeSelect.addEventListener('change', () => applyTheme(themeSelect.value));

    function appendInline(parent, text) {
      const pieces = text.split(/(`[^`]+`|\*\*[^*]+\*\*|\[[^\]]+\]\([^)]*\))/g).filter(Boolean);
      for (const piece of pieces) {
        if (piece.startsWith('`') && piece.endsWith('`')) {
          const code = document.createElement('code');
          code.textContent = piece.slice(1, -1);
          parent.appendChild(code);
          continue;
        }
        if (piece.startsWith('**') && piece.endsWith('**')) {
          const strong = document.createElement('strong');
          strong.textContent = piece.slice(2, -2);
          parent.appendChild(strong);
          continue;
        }
        const link = piece.match(/^\[([^\]]+)\]\(([^)]*)\)$/);
        if (!link) {
          parent.appendChild(document.createTextNode(piece));
          continue;
        }
        const url = link[2].trim();
        if (!/^https?:\/\//i.test(url)) {
          parent.appendChild(document.createTextNode(link[1]));
          continue;
        }
        const anchor = document.createElement('a');
        anchor.textContent = link[1];
        anchor.href = url;
        anchor.target = '_blank';
        anchor.rel = 'noopener noreferrer';
        parent.appendChild(anchor);
      }
    }

    function renderMarkdown(text) {
      const fragment = document.createDocumentFragment();
      const lines = String(text ?? '').replace(/\r/g, '').split('\n');
      let list = null;
      let listType = null;
      let paragraph = [];
      let inCode = false;
      let codeLines = [];
      const flushParagraph = () => {
        if (!paragraph.length) return;
        const p = document.createElement('p');
        appendInline(p, paragraph.join(' '));
        fragment.appendChild(p);
        paragraph = [];
      };
      const closeList = () => { list = null; listType = null; };
      for (const line of lines) {
        if (line.trim().startsWith('```')) {
          flushParagraph(); closeList();
          if (inCode) {
            const pre = document.createElement('pre');
            const code = document.createElement('code');
            code.textContent = codeLines.join('\n');
            const copy = document.createElement('button');
            copy.type = 'button'; copy.className = 'copy-code'; copy.textContent = 'copy';
            copy.addEventListener('click', async () => {
              try {
                if (!navigator.clipboard) throw new Error('Clipboard unavailable');
                await navigator.clipboard.writeText(code.textContent ?? '');
                copy.textContent = 'đã copy';
              } catch { copy.textContent = 'lỗi copy'; }
              setTimeout(() => { copy.textContent = 'copy'; }, 1200);
            });
            pre.append(copy, code); fragment.appendChild(pre);
            codeLines = []; inCode = false;
          } else inCode = true;
          continue;
        }
        if (inCode) { codeLines.push(line); continue; }
        const heading = line.match(/^(#{1,3})\s+(.+)$/);
        if (heading) { flushParagraph(); closeList(); const h = document.createElement(`h${heading[1].length}`); appendInline(h, heading[2]); fragment.appendChild(h); continue; }
        const bullet = line.match(/^\s*[-*]\s+(.+)$/);
        const ordered = line.match(/^\s*\d+[.)]\s+(.+)$/);
        if (bullet || ordered) {
          flushParagraph();
          const nextType = bullet ? 'ul' : 'ol';
          if (!list || listType !== nextType) { closeList(); list = document.createElement(nextType); listType = nextType; fragment.appendChild(list); }
          const item = document.createElement('li'); appendInline(item, (bullet ?? ordered)[1]); list.appendChild(item); continue;
        }
        if (!line.trim()) { flushParagraph(); closeList(); continue; }
        closeList(); paragraph.push(line.trim());
      }
      if (inCode) {
        const pre = document.createElement('pre');
        const code = document.createElement('code');
        code.textContent = codeLines.join('\n');
        pre.appendChild(code);
        fragment.appendChild(pre);
      }
      flushParagraph();
      return fragment;
    }

    function addMessage(role, text, extraClass = '') {
      const row = document.createElement('div');
      row.className = `message ${role} ${extraClass}`.trim();
      const avatar = document.createElement('div');
      avatar.className = 'avatar';
      avatar.textContent = role === 'user' ? 'bạn' : 'loren';
      const bubble = document.createElement('div');
      bubble.className = 'bubble';
      if (role === 'assistant' && extraClass !== 'pending') bubble.appendChild(renderMarkdown(text));
      else bubble.textContent = text;
      row.appendChild(avatar);
      row.appendChild(bubble);
      chat.appendChild(row);
      row.scrollIntoView({ behavior: 'smooth', block: 'end' });
      return row;
    }

    function markActiveConversation() {
      for (const button of conversationList.querySelectorAll('button')) {
        const active = Boolean(conversationId) && button.dataset.id === conversationId;
        button.classList.toggle('active', active);
        if (active) button.setAttribute('aria-current', 'true'); else button.removeAttribute('aria-current');
      }
    }

    function resetConversation(explicit = true) {
      if (send.disabled) return;
      conversationId = null;
      if (explicit) { try { localStorage.removeItem('loren-active-conversation'); } catch {} }
      history = [];
      chat.replaceChildren();
      const logo = document.createElement('pre');
      logo.className = 'logo'; logo.setAttribute('aria-hidden', 'true'); logo.textContent = LOGO;
      chat.appendChild(logo);
      addMessage('assistant', 'Hôm nay mày muốn kể gì, hay mình tiếp việc đang dở?');
      welcomeActions = document.createElement('div');
      welcomeActions.className = 'welcome-actions';
      const catchUp = document.createElement('button');
      catchUp.type = 'button'; catchUp.textContent = 'Bắt nhịp hôm nay';
      catchUp.title = 'Hỏi Loren về task đang mở và các quyết định đã lưu';
      catchUp.addEventListener('click', () => { if (message.value.trim()) { message.focus(); return; } message.value = 'Bắt nhịp hôm nay trên tất cả project: hãy tóm tắt các task đang mở và các quyết định gần đây từ dữ liệu đã lưu, theo từng project nếu có. Nếu không có dữ liệu, nói rõ là chưa có.'; void sendMessage(); });
      const talk = document.createElement('button');
      talk.type = 'button'; talk.textContent = 'Mình nói chuyện chút';
      talk.addEventListener('click', () => message.focus());
      welcomeActions.append(catchUp, talk); chat.appendChild(welcomeActions);
      meta.textContent = 'Chưa có lượt chạy.';
      audit.replaceChildren();
      const emptyAudit = document.createElement('tr');
      const emptyAuditCell = document.createElement('td');
      emptyAuditCell.colSpan = 4; emptyAuditCell.className = 'empty'; emptyAuditCell.textContent = 'Chưa có hoạt động tool.';
      emptyAudit.appendChild(emptyAuditCell); audit.appendChild(emptyAudit);
      status.textContent = 'Sẵn sàng · Enter để gửi · Shift+Enter xuống dòng';
      markActiveConversation();
      message.focus();
    }

    function renderConversationList(items) {
      conversationList.replaceChildren();
      if (!items?.length) { conversationList.textContent = 'Chưa có cuộc trò chuyện đã lưu.'; return; }
      for (const item of items) {
        const button = document.createElement('button');
        button.type = 'button'; button.textContent = item.title || 'Cuộc trò chuyện';
        button.title = item.title || 'Cuộc trò chuyện';
        button.dataset.id = item.id;
        button.disabled = send.disabled;
        button.addEventListener('click', () => { if (isMobile()) setContextLayout(false); void loadConversation(item.id); });
        conversationList.appendChild(button);
      }
      markActiveConversation();
    }

    async function loadConversations(refreshOnly = false) {
      try {
        const items = await readJson(await fetch('/api/conversations'));
        renderConversationList(items);
        if (refreshOnly) return;
        let active = null; try { active = localStorage.getItem('loren-active-conversation'); } catch {}
        if (active && items.some(item => item.id === active)) await loadConversation(active);
      }
      catch (error) { conversationList.textContent = error instanceof Error ? error.message : String(error); }
    }

    async function loadConversation(id) {
      if (conversationLoading || (send.disabled && !initializing)) return;
      conversationLoading = true;
      send.disabled = true;
      const loadVersion = ++conversationLoadVersion;
      try {
        const item = await readJson(await fetch(`/api/conversations/${encodeURIComponent(id)}`));
        if (loadVersion !== conversationLoadVersion) return;
        conversationId = item.id; try { localStorage.setItem('loren-active-conversation', conversationId); } catch {}
        history = []; chat.replaceChildren(); welcomeActions = null; audit.replaceChildren();
        meta.textContent = 'Đã mở lại cuộc trò chuyện. Đề xuất cũ không có nút duyệt; hãy yêu cầu kiểm tra mới nếu cần.';
        projectContext.value = item.projectAlias ?? '';
        for (const entry of item.messages ?? []) { addMessage(entry.role === 'user' ? 'user' : 'assistant', entry.content); history.push({ role: entry.role, content: entry.content }); }
        if ((item.messages ?? []).length >= 200) meta.textContent = 'Đang hiển thị 200 tin nhắn gần nhất. Các đề xuất cũ không có nút duyệt; hãy yêu cầu kiểm tra mới nếu cần.';
        status.textContent = 'Đã mở cuộc trò chuyện'; markActiveConversation(); message.focus();
      } catch (error) { status.textContent = error instanceof Error ? error.message : String(error); }
      finally { if (loadVersion === conversationLoadVersion) { conversationLoading = false; if (!initializing) send.disabled = false; } }
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

    function setSegment(id, label, value, tone) {
      const segment = document.getElementById(id);
      if (!segment) return;
      const dot = document.createElement('span');
      dot.className = `dot ${tone}`; dot.textContent = '●';
      const strong = document.createElement('b');
      strong.className = tone; strong.textContent = String(value ?? '—');
      segment.replaceChildren(dot, document.createTextNode(` ${label}:`), strong);
      segment.title = `${label}: ${value ?? '—'}`;
    }

    async function loadReadiness() {
      try {
        const report = await readJson(await fetch('/api/readiness'));
        const tone = (value, good) => good.includes(value) ? 'ok' : (value === 'empty' ? 'dim' : 'warn');
        setSegment('seg-sys', 'sys', report.status === 'ready' ? 'ready' : report.status, report.status === 'ready' ? 'ok' : 'warn');
        setSegment('seg-brain', 'brain', report.brain?.status, tone(report.brain?.status, ['configured']));
        setSegment('seg-web', 'web', report.webResearch?.status, tone(report.webResearch?.status, ['configured']));
        const writes = report.externalWrites ?? {};
        setSegment('seg-writes', 'writes', writes.enabled ? `ON · ${writes.status}` : 'off', writes.enabled ? (writes.status === 'ready' ? 'warn' : 'err') : 'ok');
        const projectState = report.projects?.status;
        setSegment('seg-projects', 'projects', projectState === 'unavailable' ? 'unavailable' : (report.projects?.count ?? 0), projectState === 'unavailable' ? 'err' : (projectState === 'empty' ? 'dim' : 'ok'));
      } catch {
        setSegment('seg-sys', 'sys', 'không rõ', 'err');
      }
    }

    async function loadProjects(preferredAlias = null) {
      try {
        const response = await fetch('/api/projects');
        projects = await readJson(response);
        const previous = preferredAlias ?? projectContext.value;
        projectContext.replaceChildren();
        const auto = document.createElement('option');
        auto.value = '';
        auto.textContent = 'Tự nhận diện từ cuộc trò chuyện';
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
        projectStatus.className = '';
        projectStatus.textContent = projects.length ? `${projects.length} project đã cấu hình` : 'Chưa có project';
      } catch (error) {
        projectStatus.textContent = error instanceof Error ? error.message : String(error);
        projectStatus.className = 'error';
      }
    }

    function outcomeTone(outcome) {
      const value = String(outcome ?? '').toLowerCase();
      if (/(fail|error|denied|deny|block|reject|revok)/.test(value)) return 'err';
      if (/(succe|complet|allow|ok|verified)/.test(value)) return 'ok';
      if (/(pending|wait)/.test(value)) return 'warn';
      return '';
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
        const tone = outcomeTone(event.outcome);
        if (tone) row.children[2].className = tone;
        audit.appendChild(row);
      }
    }

    function renderProposals(proposals) {
      for (const proposal of proposals ?? []) {
        const card = document.createElement('section');
        card.className = 'panel proposal-card';
        const title = document.createElement('h2');
        title.textContent = 'Đề xuất thay đổi GitHub';
        card.appendChild(title);
        const facts = document.createElement('div'); facts.className = 'proposal-facts';
        for (const [label, value] of [['Repository', proposal.repository], ['Branch', proposal.branch], ['Source ref', proposal.sourceRef], ['Source SHA', proposal.sourceSha], ['Hết hạn', proposal.expiresAt]]) {
          const fact = document.createElement('div'); fact.className = 'fact';
          const small = document.createElement('small'); small.textContent = label;
          const strong = document.createElement('strong'); strong.textContent = value ?? '—';
          fact.append(small, strong); facts.appendChild(fact);
        }
        card.appendChild(facts);
        const note = document.createElement('p');
        note.textContent = 'Thay đổi GitHub bên ngoài; cần mày duyệt một lần.';
        card.appendChild(note);
        const actions = document.createElement('div'); actions.className = 'proposal-actions';
        const approve = document.createElement('button'); approve.type = 'button'; approve.textContent = 'Duyệt';
        const cancel = document.createElement('button'); cancel.type = 'button'; cancel.textContent = 'Huỷ';
        actions.append(approve, cancel); card.appendChild(actions);
        const status = document.createElement('span'); status.className = 'proposal-status';
        const proposalStatus = String(proposal.status ?? '').toLowerCase();
        status.textContent = proposalStatus === 'pending' ? 'Chờ duyệt' : (proposal.status ?? 'Chờ duyệt');
        card.appendChild(status);
        const result = document.createElement('p');
        const decide = async (kind) => {
          approve.disabled = true; cancel.disabled = true;
          try {
            const response = await postJson(`/api/action-proposals/${encodeURIComponent(proposal.proposalId)}/${kind}`, {});
            result.textContent = response.message ?? (kind === 'cancel' ? 'Đã huỷ; không có thay đổi GitHub nào được thực hiện.' : 'Đã xử lý đề xuất.');
            status.textContent = kind === 'cancel' ? 'Đã huỷ' : 'Đã duyệt';
            addMessage('assistant', result.textContent);
            if (response.audit) renderActivity({ runId: 'decision', turns: 0, actionCount: 0, audit: response.audit });
          } catch (error) { result.textContent = error instanceof Error ? error.message : String(error); }
        };
        approve.addEventListener('click', () => void decide('approve'));
        cancel.addEventListener('click', () => void decide('cancel'));
        card.appendChild(result);
        chat.appendChild(card);
        card.scrollIntoView({ behavior: 'smooth', block: 'end' });
      }
    }

    function autosize() {
      message.style.height = 'auto';
      message.style.height = `${Math.min(message.scrollHeight, 200)}px`;
    }

    async function sendMessage() {
      const text = message.value.trim();
      if (!text || send.disabled) return;

      const priorHistory = history.slice();
      const selectedAlias = projectContext.value || null;
      addMessage('user', text);
      message.value = '';
      autosize();
      send.disabled = true;
      if (welcomeActions) welcomeActions.querySelectorAll('button').forEach(button => { button.disabled = true; });
      status.textContent = 'Loren đang suy nghĩ…';
      const pending = addMessage('assistant', 'Đang suy nghĩ…', 'pending');

      try {
        const result = await postJson('/api/run', {
          message: text,
          projectAlias: selectedAlias,
          history: priorHistory,
          conversationId,
          clearProjectContext: Boolean(conversationId && !selectedAlias)
        });
        conversationId = result.conversationId ?? conversationId;
        try { localStorage.setItem('loren-active-conversation', conversationId); } catch {}
        pending.remove();
        addMessage('assistant', result.finalOutput);
        renderProposals(result.proposals);
        history.push({ role: 'user', content: text });
        history.push({ role: 'assistant', content: result.finalOutput });
        renderActivity(result);

        if (!selectedAlias && result.project?.aliases?.length) {
          const inferredAlias = result.project.aliases[0];
          if ([...projectContext.options].some(option => option.value === inferredAlias)) {
            projectContext.value = inferredAlias;
            projectStatus.textContent = `Đang dùng project nhận diện: ${result.project.name}`;
          }
        }

        status.textContent = result.actionCount
          ? `✓ Xong · ${result.actionCount} tool action`
          : '✓ Xong';
        void loadConversations(true);
      } catch (error) {
        pending.remove();
        const errorText = error instanceof Error ? error.message : String(error);
        addMessage('assistant', `I couldn't complete that turn: ${errorText}`);
        status.textContent = '✗ Có lỗi';
      } finally {
        send.disabled = false;
        conversationList.querySelectorAll('button').forEach(button => { button.disabled = false; });
        message.focus();
      }
    }

    send.addEventListener('click', sendMessage);
    message.addEventListener('input', autosize);
    message.addEventListener('keydown', (event) => {
      if (event.key === 'Enter' && !event.shiftKey && !event.isComposing) {
        event.preventDefault();
        void sendMessage();
      }
    });

    newChat.addEventListener('click', resetConversation);

    const mobileQuery = window.matchMedia('(max-width: 900px)');
    const isMobile = () => mobileQuery.matches;
    const desktopPanelPreference = () => { try { return localStorage.getItem('loren-panel') !== 'closed'; } catch { return true; } };
    let panelOpen = false;
    function setContextLayout(open, persist = false) {
      panelOpen = open;
      const mobile = isMobile();
      document.body.classList.toggle('panel-collapsed', !mobile && !open);
      document.body.classList.toggle('panel-open', mobile && open);
      contextToggle.setAttribute('aria-expanded', String(open));
      if (persist && !mobile) { try { localStorage.setItem('loren-panel', open ? 'open' : 'closed'); } catch {} }
    }
    const togglePanel = () => setContextLayout(!panelOpen, true);
    contextToggle.addEventListener('click', togglePanel);
    mobileQuery.addEventListener('change', () => setContextLayout(isMobile() ? false : desktopPanelPreference()));
    setContextLayout(isMobile() ? false : desktopPanelPreference());

    document.addEventListener('keydown', (event) => {
      const tag = document.activeElement?.tagName;
      const typing = tag === 'TEXTAREA' || tag === 'INPUT' || tag === 'SELECT';
      const key = event.key.toLowerCase();
      if ((event.ctrlKey || event.metaKey) && !event.altKey && key === 'b') { event.preventDefault(); togglePanel(); return; }
      if (event.altKey && !event.ctrlKey && key === 'n') { event.preventDefault(); resetConversation(); return; }
      if (event.key === 'Escape') {
        if (isMobile() && panelOpen) setContextLayout(false);
        else if (document.activeElement !== message) message.focus();
        return;
      }
      if (!typing && !event.ctrlKey && !event.metaKey && !event.altKey && event.key === '/') { event.preventDefault(); message.focus(); }
    });

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
        await loadProjects(alias);
        void loadReadiness();
      } catch (error) {
        bootstrapResult.className = 'result error';
        bootstrapResult.textContent = error instanceof Error ? error.message : String(error);
      } finally {
        bootstrap.disabled = false;
      }
    });

    logout.addEventListener('click', async () => {
      await fetch('/auth/logout', { method: 'POST' });
      location.assign('/login');
    });

    resetConversation(false);
    send.disabled = true;
    void (async () => {
      await loadProjects();
      await loadConversations();
      initializing = false;
      send.disabled = false;
      conversationList.querySelectorAll('button').forEach(button => { button.disabled = false; });
      void loadReadiness();
    })();
  </script>
</body>
</html>
""";
}
