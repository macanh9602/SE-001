# Prompt — Run Story 000A from current SE-001 state

> **SUPERSEDED / DO NOT RUN.** Chỉ giữ làm historical evidence. Dùng
> `handoff/story-001-architecture-foundation-alignment/story.md` cho execution hiện tại.

Paste nguyên khối dưới đây cho agent đang mở project SE-001:

```text
Tiếp tục project SE-001 từ state hiện tại.

Không bắt đầu Story 001.
Chạy Story 000A — Core Runtime Foundation.

SOURCE OF TRUTH:
`handoff/story-000A-core-runtime-foundation/story.md`

CURRENT STATE CẦN TÔN TRỌNG:
- Story 000 đã chốt canonical identity/architecture:
  - project `SE-001`
  - namespace `SE001`
  - Unity 6000.0.70f1 / URP
  - canonical code root là `Assets/_Core/4_Scripts`
  - asmdef chỉ thêm tại canonical tree khi dependency thực tế yêu cầu
  - `GameScene` là scene duy nhất cho bootstrap + gameplay
  - low-end target Redmi 9A
  - Simulation-driven 2D, tolerance-based determinism
- `Assets/_Core/Scripts/SE001/Bootstrap/` là nguồn port tạm, phải xóa sau khi port + compile.
- `Assets/_Core/4_Scripts` là canonical structure; generic code reuse, CH013 gameplay-specific code replace/adapt.
- Story 000 implementation notes hiện còn post-change Unity compile/console TODO vì MCP từng mất kết nối. Nếu editor/MCP đang active, capture baseline trước khi sửa 000A.
- Working tree có thay đổi Story 000 và `Assets/_Core/Scripts`, `Assets/_Core/Resources` untracked/pre-existing. Không cleanup/revert unrelated changes.

THỨ TỰ LÀM:
1. Đọc:
   - AGENTS.md
   - Docs/project-context.md
   - standards/system-design.md
   - Docs/runtime-architecture.md
   - Docs/data-model.md
   - handoff/story-000-project-contract-bootstrap/implementation-notes.html
   - handoff/ROADMAP.md
   - story 000A
2. Capture baseline compile/console nếu Unity connection available.
3. AUDIT trước khi code các file legacy được story liệt kê.
   Với từng module ghi một trong:
   REUSE DIRECT / ADAPT-PORT / REFERENCE ONLY / IGNORE.
4. Sau audit, port core runtime vào `Assets/_Core/4_Scripts/System/Management` và Commons generic vào `Assets/_Core/4_Scripts/Commons`.
5. Giữ flow quen thuộc:
   GameScene → Bootstrap → LevelManager → LevelContext/LevelRuntimeState → readiness gate → cleanup/reload.
6. LevelManager chỉ orchestration/lifecycle. Không sand rule, không win/lose, không progression sequence.
7. Không tạo dependency từ code mới `SE001.*` sang namespace `CH013.*`.
8. Direct reuse code cũ chỉ khi nó generic, gameplay-agnostic, compile-stable và assembly boundary sạch. Nếu việc này thay policy Story 000, ghi project decision canonical.
9. Smoke flow không được invent production level schema; dùng test/dev harness cho empty foundation context.
10. Vừa code vừa cập nhật `implementation-notes.html`.
11. Verify thật:
    - compile/console
    - begin/load → reload → unload
    - 10 cycles không tăng manager/context/root
    - no old-context callback writes into new context
    - no SE001 → CH013 dependency
    - git diff --check
    - final scoped diff

KHÔNG LÀM:
- Story 001 Powder Sand
- sand renderer/VFX
- draw path
- source/cup
- win/lose
- level editor
- progression/NextLevel ordering
- SDK
- final art polish
- cleanup legacy ngoài scope
- hỏi hoặc phụ thuộc `.zip` ngoài project

DECISION POLICY:
- Local + reversible + không đổi contract/gameplay/data/scope → tự quyết, ghi notes.
- Muốn thay project architecture/data/gameplay contract hoặc promote legacy semantics không rõ → DỪNG và hỏi tôi.

KHI XONG:
- Không tự chạy Story 001.
- ROADMAP chỉ update sau Story 000A closure PASS.
- Final report đúng format:
  1. Legacy audit summary
  2. Observable runtime result
  3. Files changed
  4. Verification evidence
  5. Semantic deviations
  6. Open risks/debt
  7. Final status: DONE / DOING / BLOCKED
```
