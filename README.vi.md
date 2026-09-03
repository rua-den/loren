# Loren

[English](README.md) · **Tiếng Việt**

Loren là một hệ thống trí tuệ cá nhân: một trợ lý sống lâu dài, có trí nhớ bền vững, quyền hạn rõ ràng, khả năng sử dụng công cụ, và về sau có thể chủ động hỗ trợ xuyên suốt đời sống số của chủ sở hữu.

Loren không được xây để trở thành một giao diện chat khác hay một bản sao agent framework. Project này sở hữu những phần làm cho Loren thực sự là *Loren* — identity, memory, world model cá nhân, permission, project context, action boundary và experience — còn model và hạ tầng thực thi được xem là những thành phần có thể thay thế.

## Định nghĩa ngắn gọn

> **Loren là một hệ thống trí tuệ cá nhân có trạng thái. Model là bộ não suy luận; Loren sở hữu identity, memory, context, policy, action boundary và lịch sử bao quanh bộ não đó.**

## Nguyên tắc sản phẩm

1. **Cá nhân, không generic** — Loren phải ngày càng hữu ích khi hiểu các preference, project, người, thiết bị và quyết định ổn định của chủ sở hữu.
2. **Memory-first** — state quan trọng phải sống qua conversation, đổi model, restart và đổi hạ tầng.
3. **Tool-first** — dùng tool/API có thẩm quyền cho dữ liệu và hành động thay vì để model đoán.
4. **Permission-first** — model có thể yêu cầu hành động; Loren mới là bên authorize và execute qua policy deterministic.
5. **Model-independent** — model provider là bộ não suy luận có thể thay thế, không phải identity của Loren.
6. **Auditable** — action, approval, memory mutation quan trọng và background work phải có thể reconstruct.
7. **Ưu tiên sở hữu local khi hợp lý** — personal state và secret nên nằm dưới quyền kiểm soát của chủ sở hữu khi có thể.
8. **Tự chủ tăng dần** — Loren bắt đầu theo yêu cầu của user và chỉ tăng background/proactive behavior sau khi trust boundary thấp hơn đã được chứng minh.

## Trạng thái hiện tại

**Cập nhật lần cuối: 2026-09-03**  
**Phase:** `v0.0 — Architecture / Feasibility`  
**Gate hiện tại:** `Gate B — v0.1 implementation stack`  
**Milestone hiện tại:** `M0 — ADR-002 technical validation`  
**Blocker hiện tại:** thiếu GitHub Actions repository secret `OPENAI_API_KEY`

Gate A đã hoàn tất: ADR-001 chốt Loren-owned core, còn model/runtime/MCP là adapter có thể thay thế.

Gate B đã hoàn thành toàn bộ phần implementation không cần secret. Evidence M0 hiện tại:

| Phần | Trạng thái |
| --- | --- |
| OpenAI brain-loop compile boundary | ✅ PASS |
| Automation cho live OpenAI proof | ✅ PASS |
| Trusted live trigger dùng được qua connector | ✅ PASS |
| Repository `OPENAI_API_KEY` | ❌ MISSING / BLOCKER |
| Live OpenAI Responses round trip | ⏳ OPEN |
| Live provider cancellation execution | ⏳ OPEN |
| MCP client + Loren gateway | ✅ PASS |
| SQLite + EF Core migration/recovery | ✅ PASS |
| ASP.NET Core + Blazor host | ✅ PASS |

Trusted live validation run #53 đã chạy từ one-shot branch tại đúng SHA của `main`. SHA/security guard pass, sau đó workflow phát hiện `OPENAI_API_KEY` rỗng và fail-closed đúng thiết kế. Không có live provider call nào được thực hiện.

Sau khi repository Actions secret tên chính xác `OPENAI_API_KEY` tồn tại, trusted validation phải chứng minh cả hai path:

