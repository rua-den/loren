# Loren

[English](README.md) · **Tiếng Việt**

Loren là một hệ thống trí tuệ cá nhân sống lâu dài, có memory bền vững, permission rõ ràng, khả năng dùng tool và về sau có thể chủ động hỗ trợ xuyên suốt đời sống số của chủ sở hữu.

> **Model chỉ là compute có thể thay thế. Loren sở hữu identity, memory, context, policy, action boundary và lịch sử.**

## Nguyên tắc cốt lõi

1. **Memory-first** — state bền vững sống qua conversation, restart và đổi provider.
2. **Tool-first** — dữ liệu/action bên ngoài phải đi qua tool có thẩm quyền thay vì để model đoán.
3. **Permission-first** — model có thể request action; Loren mới authorize và execute.
4. **Model-independent** — model provider là adapter có thể thay thế.
5. **Auditable** — hành vi quan trọng phải reconstruct được.
6. **Tự chủ tăng dần** — background/proactive behavior chỉ đến sau khi trust boundary thấp hơn đã được chứng minh.

## Trạng thái hiện tại

**Cập nhật:** 2026-09-04  
**Phase:** `v0.1 — Trustworthy Core development`  
**Milestone hiện tại:** `M4 — Trusted Durable Memory`

Đã hoàn tất:

- **Gate A / ADR-001:** Loren-owned core/runtime boundary đã accept.
- **Gate B / ADR-002:** stack v0.1 provider-neutral đã accept.
- **Gate C / ADR-003:** canonical state + memory lifecycle rules đã accept.
- **M0:** technical feasibility proofs hoàn tất.
- **M1:** engineering foundation hoàn tất.
- **M2:** Walking Skeleton hoàn tất; first owner-testable Loren preview đã được chứng minh.
- **M3:** Canonical Project/Repository State hoàn tất.

Chi tiết chuẩn: [`docs/status.md`](docs/status.md).

## M3 đã chứng minh gì

M3 Slice 1 merge qua PR #15 tại `00fbba08587ba8275c121fd7f9532a785f55314d`. M3 Slice 2 merge qua PR #16 tại `56fd988d3b74c754604355e3c97a5d3656675bbb`.

Validation:

- Slice 1 exact-head CI #99 / run `33842440251` — PASS;
- Slice 2 final PR exact-head CI #108 / run `33843405386` — PASS;
- Slice 2 post-merge main CI #109 / run `33843524467` — PASS.

Canonical identity path:

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

Runtime path:

```text
Owner request + optional exact Project alias
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

Alias không tồn tại fail trước khi model chạy. Runtime và brain adapter không nhận EF `DbContext`. Project/Repository identity được cấu hình là trusted canonical context, nhưng current external facts vẫn phải fetch qua authorized tools.

## Gate C / ADR-003

Trước khi bắt đầu durable memory code, Loren đã lock:

- opaque Loren-owned GUID IDs;
- explicit EF Core migration policy;
- Project/Repository canonical schema boundary;
- memory source/trust classes;
- append/supersede correction;
- memory deletion tách khỏi audit retention;
- logical export format versioning độc lập EF schema migrations.

Các durable-memory source class bắt buộc:

```text
OWNER_EXPLICIT
OWNER_CORRECTION
VERIFIED_TOOL
OWNER_APPROVED_INFERENCE
MODEL_INFERENCE
EXTERNAL_CONTENT
```

Xem [`ADR-003`](docs/decisions/003-canonical-state-and-memory-lifecycle.md) và [`docs/memory.md`](docs/memory.md).

## Mục tiêu M4 hiện tại

M4 xây durable memory trên đúng trust rules đó.

Vertical flow đầu tiên:

```text
Owner: "Nhớ wedding-online là web đám cưới của tao."
        |
        v
OWNER_EXPLICIT MemoryRecord
        |
Project scope + provenance
        |
SQLite persistence
        |
restart
        |
trusted retrieval
        |
small prepared memory context
```

Sau đó M4 phải chứng minh owner correction/supersession và `MODEL_INFERENCE` / `EXTERNAL_CONTENT` không thể tự biến thành owner truth hay policy.

## Canonical storage

```text
database file: loren.db
default directory: OS local application data / Loren
override: LOREN_DATA_DIRECTORY
migrations: tự chạy khi host start
```

Database mới hiện chưa có Project cấu hình sẵn; owner-facing Project CRUD/configuration UI vẫn deferred.

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

Không commit secret thật. Nếu expose host ra ngoài localhost phải dùng HTTPS hoặc reverse proxy terminate TLS đáng tin cậy. Chi tiết: [`docs/development.md`](docs/development.md).

## Stack v0.1 đã accept

```text
C# 14 / .NET 10 LTS
ASP.NET Core
small Loren-owned bounded agent loop
provider-neutral IBrain
MCP C# SDK sau Loren action contracts
SQLite + EF Core
Blazor Web App
xUnit / Microsoft Testing Platform
```

## Tiếp theo

```text
M4 Slice 1
MemoryRecord + source authority model
 -> SQLite migration/persistence
 -> OWNER_EXPLICIT save + Project scope
 -> restart-safe retrieval
 -> correction/supersession
 -> poisoning tests
 -> prepared memory context
 -> M4 exit gate
```

Gate D vẫn bắt buộc trước bất kỳ GitHub write capability nào.

## Lộ trình version

```text
v0.0  architecture / feasibility        ✓ hoàn tất
v0.1  trustworthy core                 <- hiện tại / M4
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
- [`docs/development.md`](docs/development.md) — build/test/configuration
- [`docs/vision.md`](docs/vision.md) — product vision
- [`docs/architecture.md`](docs/architecture.md) — system boundaries
- [`docs/plans/master-plan.md`](docs/plans/master-plan.md) — milestones/version gates
- [`docs/plans/v0.1.md`](docs/plans/v0.1.md) — plan implementation chi tiết v0.1
- [`docs/decisions/001-agent-runtime-strategy.md`](docs/decisions/001-agent-runtime-strategy.md)
- [`docs/decisions/002-v0.1-technology-stack.md`](docs/decisions/002-v0.1-technology-stack.md)
- [`docs/decisions/003-canonical-state-and-memory-lifecycle.md`](docs/decisions/003-canonical-state-and-memory-lifecycle.md)
- [`docs/memory.md`](docs/memory.md)
- [`docs/permissions.md`](docs/permissions.md)
- [`docs/security.md`](docs/security.md)

Repository này là source of truth cho product decisions, architecture, delivery plan, implementation, progress và release history của Loren.
