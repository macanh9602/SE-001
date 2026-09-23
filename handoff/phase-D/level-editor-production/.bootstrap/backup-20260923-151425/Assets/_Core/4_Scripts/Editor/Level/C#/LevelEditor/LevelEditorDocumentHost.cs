#if UNITY_EDITOR
using SE001.Data;
using UnityEngine;

namespace SE001.Editor.Level
{
    internal sealed class LevelEditorDocumentHost : ScriptableObject
    {
        [SerializeField] private SE001LevelJson level = new SE001LevelJson();
        [SerializeField] private string currentPath = string.Empty;
        [SerializeField] private bool dirty;

        public SE001LevelJson Level => level;
        public string CurrentPath => currentPath;
        public bool IsDirty => dirty;

        public void InitializeNew(string levelId)
        {
            level = new SE001LevelJson
            {
                schemaVersion = 3,
                levelId = string.IsNullOrWhiteSpace(levelId) ? "new_level" : levelId,
                board = new BoardData()
            };
            level.EnsureCollections();
            currentPath = string.Empty;
            dirty = true;
        }

        public void ReplaceDocument(SE001LevelJson value, string path, bool isDirty)
        {
            level = value ?? new SE001LevelJson();
            level.EnsureCollections();
            currentPath = path ?? string.Empty;
            dirty = isDirty;
        }

        public void MarkDirty()
        {
            dirty = true;
        }

        public void MarkSaved(string path)
        {
            currentPath = path ?? string.Empty;
            dirty = false;
        }
    }
}
#endif