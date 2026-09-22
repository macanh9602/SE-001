using System.Collections.Generic;
using SE001.Elements.Sand;
using SE001.Gameplay;
using SE001.Geometry;
using SE001.System.Management;
using SE001.Data;
using UnityEngine;
using UnityEngine.Rendering;

namespace SE001.Diagnostics
{
    /// <summary>
    /// DEVELOPMENT-ONLY placeholder presentation for Phase C so the loop can be play-tested before real
    /// Source/Cup/DrawStroke prefabs + juice exist. Reads GameplayManager state only; no gameplay authority.
    /// Replace with SandSource/Cup/DrawStroke prefab visuals; then delete this file.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PhaseCDebugView : MonoBehaviour, ILevelLifecycleParticipant
    {
        [SerializeField] private bool showOverlay;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly string[] DevSequence = { "phase_c_level_02" };

        [SerializeField] private Color32[] palette =
        {
            new Color32(0, 0, 0, 0),
            new Color32(232, 65, 79, 255),    // 1 coral red
            new Color32(47, 107, 255, 255),   // 2 blue
        };
        [SerializeField] private Color floorColor = new Color(0.97f, 0.97f, 0.96f);
        [SerializeField] private Color cupWallColor = new Color(0.9f, 0.9f, 0.9f);

        private readonly List<Object> owned = new List<Object>();
        private readonly List<Transform> sourcePivots = new List<Transform>();
        private readonly List<Renderer> sourceBodies = new List<Renderer>();
        private readonly List<Transform> cupFills = new List<Transform>();
        private readonly List<Renderer> cupFillRenderers = new List<Renderer>();
        private readonly List<float> cupFillShown = new List<float>();
        private GameplayManager manager;
        private GameplayInputController input;
        private LevelContext context;
        private Material litMaterial;
        private Material previewMaterial;
        private JuiceProfile juiceProfile;
        private MaterialPalette materialPalette;
        private PrefabProfile prefabProfile;
        private Mesh quad;
        private MaterialPropertyBlock block;
        private LineRenderer preview;
        private Rect panelRect;
        private string pendingLevelId;

        private void Update()
        {
            if (pendingLevelId == null) return;
            string id = pendingLevelId;
            pendingLevelId = null;
            LevelManager lm = LevelManager.Instance;
            if (lm == null || lm.IsLoading) return;
            lm.BeginLevel(id);
        }

        public void Bind(LevelContext value)
        {
            CleanupForLevelUnload();
            context = value;
            manager = GetComponent<GameplayManager>();
            input = GetComponent<GameplayInputController>();
            if (manager == null) return;
            block = new MaterialPropertyBlock();

            LayoutVisualProfile layoutProfile = Resources.Load<LayoutVisualProfile>("Profiles/PhaseBLayoutVisualProfile");
            GameplayRuntimeProfile runtimeProfile = Resources.Load<GameplayRuntimeProfile>("Profiles/PhaseCGameplayRuntimeProfile");
            juiceProfile = runtimeProfile != null ? runtimeProfile.juiceProfile : null;
            materialPalette = runtimeProfile != null ? runtimeProfile.materialPalette : null;
            prefabProfile = runtimeProfile != null ? runtimeProfile.prefabProfile : null;
            litMaterial = layoutProfile != null ? layoutProfile.wallMaterial : null;
            quad = Own(BuildQuad());
            previewMaterial = layoutProfile != null ? layoutProfile.obstacleMaterial : litMaterial;

            SandFieldVisual sand = context.SandVisualRoot.GetComponentInChildren<SandFieldVisual>();
            if (sand != null) sand.SetPalette(BuildPalette());

            Vector2 board = context.BoardSize;
            CreateBox("DebugFloor", context.BoardRoot, new Vector3(board.x * 0.5f, board.y * 0.5f, 0.02f), board, floorColor, false);

            for (int i = 0; i < manager.Sources.Count; i++) BuildSource(manager.Sources[i]);
            for (int i = 0; i < manager.Cups.Count; i++) BuildCup(manager.Cups[i]);
            BuildPreview();
            manager.StrokeCommitted += OnStrokeCommitted;
        }

        private void BuildSource(SourceDomain source)
        {
            Color color = PaletteColor(source.MaterialId);
            GameObject sourceObject = prefabProfile != null && prefabProfile.sourcePrefab != null
                ? Instantiate(prefabProfile.sourcePrefab, context.SourceRoot, false)
                : new GameObject();
            sourceObject.name = "SandSource_" + source.StableId;
            var pivot = sourceObject.transform;
            if (sourceObject.transform.parent == null) pivot.SetParent(context.SourceRoot, false);
            pivot.localPosition = new Vector3(source.Position.x + source.BodyOffset.x, source.Position.y + source.BodyOffset.y, -0.3f);
            Renderer body = CreateBox("Body", pivot, Vector3.zero, source.Size, color, true);
            Vector2 nozzleSize = new Vector2(source.Size.x * 0.4f, source.Size.y * 0.18f);
            CreateBox("Nozzle", pivot, new Vector3(0f, source.Size.y * 0.5f, 0f), nozzleSize, Color.white, true);
            pivot.localRotation = Quaternion.Euler(0f, 0f, source.IsPouring ? 180f : 0f);
            sourcePivots.Add(pivot);
            sourceBodies.Add(body);
        }

        private void BuildCup(CupDomain cup)
        {
            GameObject cupObject = prefabProfile != null && prefabProfile.cupPrefab != null
                ? Instantiate(prefabProfile.cupPrefab, context.CupRoot, false)
                : new GameObject();
            cupObject.name = "Cup_" + cup.StableId;
            Transform root = cupObject.transform;
            if (cupObject.transform.parent == null) root.SetParent(context.CupRoot, false);
            float px = cup.Position.x, y0 = cup.Position.y, y1 = cup.Position.y + cup.Size.y;
            float th = cup.Size.x * 0.5f, bh = cup.Size.x * (1f - cup.Taper) * 0.5f, w = cup.EffectiveWall;
            Color wallColor = cupWallColor;

            // Slanted walls + bottom, extruded toward the camera (same geometry as the sim mask).
            CreateExtruded("WallL", root, new List<Vector2> { new Vector2(px - bh, y0), new Vector2(px - bh + w, y0), new Vector2(px - th + w, y1), new Vector2(px - th, y1) }, 0.25f, wallColor);
            CreateExtruded("WallR", root, new List<Vector2> { new Vector2(px + bh - w, y0), new Vector2(px + bh, y0), new Vector2(px + th, y1), new Vector2(px + th - w, y1) }, 0.25f, wallColor);
            CreateExtruded("WallB", root, new List<Vector2> { new Vector2(px - bh, y0), new Vector2(px + bh, y0), new Vector2(px + bh - w * 0.2f, y0 + w), new Vector2(px - bh + w * 0.2f, y0 + w) }, 0.25f, wallColor);

            // Inner back face (behind sand) + fill line marker.
            Color tint = Color.Lerp(PaletteColor(cup.AcceptedMaterialId), Color.white, 0.75f);
            CreateFlat("Back", root, new[] { new Vector2(px - bh, y0), new Vector2(px + bh, y0), new Vector2(px + th, y1), new Vector2(px - th, y1) }, 0.015f, tint);
            float lineHalf = Mathf.Lerp(bh, th, (cup.FillLineY - y0) / cup.Size.y) - w;
            CreateFlat("FillLine", root, new[] { new Vector2(px - lineHalf, cup.FillLineY - 0.02f), new Vector2(px + lineHalf, cup.FillLineY - 0.02f), new Vector2(px + lineHalf, cup.FillLineY + 0.02f), new Vector2(px - lineHalf, cup.FillLineY + 0.02f) }, -0.26f, new Color(1f, 1f, 1f, 1f));

            // Elliptic rim (mouth) like a bucket seen slightly from above.
            var rimGo = new GameObject("Rim");
            rimGo.transform.SetParent(root, false);
            var rim = rimGo.AddComponent<LineRenderer>();
            rim.useWorldSpace = false;
            rim.loop = true;
            rim.alignment = LineAlignment.TransformZ;
            rim.widthMultiplier = 0.06f;
            rim.shadowCastingMode = ShadowCastingMode.Off;
            rim.sharedMaterial = previewMaterial;
            rim.startColor = rim.endColor = Color.white;
            int segments = 32;
            rim.positionCount = segments;
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                rim.SetPosition(i, new Vector3(px + Mathf.Cos(a) * th, y1 + Mathf.Sin(a) * th * 0.18f, -0.27f));
            }

            Transform fillPivot = new GameObject("FillPivot").transform;
            fillPivot.SetParent(root, false);
            fillPivot.localPosition = new Vector3(px - th, y0 - 0.2f, -0.1f);
            Renderer fill = CreateBox("Progress", fillPivot, new Vector3(th, 0f, 0f), new Vector2(th * 2f, 1f), PaletteColor(cup.AcceptedMaterialId), false);
            fillPivot.localScale = new Vector3(0.0001f, 0.12f, 1f);
            cupFills.Add(fillPivot);
            cupFillRenderers.Add(fill);
            cupFillShown.Add(0f);
        }

