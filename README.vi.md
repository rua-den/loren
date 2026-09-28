# Loren

[English](README.md) · **Tiếng Việt**

Loren là một **thư ký / hệ thống trí tuệ cá nhân sống lâu dài**: có memory bền vững, đọc/research thông tin hiện tại có nguồn, tổ chức Note/Decision/Task, có permission rõ ràng, và về sau mới tiến tới background/proactive/voice.

> **Model chỉ là compute có thể thay thế. Loren sở hữu identity, memory, context, organization, policy, approval, action boundary và lịch sử.**

## Hướng sản phẩm

```text
NÓI CHUYỆN
 -> NHỚ
 -> ĐỌC THÔNG TIN HIỆN TẠI
 -> RESEARCH / TỔNG HỢP
 -> TỔ CHỨC
 -> ĐỀ XUẤT HÀNH ĐỘNG
 -> OWNER APPROVE
 -> THỰC HIỆN / VERIFY / AUDIT
 -> sau đó mới BACKGROUND / PROACTIVE / VOICE
```

Read/understand phải đi trước broad mutation. GitHub automation chỉ là một capability, không phải identity của Loren.

## Trạng thái hiện tại

**Cập nhật:** 2026-09-19  
**Version:** `v0.1 — Useful Trustworthy Assistant`  
**Milestone hiện tại:** `M6B — Daily Driver Readiness`  
**Delivery hiện tại:** `M6B.2 — readiness an toàn + rebaseline tài liệu`  
**Baseline đã verify gần nhất:** PR #37 merge `ba60246a410b257097ecd537f4d977b402a37b35`; post-merge CI #280 / `35373858830` xanh Ubuntu full gate, Windows integration và Windows launcher smoke.  
**Đang pause:** broad GitHub file/commit/PR write tới khi daily-use thật chứng minh đó là thứ đáng làm nhất.

Bắt đầu ở [`docs/status.md`](docs/status.md) rồi [`docs/handoff.md`](docs/handoff.md). Agent AI đọc thêm [`docs/ai-start.md`](docs/ai-start.md).

## Những gì đã chạy thật

- UI conversation-first có owner auth;
- conversation + project scope persist qua restart;
- canonical Project/Repository identity do Loren sở hữu;
- trusted durable memory có correction/forget/provenance boundary;
- GitHub repository read thật;
- web search/fetch read-only + research nhiều nguồn có bounds;
- Note / Decision / Task bền vững;
- conversational proposal + Cancel/Approve cho một external action;
- one-time approval, read-only kill, credential isolation/revocation, post-write verification;
- proof tạo non-default GitHub branch từ exact frozen SHA;
- retained audit trong SQLite + current-run audit scoped theo request;
- logical export/restore có version;
- Windows launcher + CI cross-platform.

## Daily-driver readiness

Public:

```text
GET /health
```

chỉ là **liveness**.

Sau khi login owner:

```text
GET /api/readiness
```

trả về status an toàn cho storage, owner auth, brain config, web research, project catalog/count và external-write posture.

Semantics quan trọng:

- readiness không ping provider/network bên ngoài;
- không trả về secret value;
- `externalWrites=disabled` là posture an toàn bình thường và vẫn có thể overall `ready`;
- `projects=empty` chỉ nghĩa là chưa bootstrap canonical project;
- nếu bật writes mà credential missing/revoked/not-configured thì overall là `needs_setup`.

Real provider có reachable và Loren có dùng ngon hay không phải được chứng minh bằng [`docs/owner-checkpoint.md`](docs/owner-checkpoint.md), không phải bằng readiness endpoint.

## Chạy local

Copy:

```text
src/Loren.Web/appsettings.Local.example.json
```

thành file ignored `src/Loren.Web/appsettings.Local.json`, hoặc dùng environment:

```powershell
$env:LOREN_OWNER_PASSWORD='choose-a-local-owner-password'
$env:OLLAMA_API_KEY='your-provider-secret'
$env:LOREN_ENABLE_WRITES='false'
dotnet run --project src/Loren.Web/Loren.Web.csproj
```

Environment/command line override local JSON. Đổi config thì restart host.

Normal daily use nên giữ external writes OFF. Chỉ khi làm owner live branch proof mới cấu hình riêng `GITHUB_WRITE_TOKEN`, bật writes có chủ đích, proof xong thì tắt lại. Không commit secret thật.

## Provider hiện tại

Core có contract `IBrain` provider-neutral, nhưng production DI hiện đang dùng `OllamaBrain`. `Loren.Brain.OpenAI` mới chỉ là stub. Vì vậy Loren **chưa được chứng minh multi-provider trong runtime thật**.

Hướng v0.2 hợp lý:

```text
prove brain provider thứ hai
 -> rồi làm một personal-secretary integration read-only thật
 -> candidate mạnh: Calendar read/search
```

Làm vertical slice thật trước rồi mới rút ra generic connector abstraction cần thiết.

## Trust boundary

- chat/model text là intent, không phải external-write approval;
- external/retrieved content là evidence, không phải authority;
- owner auth không phải write approval;
- external writes default OFF;
- consequential write cần exact one-time owner approval;
- credential không đi vào model context/readiness output;
- write chỉ thành công sau independent verification;
- background/reminder execution cần Gate E.

## Test

```powershell
dotnet restore Loren.slnx
dotnet build Loren.slnx --configuration Release --no-restore
dotnet test Loren.slnx --configuration Release --no-build --no-restore
dotnet format Loren.slnx --verify-no-changes --no-restore
```

CI chạy Ubuntu full gate + Windows integration + Windows launcher smoke.

## Lộ trình version

```text
v0.0  architecture / feasibility             ✓ hoàn tất
v0.1  useful trustworthy assistant           <- hiện tại
v0.2  provider portability + secretary reads
v0.3  personal/project operations
v0.4  voice + device presence
v0.5  proactive/background Loren
v0.6+ daily-use hardening
v1.0  stable personal daily driver
```

## Tài liệu

- [`docs/status.md`](docs/status.md) — tiến độ authoritative
- [`docs/handoff.md`](docs/handoff.md) — checkpoint mở thread mới
- [`docs/ai-start.md`](docs/ai-start.md) — entrypoint cho AI contributor
- [`docs/owner-checkpoint.md`](docs/owner-checkpoint.md) — real-provider acceptance
- [`docs/roadmap.md`](docs/roadmap.md) — version/capability path
- [`docs/plans/master-plan.md`](docs/plans/master-plan.md) — master plan
- [`docs/plans/v0.1.md`](docs/plans/v0.1.md) — release plan hiện tại
- [`docs/architecture.md`](docs/architecture.md) — system boundaries
- [`docs/recovery.md`](docs/recovery.md) — logical export/restore
- [`docs/memory.md`](docs/memory.md) — memory semantics
- [`docs/permissions.md`](docs/permissions.md) — approval/permission
- [`docs/security.md`](docs/security.md) — security baseline
- [`docs/development.md`](docs/development.md) — build/config

Repository này là source of truth cho product decision, architecture, implementation và delivery history của Loren.
