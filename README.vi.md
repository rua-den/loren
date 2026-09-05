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

**Cập nhật:** 2026-09-05  
**Phase:** `v0.1 — Trustworthy Core development`  
**Milestone đã hoàn tất:** `M4 — Trusted Durable Memory`  
**Decision gates đã pass:** `Gate A`, `Gate B`, `Gate C`, `Gate D / ADR-004`  
**Milestone hiện tại:** `M5 — Action/Credential Boundary + Narrow GitHub Writes`  
**Target hiện tại:** `Chốt exact-head gate của PR #25, sau đó M5 Slice 2 — write credential resolver + redaction/revocation`

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

Chi tiết chuẩn: [`docs/status.md`](docs/status.md). Checkpoint để mở thread mới: [`docs/handoff.md`](docs/handoff.md).

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

M4 được merge qua PR #18–#22. PR #23 sau đó harden SQLite integration trên Windows và thêm permanent `windows-latest` integration job. PR #23 merge tại `1cdd849126310745652d87f1d100c34aed624079`; PR CI #162 / `33893832128`, main CI #163 / `33894104116`, và local Windows integration suite của owner đều pass.

## Gate D / ADR-004 [PASSED]

PR #24 merge vào `main` tại `b8649cb563e30af845a0b383103797632bed79a4`. Exact-head CI #164 / `33896004193` pass Ubuntu full gate và Windows integration.

Gate D khóa trust boundary đầu tiên cho write:

```text
brain request write
 -> resolve canonical target
 -> deterministic policy
 -> exact Loren-owned owner approval
 -> atomic one-time consume / chống replay
 -> host-controlled global read-only
 -> write-specific credential resolver
 -> controlled executor
 -> independent post-write verification
 -> correlated redacted audit
```

Authentication chỉ chứng minh owner identity; **không phải write approval**. Mọi GitHub mutation thật ở phiên bản đầu đều cần explicit owner approval. Model/external content không thể tự tạo/broaden approval, chọn credential, tắt read-only hay tự tuyên bố write đã verify.

Mutation scope v0.1 chỉ được bật sau khi M5 foundations cần thiết xanh:

```text
create non-default branch
controlled file/commit path trên non-default branch
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

## M5 Slice 1 — policy + one-time approval foundation

PR #25 implement policy/approval/read-only foundation của Gate D nhưng **cố ý chưa register GitHub mutation executor thật**.

Đã có:

- typed `ActionAccessClass`: `READ`, `REVERSIBLE_WRITE`, `EXTERNAL_WRITE`, `PRIVILEGED_WRITE`;
- trusted `ActionAuthorizationContext` mang canonical Project/Repository target ở ngoài model-visible action arguments;
- model-visible action arguments và trusted normalized target được defensive-copy thành immutable snapshot, chặn TOCTOU kiểu approval fingerprint thấy A nhưng executor lại thấy B;
- deterministic SHA-256 action-intent fingerprint bind action/access/canonical target/owner/normalized target/model arguments;
- Loren-owned `ApprovalId`, `ActionApproval`, provider-neutral `IActionApprovalStore`;
- `GateDActionPolicy` và invariant trong ActionGateway bắt mọi non-read action phải có approval ngay cả khi permissive policy lỡ trả `Allow`;
- kiểm tra executor đã register **trước khi consume approval**, tránh burn approval chỉ vì host misconfiguration;
- exact one-time approval consume ngay trước executor;
- approval missing, expired, revoked, mismatch, unknown hoặc replay đều fail closed;
- text `approvalId` do model nhét vào argument không có authority;
- SQLite `ActionApprovals` qua migration `202609040003_AddActionApprovals`;
- atomic compare-and-consume, concurrent attempt chỉ đúng một winner;
- host config `LOREN_ENABLE_WRITES` fail-closed;
- permanent EF migration-drift regression test.

Safe default:

```text
LOREN_ENABLE_WRITES thiếu/false/sai -> read-only
LOREN_ENABLE_WRITES=true -> eligible write mới có thể đi tới approval evaluation
Slice 1 vẫn không có GitHub mutation executor
```

Validation:

- base implementation head `15a2b2c4c853324a546a55d13da22d94d4ac5765`, CI #172 / `33898878125` — Ubuntu full gate + Windows integration **PASS**;
- self-review hardening head `5ed9049eeedf3210f1df13a0c8735b67d7e4766e`, CI #186 / `33900018499` — immutable approved-intent snapshot + không burn approval khi thiếu executor; Ubuntu full gate + Windows integration **PASS**.

Một rule chủ ý: approval được consume **trước** first consequential executor attempt. Nếu attempt fail/mơ hồ và muốn retry độc lập thì phải approval mới; một approval không biến thành replay token.

### Trạng thái handoff của PR #25

```text
state: OPEN / mergeable / chưa merge
base main: b8649cb563e30af845a0b383103797632bed79a4
last code-changing validated head: 5ed9049eeedf3210f1df13a0c8735b67d7e4766e
latest green code CI: #186 / 33900018499
sau code head này branch chỉ đang có các commit đồng bộ documentation
```

Trước khi merge: review `docs/architecture.md` theo execution order đã harden, freeze PR head, bắt buộc final exact-head Ubuntu + Windows CI xanh, self-review final diff, squash-merge bằng expected head SHA, rồi verify post-merge main CI. **Chưa được start Slice 2 trước khi hoàn tất chuỗi này.**

## Tiếp theo — M5 Slice 2 credential boundary

Trước khi thêm real GitHub mutation executor đầu tiên, Slice 2 phải chứng minh:

- write-specific credential resolver abstraction;
- secret value chỉ tồn tại trong controlled executor boundary;
- read/write credential purpose được tách logic;
- credential thiếu/revoked fail closed, không fallback sang token rộng hơn;
- revocation thắng intent đã approval trước đó;
- secret bị redact khỏi log, exception, audit, action result và brain context.

Chỉ sau khi Slice 1–2 xanh trên `main` Loren mới sang verified create-branch, controlled file/commit và open-PR slices.

## Canonical storage

```text
database file: loren.db
default directory: OS local application data / Loren
override: LOREN_DATA_DIRECTORY
migrations: tự chạy khi host start
```

Database mới hiện chưa có Project cấu hình sẵn; Project CRUD/configuration, memory management và approval UX vẫn là phần UI v0.1 sau.

## Chạy local

```powershell
$env:LOREN_OWNER_PASSWORD='choose-a-local-owner-password'
$env:OLLAMA_API_KEY='your-provider-secret'
$env:LOREN_ENABLE_WRITES='false'
dotnet run --project src/Loren.Web/Loren.Web.csproj
```

Bash:

```bash
export LOREN_OWNER_PASSWORD='choose-a-local-owner-password'
export OLLAMA_API_KEY='your-provider-secret'
export LOREN_ENABLE_WRITES='false'
dotnet run --project src/Loren.Web/Loren.Web.csproj
```

Không commit secret thật. Hiện nên để `LOREN_ENABLE_WRITES=false`; set true cũng **không tự tạo mutation capability** trong Slice 1.

## Test

```powershell
dotnet restore Loren.slnx
dotnet build Loren.slnx --configuration Release --no-restore
dotnet test Loren.slnx --configuration Release --no-build --no-restore
```

Windows là first-class integration-test CI platform bên cạnh Ubuntu full gate.

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
- [`docs/handoff.md`](docs/handoff.md) — checkpoint ngắn để tiếp tục ở thread mới
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
