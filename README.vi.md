# Loren

[English](README.md) · **Tiếng Việt**

Loren là một hệ thống trí tuệ cá nhân sống lâu dài, có memory bền vững, permission rõ ràng, khả năng dùng tool và về sau có thể chủ động hỗ trợ xuyên suốt đời sống số của chủ sở hữu.

> **Model chỉ là compute có thể thay thế. Loren sở hữu identity, memory, context, policy, approval, action boundary và lịch sử.**

## Nguyên tắc cốt lõi

1. **Memory-first** — state bền vững sống qua conversation, restart và đổi provider.
2. **Tool-first** — dữ liệu/action bên ngoài đi qua tool có thẩm quyền thay vì để model đoán.
3. **Permission-first** — model có thể request action; Loren mới authorize và execute.
4. **Model-independent** — model provider là adapter có thể thay thế.
5. **Auditable** — hành vi quan trọng phải reconstruct được.
6. **Tự chủ tăng dần** — background/proactive behavior chỉ đến sau khi trust boundary thấp hơn đã được chứng minh.

## Trạng thái hiện tại

**Cập nhật:** 2026-09-06  
**Phase:** `v0.1 — Trustworthy Core development`  
**Milestone đã hoàn tất:** `M4 — Trusted Durable Memory`  
**Decision gates đã pass:** `Gate A`, `Gate B`, `Gate C`, `Gate D / ADR-004`  
**Milestone hiện tại:** `M5 — Action/Credential Boundary + Narrow GitHub Writes`  
**M5 slices đã hoàn tất:** `Slice 1 — policy/approval/read-only`, `Slice 2 — credential isolation/revocation/redaction`  
**Checkpoint hiện tại:** `Slice 3 — explicit-owner verified create-non-default-branch`

Chi tiết chuẩn: [`docs/status.md`](docs/status.md). Checkpoint để mở thread mới: [`docs/handoff.md`](docs/handoff.md).

## Gate D / ADR-004 [PASSED]

Trust boundary cho write:

```text
brain request write
 -> resolve canonical target
 -> deterministic policy / global read-only
 -> explicit exact owner approval
 -> atomic one-time consume / chống replay
 -> write-specific credential resolver
 -> trusted controlled executor
 -> independent post-write verification
 -> correlated redacted audit
```

Authentication chỉ chứng minh owner identity; **không phải write approval**. Model/external content không thể tự tạo/broaden approval, chọn credential, tắt read-only, đổi canonical repository hay tự tuyên bố write đã verify.

Mutation scope v0.1 cho phép:

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

## M5 Slice 1 — policy + one-time approval [HOÀN TẤT]

PR #25 merge tại `caa65fbbd7c3828b68aa198dad625e73e9c096b4`.

```text
frozen PR head: c9bfb9f82b70963c196a689d4b0be2feb9bfedb5
PR CI #194 / 33973579862: Ubuntu full gate PASS + Windows integration PASS
post-merge main CI #195 / 33973694524: Ubuntu full gate PASS + Windows integration PASS
```

Các invariant chính:

- typed `ActionAccessClass`;
- trusted `ActionAuthorizationContext` nằm ngoài model-visible arguments;
- proposed/trusted target được freeze chống TOCTOU;
- deterministic SHA-256 exact-intent fingerprint;
- SQLite-backed `ActionApproval` / `IActionApprovalStore`;
- mọi non-read action đều cần approval kể cả khi policy lỡ trả `Allow`;
- check executor tồn tại trước khi consume approval;
- consume atomically đúng một lần ngay trước consequential executor attempt;
- missing/expired/revoked/mismatch/replay đều fail closed;
- text `approvalId` do model đưa vào không có authority;
- `LOREN_ENABLE_WRITES` mặc định read-only.

Approval chủ ý được consume trước first consequential executor attempt. Retry sau failure/ambiguity cần approval mới.

## M5 Slice 2 — credential boundary [HOÀN TẤT]

PR #26 merge tại `f7fb36bae324dbd7bb8d12e02daf3fe0dd98e7da`.

```text
frozen PR head: e9e2b07378e1435e62e6090829619603ac7df42b
PR CI #201 / 34027113298: Ubuntu full gate PASS + Windows integration PASS
post-merge main CI #202 / 34027255592: Ubuntu full gate PASS + Windows integration PASS
```

Các invariant chính:

