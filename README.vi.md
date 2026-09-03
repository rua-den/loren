# Loren

[English](README.md) · **Tiếng Việt**

Loren là một hệ thống trí tuệ cá nhân sống lâu dài, có memory bền vững, permission rõ ràng, khả năng dùng tool và về sau có thể chủ động hỗ trợ xuyên suốt đời sống số của chủ sở hữu.

> **Model chỉ là compute có thể thay thế. Loren sở hữu identity, memory, context, policy, action boundary và lịch sử.**

## Nguyên tắc cốt lõi

1. **Memory-first** — state bền vững phải sống qua conversation, restart và đổi provider.
2. **Tool-first** — dữ liệu/action ngoài hệ thống phải đi qua tool có thẩm quyền thay vì để model đoán.
3. **Permission-first** — model có thể request action; Loren mới authorize và execute.
4. **Model-independent** — OpenAI, Ollama, Claude, local model và provider tương lai đều chỉ là adapter.
5. **Auditable** — action quan trọng và state mutation phải reconstruct được.
6. **Tự chủ tăng dần** — background/proactive behavior chỉ đến sau khi trust boundary thấp hơn đã được chứng minh.

## Trạng thái hiện tại

**Cập nhật:** 2026-09-03  
**Phase:** `v0.0 — Architecture / Feasibility`  
**Gate:** `Gate B — v0.1 implementation stack`  
**Milestone:** `M0 — ADR-002 technical validation`

Gate A đã **PASS**. Loren sở hữu canonical state và action/security boundary.

Gate B giờ được đóng theo hướng **provider-neutral brain proof**. OpenAI credential/provider path đã được chứng minh, nhưng model chưa chạy vì API trả `429 credit_balance_exhausted`. Billing của một vendor không nên trở thành architecture gate của Loren.

PR #6 thêm native Ollama Cloud brain path và trusted validation sẽ chọn provider có credential:

```text
có OLLAMA_API_KEY  -> Ollama
nếu không, có OPENAI_API_KEY -> OpenAI
không có cái nào   -> fail closed
```

Evidence M0 hiện tại:

| Phần | Trạng thái |
| --- | --- |
| Loren ActionGateway / bounded loop | ✅ PASS |
| OpenAI adapter compile + provider reachability | ✅ PASS |
| OpenAI behavioral proof | ⚠️ bị chặn bởi provider credit |
| Ollama brain spike compile | ✅ PASS |
| Ollama live tool round trip | ⏳ OPEN sau khi PR #6 merge |
| Ollama live cancellation | ⏳ OPEN sau khi PR #6 merge |
| MCP client + Loren gateway | ✅ PASS |
| SQLite + EF Core migration/recovery | ✅ PASS |
| ASP.NET Core + Blazor host | ✅ PASS |

Chi tiết chuẩn: [`docs/status.md`](docs/status.md).

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

## Mốc đầu tiên mày có thể test Loren như user

Preview đầu tiên vẫn là **v0.1 M2 — Walking Skeleton**:

```text
"Loren, check repo rua-den/loren."

UI
 -> Loren Runtime
 -> IBrain
 -> github.read_repository ActionRequest
 -> Action Gateway
 -> GitHub read executor
 -> structured result
 -> IBrain final response
 -> Audit
```

M1 là engineering foundation. M2 là milestone đầu tiên được thiết kế để có cảm giác đang thực sự dùng Loren.

## Stack đề xuất cho v0.1

Chờ ADR-002 accept chính thức:

```text
C# 14 / .NET 10
ASP.NET Core
small Loren-owned agent loop
provider-neutral IBrain
Ollama và OpenAI adapters
MCP C# SDK sau Loren adapters
SQLite + EF Core
Blazor Web App
xUnit
```

## Lộ trình version

```text
v0.0  architecture / feasibility        <- hiện tại
v0.1  trustworthy core
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
- [`docs/plans/v0.1.md`](docs/plans/v0.1.md) — plan chi tiết v0.1
- [`docs/decisions/001-agent-runtime-strategy.md`](docs/decisions/001-agent-runtime-strategy.md) — Loren-owned core/runtime boundary đã accept
- [`docs/decisions/002-v0.1-technology-stack.md`](docs/decisions/002-v0.1-technology-stack.md) — stack v0.1 đề xuất và evidence M0
- [`docs/memory.md`](docs/memory.md) — memory model
- [`docs/permissions.md`](docs/permissions.md) — permission model
- [`docs/security.md`](docs/security.md) — security baseline
- [`docs/skills.md`](docs/skills.md) — skill/tool model

## Vai trò của repository

Repository này là source of truth cho product decisions, architecture, delivery plan, implementation, progress và release history của Loren.
