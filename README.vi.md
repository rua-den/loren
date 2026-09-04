# Loren

[English](README.md) · **Tiếng Việt**

Loren là một hệ thống trí tuệ cá nhân sống lâu dài, có memory bền vững, permission rõ ràng, khả năng dùng tool và về sau có thể chủ động hỗ trợ xuyên suốt đời sống số của chủ sở hữu.

> **Model chỉ là compute có thể thay thế. Loren sở hữu identity, memory, context, policy, approval, action boundary và lịch sử.**

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
**Decision gates đã pass:** `Gate A`, `Gate B`, `Gate C`, `Gate D / ADR-004`  
**Milestone hiện tại:** `M5 — Action/Credential Boundary + Narrow GitHub Writes`  
**Đang làm:** `M5 Slice 1 — typed write policy + one-time approval + global read-only`

Đã hoàn tất:

- Gate A / ADR-001 — Loren-owned core/runtime boundary.
- Gate B / ADR-002 — stack v0.1 provider-neutral.
- Gate C / ADR-003 — canonical state + memory lifecycle.
- Gate D / ADR-004 — action approval + credential boundary.
- M0 — technical feasibility.
- M1 — engineering foundation.
- M2 — Walking Skeleton.
- M3 — Canonical Project/Repository State.
- M4 — Trusted Durable Memory.

Chi tiết chuẩn: [`docs/status.md`](docs/status.md).

## M4 đã chứng minh gì

```text
owner durable fact
 -> canonical MemoryRecord + provenance
 -> SQLite
 -> restart-safe retrieval

owner correction
 -> append OWNER_CORRECTION
 -> supersede claim cũ atomically
 -> history vẫn reconstruct được

project request
 -> current memories
 -> authority/provenance/lifecycle filtering
 -> deterministic hard bounds
 -> prepared Loren memory data
 -> BrainContext

owner forget
 -> purge toàn bộ correction chain trong transaction
 -> restart
 -> fact đã quên vẫn không quay lại

adversarial content
 -> MODEL_INFERENCE / EXTERNAL_CONTENT không thể tự thành owner truth
 -> provenance vẫn chỉ là data, không phải action authorization
```

M4 được merge qua PR #18–#22. PR #23 sau đó fix lỗi SQLite temp-file cleanup riêng trên Windows bằng cách tắt pooling **chỉ cho temp integration database** và thêm permanent `windows-latest` integration CI job.

Baseline mới nhất đã verify:

- main commit sau Windows hardening: `1cdd849126310745652d87f1d100c34aed624079`;
- PR CI #162 / `33893832128`: Ubuntu full gate + Windows integration — PASS;
- post-merge main CI #163 / `33894104116`: Ubuntu full gate + Windows integration — PASS;
- owner chạy local toàn bộ Windows integration suite — PASS.

## Gate D / ADR-004 [PASSED]

Gate D khóa trust boundary đầu tiên cho write trước khi bất kỳ real write executor nào tồn tại.

### Mọi GitHub write thật ở phiên bản đầu đều cần owner approval rõ ràng

Authentication chỉ chứng minh owner identity; **không phải write approval**.

Approval là artifact do Loren sở hữu, bind vào exact normalized intent:

```text
ApprovalId
owner/session binding
action identity
ProjectId + RepositoryId
normalized target/resource
security-relevant parameter digest
approved timestamp
expiry/task boundary
one-time consumption state
optional prerequisite digest
```

Nếu thay đổi repo, branch, path, content digest, PR base/head hoặc action intent quan trọng thì phải approval mới.

Approval được consume atomically đúng một lần. Approval đã consume, expired, mismatch, unknown, revoked hoặc replay đều fail closed.

### Canonical target trước authorization

Chuỗi repo do model đưa ra không tạo authority. Write policy phải resolve request về canonical Project/Repository của Loren và normalized security-relevant target parameters trước khi authorize.

### Global read-only mặc định an toàn

Trước khi có write executor, Loren phải có host-controlled global read-only posture.

```text
write-enable thiếu/sai -> read-only
read-only -> không gọi write executor
read-only -> không resolve write credential
read action vẫn dùng được
```

Model không được toggle trạng thái này qua ordinary action.

### Credential nằm sau executor boundary

Write credential value không bao giờ đi vào:

- `BrainContext`;
- model-visible action parameters;
- canonical state;
- durable memory;
- audit payload;
- owner-visible result.

Chỉ opaque credential purpose/reference được đi qua application boundary. Credential thiếu/revoked thì fail closed. Credential revocation thắng approval đã cấp trước đó. Read/write credential purpose luôn được tách logic.

### External write chỉ thành công sau verification

API trả success chưa đủ.

```text
create branch -> fetch ref -> confirm exact SHA
file/commit write -> fetch commit/ref/file state -> confirm expected identity
open PR -> fetch PR -> confirm repo/base/head/state/PR identity
```

Verification mơ hồ thì không được báo success.

### v0.1 write allowlist

Chỉ được bật sau khi M5 foundations xanh:

```text
create non-default branch
create/update file qua controlled commit path trên non-default branch
create commit/update ref chỉ khi path đó cần
open pull request
```

Vẫn cấm:

```text
write trực tiếp default branch
merge pull request
force push / rewrite history
delete repository/branch/data
repository admin/security changes
secret-management actions
production deployment
```

## M5 implementation sequence

```text
Slice 1  typed action policy context + one-time approval + global read-only
Slice 2  write credential resolver + redaction/revocation
Slice 3  create non-default GitHub branch + verify exact ref/SHA
Slice 4  controlled file/commit path + verify
Slice 5  open pull request + verify
Slice 6  replay/revocation/injection/audit E2E
```

Slice 1 **không bật GitHub mutation thật**. Không mutation nào được bật trước khi policy/approval/read-only/credential foundations đã có test xanh.

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

## Test

```powershell
dotnet restore Loren.slnx
dotnet build Loren.slnx --configuration Release --no-restore
dotnet test Loren.slnx --configuration Release --no-build --no-restore
```

Windows giờ là first-class integration-test CI platform bên cạnh Ubuntu full gate.

## Lộ trình version

```text
v0.0  architecture / feasibility        ✓ hoàn tất
v0.1  trustworthy core                 <- hiện tại / M5
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
- [`docs/permissions.md`](docs/permissions.md) — permission/approval baseline hiện hành
- [`docs/security.md`](docs/security.md) — security baseline hiện hành
- [`docs/plans/master-plan.md`](docs/plans/master-plan.md) — milestones/version gates
- [`docs/plans/v0.1.md`](docs/plans/v0.1.md) — plan implementation chi tiết v0.1
- [`docs/decisions/003-canonical-state-and-memory-lifecycle.md`](docs/decisions/003-canonical-state-and-memory-lifecycle.md)
- [`docs/decisions/004-action-approval-and-credential-boundary.md`](docs/decisions/004-action-approval-and-credential-boundary.md)
- [`docs/memory.md`](docs/memory.md)

Repository này là source of truth cho product decisions, architecture, delivery plan, implementation, progress và release history của Loren.
