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
**Milestone đã hoàn tất:** `M4 — Trusted Durable Memory`  
**Decision gate tiếp theo:** `Gate D — Action/Credential Policy trước M5 writes`

Đã hoàn tất:

- Gate A / ADR-001 — Loren-owned core/runtime boundary.
- Gate B / ADR-002 — stack v0.1 provider-neutral.
- Gate C / ADR-003 — canonical state + memory lifecycle.
- M0 — technical feasibility.
- M1 — engineering foundation.
- M2 — Walking Skeleton.
- M3 — Canonical Project/Repository State.
- M4 — Trusted Durable Memory.

Chi tiết chuẩn: [`docs/status.md`](docs/status.md).

## Memory path M4 [COMPLETE]

```text
owner durable fact
 -> MemoryRecord / MemoryRecordId
 -> Project / Repository scope + provenance
 -> SQLite
 -> restart-safe retrieval

owner correction
 -> append OWNER_CORRECTION
 -> supersede claim cũ atomically
 -> history cũ vẫn reconstruct được

project request
 -> current memories
 -> source/provenance + lifecycle filtering
 -> deterministic ordering
 -> hard content/provenance bounds
 -> prepared Loren memory data
 -> BrainContext

owner forget
 -> current memory
 -> purge toàn bộ correction chain trong transaction
 -> restart
 -> fact đã quên vẫn không quay lại
```

### Slice 1 — OWNER_EXPLICIT persistence

PR #18 merge tại `78adc287f7ae3744352b7019e3b8a838a5de499e`. Final CI #117 / run `33860985267` và post-merge main CI #118 / run `33861089270` pass.

### Slice 2 — correction + supersession

PR #19 merge tại `201b83eff0c6c3143856e348b4c9f029cc14a8b1`. Implementation CI #119 / run `33861345949` và final CI #123 / run `33861630472` pass.

Correction là explicit append + supersede. Content cũ được giữ nguyên; sai authority/scope, stale target, duplicate replacement ID hoặc partial failure đều fail closed.

### Slice 3 — authority-aware prepared memory

PR #20 merge tại `732b85db3a799638bcd73558f98232b276f3cb5e`. Implementation CI #127 / run `33864695658` và final exact-head CI #131 / run `33864946328` pass.

Prepared memory:

- chỉ include `OWNER_CORRECTION`, `OWNER_EXPLICIT`, `OWNER_APPROVED_INFERENCE`, `VERIFIED_TOOL` khi source semantics hợp lệ;
- mặc định exclude `MODEL_INFERENCE` và `EXTERNAL_CONTENT` khỏi trusted model context;
- exclude superseded records;
- giữ canonical IDs, scope, provenance và timestamps;
- deterministic ordering + hard record/content bounds trước khi model chạy;
- được ghi rõ là data, không bao giờ là action authorization hay policy override.

### Slice 4 — owner forget/delete

PR #21 merge tại `87b5a39ccae7c931de9668fed5283a4742be73f7`. Implementation CI #133 / run `33865419023` và final exact-head CI #137 / run `33865716479` pass.

Với `A -> B -> C(current)`, `ForgetAsync(C)` validate correction history tuyến tính cùng scope rồi physical purge A, B, C trong một SQLite transaction. Restart acceptance chứng minh fact đã quên không resurrect còn unrelated memories vẫn tồn tại. Memory forgetting không cascade sang audit retention.

### Slice 5 — poisoning / trust boundary

PR #22 đóng trust gate cuối của M4.

Hardening và adversarial acceptance chứng minh:

- record vào trusted prepared context phải có provenance;
- provenance/source reference có bound riêng độc lập content;
- toàn bộ serialized memory payload — content, provenance, IDs, scope, timestamps — là inert data, không phải instruction, permission, policy hay action authorization;
- `MODEL_INFERENCE` / `EXTERNAL_CONTENT` giả provenance giống owner vẫn bị loại;
- `OWNER_APPROVED_INFERENCE` / `VERIFIED_TOOL` không có provenance hợp lệ vẫn bị loại;
- `VERIFIED_TOOL` không tự đại diện current external state và không grant owner permission;
- owner correction vẫn là current owner truth trong scope;
- execution bình thường qua `LorenRunService` chỉ đọc prepared memory, không âm thầm gọi Add/Correct/Forget.

Implementation CI #140 / run `33866182751` đã pass restore, zero-warning build, toàn bộ tests, format, secret scan, dependency scan và web/auth smoke.

## Durable-memory source classes

```text
OWNER_EXPLICIT
OWNER_CORRECTION
VERIFIED_TOOL
OWNER_APPROVED_INFERENCE
MODEL_INFERENCE
EXTERNAL_CONTENT
```

ADR-003 giữ authority theo ngữ cảnh, không gom thành một confidence score duy nhất.

## Canonical storage

```text
database file: loren.db
default directory: OS local application data / Loren
override: LOREN_DATA_DIRECTORY
migrations: tự chạy khi host start
```

Database mới hiện chưa có Project cấu hình sẵn; owner-facing Project CRUD/configuration và memory-management UI vẫn deferred sang phần UI v0.1 sau.

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

## Tiếp theo — Gate D / M5

Trước khi bật bất kỳ GitHub write capability thật nào, Gate D phải lock:

- write action contracts và policy dimensions;
- approval binding chính xác + non-replay rules;
- credential storage/resolution và tách read/write credential;
- secret redaction, rotation, revocation;
- global read-only / kill behavior;
- post-write verification và audit expectations.

Chỉ sau Gate D thì M5 mới được implement narrow v0.1 write set: create branch, create/update file hoặc equivalent commit path, create commit và open pull request. Merge-main, force-push, repository deletion/admin changes và production deploy vẫn ngoài write scope này.

## Lộ trình version

```text
v0.0  architecture / feasibility        ✓ hoàn tất
v0.1  trustworthy core                 <- hiện tại / Gate D trước M5
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
