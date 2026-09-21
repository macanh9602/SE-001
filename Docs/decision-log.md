# Decision Log — SE-001

> Append-only. Decision mới đặt ở trên cùng. Project-level decision ghi ở đây; micro-decision ghi trong story implementation notes.

## D-003 — Giữ `Assets/_Core/4_Scripts` làm canonical script structure

Status: Accepted — 2026-09-22
Story: `handoff/story-000A-core-runtime-foundation/story.md`
Supersedes: D-001 code-root, assembly-placement và legacy-folder clauses

### Vấn đề

Story 000/000A đã hiểu “tách code mới khỏi legacy semantics” thành tạo cây `Assets/_Core/Scripts/SE001`, làm trùng framework và bỏ qua flow quen thuộc đã có trong `4_Scripts`.

### Chốt

- `Assets/_Core/4_Scripts` là canonical script structure của SE-001.
- Audit existing code theo từng file: generic code reuse trực tiếp sau khi đổi về namespace/contract SE001; CH013 gameplay-specific code replace hoặc adapt tại đúng owner.
- Không giữ class `SE001LevelManager` song song; lifecycle foundation nằm ở `System/Management/LevelManager.cs`.
- Không tạo `Assets/_Core/Scripts` cho code production mới. Nếu cần asmdef, đặt theo module trong canonical tree và chỉ khi dependency thực tế yêu cầu.

### Loại phương án nào, vì sao

Loại cây framework song song vì tăng coupling chuyển tiếp, tạo hai nơi tìm code và làm flow project khác cấu trúc quen thuộc mà không đem lại gameplay value.

### Đánh đổi đã chấp nhận

Một số file CH013 cùng nằm trong canonical tree trong thời gian migration; vì vậy mọi story phải audit theo file, không được coi toàn folder là reusable hoặc legacy.

### Hệ quả

Story 000/000A và canonical docs dùng `Assets/_Core/4_Scripts`; code foundation đã port về `System/Management` và Commons generic về `SE001.Commons`.

### Xem lại khi

Một module thật sự cần assembly isolation vì compile dependency, Editor/runtime separation hoặc build-time boundary; khi đó thêm asmdef tại chỗ, không dựng root song song.

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
- Code mới ban đầu được định hướng feature-scoped dưới `Assets/_Core/Scripts/SE001`; clause này đã bị D-003 supersede.
- `GameScene` là scene duy nhất cho bootstrap và gameplay; quyết định này được supersede chi tiết tại D-002.
- Physics authority là Simulation-driven, dimension 2D, determinism tolerance-based.
- Level JSON tại `Assets/_Core/Resources/Levels/` là source of truth; generated/runtime state không serialize.
- Low-end target là Redmi 9A; Creative mode để future story định nghĩa.

### Loại phương án nào, vì sao

- Loại bỏ Rigidbody-authoritative 2D/3D vì sand grid là gameplay authority.
- Việc loại `4_Scripts` khỏi source of truth đã bị D-003 supersede; guardrail còn hiệu lực là không resurrect CH013 gameplay semantics mù quáng.
- Loại bỏ việc serialize scene hierarchy/generated grid vì không đảm bảo authoring portability.

### Đánh đổi đã chấp nhận

Simulation và assembly boundaries cần thêm contract/bridge, nhưng giảm coupling và cho phép đo performance/quality riêng trên mobile.

### Hệ quả

Story 001 trở đi phải tuân thủ `SE001` namespace, feature assembly, JSON source-of-truth và Simulation semantic signals.

### Xem lại khi

Physics authority hoặc serialization contract thay đổi ở story cấp project; khi đó phải tạo decision mới và supersede entry này.
