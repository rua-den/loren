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

**Cập nhật:** 2026-09-04  
**Phase:** `v0.1 — Trustworthy Core development`  
**Milestone hiện tại:** `M3 — Canonical State`

Đã hoàn tất:

- **Gate A / ADR-001:** Loren-owned core/runtime boundary đã accept.
- **Gate B / ADR-002:** stack v0.1 provider-neutral đã accept.
- **M0:** technical feasibility proofs hoàn tất.
- **M1:** engineering foundation hoàn tất.
- **M2:** **Walking Skeleton hoàn tất.**

Trusted exact-main production proof của M2 đã PASS ở run `33840149005` trên commit `94ce6d1e74f2dfdf0584b8dbf8a4edbbb3774f7d`:

```text
unauthenticated /api/run -> 401
owner login -> 200 + cookie session
authenticated /api/run
 -> Ollama gpt-oss:120b
 -> github.read_repository
 -> real GitHub GET rua-den/loren
 -> Ollama final answer
 -> correlated owner-visible audit
```

Kết quả thật:

```text
runId:       5bb9cc341387430c82759d58309da85a
turns:       2
actionCount: 1
final:       Repository rua-den/loren
             Default branch: main
```

Audit đã PASS:

```text
ActionRequested -> PolicyEvaluated -> ActionCompleted
requested       -> allow           -> succeeded
```

Workflow cũng kiểm tra owner/provider credential không xuất hiện trong response owner thấy được và `/internal/dev/run` vẫn trả `404` ở Production.

**First owner-testable Loren preview: đã đạt.**

Chi tiết chuẩn: [`docs/status.md`](docs/status.md).

## Mục tiêu M3 hiện tại

M3 đưa Project/Repository của Loren thành canonical state độc lập provider.

```text
cách owner gọi project / alias
        |
        v
canonical Loren Project ID
        |
        v
canonical Repository record
        |
        +--> integration metadata: GitHub owner/repo
        |
        v
prepared runtime context / authoritative tool use
```

Acceptance target:

```text
"wedding project"
"web đám cưới"
"wedding-online"
        |
        v
cùng một Loren Project
        |
        v
rua-den/wedding-online
```

Mapping này phải sống qua restart và không phụ thuộc provider conversation/session.

M3 cố ý giữ world model nhỏ. Chưa thêm generic graph hay các entity `Person`, `Task`, `Decision`, `Preference` nếu chưa có flow thật cần chúng.

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

## Chạy owner preview hiện tại local

```powershell
$env:LOREN_OWNER_PASSWORD='choose-a-local-owner-password'
$env:OLLAMA_API_KEY='your-provider-secret'
dotnet run --project src/Loren.Web/Loren.Web.csproj
```

Sau đó mở root URL do ASP.NET Core in ra, login và chạy:

```text
Loren, check repo rua-den/loren.
```

Không commit secret thật. Nếu expose host ra ngoài localhost thì phải dùng HTTPS hoặc reverse proxy terminate TLS đáng tin cậy. Chi tiết: [`docs/development.md`](docs/development.md).

## Tiếp theo

```text
M3 canonical IDs + Project/Repository schema
 -> SQLite / EF Core persistence
 -> alias resolution + restart tests
 -> canonical Project/Repository context
 -> Gate C checkpoint
 -> M4 Trusted Memory
```

## Lộ trình version

```text
v0.0  architecture / feasibility        ✓ hoàn tất
v0.1  trustworthy core                 <- hiện tại / M3
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
