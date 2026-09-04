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
- **M2:** Walking Skeleton hoàn tất.
- **M3:** Canonical Project/Repository State hoàn tất.
- **M4 Slice 1:** canonical OWNER_EXPLICIT memory persistence hoàn tất.

Chi tiết chuẩn: [`docs/status.md`](docs/status.md).

## M4 Slice 1 — durable owner-explicit memory [COMPLETE]

PR #18 merge tại `78adc287f7ae3744352b7019e3b8a838a5de499e`.

Validation:

- implementation CI #113 / run `33860641367` — PASS;
- final exact-head CI #117 / run `33860985267` — PASS;
- post-merge main CI #118 / run `33861089270` — PASS.

Slice 1 đã tạo boundary:

```text
OWNER_EXPLICIT durable fact
 -> Loren-owned MemoryRecordId / MemoryRecord
 -> Project / Repository scope + provenance
 -> EF-neutral IMemoryStore
 -> SQLite / EF Core migration
 -> restart
 -> cùng memory ID + authority + scope
```

Canonical store dùng migration `202609040002_AddMemoryRecords`. Repository-scoped memory fail closed nếu Repository không thuộc Project được đưa vào. Cố ý không có generic content-update API.

## M4 Slice 2 — owner correction + supersession

PR #19 hiện implement correction semantics rõ ràng:

```text
old current memory
 -> CorrectAsync(oldId, OWNER_CORRECTION)
 -> append correction mới
 -> old.SupersededById = new.Id
 -> một SQLite transaction
 -> current query chỉ trả record mới
 -> history old -> new vẫn reconstruct được
```

Correction boundary bắt buộc:

- source authority là `OWNER_CORRECTION`;
- `MemoryRecordId` mới;
- target tồn tại và vẫn current;
- Project/Repository scope giữ nguyên;
- lifecycle timestamp không lùi;
- correction ID chưa tồn tại.

Content cũ không bị rewrite. Correction dùng model-source, đổi scope hoặc target stale đều fail mà không để lại partial insert.

Real SQLite tests chứng minh correction chain sống qua restart và default current retrieval loại record đã superseded. Implementation CI #119 / run `33861345949` đã **PASS** restore, zero-warning build, tests, format, secret scan, dependency scan và web/auth smoke.

PR #19 chưa complete cho tới khi final exact-head CI trên head đã sync docs pass và PR được merge.

## Gate C / durable-memory authority classes

```text
OWNER_EXPLICIT
OWNER_CORRECTION
VERIFIED_TOOL
OWNER_APPROVED_INFERENCE
MODEL_INFERENCE
EXTERNAL_CONTENT
```

ADR-003 cũng lock append/supersede correction, memory deletion tách khỏi audit retention và logical export `format_version = 1` độc lập EF migrations.

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

Không commit secret thật. Nếu expose host ra ngoài localhost phải dùng HTTPS hoặc reverse proxy terminate TLS đáng tin cậy.

## Tiếp theo

```text
PR #19 final exact-head CI
 -> merge M4 Slice 2
 -> M4 Slice 3 authority-aware prepared memory context
 -> M4 Slice 4 forget/delete
 -> M4 Slice 5 poisoning/trust acceptance
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

## Tài liệu

- [`docs/status.md`](docs/status.md) — tiến độ chuẩn hiện tại
- [`docs/development.md`](docs/development.md) — build/test/configuration
- [`docs/architecture.md`](docs/architecture.md) — system boundaries
- [`docs/plans/master-plan.md`](docs/plans/master-plan.md) — milestones/version gates
- [`docs/plans/v0.1.md`](docs/plans/v0.1.md) — plan implementation chi tiết v0.1
- [`docs/decisions/003-canonical-state-and-memory-lifecycle.md`](docs/decisions/003-canonical-state-and-memory-lifecycle.md)
- [`docs/memory.md`](docs/memory.md)

Repository này là source of truth cho product decisions, architecture, delivery plan, implementation, progress và release history của Loren.
