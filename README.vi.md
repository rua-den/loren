# Loren

[English](README.md) · **Tiếng Việt**

Loren là một **thư ký / hệ thống trí tuệ cá nhân sống lâu dài**, có memory bền vững, đọc được thông tin hiện tại, research có nguồn, tổ chức Note/Decision/Task, có permission rõ ràng, và về sau mới tiến tới voice + proactive behavior.

> **Model chỉ là compute có thể thay thế. Loren sở hữu identity, memory, context, organization, policy, approval, action boundary và lịch sử.**

## Hướng sản phẩm

Loren phải giống một thư ký/Jarvis riêng của owner hơn là bot automation cho GitHub.

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

**Read/understand phải đi trước broad write/automation.**

## Nguyên tắc cốt lõi

1. **Conversation-first** — giao tiếp tự nhiên với owner là surface chính.
2. **Memory-first** — state bền vững sống qua conversation, restart và đổi provider.
3. **Tool-first cho current facts** — thông tin hiện tại phải lấy từ read tool thay vì model đoán từ kiến thức cũ.
4. **Read before write** — integration phải chứng minh hữu ích ở read-only trước khi mở rộng mutation.
5. **Permission-first** — model có thể request action; Loren mới authorize và execute.
6. **Owner-state khác external write** — Note/Task local cần authenticated owner context nhưng không đốt external-write approval/token.
7. **Model-independent** — brain/search/tool provider là adapter có thể thay thế.
8. **Auditable** — hành vi quan trọng phải reconstruct được.
9. **Tự chủ tăng dần** — scheduler/voice/proactive chỉ đến sau khi trust boundary thấp hơn đã chứng minh.

## Trạng thái hiện tại

