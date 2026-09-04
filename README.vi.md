# Loren

[English](README.md) · **Tiếng Việt**

Loren là một hệ thống trí tuệ cá nhân sống lâu dài, có memory bền vững, permission rõ ràng, khả năng dùng tool và về sau có thể chủ động hỗ trợ xuyên suốt đời sống số của chủ sở hữu.

> **Model chỉ là compute có thể thay thế. Loren sở hữu identity, memory, context, policy, action boundary và lịch sử.**

## Nguyên tắc cốt lõi

1. **Memory-first** — state bền vững sống qua conversation, restart và đổi provider.
2. **Tool-first** — dữ liệu/action bên ngoài phải đi qua tool có thẩm quyền thay vì để model đoán.
3. **Permission-first** — model có thể request action; Loren mới authorize và execute.
4. **Model-independent** — Ollama, OpenAI, local model và provider tương lai đều chỉ là adapter.
5. **Auditable** — hành vi quan trọng phải reconstruct được.
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
- **M2:** Walking Skeleton hoàn tất; first owner-testable Loren preview đã được chứng minh.
- **M3 Slice 1:** canonical Project/Repository identity + SQLite persistence hoàn tất.

M3 Slice 1 đã merge qua PR #15 tại `00fbba08587ba8275c121fd7f9532a785f55314d`. Exact-head CI run `33842440251` pass restore/build/test/format/secret/dependency/web-auth gates.

M3 Slice 2 hiện là candidate trong PR #16: deterministic alias resolution + prepared runtime context. Implementation CI run `33843033700` đã pass trước lần đồng bộ docs cuối; final PR head vẫn phải pass CI thêm lần nữa trước khi merge.

Chi tiết chuẩn: [`docs/status.md`](docs/status.md).

## Kiến trúc M3 hiện tại

```text
Owner request + optional exact Project alias
        |
        v
Loren.Web
        |
        v
IProjectCatalog
        |
        v
SQLite / EF Core canonical state
        |
        v
ProjectSnapshot
        |
        v
small prepared BrainContext
        |
        v
AgentLoop -> IBrain -> authorized tools
```

Ví dụ canonical identity:

```text
"wedding project"
"web đám cưới"
"wedding-online"
        |
        v
cùng Loren ProjectId
        |
        v
canonical RepositoryId
        |
        v
github locator: rua-den/wedding-online
```

Boundary quan trọng: Project/Repository được cấu hình là trusted canonical identity/context, nhưng **không phải live external state**. Dữ liệu GitHub hiện tại vẫn phải fetch qua authorized tool.

Alias không tồn tại sẽ fail trước khi model chạy. Runtime và brain adapter không bao giờ nhận EF `DbContext`.

Database mới mặc định chưa có Project nào; M3 chưa thêm owner-facing Project CRUD UI.

## Canonical storage

M3 dùng baseline SQLite + EF Core đã accept.

```text
database file: loren.db
default directory: OS local application data / Loren
override: LOREN_DATA_DIRECTORY
migrations: tự chạy khi host start
```

`ProjectId` / `RepositoryId` do Loren sở hữu, độc lập với GitHub ID, provider ID và runtime session ID.

## Chạy local

```powershell
$env:LOREN_OWNER_PASSWORD='choose-a-local-owner-password'
$env:OLLAMA_API_KEY='your-provider-secret'
# optional: $env:LOREN_DATA_DIRECTORY='D:\loren-data'
dotnet run --project src/Loren.Web/Loren.Web.csproj
```

Bash:

```bash
export LOREN_OWNER_PASSWORD='choose-a-local-owner-password'
export OLLAMA_API_KEY='your-provider-secret'
# optional: export LOREN_DATA_DIRECTORY='/path/to/loren-data'
dotnet run --project src/Loren.Web/Loren.Web.csproj
```

Mở root URL do ASP.NET Core in ra rồi login. Ô Project alias chỉ resolve alias đã tồn tại trong canonical state.

Không commit secret thật. Nếu expose host ra ngoài localhost phải dùng HTTPS hoặc reverse proxy terminate TLS đáng tin cậy. Chi tiết: [`docs/development.md`](docs/development.md).

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

## Tiếp theo

```text
M3 Slice 2 final CI + merge
 -> M3 Slice 3 / Gate C checkpoint
 -> M3 COMPLETE
 -> M4 Trusted Memory
```

M3 Slice 3 sẽ lock canonical ID rules, migration policy, Project/Repository schema boundary, memory-vs-audit deletion semantics và hướng export versioning trước khi bắt đầu durable memory.

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

Version chỉ nâng khi vượt exit gate, không dựa vào ngày hay số lượng code.

## Tài liệu

- [`docs/status.md`](docs/status.md) — tiến độ chuẩn hiện tại
- [`docs/development.md`](docs/development.md) — build/test/config/dependency
- [`docs/vision.md`](docs/vision.md) — product vision
- [`docs/architecture.md`](docs/architecture.md) — system boundaries
- [`docs/plans/master-plan.md`](docs/plans/master-plan.md) — milestones/version gates
- [`docs/plans/v0.1.md`](docs/plans/v0.1.md) — plan implementation chi tiết v0.1
- [`docs/decisions/001-agent-runtime-strategy.md`](docs/decisions/001-agent-runtime-strategy.md)
- [`docs/decisions/002-v0.1-technology-stack.md`](docs/decisions/002-v0.1-technology-stack.md)
- [`docs/memory.md`](docs/memory.md)
- [`docs/permissions.md`](docs/permissions.md)
- [`docs/security.md`](docs/security.md)
- [`docs/skills.md`](docs/skills.md)

Repository này là source of truth cho product decisions, architecture, delivery plan, implementation, progress và release history của Loren.