        private void CreateExtruded(string name, Transform parent, List<Vector2> polygon, float height, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = Own(ExtrudedBevelMeshBuilder.Build(polygon, height, 0f, 1, name));
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = litMaterial;
            block.SetColor(BaseColorId, color);
            r.SetPropertyBlock(block);
        }

        private void CreateFlat(string name, Transform parent, Vector2[] quadCcw, float z, Color color)
        {
            var m = Own(new Mesh { name = name });
            m.vertices = new[] { (Vector3)quadCcw[0] + Vector3.forward * z, (Vector3)quadCcw[1] + Vector3.forward * z, (Vector3)quadCcw[2] + Vector3.forward * z, (Vector3)quadCcw[3] + Vector3.forward * z };
            m.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            m.RecalculateBounds();
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = litMaterial;
            r.shadowCastingMode = ShadowCastingMode.Off;
            block.SetColor(BaseColorId, color);
            r.SetPropertyBlock(block);
        }

        private void BuildPreview()
        {
            var go = new GameObject("DebugStrokePreview");
            go.transform.SetParent(context.DynamicDrawRoot, false);
            preview = go.AddComponent<LineRenderer>();
            preview.useWorldSpace = false;
            preview.alignment = LineAlignment.TransformZ;
            preview.numCapVertices = 4;
            preview.numCornerVertices = 2;
            preview.shadowCastingMode = ShadowCastingMode.Off;
            preview.sharedMaterial = previewMaterial;
            preview.startColor = preview.endColor = new Color(0.25f, 0.2f, 0.45f, 0.6f);
            preview.positionCount = 0;
        }

