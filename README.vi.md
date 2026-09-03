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
**Milestone hiện tại:** `M1 — Engineering foundation`

Hai architecture gate cần để bắt đầu production đều đã PASS:

- **Gate A / ADR-001:** Loren sở hữu canonical identity/state/policy/action authorization.
- **Gate B / ADR-002:** stack v0.1 provider-neutral đã được accept và M0 đã hoàn tất.

Trusted M0 run #70 đã chứng minh complete brain path bằng Ollama Cloud (`gpt-oss:120b`):

```text
real model
 -> get_project_status ActionRequest
 -> Loren ActionGateway
 -> structured ActionResult
 -> real model final answer
 -> PASS
```

Cùng run đó đã PASS live provider cancellation, MCP, SQLite/EF recovery và ASP.NET/Blazor regressions. Provider secrets chỉ xuất hiện dưới dạng masked `***`.

Ollama là **provider đầu tiên đóng được brain proof**, không phải identity hay provider vĩnh viễn của Loren. OpenAI vẫn là optional adapter; API account hiện tại đang bị provider credit chặn, nhưng chuyện đó không còn chặn architecture của Loren.

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
xUnit
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

## Công việc hiện tại — M1

M1 dựng engineering foundation cho production:

- production .NET solution/project boundaries;
- pin SDK/packages;
- deterministic xUnit tests;
- CI restore/build/test/static checks;
- nullable/warnings/analyzers/formatting policy;
- `.env.example` và local setup docs;
- secret/dependency scanning;
- startup/health test;
- `Loren.Core` không phụ thuộc Ollama/OpenAI/MCP/EF/Blazor.

Thư mục `spikes/` tiếp tục là technical evidence, không phải production architecture.

## Mốc đầu tiên mày có thể test Loren như user

Preview đầu tiên vẫn là **v0.1 M2 — Walking Skeleton**:

```text
"Loren, check repo rua-den/loren."

UI
 -> Loren Runtime
 -> configured IBrain
 -> github.read_repository ActionRequest
 -> Action Gateway
 -> GitHub read executor
 -> structured result
 -> IBrain final response
 -> Audit
```

M1 là engineering foundation. M2 là milestone đầu tiên được thiết kế để có cảm giác đang thực sự dùng Loren.

## Lộ trình version

```text
v0.0  architecture / feasibility        ✓ hoàn tất
v0.1  trustworthy core                 <- đang development
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
