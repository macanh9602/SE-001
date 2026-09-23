using System;
using UnityEngine;

namespace SE001.Data
{
    [Serializable]
    public struct BowlRowSpan
    {
        public float minX;
        public float maxX;

        public bool IsValid => maxX >= minX;

        public BowlRowSpan(float minimum, float maximum)
        {
            minX = minimum;
            maxX = maximum;
        }
    }

    [CreateAssetMenu(fileName = "BowlVisualProfile", menuName = "SE001/Profiles/Bowl Visual")]
    public sealed class BowlVisualProfile : ScriptableObject
    {
        [Header("Measured art")]
        public Texture2D mainTexture;
        public Texture2D specTexture;
        public int mainWidthPixels = 352;
        public int mainHeightPixels = 164;
        public int specWidthPixels = 312;
        public int specHeightPixels = 144;
        public int alphaThreshold = 20;
        public int rimPixelY = 24;
        public float wallThicknessPixels = 10.56f;
        public float defaultWorldWidth = 3.52f;
        [Tooltip("Bowl width the levels were authored at. Grains per logical unit scale with (defaultWorldWidth / this)^2, "
            + "so resizing the Bowl keeps every level's fill ratio without editing level data.")]
        [Min(0.01f)] public float referenceWorldWidth = 3.52f;
        public Vector2 shadowOffsetPixels = new Vector2(-6f, -24f);
        public Vector2 specOffsetPixels = Vector2.zero;

        [Header("Baked silhouette and receiver rows")]
        public Vector2[] outerContour = new Vector2[0];
        public Vector2[] innerContour = new Vector2[0];
        public BowlRowSpan[] outerRowSpans = new BowlRowSpan[0];
        public BowlRowSpan[] innerRowSpans = new BowlRowSpan[0];
        public Texture2D silhouetteMask;

        public bool IsBaked =>
            mainWidthPixels > 0 && mainHeightPixels > 0 &&
            outerRowSpans != null && outerRowSpans.Length == mainHeightPixels &&
            innerRowSpans != null && innerRowSpans.Length == mainHeightPixels;

        public float WorldHeightForWidth(float width)
        {
            return Mathf.Max(0.001f, width) * mainHeightPixels / Mathf.Max(1f, mainWidthPixels);
        }

        public Vector2 WorldSize => new Vector2(defaultWorldWidth, WorldHeightForWidth(defaultWorldWidth));

        /// <summary>Area ratio of the current Bowl vs the authoring reference.</summary>
        public float UnitAreaScale
        {
            get
            {
                float ratio = Mathf.Max(0.001f, defaultWorldWidth) / Mathf.Max(0.01f, referenceWorldWidth);
                return ratio * ratio;
            }
        }

        /// <summary>
        /// Grains per logical unit for the active receiver style. Bowl: scaled by UnitAreaScale so Source amounts and
        /// Bowl targets shrink/grow together (decision 2026-09-24). Cup: unchanged.
        /// </summary>
        public static int EffectiveGrainsPerUnit(int baseGrainsPerUnit, ReceiverStyle style, BowlVisualProfile bowl)
        {
            int value = Mathf.Max(1, baseGrainsPerUnit);
            if (style != ReceiverStyle.Bowl || bowl == null) return value;
            return Mathf.Max(1, Mathf.RoundToInt(value * bowl.UnitAreaScale));
        }

        public float WorldPixelsPerPixel(float width)
        {
            return Mathf.Max(0.001f, width) / Mathf.Max(1, mainWidthPixels);
        }

        public int PixelRowForLocalY(float localY, float width)
        {
            float scale = WorldPixelsPerPixel(width);
            return Mathf.Clamp(Mathf.FloorToInt(localY / scale), 0, mainHeightPixels - 1);
        }

        public bool TryGetOuterSpan(int pixelY, out BowlRowSpan span)
        {
            if (outerRowSpans != null && pixelY >= 0 && pixelY < outerRowSpans.Length)
            {
                span = outerRowSpans[pixelY];
                return span.IsValid;
            }

            span = default(BowlRowSpan);
            return false;
        }

        public bool TryGetInnerSpan(int pixelY, out BowlRowSpan span)
        {
            if (innerRowSpans != null && pixelY >= 0 && pixelY < innerRowSpans.Length)
            {
                span = innerRowSpans[pixelY];
                return span.IsValid;
            }

            span = default(BowlRowSpan);
            return false;
        }
    }
}
