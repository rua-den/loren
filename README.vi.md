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

Đã hoàn tất: Gate A/B/C, M0, M1, M2, M3, M4 Slice 1, M4 Slice 2 và M4 Slice 3.

Chi tiết chuẩn: [`docs/status.md`](docs/status.md).

## Memory path M4

```text
owner durable fact
 -> MemoryRecord / MemoryRecordId
 -> Project / Repository scope + provenance
 -> SQLite
 -> restart-safe retrieval

owner correction
 -> append OWNER_CORRECTION
 -> supersede claim cũ atomically

project request
 -> current memories
 -> authority/lifecycle filtering
 -> hard context bounds
 -> prepared memory data
 -> BrainContext

owner forget
 -> current memory
 -> purge toàn bộ correction chain trong transaction
 -> restart
 -> fact đã quên vẫn không quay lại
```

### Slice 1 [COMPLETE]

PR #18 merge tại `78adc287f7ae3744352b7019e3b8a838a5de499e`. Final CI #117 / run `33860985267` và post-merge main CI #118 / run `33861089270` pass.

### Slice 2 [COMPLETE]

PR #19 merge tại `201b83eff0c6c3143856e348b4c9f029cc14a8b1`. Implementation CI #119 / run `33861345949` và final CI #123 / run `33861630472` pass.

Correction là explicit append + supersede; content cũ được giữ nguyên và replacement sai authority/scope/stale đều fail closed.

### Slice 3 [COMPLETE]

PR #20 merge tại `732b85db3a799638bcd73558f98232b276f3cb5e`. Implementation CI #127 / run `33864695658` và final exact-head CI #131 / run `33864946328` pass.

Prepared memory:
- include owner correction, owner explicit, owner-approved inference và verified-tool;
- mặc định exclude model inference và external content;
- exclude superseded record;
- giữ IDs, scope, provenance và timestamps;
- deterministic ordering + hard bounds record/character;
- được ghi rõ là data, không phải action authorization hay policy override.

### Slice 4 [IMPLEMENTED / PR #21 FINAL GATE]

PR #21 thêm `IMemoryStore.ForgetAsync(...)`.

Với chain `A -> B -> C(current)`, forget C sẽ đi ngược history cùng scope rồi physical delete A, B, C trong một SQLite transaction. Như vậy fact cũ đã từng bị correction không thể tự sống lại khi record current bị quên.

Real SQLite acceptance chứng minh:
- cả chain vẫn biến mất sau restart;
- prepared context không resurrect forgotten content;
- unrelated memories vẫn còn;
- forget stale/superseded hoặc unknown target fail closed.

Implementation CI #133 / run `33865419023` đã **PASS** restore, zero-warning build, toàn bộ tests, format, secret scan, dependency scan và web/auth smoke.

Memory forget không dùng audit cascade; audit retention vẫn là concern riêng theo ADR-003. PR #21 vẫn cần final exact-head CI trên head đã sync docs trước khi merge.

## Durable-memory source classes

```text
OWNER_EXPLICIT
OWNER_CORRECTION
VERIFIED_TOOL
OWNER_APPROVED_INFERENCE
MODEL_INFERENCE
EXTERNAL_CONTENT
```

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
dotnet run --project src/Loren.Web/Loren.Web.csproj
```

Bash:

```bash
export LOREN_OWNER_PASSWORD='choose-a-local-owner-password'
export OLLAMA_API_KEY='your-provider-secret'
dotnet run --project src/Loren.Web/Loren.Web.csproj
```

Không commit secret thật.

## Tiếp theo

```text
PR #21 final exact-head CI
 -> merge M4 Slice 4
 -> M4 Slice 5 poisoning/trust acceptance
 -> M4 exit gate
 -> Gate D / M5
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
