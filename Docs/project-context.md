# Project Context — SE-001 Salt/Pepper Sand Clone

Version: 0.1
Stage: Prototype / Discovery
Primary platform: Mobile

## 1. Product context

| | |
|---|---|
| Internal name / code | `SE-001` |
| Namespace root | `SE001` |
| Class name prefix | Không dùng prefix |
| Genre | Mobile puzzle / physics-drawing |
| Gameplay reference | Salt & Pepper, Don't mix em up (`com.aa.dontmixemup`) |
| Product direction | Clone core gameplay; khác biệt có chủ đích là powder sand feel |
| Current phase | Bootstrap / technical discovery |
| Gameplay decision owner | Product Owner / GD |
| Level editor user | GD / Designer |
| Creative mode | Future mode; chưa có semantics hoặc quality contract trong Story 000 |

## 2. Source of truth

| Thứ | File |
|---|---|
| Project guardrail | `AGENTS.md` + file này |
| System architecture | `standards/system-design.md` |
| Game architecture | `Docs/runtime-architecture.md` |
| Data contract | `Docs/data-model.md` |
| Vocabulary | `Docs/glossary.md` |
| Scope / acceptance | `handoff/story-XXX-*/story.md` |
| Project decisions | `Docs/decision-log.md` |

## 3. Project facts

| Fact | Giá trị |
|---|---|
| Unity version | `6000.0.70f1` |
| Render pipeline | URP |
| Root scripts folder | `Assets/_Core/4_Scripts` |
| Existing script policy | Audit theo từng file: generic thì reuse, game-specific CH013 thì replace/adapt |
| Assembly convention | Canonical tree hiện compile trong project assembly; chỉ tách asmdef tại chỗ khi dependency thực tế yêu cầu |
| Existing module policy | Giữ structure quen thuộc; không resurrect CH013 gameplay semantics mù quáng |
| Level serialization | JSON (`JsonUtility`) tại `Assets/_Core/Resources/Levels/` |
| Async | UniTask |
| Tween | DOTween nếu assembly reference được; nếu không dùng UniTask + lerp |
| Pool | `VTLTools.ObjectPool` |
| Text | TextMeshPro qua prefab có script quản lý |
| Inspector | Odin |
| Test framework | Unity Test Framework |
| Low-end target | Redmi 9A |
| Frame budget | 60 fps mid / 30 fps low |
| Physics authority | Simulation-driven |
| Physics dimension | 2D board XY |
| Determinism | Tolerance-based; accounting/count phải deterministic |
| Unity MCP | Active session `SE-001@49dd5fcfcb6952e3` |
| Build scene flow | `GameScene` là scene duy nhất cho bootstrap và gameplay |

## 4. Development philosophy

- Authoring data là source of truth; generated cache và runtime state không được save thay thế authoring data.
- Simulation sở hữu pose/transition của sand; Domain nhận semantic signals, không query raw Physics.
- Tunable values nằm ở prefab/component, Profile ScriptableObject hoặc level JSON.
- Mobile performance guardrails theo `standards/performance-budget.md`.
- Không SDK, ads, analytics, IAP hoặc final art polish trong roadmap hiện tại.

## 5. Decisions đã chốt

### Gameplay

- Dùng core loop của reference game; khác biệt sản phẩm có chủ đích là powder sand feel.

### Data & authoring

- Level authoring dùng JSON `SE001LevelJson`; scene hierarchy, collider và generated grid không phải source of truth.

### Visual & feel

- ParticleSystem/VFX chỉ presentation; không quyết định sand logic, collection hoặc win/lose.
- Creative mode để future story định nghĩa, không ảnh hưởng Story 000 contract.

### Performance

- Redmi 9A là low-end measurement target; project default là 60 fps mid / 30 fps low.
- Simulation-driven 2D và tolerance-based determinism là contract nền.

## 6. Frontier collaboration

| Fact | Value |
|---|---|
| Frontier collaboration mode | AUTO |
| Frontier provider | codex-with-chatgpt |
| Readability sentinel | `AGENTS.md` |
| Identity sentinel | `Docs/project-context.md` |

Project chỉ lưu mode/provider/sentinels; không lưu connector name.

## 7. Canonical script structure

- `Assets/_Core/4_Scripts` là canonical script structure của project.
- Existing CH013 gameplay code được audit theo từng file: generic code thì reuse, game-specific code thì replace/adapt.
- Không tạo một framework tree song song dưới `Assets/_Core/Scripts`; module mới nằm trong structure canonical theo đúng owner.
