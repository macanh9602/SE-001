# Bowl measurement evidence

Measurement source: `Assets/_Core/0_Texture2D/Env/T_Bowl_Main.png` and `T_Bowl_Spec.png`.

| Item | Measured/baked value |
|---|---:|
| Main texture | 352 × 164 px |
| Spec texture | 312 × 144 px |
| Main alpha bbox, threshold 64 | x=1..351, y=0..163 (top-origin inspection) |
| Spec alpha bbox, threshold 64 | x=5..307, y=10..138 (top-origin inspection) |
| Bowl rim | profile row 130 (Unity bottom-origin; approximately row 33 top-origin) |
| Baked wall thickness | 10.56 px |
| Main native aspect | 352 / 164 = 2.1463 |
| Default world size | 3.52 × 1.64 world units |
| Spec canvas offset | 20 × 10 px, centered against the main canvas |

The generated `BowlVisualProfile.asset` stores the outer/inner row spans and contours from the Unity texture pixels. Runtime Bowl geometry, editor footprint validation, preview rendering and the `View/{Shadow,Main,Spec}` presentation all consume this profile. Bowl width is global/profile-driven; no level JSON or schema field was added.

The seven Bowl tint materials reuse the existing V1 color IDs: Red, Cobalt, Yellow, Purple, Pink, Green and White. Spec and shadow remain shared materials.
