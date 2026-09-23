using System;
using SE001.Geometry;
using UnityEngine;

namespace SE001.Data
{
    [CreateAssetMenu(fileName = "LayoutMask", menuName = "SE001/Layouts/Layout Mask")]
    public sealed class LayoutMaskAsset : ScriptableObject
    {
        public int width;
        public int height;
        public float cellSize;
        public Vector2 boardSize;
        public string contourHash = string.Empty;
        public int importerVersion;
        public byte[] staticBits;
        public byte[] validBits;

        public bool TryBuildMaskSet(
            float runtimeCellSize,
            int maxCells,
            out LayoutMaskSet masks,
            out string error)
        {
            masks = null;
            error = string.Empty;
            if (width <= 0 || height <= 0)
            {
                error = "Baked layout mask dimensions are invalid.";
                return false;
            }

            if (Mathf.Abs(runtimeCellSize - cellSize) > 0.000001f)
            {
                error = "Baked layout cell size " + cellSize.ToString("0.######") +
                    " does not match runtime cell size " + runtimeCellSize.ToString("0.######") + ". Rebake this layout.";
                return false;
            }

            long cellCount = (long)width * height;
            if (cellCount > maxCells)
            {
                error = "Baked layout grid " + width + "x" + height +
                    " exceeds runtime capacity " + maxCells + ".";
                return false;
            }

            int byteCount = (int)((cellCount + 7L) / 8L);
            if (staticBits == null || staticBits.Length != byteCount || validBits == null || validBits.Length != byteCount)
            {
                error = "Baked layout mask data length is invalid. Rebake this layout.";
                return false;
            }

            masks = new LayoutMaskSet(width, height);
            for (int index = 0; index < masks.ValidMask.Length; index++)
            {
                masks.ValidMask[index] = ReadBit(validBits, index);
                masks.StaticMask[index] = ReadBit(staticBits, index);
            }

            return true;
        }

        public static int GetByteCount(int width, int height)
        {
            if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            long cellCount = (long)width * height;
            return checked((int)((cellCount + 7L) / 8L));
        }

        public static bool ReadBit(byte[] bits, int index)
        {
            return bits != null && (bits[index >> 3] & (1 << (index & 7))) != 0;
        }

        public static void WriteBit(byte[] bits, int index, bool value)
        {
            if (!value) return;
            bits[index >> 3] |= (byte)(1 << (index & 7));
        }
    }
}