        private void OnStrokeCommitted(IList<Vector2> points, float thickness)
        {
            // 3D-looking dev stroke: each segment and joint extruded toward the camera.
            var combine = new List<CombineInstance>(points.Count * 2);
            var poly = new List<Vector2>(8);
            float half = thickness * 0.5f;
            for (int i = 0; i < points.Count; i++)
            {
                poly.Clear();
                for (int k = 0; k < 8; k++) { float a = k * Mathf.PI / 4f; poly.Add(points[i] + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * half); }
                combine.Add(new CombineInstance { mesh = ExtrudedBevelMeshBuilder.Build(poly, 0.18f, 0f, 1, "joint"), transform = Matrix4x4.identity });
                if (i == 0) continue;
                Vector2 a0 = points[i - 1], b0 = points[i];
                Vector2 dir = b0 - a0; if (dir.sqrMagnitude < 1e-8f) continue;
                Vector2 n = new Vector2(-dir.y, dir.x).normalized * half;
                poly.Clear(); poly.Add(a0 - n); poly.Add(b0 - n); poly.Add(b0 + n); poly.Add(a0 + n);
                combine.Add(new CombineInstance { mesh = ExtrudedBevelMeshBuilder.Build(poly, 0.18f, 0f, 1, "segment"), transform = Matrix4x4.identity });
            }

            Mesh mesh = Own(new Mesh { name = "DebugStroke", indexFormat = IndexFormat.UInt32 });
            mesh.CombineMeshes(combine.ToArray(), true, true);
            for (int i = 0; i < combine.Count; i++) Destroy(combine[i].mesh);

            GameObject go = prefabProfile != null && prefabProfile.drawStrokePrefab != null
                ? Instantiate(prefabProfile.drawStrokePrefab, context.DynamicDrawRoot, false)
                : new GameObject("DrawStroke");
            go.name = "DrawStroke";
            if (go.transform.parent == null) go.transform.SetParent(context.DynamicDrawRoot, false);
            MeshFilter meshFilter = go.GetComponent<MeshFilter>() ?? go.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
            MeshRenderer r = go.GetComponent<MeshRenderer>() ?? go.AddComponent<MeshRenderer>();
            r.sharedMaterial = litMaterial;
            block.SetColor(BaseColorId, new Color(0.35f, 0.3f, 0.55f));
            r.SetPropertyBlock(block);
        }

