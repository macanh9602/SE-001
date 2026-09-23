#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SE001.Data
{
    /// <summary>
    /// Editor-to-runtime one-shot payload for testing the current in-memory level without writing Resources.
    /// </summary>
    public static class LevelPlayTestOverride
    {
#if UNITY_EDITOR
        private const string LevelIdKey = "SE001.LevelPlayTest.LevelId";
        private const string PayloadKey = "SE001.LevelPlayTest.Payload";

        public static void Set(string levelId, string json)
        {
            SessionState.SetString(LevelIdKey, levelId ?? string.Empty);
            SessionState.SetString(PayloadKey, json ?? string.Empty);
        }

        public static bool TryPeekLevelId(out string levelId)
        {
            levelId = SessionState.GetString(LevelIdKey, string.Empty);
            return !string.IsNullOrWhiteSpace(levelId) &&
                !string.IsNullOrWhiteSpace(SessionState.GetString(PayloadKey, string.Empty));
        }

        public static bool HasPendingFor(string requestedLevelId)
        {
            string levelId;
            return TryPeekLevelId(out levelId) && string.Equals(levelId, requestedLevelId, global::System.StringComparison.Ordinal);
        }

        public static bool TryConsume(string requestedLevelId, out string json)
        {
            json = string.Empty;
            string pendingLevelId;
            if (!TryPeekLevelId(out pendingLevelId) ||
                !string.Equals(pendingLevelId, requestedLevelId, global::System.StringComparison.Ordinal)) return false;

            json = SessionState.GetString(PayloadKey, string.Empty);
            Clear();
            return !string.IsNullOrWhiteSpace(json);
        }

        public static void Clear()
        {
            SessionState.EraseString(LevelIdKey);
            SessionState.EraseString(PayloadKey);
        }
#else
        public static bool TryPeekLevelId(out string levelId)
        {
            levelId = string.Empty;
            return false;
        }

        public static bool HasPendingFor(string requestedLevelId)
        {
            return false;
        }

        public static bool TryConsume(string requestedLevelId, out string json)
        {
            json = string.Empty;
            return false;
        }

        public static void Clear() { }
#endif
    }
}