```text
normal path:
OpenAI brain
  -> ActionRequest
  -> Loren ActionGateway
  -> structured fake result
  -> OpenAI brain
  -> final response
  -> PASS

cancellation path:
OpenAI provider call
  -> Loren cancellation token
  -> provider call bị cancel
  -> PASS
```

ADR-002 vẫn là **Proposed** cho tới khi cả hai live check pass. Chỉ sau đó mới bắt đầu scaffold production cho v0.1.

Chi tiết tiến độ chuẩn nằm tại [`docs/status.md`](docs/status.md).

## Mốc đầu tiên mày có thể test Loren như user

Preview đầu tiên cho owner được lên kế hoạch ở **v0.1 M2 — Walking Skeleton**:

```text
"Loren, check repo rua-den/loren."

UI
 -> Loren Runtime
 -> Brain
 -> github.read_repository ActionRequest
 -> Action Gateway
 -> GitHub read executor
 -> structured result
 -> Brain final response
 -> Audit
```

M1 là engineering foundation; M2 là milestone đầu tiên được thiết kế để có cảm giác đang thực sự dùng Loren chứ không chỉ test hạ tầng.

## Stack đề xuất cho v0.1

Chờ ADR-002 accept chính thức:

```text
C# 14 / .NET 10
ASP.NET Core
small Loren-owned agent loop
OpenAI Responses API làm brain đầu tiên
MCP C# SDK sau Loren adapter
SQLite + EF Core
Blazor Web App
```

## Lộ trình version

```text
v0.0  architecture / feasibility        <- hiện tại
v0.1  trustworthy core
v0.2  useful project assistant
v0.3  personal operations
v0.4  voice + device presence
v0.5  proactive/background Loren
v0.6+ hardening từ sử dụng thực tế
v1.0  stable personal daily driver
```

Version chỉ được nâng khi vượt exit gate, không dựa vào ngày tháng hay số lượng code.

## Kỷ luật cập nhật tiến độ

Tiến độ trong repo phải luôn đồng bộ với implementation.

Bất kỳ merge nào làm thay đổi capability, milestone completion, ADR status, dependency version đã validate hoặc next execution target đều phải cập nhật:

- [`docs/status.md`](docs/status.md) — trạng thái chi tiết chuẩn;
- [`README.md`](README.md) — bản tóm tắt tiếng Anh;
- `README.vi.md` — bản tóm tắt tiếng Việt;
- ADR/plan tương ứng khi decision hoặc milestone thay đổi.

Một milestone chưa được xem là đóng hoàn toàn nếu code/tests và documentation trong repo chưa nói cùng một trạng thái.

## Tài liệu

- [`docs/status.md`](docs/status.md) — tiến độ hiện tại và bước tiếp theo
- [`docs/vision.md`](docs/vision.md) — product vision và target experience
- [`docs/architecture.md`](docs/architecture.md) — system boundaries đang áp dụng
- [`docs/plans/master-plan.md`](docs/plans/master-plan.md) — milestones/version gates chuẩn
- [`docs/plans/v0.1.md`](docs/plans/v0.1.md) — implementation plan chi tiết cho trustworthy core
- [`docs/roadmap.md`](docs/roadmap.md) — capability roadmap ngắn gọn
- [`docs/research/agent-landscape.md`](docs/research/agent-landscape.md) — research ecosystem và cơ hội reuse
- [`docs/decisions/001-agent-runtime-strategy.md`](docs/decisions/001-agent-runtime-strategy.md) — Loren-owned core/runtime boundary đã accept
- [`docs/decisions/002-v0.1-technology-stack.md`](docs/decisions/002-v0.1-technology-stack.md) — stack v0.1 đề xuất và validation evidence
- [`docs/memory.md`](docs/memory.md) — memory model
- [`docs/permissions.md`](docs/permissions.md) — permission model
- [`docs/security.md`](docs/security.md) — security baseline
- [`docs/skills.md`](docs/skills.md) — skill/tool model

## Vai trò của repository

Repository này là source of truth cho product decisions, architecture, delivery plan, implementation, tiến độ hiện tại và release history của Loren.
