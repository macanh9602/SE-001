# Decision Log — SE-001

> Append-only. Decision mới đặt ở trên cùng. Project-level decision ghi ở đây; micro-decision ghi trong story implementation notes.

## D-002 — Dùng GameScene duy nhất cho bootstrap và gameplay

Status: Accepted — 2026-09-22
Story: `handoff/story-000-project-contract-bootstrap/story.md`
Supersedes: D-001 scene-flow clause

### Vấn đề

User chốt dùng `GameScene` thay vì tách `MainScene` bootstrap và `GameScene` gameplay.

### Chốt

`GameScene` là scene duy nhất trong Build Settings và sở hữu cả bootstrap entry cùng gameplay lifecycle.

### Loại phương án nào, vì sao

Loại bỏ scene bootstrap riêng để giữ flow hiện tại đơn giản và không tạo thêm scene ownership không cần thiết.

### Đánh đổi đã chấp nhận

Bootstrap code và gameplay code cùng scene boundary; các layer vẫn phải giữ ownership tách biệt trong component/module contract.

### Hệ quả

Các story sau tham chiếu `GameScene` cho bootstrap/load level; không tạo hoặc yêu cầu `MainScene`.

### Xem lại khi

Có yêu cầu meta flow hoặc scene loading độc lập làm thay đổi project-level scene ownership.

## D-001 — SE-001 dùng Simulation-driven 2D và feature-scoped contract

Status: Accepted — 2026-09-22
Story: `handoff/story-000-project-contract-bootstrap/story.md`

### Vấn đề

Story sau cần identity, scene ownership, physics authority và data source of truth nhất quán trước khi thêm sand runtime.

### Chốt

- Internal code: `SE-001`.
- Namespace root: `SE001`, không dùng class prefix.
- Unity `6000.0.70f1`, URP, mobile.
- Code mới dùng feature-scoped `Runtime` / `Editor` / `Tests` assemblies dưới `Assets/_Core/Scripts/SE001`.
- `GameScene` là scene duy nhất cho bootstrap và gameplay; quyết định này được supersede chi tiết tại D-002.
- Physics authority là Simulation-driven, dimension 2D, determinism tolerance-based.
- Level JSON tại `Assets/_Core/Resources/Levels/` là source of truth; generated/runtime state không serialize.
- Low-end target là Redmi 9A; Creative mode để future story định nghĩa.

### Loại phương án nào, vì sao

- Loại bỏ Rigidbody-authoritative 2D/3D vì sand grid là gameplay authority.
- Loại bỏ legacy `4_Scripts` làm source of truth để tránh resurrect template gameplay.
- Loại bỏ việc serialize scene hierarchy/generated grid vì không đảm bảo authoring portability.

### Đánh đổi đã chấp nhận

Simulation và assembly boundaries cần thêm contract/bridge, nhưng giảm coupling và cho phép đo performance/quality riêng trên mobile.

### Hệ quả

Story 001 trở đi phải tuân thủ `SE001` namespace, feature assembly, JSON source-of-truth và Simulation semantic signals.

### Xem lại khi

Physics authority hoặc serialization contract thay đổi ở story cấp project; khi đó phải tạo decision mới và supersede entry này.
