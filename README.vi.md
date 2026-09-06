# Loren

[English](README.md) · **Tiếng Việt**

Loren là một **thư ký / hệ thống trí tuệ cá nhân sống lâu dài**, có memory bền vững, biết lấy thông tin hiện tại qua tool, có permission rõ ràng, và về sau có thể có voice + proactive behavior xuyên suốt đời sống số của owner.

> **Model chỉ là compute có thể thay thế. Loren sở hữu identity, memory, context, organization, policy, approval, action boundary và lịch sử.**

## Hướng sản phẩm

Loren phải giống một thư ký/Jarvis riêng của owner hơn là một con bot automation cho GitHub.

Thứ tự capability mặc định:

```text
NÓI CHUYỆN
 -> NHỚ
 -> ĐỌC THÔNG TIN HIỆN TẠI
 -> RESEARCH / TỔNG HỢP
 -> TỔ CHỨC CÔNG VIỆC / THÔNG TIN
 -> ĐỀ XUẤT HÀNH ĐỘNG
 -> OWNER APPROVE
 -> THỰC HIỆN / VERIFY / AUDIT
 -> sau đó mới BACKGROUND / PROACTIVE / VOICE
```

**Read/understand phải đi trước broad write/automation.**

## Nguyên tắc cốt lõi

1. **Conversation-first** — giao tiếp tự nhiên với owner là bề mặt sản phẩm chính.
2. **Memory-first** — state bền vững sống qua conversation, restart và đổi provider.
3. **Tool-first cho external facts** — thông tin hiện tại phải lấy từ read tool có thẩm quyền thay vì model đoán.
4. **Read before write** — integration phải chứng minh hữu ích ở read-only trước khi mở rộng mutation.
5. **Permission-first** — model có thể request action; Loren mới authorize và execute.
6. **Model-independent** — model provider là adapter có thể thay thế.
7. **Auditable** — hành vi quan trọng phải reconstruct được.
8. **Tự chủ tăng dần** — scheduler/voice/proactive chỉ đến sau khi trust boundary thấp hơn đã được chứng minh.

## Trạng thái hiện tại

**Cập nhật:** 2026-09-06  
**Phase:** `v0.1 — Useful Trustworthy Assistant`  
**Foundations đã hoàn tất:** `M1–M4`, `Gate D`, `M5 write-safety Slices 1–3`  
**Product target hiện tại:** `M6A — Conversational Secretary + Information Layer`  
**Đang pause:** `M5 file/commit/PR write expansion` cho tới khi owner interaction checkpoint dùng được

Chi tiết chuẩn: [`docs/status.md`](docs/status.md). Checkpoint thread mới: [`docs/handoff.md`](docs/handoff.md).

## Những gì đã chứng minh được

### Conversation/tool loop

M2 đã chứng minh flow production thật:

```text
owner
 -> Loren conversation
 -> real brain provider
 -> github.read_repository
 -> Loren ActionGateway
 -> real GitHub read
 -> structured result
 -> câu trả lời ngôn ngữ tự nhiên
 -> correlated audit
```

### Canonical context

M3 cho Loren-owned Project/Repository IDs + aliases, không phụ thuộc provider/session identity.

### Durable memory

M4 chứng minh owner memory sống qua restart, hỗ trợ correction/supersession/forget, có provenance và chống model/external content tự nâng thành owner truth.

### Safe action boundary

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

Real write proof đầu tiên là tạo **non-default GitHub branch** rồi verify exact SHA. Capability này vẫn giữ trong code, nhưng **không còn là product priority tiếp theo**.

Evidence:

```text
PR #25 merge caa65fbbd7c3828b68aa198dad625e73e9c096b4
post-merge CI #195 / 33973694524 PASS Ubuntu + Windows

PR #26 merge f7fb36bae324dbd7bb8d12e02daf3fe0dd98e7da
post-merge CI #202 / 34027255592 PASS Ubuntu + Windows

PR #27 merge bd0220550592a3ba55a2c722192e43df6e8ca321
PR CI #217 / 34029409983 PASS Ubuntu + Windows
post-merge CI #218 / 34029500883 PASS Ubuntu + Windows
```