        private void LateUpdate()
        {
            if (manager == null || context == null) return;
            float dt = Time.deltaTime;
            float rotateDuration = Mathf.Max(0.01f, juiceProfile != null ? juiceProfile.valveOpenDuration : 0.2f);
            float valveRotateSpeed = 180f / rotateDuration;

            for (int i = 0; i < sourcePivots.Count && i < manager.Sources.Count; i++)
            {
                SourceDomain s = manager.Sources[i];
                float target = s.IsPouring ? 180f : 0f;
                float z = Mathf.MoveTowardsAngle(sourcePivots[i].localEulerAngles.z, target, valveRotateSpeed * dt);
                sourcePivots[i].localRotation = Quaternion.Euler(0f, 0f, z);
                Color c = PaletteColor(s.MaterialId);
                if (s.State == SourceValveState.Empty) c = Color.Lerp(c, Color.gray, 0.7f);
                block.SetColor(BaseColorId, c);
                sourceBodies[i].SetPropertyBlock(block);
                float pulseFrequency = 1f / Mathf.Max(0.01f, juiceProfile != null ? juiceProfile.cupPunchDuration : 0.12f);
                float pulse = s.State == SourceValveState.Open ? 1f + Mathf.Sin(Time.time * pulseFrequency * Mathf.PI * 2f) * 0.03f : 1f;
                sourcePivots[i].localScale = new Vector3(pulse, 2f - pulse, 1f);
            }

            for (int i = 0; i < cupFills.Count && i < manager.Cups.Count; i++)
            {
                CupDomain cup = manager.Cups[i];
                                cupFillShown[i] = Mathf.Lerp(cupFillShown[i], cup.FillRatio, 1f - Mathf.Exp(-10f * dt));
                // Sand fills the cup physically now; this is only a progress bar below the cup.
                cupFills[i].localScale = new Vector3(Mathf.Max(0.0001f, cupFillShown[i]), 0.12f, 1f);
                float punchFrequency = 1f / Mathf.Max(0.01f, juiceProfile != null ? juiceProfile.cupPunchDuration : 0.12f);
                Color c = cup.ForeignDetected ? Color.Lerp(PaletteColor(cup.AcceptedMaterialId), Color.red, Mathf.PingPong(Time.time * punchFrequency, 1f)) : PaletteColor(cup.AcceptedMaterialId);
                if (cup.Full) c = Color.Lerp(c, Color.white, Mathf.PingPong(Time.time * punchFrequency * 0.33f, 0.25f));
                block.SetColor(BaseColorId, c);
                cupFillRenderers[i].SetPropertyBlock(block);
            }

            if (preview != null && input != null)
            {
                var pts = input.PreviewStroke;
                int count = input.IsDrawing ? pts.Count : 0;
                preview.positionCount = count;
                preview.widthMultiplier = input.DrawThickness;
                for (int i = 0; i < count; i++) preview.SetPosition(i, new Vector3(pts[i].x, pts[i].y, -0.25f));
            }
        }

