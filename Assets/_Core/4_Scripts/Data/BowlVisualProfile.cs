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
