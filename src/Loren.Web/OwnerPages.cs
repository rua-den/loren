namespace Loren.Web;

internal static class OwnerPages
{
    public const string Login = """
<!doctype html>
<html lang="vi">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>Loren — Owner login</title>
  <style>
    :root { color-scheme: dark; font-family: Inter, ui-sans-serif, system-ui, sans-serif; --bg:#071018; --surface:#0d1922; --surface-2:#10222c; --line:#1f3a45; --line-strong:#2d5964; --text:#e6f3f5; --muted:#8daab0; --cyan:#7dd3c7; --cyan-strong:#a6f2e5; --danger:#f3a4a4; }
    * { box-sizing: border-box; }
    body { margin: 0; min-height: 100vh; display: grid; place-items: center; background: radial-gradient(circle at 50% 0, rgba(42,105,111,.28), transparent 42%), var(--bg); color: var(--text); }
    main { width: min(92vw, 420px); padding: 32px; border: 1px solid var(--line); border-radius: 20px; background: rgba(13,25,34,.92); box-shadow: 0 22px 70px rgba(0,0,0,.35); }
    .brand { display: flex; align-items: center; gap: 12px; margin-bottom: 18px; }
    .mark { width: 42px; height: 42px; display: grid; place-items: center; border-radius: 14px; background: var(--cyan); color: #092126; font-weight: 900; }
    h1 { margin: 0; font-size: 28px; }
    p { color: var(--muted); line-height: 1.55; }
    label { display: block; margin: 20px 0 8px; font-weight: 650; }
    input, button { width: 100%; border-radius: 11px; border: 1px solid var(--line-strong); padding: 12px 14px; font: inherit; }
    input { background: #09151c; color: var(--text); }
    button { margin-top: 12px; cursor: pointer; background: var(--cyan); color: #092126; border: 0; font-weight: 750; }
    #error { min-height: 24px; margin-top: 12px; color: var(--danger); }
  </style>
</head>
<body>
  <main>
    <div class="brand"><div class="mark">L</div><div><h1>Loren</h1><p style="margin:4px 0 0">Your persistent personal assistant.</p></div></div>
    <form id="login-form">
      <label for="password">Mật khẩu chủ tài khoản</label>
      <input id="password" name="password" type="password" autocomplete="current-password" required autofocus />
      <button type="submit">Đăng nhập</button>
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
  <title>Loren — v0.1 Conversation</title>
  <style>
    :root { color-scheme: dark; font-family: Inter, ui-sans-serif, system-ui, sans-serif; --bg:#071018; --surface:#0d1922; --surface-2:#10222c; --line:#1f3a45; --line-strong:#2d5964; --text:#e6f3f5; --muted:#8daab0; --cyan:#7dd3c7; --cyan-strong:#a6f2e5; --danger:#f3a4a4; }
    * { box-sizing: border-box; }
    body { margin: 0; min-height: 100vh; background: radial-gradient(circle at 55% -10%, rgba(42,105,111,.24), transparent 35%), var(--bg); color: var(--text); }
    button, input, textarea, select { font: inherit; }
    button { cursor: pointer; }
    header { position: sticky; top: 0; z-index: 5; display: flex; align-items: center; justify-content: space-between; gap: 14px; padding: 14px 22px; border-bottom: 1px solid var(--line); background: rgba(7,16,24,.88); backdrop-filter: blur(16px); }
    .brand { display: flex; align-items: center; gap: 11px; min-width: 0; }
    .brand .mark { flex: 0 0 36px; }
    .mark { width: 36px; height: 36px; display: grid; place-items: center; border-radius: 12px; background: var(--cyan); color: #092126; font-weight: 900; box-shadow: 0 0 0 5px rgba(125,211,199,.08); }
    .brand h1 { margin: 0; font-size: 18px; }
    .brand small { display: block; margin-top: 2px; color: var(--muted); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .header-actions { display: flex; align-items: center; gap: 8px; }
    .ghost { border: 1px solid var(--line-strong); border-radius: 10px; padding: 8px 11px; background: rgba(13,25,34,.86); color: #cde4e6; }
    .ghost:hover, .send:hover, .advanced button:hover { border-color: var(--cyan); }
    .ghost:focus-visible, .send:focus-visible, summary:focus-visible, button:focus-visible, textarea:focus-visible, select:focus-visible, input:focus-visible { outline: 2px solid var(--cyan-strong); outline-offset: 2px; }
    .shell { width: min(1240px, calc(100% - 36px)); margin: 0 auto; display: grid; grid-template-columns: minmax(0, 1fr) 306px; gap: 24px; align-items: start; }
    .main-column { min-width: 0; }
    .context-bar { display: flex; align-items: center; gap: 10px; padding: 18px 2px 8px; color: var(--muted); font-size: 13px; }
    .context-bar select { min-width: 180px; max-width: 340px; border: 1px solid var(--line-strong); border-radius: 9px; padding: 7px 10px; background: var(--surface); color: #dbeeed; }
    #context-toggle { display:none; }
    #chat { min-height: calc(100vh - 270px); padding: 20px 0 150px; display: flex; flex-direction: column; gap: 18px; }
    .message { display: flex; gap: 11px; max-width: 86%; }
    .message.user { align-self: flex-end; flex-direction: row-reverse; }
    .avatar { flex: 0 0 32px; width: 32px; height: 32px; display: grid; place-items: center; border-radius: 10px; background: #19313a; color: var(--cyan-strong); font-size: 12px; font-weight: 800; }
    .message.user .avatar { background: #24414a; }
    .bubble { border: 1px solid var(--line); border-radius: 16px; padding: 12px 14px; background: rgba(13,25,34,.9); overflow-wrap: anywhere; line-height: 1.58; box-shadow: 0 8px 24px rgba(0,0,0,.12); }
    .message.user .bubble { background: #14303a; border-color: #285563; white-space: pre-wrap; }
    .message.pending .bubble { color: var(--muted); }
    .bubble h1, .bubble h2, .bubble h3 { margin: 0 0 9px; line-height: 1.22; }
    .bubble h1 { font-size: 1.25rem; } .bubble h2 { font-size: 1.1rem; } .bubble h3 { font-size: 1rem; }
    .bubble p { margin: 0 0 10px; } .bubble p:last-child { margin-bottom: 0; }
    .bubble ul, .bubble ol { margin: 6px 0 10px 20px; padding: 0; }
    .bubble code { padding: 2px 5px; border-radius: 5px; background: #09151c; color: var(--cyan-strong); font-size: .9em; }
    .bubble pre { position: relative; margin: 10px 0; padding: 13px; padding-top: 38px; border: 1px solid var(--line); border-radius: 10px; overflow: auto; background: #08131a; }
    .bubble pre code { padding: 0; color: #d7eeee; white-space: pre; background: transparent; }
    .bubble a { color: var(--cyan-strong); text-decoration: underline; text-underline-offset: 3px; }
    .copy-code { position: absolute; top: 7px; right: 7px; border: 1px solid var(--line-strong); border-radius: 7px; padding: 4px 7px; background: #10252c; color: #c9e7e5; font-size: 11px; }
    .composer-wrap { position: fixed; left: max(12px, calc((100vw - 1240px)/2)); right: max(342px, calc((100vw - 1240px)/2 + 342px)); bottom: 0; padding: 16px 0 20px; background: linear-gradient(transparent, var(--bg) 28%); z-index: 4; }
    .composer { width: 100%; margin: 0; border: 1px solid var(--line-strong); border-radius: 17px; padding: 10px; background: rgba(13,25,34,.96); box-shadow: 0 15px 50px rgba(0,0,0,.32); }
    textarea { width: 100%; min-height: 52px; max-height: 180px; resize: vertical; border: 0; outline: 0; padding: 9px 10px; background: transparent; color: #f8fafc; line-height: 1.45; }
    .composer-actions { display: flex; align-items: center; justify-content: space-between; gap: 10px; padding: 4px 4px 0; }
    .status { color: var(--muted); font-size: 12px; }
    .send { border: 1px solid var(--cyan); border-radius: 11px; padding: 9px 15px; background: var(--cyan); color: #092126; font-weight: 800; }
    .send:disabled { opacity: .55; cursor: wait; }
    .side-panel { position: sticky; top: 76px; margin-top: 18px; }
    details.panel { margin: 8px 0 18px; border: 1px solid var(--line); border-radius: 14px; background: rgba(13,25,34,.88); overflow: hidden; }
    details.panel > summary { cursor: pointer; padding: 12px 14px; color: #b7d0d2; font-weight: 650; }
    .panel-body { border-top: 1px solid var(--line); padding: 14px; }
    .meta { display: flex; flex-wrap: wrap; gap: 7px 14px; margin-bottom: 12px; color: var(--muted); font-size: 12px; }
    table { width: 100%; border-collapse: collapse; font-size: 12px; }
    th, td { padding: 8px 7px; text-align: left; vertical-align: top; border-bottom: 1px solid var(--line); overflow-wrap: anywhere; }
    th { color: var(--muted); }
    .empty { color: #64748b; }
    .error { color: var(--danger); }
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
    .warning { padding: 10px 12px; border: 1px solid #795b2c; border-radius: 9px; background: #2e2414; color: #f2d38c !important; }
    .proposal-card { padding: 15px; margin-top: 2px; border: 1px solid #37656d; border-radius: 14px; background: rgba(13,25,34,.9); }
    .proposal-card h2 { margin: 0 0 8px; color: var(--cyan-strong); font-size: 15px; }
    .proposal-card p { margin: 7px 0; color: #b8d0d3; font-size: 13px; line-height: 1.5; }
    .proposal-card .proposal-facts { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 8px; margin: 12px 0; }
    .proposal-card .fact { padding: 8px; border: 1px solid var(--line); border-radius: 9px; background: rgba(7,16,24,.5); }
    .proposal-card .fact small { display:block; color: var(--muted); font-size: 10px; text-transform: uppercase; letter-spacing:.08em; }
    .proposal-card .fact strong { display:block; margin-top: 3px; overflow-wrap:anywhere; font-size:12px; }
    .proposal-actions { display:flex; flex-wrap:wrap; gap:8px; margin-top:12px; }
    .proposal-actions button { width:auto; margin:0; border:1px solid var(--line-strong); border-radius:9px; padding:8px 13px; font-weight:750; }
    .proposal-actions button:first-child { background:var(--cyan); color:#092126; border-color:var(--cyan); }
    .proposal-actions button:last-child { background:transparent; color:#cde4e6; }
    .proposal-actions button:disabled { opacity:.55; cursor:wait; }
    .proposal-status { display:inline-flex; margin-top:10px; padding:4px 8px; border-radius:999px; background:#173c43; color:var(--cyan-strong); font-size:11px; font-weight:700; }
    @media (max-width: 980px) {
      header { padding: 12px; }
      .shell { display:flex; flex-direction:column; width:min(100% - 24px, 900px); }
      header { flex-wrap:wrap; }
      .header-actions { margin-left:auto; }
      #context-toggle { display:inline-block; }
      .side-panel { display:none; order:0; position:static; width:100%; margin:0; padding:0 0 10px; overflow:visible; }
      .side-panel.mobile-open { display:block; }
      .main-column { order:1; }
      .composer-wrap { left:12px; right:12px; padding-bottom:12px; }
      .composer { width:100%; margin:0 auto; }
      .brand small { display: none; }
      #new-chat { font-size:0; padding-inline:10px; }
      #new-chat::after { content:'Mới'; font-size:13px; }
      .message { max-width: 95%; }
      .context-bar { align-items: stretch; flex-direction: column; }
      .context-bar select { max-width: none; width: 100%; }
      .proposal-card .proposal-facts { grid-template-columns:1fr; }
    }
    @media (prefers-reduced-motion: reduce) { *, *::before, *::after { scroll-behavior:auto !important; transition-duration: .01ms !important; animation-duration: .01ms !important; } }
  </style>
</head>
<body>
  <header>
    <div class="brand">
      <div class="mark">L</div>
      <div><h1>Loren</h1><small>Trợ lý cá nhân · v0.1 · Loren owner console</small></div>
    </div>
    <div class="header-actions">
      <button id="context-toggle" class="ghost" type="button" aria-expanded="false" aria-controls="context-panel">Ngữ cảnh</button>
      <button id="new-chat" class="ghost" type="button" aria-label="Cuộc trò chuyện mới">Cuộc trò chuyện mới</button>
      <button id="logout" class="ghost" type="button">Đăng xuất</button>
    </div>
  </header>

  <main class="shell">
    <div class="main-column">
    <div class="context-bar">
      <span>Project đang dùng</span>
      <select id="project-context" aria-label="Project context">
        <option value="">Tự nhận diện từ cuộc trò chuyện</option>
      </select>
      <span id="project-status">Loading projects…</span>
    </div>

    <div id="chat" aria-live="polite"></div>
    </div>

    <aside class="side-panel" id="context-panel" aria-label="Ngữ cảnh và hoạt động">
    <details class="panel" id="activity-panel" open>
      <summary>Hoạt động &amp; audit lượt gần nhất</summary>
      <div class="panel-body">
        <div id="meta" class="meta"><span class="empty">Chưa có lượt chạy.</span></div>
        <table>
          <thead><tr><th>Loại</th><th>Hành động</th><th>Kết quả</th><th>Chi tiết</th></tr></thead>
            <tbody id="audit"><tr><td colspan="4" class="empty">Chưa có hoạt động tool.</td></tr></tbody>
        </table>
      </div>
    </details>

    <details class="panel advanced" id="advanced-panel">
      <summary>Thiết lập project &amp; kiểm tra an toàn</summary>
      <div class="panel-body">
        <p>Các control này dành cho thiết lập local và kiểm tra ranh giới tin cậy. Dùng Loren bình thường qua cuộc trò chuyện phía trên.</p>

        <section>
          <h2>Khởi tạo project GitHub chuẩn</h2>
          <p>Dùng một lần trên database mới. Alias hiện có không bị đổi ngầm.</p>
          <div class="grid">
            <div><label for="bootstrap-project-name">Tên project</label><input id="bootstrap-project-name" placeholder="Loren" /></div>
            <div><label for="bootstrap-project-alias">Alias project</label><input id="bootstrap-project-alias" placeholder="loren" /></div>
            <div><label for="bootstrap-github-owner">Owner GitHub</label><input id="bootstrap-github-owner" placeholder="rua-den" /></div>
            <div><label for="bootstrap-github-repository">Repository GitHub</label><input id="bootstrap-github-repository" placeholder="loren" /></div>
          </div>
          <button id="bootstrap" type="button">Lưu project chuẩn</button>
          <pre id="bootstrap-result" class="result empty">Chưa cấu hình trong phiên này.</pre>
        </section>

      </div>
    </details>
    </aside>
  </main>

  <div class="composer-wrap">
    <div class="composer">
      <textarea id="message" aria-label="Tin nhắn cho Loren" placeholder="Nhắn Loren…" autofocus></textarea>
      <div class="composer-actions">
        <span id="status" class="status">Sẵn sàng · Enter để gửi · Shift+Enter xuống dòng</span>
        <button id="send" class="send" type="button">Gửi</button>
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
    const contextToggle = document.getElementById('context-toggle');
    const contextPanel = document.getElementById('context-panel');
    const logout = document.getElementById('logout');
    const bootstrap = document.getElementById('bootstrap');
    const bootstrapResult = document.getElementById('bootstrap-result');

    let history = [];
    let projects = [];

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
            copy.type = 'button'; copy.className = 'copy-code'; copy.textContent = 'Copy';
            copy.addEventListener('click', async () => {
              try {
                if (!navigator.clipboard) throw new Error('Clipboard unavailable');
                await navigator.clipboard.writeText(code.textContent ?? '');
                copy.textContent = 'Đã copy';
              } catch { copy.textContent = 'Không copy được'; }
              setTimeout(() => { copy.textContent = 'Copy'; }, 1200);
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
      avatar.textContent = role === 'user' ? 'BẠN' : 'L';
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

    function resetConversation() {
      history = [];
      chat.replaceChildren();
      addMessage('assistant', 'Tao là Loren. Cứ hỏi bình thường; nếu câu hỏi cần project, tao sẽ dùng project mày chọn hoặc thử nhận ra từ câu nói. Tool activity nằm ở phần phụ bên dưới.');
      meta.textContent = 'Chưa có lượt chạy.';
      audit.replaceChildren();
      const emptyAudit = document.createElement('tr');
      const emptyAuditCell = document.createElement('td');
      emptyAuditCell.colSpan = 4; emptyAuditCell.className = 'empty'; emptyAuditCell.textContent = 'Chưa có hoạt động tool.';
      emptyAudit.appendChild(emptyAuditCell); audit.appendChild(emptyAudit);
      status.textContent = 'Sẵn sàng · Enter để gửi · Shift+Enter xuống dòng';
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
        projectStatus.textContent = projects.length ? `${projects.length} project đã cấu hình` : 'Chưa có project';
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
        const approve = document.createElement('button'); approve.textContent = 'Duyệt';
        const cancel = document.createElement('button'); cancel.textContent = 'Huỷ';
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
      status.textContent = 'Loren đang suy nghĩ…';
      const pending = addMessage('assistant', 'Đang suy nghĩ…', 'pending');

      try {
        const result = await postJson('/api/run', {
          message: text,
          projectAlias: selectedAlias,
          history: priorHistory
        });
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
          ? `Xong · ${result.actionCount} tool action`
          : 'Xong';
      } catch (error) {
        pending.remove();
        const errorText = error instanceof Error ? error.message : String(error);
        addMessage('assistant', `I couldn't complete that turn: ${errorText}`);
        status.textContent = 'Có lỗi';
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
    contextToggle.addEventListener('click', () => {
      const open = contextPanel.classList.toggle('mobile-open');
      contextToggle.setAttribute('aria-expanded', String(open));
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

    resetConversation();
    void loadProjects();
  </script>
</body>
</html>
""";
}
