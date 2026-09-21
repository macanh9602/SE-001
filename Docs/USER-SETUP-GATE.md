# USER SETUP GATE — trước Story 000/001

Project: `SE-001` (workspace hiện tại)  
Reference gameplay: **Salt & Pepper, Don't mix em up** (`com.aa.dontmixemup`)  
Product direction: clone core gameplay; **khác biệt chủ đích duy nhất là sand feel**.  
Excluded toàn roadmap này: **SDK / ads / analytics / IAP** và **final art polish**.

## A. Bạn cần chốt trước khi agent chạy production story

| Contract | Recommend | Vì sao |
|---|---|---|
| Internal code | `SE-001` | Đã trùng workspace hiện tại |
| Namespace root | `SE001` | Ngắn, không dính project cũ |
| Class prefix | không dùng prefix hoặc `SE` nếu team bắt buộc | Namespace đã đủ phân tách; tránh tên dài |
| Assembly strategy | feature-scoped Runtime / Editor / Tests | Khớp template contract; Sand/Editor dễ cô lập |
| Physics authority | **Simulation-driven** | Sand grid quyết pose/chuyển động |
| Physics dimension | **2D** | Gameplay trên board XY; không dùng Rigidbody làm authority |
| Determinism | **Tolerance-based** | Không cần bit-exact replay; cần accounting/count deterministic |
| Low-end target device | **BẮT BUỘC chọn model thật** | Story 001 + 014 cần số đo thật |
| Frame target | 60 fps mid / 30 fps low | Đang là project default |
| Scene ownership | `MainScene` = bootstrap, `GameScene` = gameplay | Tránh nhét bootstrap rule vào gameplay scene |
| Gameplay start | source bắt đầu sau `pourStartDelay`; player được vẽ từ frame đầu và khi đang pour | Clone flow, vẫn cho tune delay |

Nếu không muốn đổi, chỉ cần xác nhận các recommend trên và cung cấp **tên máy low-end target**.

## B. Trước khi bắt đầu code

- [ ] Unity Editor đúng `6000.0.70f1`.
- [ ] Unity MCP package đã cài và session đang active.
- [ ] Baseline project compile sạch trước Story 000.
- [ ] Commit/stash các thay đổi có sẵn không thuộc roadmap. Hiện audit thấy `GameScene` + `Docs/visualizers/` là pre-existing; không để story sau vô tình nhận ownership.
- [ ] Chọn low-end Android device thật để đo.
- [ ] Không import `Assets(2).zip` hay bất kỳ Sand package ngoài nào.

## C. Agent sẽ tự setup, bạn KHÔNG cần làm tay

- Namespace / folder / asmdef theo contract đã chốt.
- `SandFeelProfile`, `SandRuntimeProfile`, prefab/material/shader placeholder.
- Level JSON schema và `Resources/Levels/`.
- Test scenes/harness.
- Sand simulation code từ spec trong story.
- Dynamic obstacle rasterization.
- Stream / interaction VFX.
- Cups, source, runtime state, gameplay rules.
- Level editor.
- Save/progression.
- Profiling/quality tiers.

## D. Sand source specification — self-contained, không phụ thuộc `.zip`

Production implementation phải xây từ các behavior sau, không được yêu cầu legacy file:

1. Falling-sand grid dùng persistent native buffers; `type=0` là empty, material id khác 0.
2. Burst `IJob`/equivalent deterministic single-writer step; scan bottom-up, đảo chiều trái/phải giữa step để giảm bias.
3. Mỗi logical grain mang tối thiểu:
   - material/type
   - shade/noise seed
   - vertical fall velocity
   - horizontal momentum
   - optional stable ticks
4. Separate masks:
   - valid board cells
   - static obstacle cells
   - player-drawn obstacle cells
5. Powder baseline từ `Docs/visualizers/sand-feel-lab.html`:
   - visual grain scale tương đương `1.5 px`
   - gravity acceleration `0.30`
   - max fall `4 cells/step`
   - diagonal/repose chance `1.00`
   - momentum retention `0.95`
   - impact-to-horizontal `0.50`
   - surface flow chance `0.14`
   - color jitter `0.35`
   - surface highlight `0.30`
   - sparkle off
   - soft/bilinear render
   - source rate baseline `16 logical grains/frame-equivalent`
   - stream width baseline `6 cells`
   - simulation baseline `1 substep/frame-equivalent`
6. Logical grain != visual crumb. Visual stream/VFX có thể vẽ nhiều crumb cho một logical grain nhưng không tăng gameplay quantity.
7. Rendering ưu tiên state texture + shader palette trên **1 sand quad**; không GameObject/Rigidbody per grain.
8. Player line rasterize trực tiếp vào grid obstacle mask. Không tạo collider per cell.
9. Sand accounting phải bảo toàn count: `emitted = inField + collected + spilled/lost + pending`.
10. ParticleSystem chỉ presentation. Không quyết định sand logic, collection, win/lose.

