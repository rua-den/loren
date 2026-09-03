# Loren

[English](README.md) · **Tiếng Việt**

Loren là một hệ thống trí tuệ cá nhân sống lâu dài, có memory bền vững, permission rõ ràng, khả năng dùng tool và về sau có thể chủ động hỗ trợ xuyên suốt đời sống số của chủ sở hữu.

> **Model chỉ là compute có thể thay thế. Loren sở hữu identity, memory, context, policy, action boundary và lịch sử.**

## Nguyên tắc cốt lõi

1. **Memory-first** — state bền vững phải sống qua conversation, restart và đổi provider.
2. **Tool-first** — dữ liệu/action ngoài hệ thống phải đi qua tool có thẩm quyền thay vì để model đoán.
3. **Permission-first** — model có thể request action; Loren mới authorize và execute.
4. **Model-independent** — Ollama, OpenAI, Claude, local model và provider tương lai đều chỉ là adapter.
5. **Auditable** — action quan trọng và state mutation phải reconstruct được.
6. **Tự chủ tăng dần** — background/proactive behavior chỉ đến sau khi trust boundary thấp hơn đã được chứng minh.

## Trạng thái hiện tại

**Cập nhật:** 2026-09-03  
**Phase:** `v0.1 — Trustworthy Core development`  
**Milestone hiện tại:** `M2 — Walking Skeleton`

Các gate/foundation cần để bắt đầu làm product thật đã hoàn tất:

- **Gate A / ADR-001:** Loren sở hữu canonical identity/state/policy/action authorization.
- **Gate B / ADR-002:** stack v0.1 provider-neutral đã được accept và M0 đã hoàn tất.
- **M1:** production solution, provider-neutral contracts, bounded runtime loop, deterministic tests, CI/static checks, secret/dependency checks và health smoke test đều đã hoàn tất.

M2 giờ là vertical slice đầu tiên mà owner có thể test Loren theo flow thật.

Chi tiết chuẩn: [`docs/status.md`](docs/status.md).

## Stack v0.1 đã accept

```text
C# 14 / .NET 10 LTS
ASP.NET Core
small Loren-owned bounded agent loop
provider-neutral IBrain
  ├─ Ollama adapter
  ├─ OpenAI adapter
  └─ future providers/local models
MCP C# SDK sau Loren action contracts
SQLite + EF Core
Blazor Web App
xUnit / Microsoft Testing Platform
```

## Kiến trúc brain

```text
                Loren Core
                    │
                  IBrain
                    │
        ┌───────────┼───────────┐
        │           │           │
     Ollama       OpenAI      future
        │           │           │
        └──── ActionRequest ─────┘
                    │
             Loren ActionGateway
                    │
            Policy / Executor / Audit
```

Đổi provider không được kéo theo migrate identity, memory, permission, project hay audit history của Loren.

## Proof M0

Trusted M0 run #70 đã chứng minh complete real-brain path bằng Ollama Cloud (`gpt-oss:120b`):

```text
real model
 -> get_project_status ActionRequest
 -> Loren ActionGateway
 -> structured ActionResult
 -> real model final answer
 -> PASS
```

Cùng run đó đã PASS live provider cancellation, MCP, SQLite/EF recovery và ASP.NET/Blazor regressions. Provider secrets chỉ xuất hiện dưới dạng masked `***`.

Ollama chỉ là provider đầu tiên đóng được brain proof, không phải identity/provider vĩnh viễn của Loren. OpenAI vẫn là optional adapter.

## M1 engineering foundation — hoàn tất

Production code hiện bắt đầu với boundaries cố ý gọn:

```text
src/
├── Loren.Core/
├── Loren.Runtime/
├── Loren.Brain.Ollama/
├── Loren.Brain.OpenAI/
├── Loren.Infrastructure/
└── Loren.Web/

tests/
├── Loren.Core.Tests/
└── Loren.Runtime.Tests/
```

M1 đã dựng:

- .NET SDK `10.0.400`, `net10.0`, C# 14;
- central package versions;
- nullable + warnings-as-errors + formatting policy;
- provider-neutral `IBrain` và action contracts trong `Loren.Core`;
- bounded/cancellable `AgentLoop` trong `Loren.Runtime`;
- deterministic xUnit/Microsoft Testing Platform tests;
- CI restore/build/test/format;
- basic secret scan và dependency vulnerability check;
- `/health` startup smoke test;
- `.env.example` và [`docs/development.md`](docs/development.md).

`Loren.Core` không phụ thuộc provider/MCP/EF Core/ASP.NET Core/Blazor package. Thư mục `spikes/` vẫn chỉ là technical evidence, không phải production architecture.

## Công việc hiện tại — M2 Walking Skeleton

M2 là milestone đầu tiên được thiết kế để có cảm giác đang dùng Loren thật:

```text
"Loren, check repo rua-den/loren."

minimal UI
 -> Loren Runtime
 -> configured IBrain
 -> github.read_repository ActionRequest
 -> Loren ActionGateway
 -> GitHub read executor
 -> structured ActionResult
 -> IBrain final response
 -> Audit
```

M2 sẽ thêm đúng các mảnh production cần cho flow này: one-owner auth/session, production brain adapter + fake brain, read-only GitHub executor, correlation IDs và minimal audit. Chưa mở GitHub write path ở M2.

## Lộ trình version

```text
v0.0  architecture / feasibility        ✓ hoàn tất
v0.1  trustworthy core                 <- đang development / M2
v0.2  useful project assistant
v0.3  personal operations
v0.4  voice + device presence
v0.5  proactive/background Loren
v0.6+ hardening từ sử dụng thực tế
v1.0  stable personal daily driver
```

Version chỉ được nâng khi vượt exit gate, không dựa vào ngày tháng hay số lượng code.

## Kỷ luật cập nhật tiến độ

Bất kỳ merge nào làm thay đổi capability, milestone completion, ADR status, provider/dependency đã validate hoặc next execution target đều phải cập nhật:

- [`docs/status.md`](docs/status.md)
- [`README.md`](README.md)
- `README.vi.md`
- ADR/plan liên quan khi cần

Một milestone chưa đóng nếu code/tests và documentation trong repo chưa nói cùng một trạng thái.

## Tài liệu

- [`docs/status.md`](docs/status.md) — tiến độ hiện tại chuẩn
- [`docs/development.md`](docs/development.md) — hướng dẫn build/test/config/dependency
- [`docs/vision.md`](docs/vision.md) — product vision
- [`docs/architecture.md`](docs/architecture.md) — system boundaries
- [`docs/plans/master-plan.md`](docs/plans/master-plan.md) — milestones/version gates
- [`docs/plans/v0.1.md`](docs/plans/v0.1.md) — plan implementation chi tiết v0.1
- [`docs/decisions/001-agent-runtime-strategy.md`](docs/decisions/001-agent-runtime-strategy.md) — Loren-owned core/runtime boundary đã accept
- [`docs/decisions/002-v0.1-technology-stack.md`](docs/decisions/002-v0.1-technology-stack.md) — stack v0.1 provider-neutral đã accept và evidence M0
- [`docs/memory.md`](docs/memory.md) — memory model
- [`docs/permissions.md`](docs/permissions.md) — permission model
- [`docs/security.md`](docs/security.md) — security baseline
- [`docs/skills.md`](docs/skills.md) — skill/tool model

## Vai trò của repository

Repository này là source of truth cho product decisions, architecture, delivery plan, implementation, progress và release history của Loren.