**Cập nhật:** 2026-09-14  
**Phase:** `v0.1 — Useful Trustworthy Assistant`  
**Baseline đã hoàn tất:** `M1–M4`, `Gate D`, `M5 write-safety Slices 1–3`, implementation `M6A.1–M6A.5`; live proof của owner cho M6A.5 vẫn đang pending.  
**Branch đang làm:** `codex/conversation-continuity` — draft [PR #36](https://github.com/rua-den/loren/pull/36), gồm persistent conversation, Windows launcher, logical recovery và retained-audit hardening.  
**Verification hiện tại:** batch fix từ source review ngày 2026-09-14 đã được chuẩn bị; exact-head build/test/format/CI vẫn phải chạy trước khi merge.  
**Đang pause:** `M5 file/commit/PR write expansion` tới khi v0.1 owner checkpoint dùng được.

Chi tiết chuẩn: [`docs/status.md`](docs/status.md). Handoff: [`docs/handoff.md`](docs/handoff.md). Recovery: [`docs/recovery.md`](docs/recovery.md).

## Những gì đã chứng minh

### Conversation-first — M6A.1

```text
owner login
 -> conversation-first UI
 -> Loren identity
 -> bounded multi-turn history
 -> optional/inferred canonical project context
 -> trusted durable memory
 -> read tools
 -> câu trả lời tự nhiên
 -> activity/audit nằm phụ
```

PR #29 merge `a1652b2451fe2e706aa83373932b210178f63ebe`; exact-head CI #224 và main CI #225 xanh Ubuntu + Windows.

### Current information — M6A.2

Read-only `web.search` chạy qua ActionGateway bình thường, dùng `OLLAMA_API_KEY` hiện có. Search evidence được bound, URL nguồn được validate, provider failure body/secret không bị surface, và câu trả lời current có thể grounded vào nguồn trả về.

PR #30 merge `a8d3e7bbc94c9a468ebc234deb1fe87dcb7d23e9`; exact-head CI #237 và main CI #238 xanh Ubuntu + Windows.

### Source-aware research — M6A.3

```text
web.search
 -> chọn source
 -> web.fetch
 -> bounded page evidence
 -> compare / synthesize trong bounded AgentLoop
 -> sourced facts + Loren inference rõ ràng
```

`PublicWebUrlPolicy` chặn unsafe scheme, URL credential, localhost/private literal address và non-standard port. External evidence luôn là inert data.

PR #31 merge `d789ccc7f7540cb802b14f677d317db3e571a7d3`; exact-head CI #240 / `34045079047` và main CI #241 / `34052513007` xanh Ubuntu + Windows.

### Canonical context + durable memory

M3 cho Loren-owned Project/Repository ID + alias, không phụ thuộc provider/session identity. M4 chứng minh owner memory sống qua restart, hỗ trợ correction/supersession/forget, giữ provenance và chống model/external content tự nâng thành owner truth.

### Safe external action boundary

Gate D + M5 Slices 1–3 chứng minh:

```text
canonical target
 -> typed policy / read-only kill
 -> explicit exact owner approval
 -> atomic one-time consume
 -> dedicated write credential
 -> trusted executor
 -> post-write verification
 -> redacted audit
```

Real write proof đầu tiên là tạo **non-default GitHub branch** rồi verify exact SHA. Broad GitHub write vẫn đang pause.

## Execution hiện tại — continuity và local readiness

M6A.5 conversational approval đã merge qua [PR #33](https://github.com/rua-den/loren/pull/33). Batch kỹ thuật đang active là PR #36: conversation continuity bền vững, Windows startup và versioned recovery.

Recovery export logical owner state thay vì copy raw runtime config. Restore luôn vào directory mới, áp dụng checked-in migrations, giữ canonical IDs, revoke approval khi restore, cancel pending proposal và reject malformed domain/history state. Credential và provider config không được export. Xem [`docs/recovery.md`](docs/recovery.md).

Không có background scheduler/reminder delivery trong batch này; Gate E vẫn bắt buộc trước background execution.

## Tiếp theo — verify exact HEAD rồi mới owner acceptance

Trước khi PR #36 được merge, exact HEAD hiện tại phải qua restore/build/tests/format/dependency checks, Windows launcher smoke khi có môi trường phù hợp, và CI Ubuntu + Windows. CI là final verification gate, không phải development loop.

Sau khi continuity batch merge, owner checkpoint vẫn cần real-provider conversational approval proof:

```text
Owner: "Tạo branch abc cho Loren từ main."
 -> Loren resolve canonical target + exact source SHA
 -> conversation hiện exact proposal + risk
 -> owner bấm Approve rõ ràng
 -> Loren tạo exact one-time approval
 -> credential-bound executor hiện có chạy
 -> branch state được verify độc lập
 -> Loren báo kết quả tự nhiên + audit context
```

Tin nhắn chat chỉ là **intent**, không phải Gate D approval. Không cần thêm GitHub mutation primitive mới cho checkpoint này.

## Mốc owner test v0.1

Chỉ kêu owner pull để test sản phẩm khi Loren làm đủ flow tự nhiên này:

```text
1. Chat bình thường.
2. Trả lời knowledge/reasoning ổn định.
3. Lấy current information kèm nguồn.
4. Research nhiều nguồn có bounds.
5. Kết hợp project context + memory + live read.
6. Lưu/retrieve Note hoặc Decision bền vững qua restart.
7. Tạo/list/complete Task qua chat.
8. Nhận yêu cầu tạo branch bằng ngôn ngữ tự nhiên.
9. Hiện exact proposal, owner approve rõ ràng rồi mới execute + verify.
10. Giải thích chuyện đã xảy ra kèm audit context.
```

**Controlled file/commit và open-PR tiếp tục pause tới khi checkpoint này tồn tại.**

## Chạy local

Posture mặc định chặn external write:

```powershell
$env:LOREN_OWNER_PASSWORD='choose-a-local-owner-password'
$env:OLLAMA_API_KEY='your-provider-secret'
$env:LOREN_ENABLE_WRITES='false'
dotnet run --project src/Loren.Web/Loren.Web.csproj
```

`OLLAMA_API_KEY` dùng cho Ollama brain + web search/fetch read path. Có thể override endpoint trusted qua:

```text
LOREN_OLLAMA_WEB_SEARCH_ENDPOINT
LOREN_OLLAMA_WEB_FETCH_ENDPOINT
```

`LOREN_ENABLE_WRITES=false` chặn external mutation, **không** tắt authenticated local Notes / Decisions / Tasks.

Không commit secret thật.

## Test

```powershell
dotnet restore Loren.slnx
dotnet build Loren.slnx --configuration Release --no-restore
dotnet test Loren.slnx --configuration Release --no-build --no-restore
dotnet format Loren.slnx --verify-no-changes --no-restore
```

Windows là first-class integration-test CI platform bên cạnh Ubuntu full gate.

## Lộ trình version

```text
v0.0  architecture / feasibility             ✓ hoàn tất
v0.1  useful trustworthy assistant           <- hiện tại
v0.2  personal secretary integrations
v0.3  personal/project operations
v0.4  voice + device presence
v0.5  proactive/background Loren
v0.6+ daily-use hardening
v1.0  stable personal daily driver
```

## Tài liệu

- [`docs/status.md`](docs/status.md) — tiến độ chuẩn hiện tại
- [`docs/handoff.md`](docs/handoff.md) — checkpoint ngắn để mở thread mới
- [`docs/recovery.md`](docs/recovery.md) — runbook logical export/restore + security semantics
- [`docs/plans/master-plan.md`](docs/plans/master-plan.md) — product/version roadmap
- [`docs/plans/v0.1.md`](docs/plans/v0.1.md) — plan chi tiết version hiện tại
- [`docs/architecture.md`](docs/architecture.md) — system boundaries
- [`docs/memory.md`](docs/memory.md) — durable memory semantics
- [`docs/permissions.md`](docs/permissions.md) — permission/approval baseline
- [`docs/security.md`](docs/security.md) — security baseline
- [`docs/development.md`](docs/development.md) — build/test/configuration

Repository này là source of truth cho product decisions, architecture, delivery plan, implementation, progress và release history của Loren.