## Execution hiện tại — M6A

### M6A.1 — Conversation là primary surface [NEXT]

Loren phải có cảm giác là thư ký chứ không phải admin dashboard:

- login xong conversation là surface chính;
- hỏi kiến thức/reasoning ổn định thì trả lời tự nhiên;
- trusted memory + project context tham gia normal chat;
- owner không phải nhập low-level canonical ID khi dùng bình thường;
- bootstrap/debug controls chuyển vào secondary admin/settings UI;
- tool/audit activity vẫn nhìn được nhưng là secondary.

### M6A.2 — Current-information / web read

Thêm provider-neutral read-only search/retrieval để Loren trả lời được các câu mà dữ liệu có thể đã thay đổi sau thời điểm model được train.

Yêu cầu:

- source URL/title/time/provider metadata;
- bounded retrieved content;
- external page được xem là untrusted data;
- deterministic fake-provider tests;
- khi tool fail thì báo uncertainty, không bịa current fact.

### M6A.3 — Source-aware research

Multi-source retrieval, compare, dedupe, xử lý stale/conflict và phân biệt sourced fact với Loren inference.

### M6A.4 — Notes / Decisions / Tasks

Durable Loren-owned organization primitives dùng trực tiếp qua conversation:

```text
Note
Decision
Task
TaskStatus
optional Project scope
provenance/timestamps
```

Scheduled/background reminders chờ Gate E.

### M6A.5 — Conversational approval

Reuse create-branch executor đã an toàn nhưng đưa qua UX đúng:

```text
Owner: "Tạo branch abc cho Loren."
 -> brain propose typed action
 -> Loren resolve exact canonical target
 -> conversation/UI hiện approval proposal
 -> owner approve
 -> Gate D boundary hiện có execute
 -> branch được verify độc lập
 -> Loren báo lại bằng ngôn ngữ tự nhiên
```

Checkpoint này **không cần thêm write primitive mới**.

## Mốc owner test tiếp theo

Lần pull/test có ý nghĩa tiếp theo phải là:

```text
1. Chat bình thường với Loren.
2. Hỏi một câu kiến thức ổn định.
3. Hỏi một câu current info và thấy Loren retrieve external data + source.
4. Hỏi về project đã biết; Loren dùng canonical context + memory + live read.
5. Dạy Loren một fact/decision; restart; hỏi lại vẫn nhớ.
6. Tạo/list/complete task qua chat.
7. Yêu cầu tạo branch bằng ngôn ngữ tự nhiên.
8. Review exact approval proposal và approve.
9. Nhận verified completion tự nhiên.
10. Hỏi tại sao Loren làm việc đó và xem explanation/audit.
```

**Controlled file/commit và open-PR tiếp tục pause cho tới khi checkpoint này tồn tại.**

## Chạy local

Read-only development posture:

```powershell
$env:LOREN_OWNER_PASSWORD='choose-a-local-owner-password'
$env:OLLAMA_API_KEY='your-provider-secret'
$env:LOREN_ENABLE_WRITES='false'
dotnet run --project src/Loren.Web/Loren.Web.csproj
```

Không commit secret thật.

## Test

```powershell
dotnet restore Loren.slnx
dotnet build Loren.slnx --configuration Release --no-restore
dotnet test Loren.slnx --configuration Release --no-build --no-restore
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
- [`docs/plans/master-plan.md`](docs/plans/master-plan.md) — product/version roadmap
- [`docs/plans/v0.1.md`](docs/plans/v0.1.md) — plan chi tiết version hiện tại
- [`docs/architecture.md`](docs/architecture.md) — system boundaries
- [`docs/memory.md`](docs/memory.md) — durable memory semantics
- [`docs/permissions.md`](docs/permissions.md) — permission/approval baseline
- [`docs/security.md`](docs/security.md) — security baseline
- [`docs/development.md`](docs/development.md) — build/test/configuration

Repository này là source of truth cho product decisions, architecture, delivery plan, implementation, progress và release history của Loren.
