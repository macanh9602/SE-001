using SE001.Geometry;
using UnityEngine;

namespace SE001.Data
{
    [CreateAssetMenu(fileName = "LayoutDefinition", menuName = "SE001/Layouts/Layout Definition")]
    public sealed class LayoutDefinition : ScriptableObject
    {
        public const string RebakeHint = "Open SE001/Layout Bake and rebake this layout.";

        public string layoutId = string.Empty;
        public GameObject layoutPrefab;
        public LayoutMaskAsset mask;
        public Vector2 boardSize;
        public Texture2D thumbnail;

        // Expected baked geometry identity. LayoutBaker writes the same value to this field and to
        // mask.contourHash in one bake; any mismatch means the pair is stale or half-baked.
        [HideInInspector]
        public string contourHash = string.Empty;

        [HideInInspector]
        public string sourceSvgPath = string.Empty;

        /// <summary>
        /// Single authoritative runtime entry point for turning a baked layout into simulation masks.
        /// Never rasterizes and never parses SVG; any inconsistency is a blocking error telling the user to rebake.
        /// </summary>
        public bool TryBuildMaskSet(float runtimeCellSize, int maxCells, out LayoutMaskSet masks, out string error)
        {
            masks = null;
            error = string.Empty;
            string label = "Layout '" + layoutId + "'";
            if (mask == null)
            {
                error = label + " has no baked mask. " + RebakeHint;
                return false;
            }

            if (string.IsNullOrEmpty(contourHash))
            {
                error = label + " is missing its baked geometry identity. " + RebakeHint;
                return false;
            }

            if (!string.Equals(contourHash, mask.contourHash, global::System.StringComparison.Ordinal))
            {
                error = label + " mask does not match its layout geometry (stale or partial bake). " + RebakeHint;
                return false;
            }

            if (!Approximately(boardSize, mask.boardSize))
            {
                error = label + " board size does not match its baked mask. " + RebakeHint;
                return false;
            }

            if (mask.cellSize > 0f && !HasExpectedGridSize(mask))
            {
                error = label + " mask grid does not match its board size. " + RebakeHint;
                return false;
            }

            string maskError;
            if (!mask.TryBuildMaskSet(runtimeCellSize, maxCells, out masks, out maskError))
            {
                error = label + ": " + maskError;
                if (!maskError.Contains("Rebake")) error += " " + RebakeHint;
                return false;
            }

            return true;
        }

        private static bool HasExpectedGridSize(LayoutMaskAsset value)
        {
            int expectedWidth = Mathf.Max(1, Mathf.CeilToInt(value.boardSize.x / value.cellSize));
            int expectedHeight = Mathf.Max(1, Mathf.CeilToInt(value.boardSize.y / value.cellSize));
            return value.width == expectedWidth && value.height == expectedHeight;
        }

        private static bool Approximately(Vector2 a, Vector2 b)
        {
            return Mathf.Abs(a.x - b.x) <= 0.0005f && Mathf.Abs(a.y - b.y) <= 0.0005f;
        }
    }
}
