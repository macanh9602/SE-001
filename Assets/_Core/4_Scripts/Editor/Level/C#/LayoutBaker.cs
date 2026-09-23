#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using SE001.Data;
using SE001.Geometry;
using SE001.Simulation.Sand;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SE001.Editor.Level
{
    public sealed class LayoutBakeSnapshot
    {
        public int Width;
        public int Height;
        public float CellSize;
        public Vector2 BoardSize;
        public string ContourHash = string.Empty;
        public byte[] StaticBits;
        public byte[] ValidBits;
    }

    public static class LayoutBaker
    {
        public const int ImporterVersion = 1;
        private const string LayoutResourceFolder = "Assets/_Core/Resources/Layouts";
        private const string LayoutPrefabFolder = "Assets/_Core/3_Prefabs/Gameplay/Layout/Baked";
        private const string SimulationProfilePath =
            "Assets/_Core/Resources/Profiles/PhaseBSandSimulationProfile.asset";
        private const string VisualProfilePath =
            "Assets/_Core/Resources/Profiles/PhaseBLayoutVisualProfile.asset";

        public static bool TryBakeSvg(
            string sourceSvgPath,
            string layoutId,
            PhaseBSvgImportSettings settings,
            out LayoutDefinition definition,
            out string error)
        {
            definition = null;
            error = string.Empty;
            SE001LevelJson level;
            if (!PhaseBSvgImporter.TryParse(sourceSvgPath, settings, out level, out error)) return false;
            return TryBakeLevel(level, layoutId, sourceSvgPath, out definition, out error);
        }

        public static bool TryBakeLevel(
            SE001LevelJson level,
            string layoutId,
            string sourceSvgPath,
            out LayoutDefinition definition,
            out string error)
        {
            definition = null;
            error = string.Empty;
            try
            {
                ValidateBakeInput(level, layoutId);
                SandSimulationProfile simulationProfile =
                    AssetDatabase.LoadAssetAtPath<SandSimulationProfile>(SimulationProfilePath);
                LayoutVisualProfile visualProfile =
                    AssetDatabase.LoadAssetAtPath<LayoutVisualProfile>(VisualProfilePath);
                if (simulationProfile == null) throw new InvalidOperationException("Sand simulation profile is missing.");
                if (visualProfile == null) throw new InvalidOperationException("Layout visual profile is missing.");

                EnsureFolder(LayoutResourceFolder);
                EnsureFolder(LayoutPrefabFolder);
                LayoutBakeSnapshot snapshot = BuildSnapshot(level, simulationProfile.cellSize, simulationProfile.maxCells);
                string maskPath = LayoutResourceFolder + "/" + layoutId + "_Mask.asset";
                string definitionPath = LayoutResourceFolder + "/" + layoutId + ".asset";
                string prefabPath = LayoutPrefabFolder + "/" + layoutId + ".prefab";

                LayoutMaskAsset mask = SaveMaskAsset(maskPath, snapshot);
                definition = AssetDatabase.LoadAssetAtPath<LayoutDefinition>(definitionPath);
                if (definition == null)
                {
                    definition = ScriptableObject.CreateInstance<LayoutDefinition>();
                    AssetDatabase.CreateAsset(definition, definitionPath);
                }

                RemoveGeneratedMeshes(definitionPath);
                GameObject prefabRoot = BuildPrefabRoot(level, visualProfile, definition);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                UnityEngine.Object.DestroyImmediate(prefabRoot);
                AssetDatabase.ImportAsset(prefabPath, ImportAssetOptions.ForceUpdate);

                definition.layoutId = layoutId;
                definition.layoutPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                definition.mask = mask;
                definition.boardSize = level.board.size;
                definition.sourceSvgPath = NormalizeProjectPath(sourceSvgPath);
                EditorUtility.SetDirty(definition);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        public static LayoutBakeSnapshot BuildSnapshot(SE001LevelJson level, float cellSize, int maxCells)
        {
            LayoutMaskSet masks = LayoutRasterizer.Rasterize(level, cellSize, maxCells);
            int byteCount = LayoutMaskAsset.GetByteCount(masks.Width, masks.Height);
            LayoutBakeSnapshot snapshot = new LayoutBakeSnapshot
            {
                Width = masks.Width,
                Height = masks.Height,
                CellSize = cellSize,
                BoardSize = level.board.size,
                ContourHash = ComputeContourHash(level),
                StaticBits = new byte[byteCount],
                ValidBits = new byte[byteCount]
            };
            for (int index = 0; index < masks.ValidMask.Length; index++)
            {
                LayoutMaskAsset.WriteBit(snapshot.StaticBits, index, masks.StaticMask[index]);
                LayoutMaskAsset.WriteBit(snapshot.ValidBits, index, masks.ValidMask[index]);
            }
            return snapshot;
        }

        public static LayoutMaskAsset BuildMaskAsset(SE001LevelJson level, float cellSize, int maxCells)
        {
            LayoutBakeSnapshot snapshot = BuildSnapshot(level, cellSize, maxCells);
            LayoutMaskAsset asset = ScriptableObject.CreateInstance<LayoutMaskAsset>();
            ApplySnapshot(asset, snapshot);
            return asset;
        }

        public static string ComputeContourHash(SE001LevelJson level)
        {
            StringBuilder source = new StringBuilder();
            AppendVector(source, level.board.size);
            AppendContours(source, level.board.wallContours);
            for (int i = 0; i < level.staticObstacles.Count; i++)
            {
                StaticObstacleData obstacle = level.staticObstacles[i];
                source.Append(obstacle != null ? obstacle.stableId : string.Empty).Append('|');
                AppendContours(source, obstacle != null ? obstacle.contours : null);
            }

            using (SHA1 sha = SHA1.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(source.ToString());
                byte[] hash = sha.ComputeHash(bytes);
                StringBuilder result = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++) result.Append(hash[i].ToString("x2"));
                return result.ToString();
            }
        }

        private static LayoutMaskAsset SaveMaskAsset(string path, LayoutBakeSnapshot snapshot)
        {
            LayoutMaskAsset mask = AssetDatabase.LoadAssetAtPath<LayoutMaskAsset>(path);
            if (mask == null)
            {
                mask = ScriptableObject.CreateInstance<LayoutMaskAsset>();
                AssetDatabase.CreateAsset(mask, path);
            }
            ApplySnapshot(mask, snapshot);
            EditorUtility.SetDirty(mask);
            return mask;
        }

        private static void ApplySnapshot(LayoutMaskAsset asset, LayoutBakeSnapshot snapshot)
        {
            asset.width = snapshot.Width;
            asset.height = snapshot.Height;
            asset.cellSize = snapshot.CellSize;
            asset.boardSize = snapshot.BoardSize;
            asset.contourHash = snapshot.ContourHash;
            asset.importerVersion = ImporterVersion;
            asset.staticBits = snapshot.StaticBits;
            asset.validBits = snapshot.ValidBits;
        }

        private static GameObject BuildPrefabRoot(
            SE001LevelJson level,
            LayoutVisualProfile profile,
            LayoutDefinition definition)
        {
            GameObject root = new GameObject(level.levelId + "_Layout");
            CreateMeshChildren(root.transform, "Wall", level.board.wallContours, profile.wallHeight,
                profile.wallMaterial, profile, definition);
            for (int i = 0; i < level.staticObstacles.Count; i++)
            {
                StaticObstacleData obstacle = level.staticObstacles[i];
                if (obstacle == null) continue;
                CreateMeshChildren(root.transform, "Obstacle_" + obstacle.stableId, obstacle.contours,
                    profile.obstacleHeight, profile.obstacleMaterial, profile, definition);
            }
            return root;
        }

        private static void CreateMeshChildren(
            Transform parent,
            string prefix,
            IList<PolygonContourData> contours,
            float height,
            Material material,
            LayoutVisualProfile profile,
            LayoutDefinition definition)
        {
            if (contours == null) return;
            for (int i = 0; i < contours.Count; i++)
            {
                PolygonContourData contour = contours[i];
                if (contour == null || contour.points == null || contour.points.Count < 3) continue;
                Mesh mesh = ExtrudedBevelMeshBuilder.Build(
                    contour.points,
                    height,
                    profile.bevelRadius,
                    profile.bevelSegments,
                    prefix + "_" + i,
                    profile.uvScale);
                AssetDatabase.AddObjectToAsset(mesh, definition);
                GameObject child = new GameObject(prefix + "_" + i.ToString("D3"));
                child.transform.SetParent(parent, false);
                MeshFilter filter = child.AddComponent<MeshFilter>();
                MeshRenderer renderer = child.AddComponent<MeshRenderer>();
                filter.sharedMesh = mesh;
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = profile.castShadows
                    ? ShadowCastingMode.On
                    : ShadowCastingMode.Off;
                renderer.receiveShadows = profile.receiveShadows;
            }
        }

        private static void RemoveGeneratedMeshes(string definitionPath)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(definitionPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Mesh) UnityEngine.Object.DestroyImmediate(assets[i], true);
            }
        }

        private static void ValidateBakeInput(SE001LevelJson level, string layoutId)
        {
            if (level == null) throw new ArgumentNullException(nameof(level));
            if (string.IsNullOrWhiteSpace(layoutId)) throw new ArgumentException("A layout id is required.", nameof(layoutId));
            level.EnsureCollections();
            if (level.board.wallContours.Count == 0 && level.staticObstacles.Count == 0)
                throw new FormatException("SVG layout contains no wall or obstacle contours.");
        }

        private static void AppendContours(StringBuilder source, IList<PolygonContourData> contours)
        {
            if (contours == null)
            {
                source.Append("<null>");
                return;
            }
            for (int i = 0; i < contours.Count; i++)
            {
                PolygonContourData contour = contours[i];
                if (contour == null || contour.points == null)
                {
                    source.Append("<null>|" );
                    continue;
                }
                for (int p = 0; p < contour.points.Count; p++) AppendVector(source, contour.points[p]);
                source.Append('|');
            }
        }

        private static void AppendVector(StringBuilder source, Vector2 value)
        {
            source.Append(value.x.ToString("F4", global::System.Globalization.CultureInfo.InvariantCulture));
            source.Append(',');
            source.Append(value.y.ToString("F4", global::System.Globalization.CultureInfo.InvariantCulture));
            source.Append((char)59);
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static string NormalizeProjectPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;
            string projectRoot = Directory.GetParent(Application.dataPath).FullName.Replace('\\', '/');
            string fullPath = Path.GetFullPath(path).Replace('\\', '/');
            if (fullPath.StartsWith(projectRoot + "/", StringComparison.OrdinalIgnoreCase))
                return fullPath.Substring(projectRoot.Length + 1);
            return fullPath;
        }
    }
}
#endif
