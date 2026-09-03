# Loren

[English](README.md) · **Tiếng Việt**

Loren là một hệ thống trí tuệ cá nhân sống lâu dài, có memory bền vững, permission rõ ràng, khả năng dùng tool và về sau có thể chủ động hỗ trợ xuyên suốt đời sống số của chủ sở hữu.

> **Model chỉ là compute có thể thay thế. Loren sở hữu identity, memory, context, policy, action boundary và lịch sử.**

## Nguyên tắc cốt lõi

1. **Memory-first** — state bền vững sống qua conversation, restart và đổi provider.
2. **Tool-first** — dữ liệu/action bên ngoài phải đi qua tool có thẩm quyền thay vì để model đoán.
3. **Permission-first** — model có thể request action; Loren mới authorize và execute.
4. **Model-independent** — Ollama, OpenAI, Claude, local model và provider tương lai đều chỉ là adapter.
5. **Auditable** — action quan trọng và state mutation phải reconstruct được.
6. **Tự chủ tăng dần** — background/proactive behavior chỉ đến sau khi trust boundary thấp hơn đã được chứng minh.

## Trạng thái hiện tại

**Cập nhật:** 2026-09-03  
**Phase:** `v0.1 — Trustworthy Core development`  
**Milestone hiện tại:** `M2 — Walking Skeleton`

Đã hoàn tất:

- **Gate A / ADR-001:** Loren sở hữu canonical identity/state/policy/action authorization.
- **Gate B / ADR-002:** stack v0.1 provider-neutral đã accept; M0 hoàn tất.
- **M1:** production engineering foundation hoàn tất.
- **M2 Slice 1:** read-only ActionGateway + structured `github.read_repository` + Loren-owned run/action IDs + audit.
- **M2 Slice 2:** production `OllamaBrain : IBrain` với typed action schema, observation replay, cancellation và provider-secret isolation.
- **M2 Slice 3:** ASP.NET host thật compose OllamaBrain, AgentLoop, ActionGateway, read-only GitHub execution và audit qua DI; deterministic production-component E2E đã PASS.
- **M2 Slice 4:** **trusted live production proof đã PASS** trên exact `main`.

Trusted run `33781183510` đã chứng minh:

```text
real Ollama Cloud (gpt-oss:120b)
 -> production Loren.Web
 -> production OllamaBrain
 -> ActionRequest(github.read_repository)
 -> production AgentLoop / ActionGateway / ReadOnlyActionPolicy
 -> real GET https://api.github.com/repos/rua-den/loren
 -> structured ActionResult
 -> real Ollama turn thứ hai
 -> final answer: rua-den/loren / main
 -> correlated audit
```

Kết quả thực tế: `turns=2`, `actionCount=1`, audit `ActionRequested -> PolicyEvaluated -> ActionCompleted`, action cuối `succeeded`.

M2 giờ đã có **vertical path model-to-tool thật**. Trước khi M2 kết thúc còn đúng phần owner-facing: **one-owner auth/session, minimal owner UI/endpoint và owner-visible audit presentation**.

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

## Kiến trúc production read hiện tại

```text
Owner (bước kế: authenticated UI)
        |
        v
Loren.Web
        |
        v
AgentLoop -> IBrain -> Ollama
        |
        v
ActionRequest
        |
        v
ActionGateway
  -> ReadOnlyActionPolicy
  -> Audit
        |
        v
GitHubReadRepositoryExecutor
        |
        v
real public GitHub GET
```

Các invariant quan trọng đã được chứng minh:

- mọi action đều phải đi qua Loren ActionGateway;
- model không thể chọn trusted Loren run/action IDs;
- action chưa đăng ký hoặc không read-only fail closed;
- provider API key không lọt vào model-visible JSON hay owner-visible live response;
- transport Ollama và GitHub được tách riêng;
- `github.read_repository` không có GitHub write credential path;
- route tạm `/internal/dev/run` mặc định không tồn tại và chỉ được bật trong Development với explicit flag;
- trusted validation có secret chỉ chạy sau exact-current-main guard.

M2 chưa được phép có GitHub write path.

## Tiếp theo

```text
one-owner authentication/session
 -> minimal owner request UI
 -> owner-visible audit
 -> "Loren, check repo rua-den/loren."
 -> FIRST OWNER-TESTABLE LOREN PREVIEW
 -> M3 Canonical State
```

## Lộ trình version

```text
v0.0  architecture / feasibility        ✓ hoàn tất
v0.1  trustworthy core                 <- hiện tại / M2
v0.2  useful project assistant
v0.3  personal operations
v0.4  voice + device presence
v0.5  proactive/background Loren
v0.6+ hardening từ sử dụng thực tế
v1.0  stable personal daily driver
```

Version chỉ được nâng khi vượt exit gate, không dựa vào ngày tháng hay số lượng code.

## Tài liệu

- [`docs/status.md`](docs/status.md) — tiến độ chuẩn hiện tại
- [`docs/development.md`](docs/development.md) — build/test/config/dependency
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

Repository này là source of truth cho product decisions, architecture, delivery plan, implementation, progress và release history của Loren.
