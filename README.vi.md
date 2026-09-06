# Loren

[English](README.md) · **Tiếng Việt**

Loren là một **thư ký / hệ thống trí tuệ cá nhân sống lâu dài**, có memory bền vững, biết lấy thông tin hiện tại qua tool, có permission rõ ràng, và về sau có thể có voice + proactive behavior xuyên suốt đời sống số của owner.

> **Model chỉ là compute có thể thay thế. Loren sở hữu identity, memory, context, organization, policy, approval, action boundary và lịch sử.**

## Hướng sản phẩm

Loren phải giống một thư ký/Jarvis riêng của owner hơn là một bot automation cho GitHub.

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

1. **Conversation-first** — giao tiếp tự nhiên với owner là bề mặt sản phẩm chính.
2. **Memory-first** — state bền vững sống qua conversation, restart và đổi provider.
3. **Tool-first cho external facts** — thông tin hiện tại phải lấy từ read tool thay vì model đoán.
4. **Read before write** — integration phải chứng minh hữu ích ở read-only trước khi mở rộng mutation.
5. **Permission-first** — model có thể request action; Loren mới authorize và execute.
6. **Model-independent** — model provider là adapter có thể thay thế.
7. **Auditable** — hành vi quan trọng phải reconstruct được.
8. **Tự chủ tăng dần** — scheduler/voice/proactive chỉ đến sau khi trust boundary thấp hơn đã được chứng minh.

## Trạng thái hiện tại

**Cập nhật:** 2026-09-06  
**Phase:** `v0.1 — Useful Trustworthy Assistant`  
**Đã hoàn tất:** `M1–M4`, `Gate D`, `M5 write-safety Slices 1–3`, `M6A.1 conversation primary surface`  
**Đang làm:** `M6A.2 — Current-information / web read`  
**Đang pause:** `M5 file/commit/PR write expansion` cho tới khi owner interaction checkpoint dùng được

Chi tiết chuẩn: [`docs/status.md`](docs/status.md). Checkpoint thread mới: [`docs/handoff.md`](docs/handoff.md).

## Những gì đã chứng minh

### Conversation-first surface — M6A.1

PR #29 đưa Loren trở lại đúng vai trò sản phẩm:

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

Bootstrap và create-branch proof form đã nằm dưới **Advanced / safety harness**.

Evidence:

```text
PR #29 merge a1652b2451fe2e706aa83373932b210178f63ebe
PR CI #224 / 34042192552 PASS Ubuntu + Windows
post-merge CI #225 / 34042352724 PASS Ubuntu + Windows
```

### Canonical context + durable memory

M3 cho Loren-owned Project/Repository IDs + aliases, không phụ thuộc provider/session identity. M4 chứng minh owner memory sống qua restart, hỗ trợ correction/supersession/forget, có provenance và chống model/external content tự nâng thành owner truth.

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

Real write proof đầu tiên là tạo **non-default GitHub branch** rồi verify exact SHA. Capability này vẫn giữ nhưng mở rộng GitHub write đang pause.

## Execution hiện tại — M6A

### M6A.1 — Conversation là primary surface [COMPLETE]

- conversation là surface mặc định;
- chọn project bằng tên/alias hoặc infer deterministic;
- bounded user/assistant history;
- trusted project memory tham gia normal chat;
- browser history không inject được system role;
- tool/audit activity là secondary.

### M6A.2 — Current-information / web read [ACTIVE — PR #30]

Thêm read-only `web.search` dùng Ollama Web Search và chính `OLLAMA_API_KEY` hiện có.

```text
câu hỏi current
 -> brain chọn web.search
 -> ActionGateway READ policy
 -> bounded Ollama web search
 -> validate source URL + bounded evidence
 -> external evidence được đánh dấu untrusted
 -> Loren tổng hợp câu trả lời có nguồn
```

Implementation chặn unsafe/overlong URL, giới hạn query/result/content, fail trước external call khi thiếu credential và không surface provider error body hay secret.

### M6A.3 — Source-aware research [NEXT]

Nhiều search/source, fetch page sâu hơn khi cần, compare/dedupe, stale/conflict handling và phân biệt sourced fact với Loren inference.

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

Reuse create-branch executor đã an toàn nhưng qua UX đúng:

```text
Owner: "Tạo branch abc cho Loren."
 -> Loren resolve exact target
 -> conversation hiện exact proposal
 -> owner approve
 -> Gate D boundary execute
 -> branch được verify độc lập
 -> Loren báo tự nhiên
```

Checkpoint này không cần thêm GitHub mutation primitive mới.

## Mốc owner test v0.1

Lần pull để test sản phẩm sẽ là khi Loren làm được:

```text
1. Chat bình thường.
2. Trả lời knowledge/reasoning ổn định.
3. Lấy current information kèm nguồn.
4. Research nhiều nguồn có bounds.
5. Kết hợp project context + memory + live read.
6. Ghi/nhớ fact hoặc decision qua restart.
7. Tạo/list/complete task qua chat.
8. Nhận yêu cầu tạo branch bằng ngôn ngữ tự nhiên.
9. Hiện exact approval rồi execute + verify sau khi owner approve.
10. Giải thích chuyện đã xảy ra kèm audit context.
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

`OLLAMA_API_KEY` dùng cho cả Ollama brain cloud endpoint và current-information web search. `LOREN_OLLAMA_WEB_SEARCH_ENDPOINT` là optional, mặc định `https://ollama.com/api/web_search`.

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
