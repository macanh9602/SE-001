#if UNITY_EDITOR
using System.IO;
using SE001.Data;
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
            Undo.RecordObject(documentHost, "New Level");
            documentHost.InitializeNew(LevelEditorDocumentService.NextLevelId(LevelSequence));
            viewState.selectedStableId = string.Empty;
            viewState.selectionKind = LevelEditorSelectionKind.None;
            derivedState.Clear();
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
            string error;
            if (!LevelEditorDocumentService.TryOpen(absolutePath, out level, out projectPath, out error))
            {
                ShowNotification(new GUIContent("Open failed: " + error));
                return;
            }

            int repaired = LevelEditorStableIds.NormalizeInMemory(level);
            Undo.RecordObject(documentHost, "Open Level");
            documentHost.ReplaceDocument(level, projectPath, repaired > 0);
            viewState.selectedStableId = string.Empty;
            viewState.selectionKind = LevelEditorSelectionKind.None;
            derivedState.Clear();
            RefreshAll();
        }

        private void SaveDocument()
        {
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
            if (HasBlockingIssues())
            {
                ShowNotification(new GUIContent("Save blocked. Fix the first blocking issue shown in Validation."));
                return;
            }
            string fileName = documentHost.Level.levelId;
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Level", fileName, "json", "Save the schema-4 level JSON.", LevelEditorDocumentService.LevelsFolder);
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
            SourceProfile profile = Resources.Load<SourceProfile>("Profiles/PhaseCSourceProfile");
            Vector2 size = profile != null ? profile.bodySize : new Vector2(0.8f, 1.2f);
            Vector2 position = FindOpenCenter(size);
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
            Vector2 size = profile != null ? profile.bodySize : new Vector2(2f, 1.5f);
            Vector2 position = FindOpenCenter(size);
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

        private Vector2 FindOpenCenter(Vector2 size, bool rotating = false)
        {
            Vector2 center = Document.board.size * 0.5f;
            if (IsPlacementOpen(center, size) &&
                LevelEditorGeometry.InBoard(center, size, Document.board.size) &&
                PlacementFootprintOpen(center, size, rotating))
                return center;
            for (int y = 1; y < 10; y++)
            for (int x = 1; x < 10; x++)
            {
                Vector2 candidate = new Vector2(Document.board.size.x * x / 10f, Document.board.size.y * y / 10f);
                if (LevelEditorGeometry.InBoard(candidate, size, Document.board.size) &&
                    PlacementFootprintOpen(candidate, size, rotating) &&
                    IsPlacementOpen(candidate, size)) return candidate;
            }
            return center;
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
                        position, size, source.position, LevelEditorGeometry.SourceSize(source))) return false;
            }
            for (int i = 0; i < Document.cups.Count; i++)
            {
                CupData cup = Document.cups[i];
                if (cup != null && LevelEditorGeometry.Overlaps(
                        position, size, cup.position, LevelEditorGeometry.CupSize(cup))) return false;
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
                if (level.board.size.x <= 0f || level.board.size.y <= 0f || level.board.size == Vector2.one)
                    level.board.size = entry.Definition.boardSize;
            }, "Choose Layout");
        }

        private void OpenLayoutBake()
        {
            EditorApplication.ExecuteMenuItem("SE001/Phase D/Layout Bake");
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
            if (string.IsNullOrWhiteSpace(stableId)) return LevelEditorSelectionKind.None;
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
        }
    }
}
#endif
