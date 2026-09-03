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

Đã hoàn tất tới đây:

- **Gate A / ADR-001:** Loren sở hữu canonical identity/state/policy/action authorization.
- **Gate B / ADR-002:** stack v0.1 provider-neutral đã được accept và M0 đã hoàn tất.
- **M1:** engineering foundation production đã hoàn tất.
- **M2 Slice 1:** production read-only ActionGateway, Loren-owned correlation IDs, audit path và structured `github.read_repository` executor đã hoàn tất.
- **M2 Slice 2:** production `OllamaBrain : IBrain` đã hoàn tất, gồm provider-neutral action-schema translation, deterministic tool-call/observation tests, cancellation và provider-secret isolation.
- **M2 Slice 3 (deterministic):** ASP.NET host thật đã wire OllamaBrain, AgentLoop, ActionGateway, read-only GitHub execution và audit qua DI; full production-component read path đã PASS deterministic integration test.

M2 vẫn ACTIVE. Proof kế tiếp là trusted live Ollama chạy xuyên production host composition này, sau đó mới tới one-owner auth/session và minimal owner UI.

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

Production code bắt đầu với boundaries cố ý gọn:

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

Target owner flow:

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

### M2 Slice 1 — read boundary đã xong

Production có:

```text
RunId / ActionId do Loren Runtime tạo
 -> ActionGateway
 -> ReadOnlyActionPolicy
 -> GitHubReadRepositoryExecutor
 -> structured ActionResult
 -> append-oriented audit
```

Các invariant quan trọng đã được chứng minh:

- model không được tự chọn trusted run/action correlation IDs;
- action chưa đăng ký hoặc không read-only bị fail closed trước executor;
- policy failure không chạm executor;
- executor error thành safe structured failure;
- cancellation ghi terminal `cancelled` audit trước khi propagate;
- `github.read_repository` chỉ HTTP GET public và không có GitHub write/credential path;
- deterministic integration test chứng minh fake brain -> gateway -> fake GitHub -> structured result -> final answer.

### M2 Slice 2 — production Ollama brain đã xong

Production có `OllamaBrain : IBrain` thật nằm sau cùng Loren-owned contract.

```text
Loren ActionDefinition
 -> provider-neutral typed parameters
 -> Ollama function-tool JSON
 -> provider tool_call
 -> Loren ActionRequest
```

Adapter reconstruct `BrainActionObservation` cũ thành assistant tool-call + tool-result messages cho provider turn kế tiếp.

Các security/behavior rule đã được deterministic test:

- `OLLAMA_API_KEY` không nằm trong serializable options hay request JSON;
- key chỉ được giữ private trong adapter và gửi qua `Authorization: Bearer ...`;
- raw provider error body không bị copy vào exception message;
- cancellation propagate tại provider await;
- parallel tool calls fail explicit cho tới khi Loren runtime chủ động support;
- provider JSON types không lọt vào `Loren.Core`.

### M2 Slice 3 — production host wiring đã PASS deterministic

ASP.NET host giờ compose production read path thật:

```text
Loren.Web DI
 -> OllamaBrain
 -> AgentLoop
 -> ActionGateway
 -> ReadOnlyActionPolicy
 -> GitHubReadRepositoryExecutor
 -> InMemoryAuditSink
```

Deterministic integration test chạy toàn bộ production components với fake HTTP endpoints và chứng minh provider bearer token chỉ xuất hiện ở Ollama request, không xuất hiện trong GitHub request.

Route tạm `/internal/dev/run` mặc định bị tắt. Nó chỉ được map khi `LOREN_ENABLE_DEVELOPMENT_RUN_ENDPOINT=true` và environment là `Development`; bật ngoài Development sẽ fail startup. CI còn assert host bình thường trả `404` cho route này.

### Proof M2 kế tiếp

```text
trusted live Ollama
 -> production host composition
 -> ActionRequest(github.read_repository)
 -> real public GitHub read
 -> structured ActionResult
 -> live Ollama final answer
```

Sau proof đó M2 tiếp tục với one-owner auth/session và minimal owner UI. M2 chưa được phép có GitHub write path.

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
