# Sand Stream Fix (B+) — Verification

Spec: `handoff/sand-stream-fix/SAND-STREAM-FIX.md` · Visualizer: `Docs/visualizers/sand-stream-lab.html` · Date: 2026-09-23

## Files changed

| File | Thay đổi |
|---|---|
| `Assets/_Core/4_Scripts/Simulation/Sand/SandSimulationProfile.cs` | + `airDrag` (0.5), header "Stream" |
| `Assets/_Core/4_Scripts/Simulation/Sand/SandSimulation.cs` | Nhánh airborne: bỏ lateral drift, `m *= airDrag`. XML summary cập nhật |
| `Assets/_Core/4_Scripts/Elements/Sand/SandFieldVisual.cs` | Overlay "Falling stream": streak + bridge + widen ±1 + grain texture trôi xuống; buffer cấp ở `Bind()` |
| `Assets/_Core/4_Scripts/Tests/Editor/SandSimulationTests.cs` | + `AirborneGrain_WithMomentum_FallsStraight`, + `StreamOverlay_PaintsOnlyEmptyNonGeometryCells_AndNeverTouchesState` |
| `Docs/decision-log.md` | + entry "Mở lại sand: stream rơi thẳng + render dòng (B+)" |

Không đổi: `maxFallCellsPerStep`, splash/roll/slide/dispersion trên mặt đất, level JSON, test cũ. Line ending từng file giữ nguyên.

## Evidence tự verify (ngoài Unity)

Máy agent không có Unity Editor → dùng `mcs` (Mono) + stub API Unity/NUnit tối thiểu.

1. **Type-check**: 5 file đã đổi + `SandSimulationState` + `LayoutMaskSet` compile sạch với stub → không lỗi cú pháp/type ở phía code game. Stub không kiểm chứng chữ ký API Unity thật (`GetPixelData`, `GetPropertyBlock`...) → cần compile trong Editor.
2. **Chạy thật logic sim** (Mono, không cần Unity):
   - `AirborneGrain_WithMomentum_FallsStraight`: PASS
   - `SandFallsWithoutEnteringInvalidOrStaticCells` (cũ): PASS
   - Determinism (2 lần cùng seed, cùng input): histogram giống hệt.
3. **So HEAD vs NEW** trên cùng scene ledge (1 gờ ngang 50 cell, source đổ lên giữa, probe dưới gờ 50 cell, 1500 step):

| | Emit được | Hạt airborne rơi trong ±2 cột quanh mép gờ |
|---|---|---|
| HEAD | 3610 | 44% (56% bắn tung) |
| NEW | 3325 (−8%) | **100%** |

## Deviation / open risk

- Tốc độ đổ giảm ~3–15% (ở đây −8%) vì hạt dưới miệng jar không còn splash ngang. Đã ghi trong decision-log.
- `PhaseCPlaythroughTests` **chưa chạy** (cần Unity). Quỹ đạo sand đổi → script stroke của level 02/03 có thể ra kết quả khác. Không sửa test/level để cho pass — báo lại để GD/Dev chốt.
- Test `StreamOverlay_...` đọc texture qua `MeshRenderer.GetPropertyBlock` → chỉ chạy được trong Unity EditMode.

## PENDING

- [ ] Unity: compile + console sạch
- [ ] EditMode: `SandSimulationTests` (3 test) + `PhaseCPlaythroughTests` — ghi kết quả vào đây
- [ ] Nếu cheap: `PhaseCPerfCapture` — `UpdateTexture` ms + GC alloc trước/sau
- [ ] PENDING MANUAL (Android): 3 level Phase C — dòng rơi thẳng, liền, có texture hạt; không hạt lạc vào cup sai; dòng dày 3 px không gây hiểu nhầm ở miệng cup
