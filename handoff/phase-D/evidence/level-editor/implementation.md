# D-B Level Editor - Implementation evidence

Status: EXECUTED (2026-09-23)

## Changed files

| Area | Files | Why |
|---|---|---|
| Document and Undo host | `LevelEditorDocumentHost.cs`, `LevelEditorDocumentService.cs`, `LevelEditorStableIds.cs`, `LevelEditorState.cs` | Schema-3 document lifecycle, atomic save, stable IDs, serialized ViewState, Unity Undo host. |
| Editor window | `LevelEditorWindow*.cs`, `LevelEditorWindow.uss` | UI Toolkit split workspace, toolbar, Levels pane, Canvas, Inspector, validation, Save gating, D-C Play Test disabled. |
| Canvas and validation | `LevelEditorCanvas.cs`, `LevelEditorValidation.cs` | Baked-mask board preview, official `JarPreviewUtility` art, selection/drag preview, WHAT/WHERE/HOW issues. |
| Tests | `LevelEditorTests.cs` | Stable ID repair, schema-3 serialization, baked footprint, validation overlap coverage. |
| Production evidence | `handoff/phase-D/evidence/level-editor/*` | Functional, walkthrough, and UX-review evidence. |

## Architecture map

| Requirement | Implementation | Skill/recipe | Verification |
|---|---|---|---|
| Document/ViewState/DerivedState | `LevelEditorDocumentHost`, `LevelEditorViewState`, `LevelEditorDerivedState` | `document-view-derived-state` | Compile + targeted EditMode tests. |
| ApplyEdit != ReloadDocument | `LevelEditorWindow.ApplyEdit` records one Undo and refreshes derived state | `apply-edit-vs-reload` | UI field edit and placement repair observed. |
| Stable selection | Stable IDs are repaired on open and used by canvas/Inspector selection | `stable-selection-remap` | `StableIds_RepairMissingAndDuplicateIds` passed. |
| Responsive workspace | Nested `TwoPaneSplitView`; Levels pane hides at 640 px and returns at 1100 px | `split-workspace-uitk` | Live Editor measurement: 640 -> `levelsDisplay=None`, 1100 -> `Flex`. |
| Validation focus | Severity rows show WHAT/WHERE/HOW and Inspector fields are scroll targets | `validation-focus-flow` | Overlap blocker visible in live UI; Inspector fix clears it. |
| Layout ownership | Level Editor reads `LayoutDefinition.TryBuildMaskSet`; Layout Bake remains the write owner | Phase D shared spec | 3/3 targeted D0.5 regression tests passed. |
| Official jar art | Canvas uses `JarPreviewUtility.GetPreview` and profile-derived dimensions | Phase V1 contract | Live canvas showed Source/Cup art with color selection. |

## Known closure limits

Save/reopen round-trip, malformed-file recovery, dirty-close, and pointer-drag/Undo walkthrough rows remain explicitly unverified in the current Editor session. They are not converted to PASS.