- provider-neutral `CredentialPurpose` / `CredentialReference`;
- GitHub write identity riêng `github.write / github.write.local-v0.1`;
- local secret contract `GITHUB_WRITE_TOKEN`;
- `LOREN_GITHUB_WRITE_CREDENTIAL_REVOKED=true` thắng intent đã approval;
- malformed revocation state fail closed;
- không fallback sang `OLLAMA_API_KEY`, read credential hay token rộng hơn;
- secret chỉ materialize bên trong credential-bound executor callback;
- result/exception được redact trước gateway/audit/brain.

## M5 Slice 3 — verified create branch [CHECKPOINT HIỆN TẠI]

Real mutation đầu tiên được giữ cực hẹp:

```text
authenticated owner
 -> explicit “Approve & create branch”
 -> resolve canonical Project + GitHub Repository
 -> freeze exact branch + existing 40-char source SHA
 -> tạo exact ActionApproval 5 phút
 -> policy + trusted-executor check
 -> fingerprint + atomic consume
 -> resolve github.write credential
 -> GET repository/default branch preflight
 -> reject default/unsafe branch
 -> POST git/refs
 -> GET exact created ref
 -> verified SHA phải == approved source SHA
 -> trả redacted result + audit
```

Slice 3 thêm `ITrustedActionExecutor`: non-read executor phải nhận Loren-owned `ActionExecutionRequest`, không chỉ model-visible `ActionRequest`. Legacy non-read executor bị reject trước khi approval bị burn. GitHub owner/repository dùng khi write luôn lấy từ `ActionAuthorizationContext.RepositoryLocator`, không lấy từ model text.

Owner console có thêm:

- form bootstrap canonical GitHub Project/Repository cho database local mới;
- form explicit **Approve & create branch**;
- read/chat console và audit display cũ vẫn giữ.

Acceptance deterministic cover request order, Git ref safety, default-branch rejection, exact SHA validation, verification mismatch, secret redaction, owner approval consume và revoked-credential không tạo HTTP write.

## Canonical storage

```text
database file: loren.db
default directory: OS local application data / Loren
override: LOREN_DATA_DIRECTORY
migrations: tự chạy khi host start
```

## Chạy local — read-only

PowerShell:

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

## Chạy checkpoint write đầu tiên

Chỉ bật khi chủ động muốn test tạo branch trên repo đã cấu hình.

PowerShell:

```powershell
$env:LOREN_OWNER_PASSWORD='choose-a-local-owner-password'
$env:LOREN_ENABLE_WRITES='true'
$env:GITHUB_WRITE_TOKEN='your-write-token'
$env:LOREN_GITHUB_WRITE_CREDENTIAL_REVOKED='false'
dotnet run --project src/Loren.Web/Loren.Web.csproj
```

Bash:

```bash
export LOREN_OWNER_PASSWORD='choose-a-local-owner-password'
export LOREN_ENABLE_WRITES='true'
export GITHUB_WRITE_TOKEN='your-write-token'
export LOREN_GITHUB_WRITE_CREDENTIAL_REVOKED='false'
dotnet run --project src/Loren.Web/Loren.Web.csproj
```

Sau đó login owner console, bootstrap canonical repo nếu DB trống, nhập **existing exact 40-character source commit SHA**, chọn **branch mới không phải default branch**, review confirmation rồi nhấn **Approve & create branch**.

Không commit secret thật. `OLLAMA_API_KEY` và `GITHUB_WRITE_TOKEN` cố ý là hai credential tách biệt.

## Test

```powershell
dotnet restore Loren.slnx
dotnet build Loren.slnx --configuration Release --no-restore
dotnet test Loren.slnx --configuration Release --no-build --no-restore
```

Windows là first-class integration-test CI platform bên cạnh Ubuntu full gate.

## M5 target tiếp theo

Sau khi create-branch checkpoint xanh trên `main`:

```text
controlled file/commit path trên approved non-default branch
 -> bind exact path/content/branch intent
 -> cấm default-branch write
 -> verify commit SHA + branch ref + content identity
```

Open-PR capability chỉ tới sau slice file/commit này.

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
- [`docs/permissions.md`](docs/permissions.md) — permission/approval baseline
- [`docs/security.md`](docs/security.md) — security baseline
- [`docs/plans/master-plan.md`](docs/plans/master-plan.md) — milestones/version gates
- [`docs/plans/v0.1.md`](docs/plans/v0.1.md) — plan implementation chi tiết v0.1
- [`docs/decisions/004-action-approval-and-credential-boundary.md`](docs/decisions/004-action-approval-and-credential-boundary.md)

Repository này là source of truth cho product decisions, architecture, delivery plan, implementation, progress và release history của Loren.