        private void OnGUI()
        {
            if (!showOverlay) return;
            if (manager == null || context == null) return;
            float scale = Mathf.Max(1f, Screen.height / 1100f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            panelRect = new Rect(8f, 120f, 260f, 150f + 22f * (manager.Sources.Count + manager.Cups.Count));
            if (input != null) input.BlockedGuiRect = new Rect(panelRect.x * scale, panelRect.y * scale, panelRect.width * scale, panelRect.height * scale);

            GUILayout.BeginArea(panelRect, GUI.skin.box);
            GUILayout.Label($"[DEV] {context.LevelId}  step {manager.StepCount}");
            string state = manager.State == GameState.Lost ? $"LOST ({manager.LastLoseReason})" : manager.State.ToString().ToUpperInvariant();
            GUILayout.Label($"State: {state}");
            GUILayout.Label($"Ink: {manager.InkRemaining:0.00} / {manager.InkBudget:0.00}");
            for (int i = 0; i < manager.Sources.Count; i++)
            {
                SourceDomain s = manager.Sources[i];
                GUILayout.Label($"{s.StableId}: {s.State}  {s.Remaining}/{s.Initial}");
            }

            for (int i = 0; i < manager.Cups.Count; i++)
            {
                CupDomain c = manager.Cups[i];
                GUILayout.Label($"{c.StableId}: {c.CollectedLogical}/{c.RequiredLogical}  ({c.Collected}/{c.Required} grains, cap {c.Capacity}){(c.Full ? " FULL" : "")}{(c.ForeignDetected ? " WRONG" : "")}");
            }

            GUILayout.BeginHorizontal();
            int index = global::System.Array.IndexOf(DevSequence, context.LevelId);
            if (GUILayout.Button("Retry")) pendingLevelId = context.LevelId;
            GUI.enabled = index >= 0 && index < DevSequence.Length - 1;
            if (GUILayout.Button("Next")) pendingLevelId = DevSequence[index + 1];
            GUI.enabled = index > 0;
            if (GUILayout.Button("Prev")) pendingLevelId = DevSequence[index - 1];
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private Renderer CreateBox(string name, Transform parent, Vector3 localPosition, Vector2 size, Color color, bool castShadow)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            go.AddComponent<MeshFilter>().sharedMesh = quad;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = litMaterial;
            r.shadowCastingMode = castShadow ? ShadowCastingMode.On : ShadowCastingMode.Off;
            block.SetColor(BaseColorId, color);
            r.SetPropertyBlock(block);
            return r;
        }

        private Color PaletteColor(byte id)
        {
            if (materialPalette != null && materialPalette.Contains(id)) return materialPalette.GetSandColor(id);
            return id < palette.Length ? (Color)palette[id] : Color.magenta;
        }

        private Color32[] BuildPalette()
        {
            if (materialPalette == null || materialPalette.entries == null || materialPalette.entries.Count == 0) return palette;
            byte maxId = 0;
            for (int i = 0; i < materialPalette.entries.Count; i++) maxId = (byte)Mathf.Max(maxId, materialPalette.entries[i].materialId);
            Color32[] result = new Color32[maxId + 1];
            for (int i = 0; i < materialPalette.entries.Count; i++) result[materialPalette.entries[i].materialId] = materialPalette.entries[i].sandColor;
            return result;
        }

        private static Mesh BuildQuad()
        {
            var m = new Mesh { name = "DebugQuad" };
            m.vertices = new[] { new Vector3(-0.5f, -0.5f), new Vector3(0.5f, -0.5f), new Vector3(0.5f, 0.5f), new Vector3(-0.5f, 0.5f) };
            m.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            m.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 }; // faces -Z (camera)
            m.RecalculateBounds();
            return m;
        }

        private T Own<T>(T obj) where T : Object { owned.Add(obj); return obj; }

        public void CleanupForLevelUnload()
        {
            if (manager != null) manager.StrokeCommitted -= OnStrokeCommitted;
            for (int i = 0; i < owned.Count; i++) if (owned[i] != null) { if (Application.isPlaying) Destroy(owned[i]); else DestroyImmediate(owned[i]); }
            owned.Clear();
            sourcePivots.Clear(); sourceBodies.Clear(); cupFills.Clear(); cupFillRenderers.Clear(); cupFillShown.Clear();
            manager = null; input = null; context = null; preview = null; litMaterial = null; previewMaterial = null; quad = null;
        }
    }
}
