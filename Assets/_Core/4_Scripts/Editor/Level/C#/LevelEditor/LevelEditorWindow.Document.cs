#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using SE001.Data;
using SE001.Simulation.Sand;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SE001.Editor.Level
{
    public sealed partial class LevelEditorWindow
    {
        private void NewDocument()
        {
            if (!ConfirmDiscardIfDirty("Create a new level?")) return;
            documentHost.InitializeNew(LevelEditorDocumentService.NextLevelId(LevelSequence));
            viewState.selectedStableId = string.Empty;
            viewState.selectionKind = LevelEditorSelectionKind.None;
            derivedState.Clear();
            SetWorkspaceTab(false);
            RefreshAll();
        }

        private void OpenDocument()
        {
            if (!ConfirmDiscardIfDirty("Open another level?")) return;
            string start = LevelEditorDocumentService.ToAbsolutePath(LevelEditorDocumentService.LevelsFolder);
            string absolutePath = EditorUtility.OpenFilePanel("Open Level", start, "json");
            if (string.IsNullOrWhiteSpace(absolutePath)) return;
            SE001LevelJson level;
            string projectPath;
            bool upgraded;
            string error;
            if (!LevelEditorDocumentService.TryOpen(absolutePath, out level, out projectPath, out upgraded, out error))
            {
                ShowNotification(new GUIContent("Open failed: " + error));
                return;
            }

            ReplaceOpenDocument(level, projectPath, upgraded);
        }

        private void OpenSavedLevel(string projectPath)
        {
            if (!ConfirmDiscardIfDirty("Open another level?")) return;
            SE001LevelJson level;
            bool upgraded;
            string error;
            if (!LevelEditorDocumentService.TryOpenProjectPath(projectPath, out level, out upgraded, out error))
            {
                ShowNotification(new GUIContent("Open failed: " + error));
                RefreshLayoutList();
                return;
            }

            ReplaceOpenDocument(level, projectPath, upgraded);
        }

        private void ReplaceOpenDocument(SE001LevelJson level, string projectPath, bool upgraded)
        {
            if (level == null) return;

            int repaired = LevelEditorStableIds.NormalizeInMemory(level);
            documentHost.ReplaceDocument(level, projectPath, upgraded || repaired > 0);
            viewState.selectedStableId = string.Empty;
            viewState.selectionKind = LevelEditorSelectionKind.None;
            derivedState.Clear();
            SetWorkspaceTab(false);
            RefreshAll();
        }

        private void DeleteSavedLevel(LevelEditorDocumentService.SavedLevelDescriptor descriptor)
        {
            if (descriptor == null) return;
            if (!ConfirmDiscardIfDirty("Delete saved level?")) return;

            string displayName = string.IsNullOrWhiteSpace(descriptor.DisplayName)
                ? descriptor.ProjectPath : descriptor.DisplayName;
            bool confirmed = EditorUtility.DisplayDialog(
                "Delete Level",
                "Delete saved level '" + displayName + "'? This removes only its JSON asset. Referenced layouts and other assets stay unchanged.",
                "Delete",
                "Cancel");
            if (!confirmed) return;

            string deletedLevelId;
            string error;
            if (!LevelEditorDocumentService.TryDeleteSavedLevel(
                    descriptor.ProjectPath, descriptor.LevelId, LevelSequence, out deletedLevelId, out error))
            {
                ShowNotification(new GUIContent("Delete failed: " + error));
                RefreshLayoutList();
                return;
            }

            bool deletedCurrent = documentHost != null &&
                string.Equals(documentHost.CurrentPath, descriptor.ProjectPath, global::System.StringComparison.OrdinalIgnoreCase);
            if (deletedCurrent)
            {
                documentHost.ClearDocument();
                viewState.selectedStableId = string.Empty;
                viewState.selectionKind = LevelEditorSelectionKind.None;
                derivedState.Clear();
            }

            RefreshAll();
            ShowNotification(new GUIContent("Deleted saved level '" + displayName + "'."));
        }

        private void SaveDocument()
        {
            if (Document == null) return;
            if (HasBlockingIssues())
            {
                ShowNotification(new GUIContent("Save blocked. Fix the first blocking issue shown in Validation."));
                return;
            }
            if (string.IsNullOrWhiteSpace(documentHost.CurrentPath)) SaveDocumentAs();
            else SaveTo(documentHost.CurrentPath);
        }

        private void SaveDocumentAs()
        {
            if (Document == null) return;
            if (HasBlockingIssues())
            {
                ShowNotification(new GUIContent("Save blocked. Fix the first blocking issue shown in Validation."));
                return;
            }
            string fileName = documentHost.Level.levelId;
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Level", fileName, "json", "Save the schema-5 level JSON.", LevelEditorDocumentService.LevelsFolder);
            if (!string.IsNullOrWhiteSpace(path)) SaveTo(path);
        }

        private void SaveTo(string projectPath)
        {
            PhaseCLevelSequence sequence = LevelSequence;
            if (sequence == null)
            {
                ShowNotification(new GUIContent("Save failed: Level Sequence profile is missing."));
                return;
            }
            string expectedFileName = documentHost.Level.levelId + ".json";
            if (!string.Equals(Path.GetFileName(projectPath), expectedFileName, global::System.StringComparison.OrdinalIgnoreCase))
            {
                ShowNotification(new GUIContent("Save failed: use " + expectedFileName + " so the level can load in game."));
                return;
            }
            string error;
            if (!LevelEditorDocumentService.TrySave(documentHost.Level, projectPath, out error))
            {
                ShowNotification(new GUIContent("Save failed: " + error));
                return;
            }
            bool addedToSequence = LevelEditorDocumentService.AddToSequenceIfMissing(sequence, documentHost.Level.levelId);
            if (addedToSequence)
            {
                EditorUtility.SetDirty(sequence);
                AssetDatabase.SaveAssets();
            }
            documentHost.MarkSaved(projectPath);
            if (addedToSequence) RefreshInspector();
            RefreshDocumentStatus();
            RefreshValidation();
        }

        private void StartPlayTest()
        {
            string blocker = GetPlayTestBlocker();
            if (blocker != null)
            {
                ShowNotification(new GUIContent(blocker));
                return;
            }

            try
            {
                LevelPlayTestOverride.Set(Document.levelId, Document.ToJson(false));
                EditorApplication.isPlaying = true;
            }
            catch (global::System.Exception exception)
            {
                LevelPlayTestOverride.Clear();
                ShowNotification(new GUIContent("Play Test could not start: " + exception.Message));
            }
        }

        private string GetPlayTestBlocker()
        {
            if (Document == null) return "Create or open a level before Play Test.";
            if (EditorApplication.isPlayingOrWillChangePlaymode) return "Exit Play Mode before starting Play Test.";
            if (EditorApplication.isCompiling) return "Wait until Unity finishes compiling before Play Test.";

            for (int i = 0; i < derivedState.Issues.Count; i++)
            {
                LevelEditorIssue issue = derivedState.Issues[i];
                if (issue.Severity == LevelEditorIssueSeverity.Blocking)
                    return "Play Test blocked: " + issue.What + " " + issue.Where + " " + issue.How;
            }

            ColorProfile colors = Resources.Load<ColorProfile>("Profiles/PhaseCColorProfile");
            SandSimulationProfile sand = Resources.Load<SandSimulationProfile>("Profiles/PhaseBSandSimulationProfile");
            if (colors == null || sand == null)
                return "Play Test blocked: required gameplay profiles are missing.";

            List<string> runtimeErrors = new List<string>();
            if (!LevelDataValidator.TryValidate(Document, colors, sand.cellSize, sand.maxCells,
                    derivedState.Layout, runtimeErrors))
            {
                string first = runtimeErrors.Count > 0 ? runtimeErrors[0] : "the level is not valid for runtime.";
                return "Play Test blocked: " + first;
            }

            return null;
        }

        private void RefreshPlayTestState()
        {
            if (playTestButton == null) return;
            string blocker = GetPlayTestBlocker();
            playTestButton.SetEnabled(blocker == null);
            playTestButton.tooltip = blocker ?? "Play the current level, including unsaved changes.";
        }

        private bool ConfirmDiscardIfDirty(string title)
        {
            if (!documentHost.IsDirty) return true;
            return EditorUtility.DisplayDialog(title,
                "The current level has unsaved changes. Continue and discard them?", "Discard", "Cancel");
        }

        private void AddSource()
        {
            if (derivedState.Layout == null || derivedState.Masks == null)
            {
                ShowNotification(new GUIContent("Choose a Ready layout before adding a Source."));
                return;
            }
            Vector2 size = LevelEditorGeometry.SourceVisualSize(Document);
            Vector2 position = FindOpenCenter(size, false, LevelEditorGeometry.SourceVisualOffset(null, Document));
            string id = LevelEditorStableIds.Create("source");
            ApplyEdit(level => level.sources.Add(new SourceData
            {
                stableId = id,
                materialId = 1,
                position = position,
                logicalAmount = 10,
                startsOpen = false
            }), "Add Source");
            SelectEntity(id, LevelEditorSelectionKind.Source);
        }

        private void AddCup()
        {
            if (derivedState.Layout == null || derivedState.Masks == null)
            {
                ShowNotification(new GUIContent("Choose a Ready layout before adding a Cup."));
                return;
            }
            CupProfile profile = Resources.Load<CupProfile>("Profiles/PhaseCCupProfile");
            Vector2 size = LevelEditorGeometry.CupSize(null, Document);
            Vector2 position = FindOpenCenter(size, false, LevelEditorGeometry.CupVisualOffset(Document));
            string id = LevelEditorStableIds.Create("cup");
            ApplyEdit(level => level.cups.Add(new CupData
            {
                stableId = id,
                acceptedMaterialId = 1,
                position = position,
                requiredAmount = 5
            }), "Add Cup");
            SelectEntity(id, LevelEditorSelectionKind.Cup);
        }

        private void AddRotatingObstacle()
        {
            if (derivedState.Layout == null || derivedState.Masks == null)
            {
                ShowNotification(new GUIContent("Choose a Ready layout before adding a Rotating Obstacle."));
                return;
            }
            string id = LevelEditorStableIds.Create("rotating_obstacle");
            RotatingObstacleData obstacle = new RotatingObstacleData
            {
                stableId = id,
                scale = 1f,
                barLength = 4f,
                initialAngle = 0f,
                degreesPerSecond = 60f
            };
            obstacle.position = FindOpenCenter(LevelEditorGeometry.RotatingSize(obstacle), true);
            ApplyEdit(level => level.rotatingObstacles.Add(obstacle), "Add Rotating Obstacle");
            SelectEntity(id, LevelEditorSelectionKind.RotatingObstacle);
        }

        private Vector2 FindOpenCenter(Vector2 size, bool rotating = false,
            Vector2 visualOffset = default(Vector2))
        {
            Vector2 center = Document.board.size * 0.5f;
            if (IsPlacementOpen(center, size) &&
                LevelEditorGeometry.InBoard(center, size, Document.board.size) &&
                PlacementFootprintOpen(center, size, rotating))
                return center - visualOffset;
            for (int y = 1; y < 10; y++)
            for (int x = 1; x < 10; x++)
            {
                Vector2 candidate = new Vector2(Document.board.size.x * x / 10f, Document.board.size.y * y / 10f);
                if (LevelEditorGeometry.InBoard(candidate, size, Document.board.size) &&
                    PlacementFootprintOpen(candidate, size, rotating) &&
                    IsPlacementOpen(candidate, size)) return candidate - visualOffset;
            }
            return center - visualOffset;
        }

        private bool PlacementFootprintOpen(Vector2 position, Vector2 size, bool rotating)
        {
            return rotating
                ? LevelEditorGeometry.RotatingFootprintOpen(derivedState.Masks, position,
                    size.x * 0.5f, derivedState.CellSize)
                : LevelEditorGeometry.FootprintOpen(derivedState.Masks, position, size,
                    derivedState.CellSize);
        }

        private bool IsPlacementOpen(Vector2 position, Vector2 size)
        {
            for (int i = 0; i < Document.sources.Count; i++)
            {
                SourceData source = Document.sources[i];
                if (source != null && LevelEditorGeometry.Overlaps(
                        position, size, source.position + LevelEditorGeometry.SourceVisualOffset(source, Document),
                        LevelEditorGeometry.SourceVisualSize(Document))) return false;
            }
            for (int i = 0; i < Document.cups.Count; i++)
            {
                CupData cup = Document.cups[i];
                if (cup != null && LevelEditorGeometry.Overlaps(
                        position, size, cup.position + LevelEditorGeometry.CupVisualOffset(Document),
                        LevelEditorGeometry.CupSize(cup, Document))) return false;
            }
            return true;
        }

        private void DeleteSelected()
        {
            if (viewState.selectionKind == LevelEditorSelectionKind.None) return;
            string id = viewState.selectedStableId;
            ApplyEdit(level =>
            {
                if (viewState.selectionKind == LevelEditorSelectionKind.Source)
                    level.sources.RemoveAll(value => value != null && value.stableId == id);
                if (viewState.selectionKind == LevelEditorSelectionKind.Cup)
                    level.cups.RemoveAll(value => value != null && value.stableId == id);
                if (viewState.selectionKind == LevelEditorSelectionKind.RotatingObstacle)
                    level.rotatingObstacles.RemoveAll(value => value != null && value.stableId == id);
            }, "Delete " + viewState.selectionKind);
            SelectEntity(string.Empty, LevelEditorSelectionKind.None);
        }

        private void SelectLayout(string layoutId)
        {
            LayoutBakeEntry entry = layoutLibrary.Find(layoutId);
            if (entry == null || entry.Status.State != LayoutBakeState.Ready)
            {
                ShowNotification(new GUIContent("Choose a Ready layout. Rebake stale layouts in Layout Bake."));
                return;
            }
            ApplyEdit(level =>
            {
                level.layoutId = layoutId;
                level.board.size = entry.Definition.boardSize;
            }, "Choose Layout");
        }

        private void OpenLayoutBake()
        {
            EditorApplication.ExecuteMenuItem("SE001/Layout Bake");
        }

        private void SelectIssue(LevelEditorIssue issue)
        {
            if (issue == null) return;
            LevelEditorSelectionKind kind = FindSelectionKind(issue.StableId);
            SelectEntity(issue.StableId, kind);
            VisualElement field;
            if (!string.IsNullOrWhiteSpace(issue.FieldKey) && inspectorFields.TryGetValue(issue.FieldKey, out field))
                inspectorPane.ScrollTo(field);
        }

        private LevelEditorSelectionKind FindSelectionKind(string stableId)
        {
            if (Document == null || string.IsNullOrWhiteSpace(stableId))
                return LevelEditorSelectionKind.None;
            for (int i = 0; i < Document.sources.Count; i++)
                if (Document.sources[i] != null && Document.sources[i].stableId == stableId) return LevelEditorSelectionKind.Source;
            for (int i = 0; i < Document.cups.Count; i++)
                if (Document.cups[i] != null && Document.cups[i].stableId == stableId) return LevelEditorSelectionKind.Cup;
            for (int i = 0; i < Document.rotatingObstacles.Count; i++)
                if (Document.rotatingObstacles[i] != null && Document.rotatingObstacles[i].stableId == stableId)
                    return LevelEditorSelectionKind.RotatingObstacle;
            return LevelEditorSelectionKind.None;
        }

        private void RefreshDocumentStatus()
        {
            if (documentStatus == null || documentHost == null) return;
            if (!documentHost.HasDocument)
            {
                documentStatus.text = "No level open";
                if (saveButton != null)
                {
                    saveButton.SetEnabled(false);
                    saveButton.tooltip = "Create or open a level first.";
                }
                if (saveAsButton != null) saveAsButton.SetEnabled(false);
                if (generalToggle != null) generalToggle.SetEnabled(false);
                SetEntityCreationEnabled(false);
                RefreshPlayTestState();
                return;
            }

            if (generalToggle != null) generalToggle.SetEnabled(true);
            SetEntityCreationEnabled(derivedState.Layout != null && derivedState.Masks != null);
            string path = string.IsNullOrWhiteSpace(documentHost.CurrentPath)
                ? "Unsaved level"
                : Path.GetFileName(documentHost.CurrentPath);
            documentStatus.text = path + (documentHost.IsDirty ? " | Unsaved changes" : " | Saved");
            bool canSave = !HasBlockingIssues();
            if (saveButton != null)
            {
                saveButton.SetEnabled(canSave);
                saveButton.tooltip = canSave ? "Save the level." : "Save is disabled until blocking issues are fixed.";
            }
            if (saveAsButton != null) saveAsButton.SetEnabled(canSave);
            RefreshPlayTestState();
        }

        private void SetEntityCreationEnabled(bool enabled)
        {
            if (addSourceButton != null) addSourceButton.SetEnabled(enabled);
            if (addCupButton != null) addCupButton.SetEnabled(enabled);
            if (addObstacleButton != null) addObstacleButton.SetEnabled(enabled);
        }
    }
}
#endif
