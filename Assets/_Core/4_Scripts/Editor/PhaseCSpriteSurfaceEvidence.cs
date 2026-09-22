using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SE001.Editor
{
    internal static class PhaseCSpriteSurfaceEvidence
    {
        private const string ScenePath = "Assets/_Core/Scenes/PhaseC_SpriteSurface_Evidence.unity";

        [MenuItem("SE001/Phase C/Create SpriteSurface evidence scene")]
        private static void CreateScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Material surfaceMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Core/1_Materials/MAT_Cup.mat");
            Material backMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Core/1_Materials/MAT_CupBack.mat");
            Material floorMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Core/1_Materials/MAT_Wall.mat");
            if (surfaceMaterial == null || backMaterial == null || floorMaterial == null)
                throw new MissingReferenceException("C-R1 evidence materials are missing. Run Create visual materials first.");

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "SpriteSurfaceEvidenceFloor";
            floor.transform.position = new Vector3(0f, -0.15f, 0f);
            floor.transform.localScale = new Vector3(7f, 0.3f, 5f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = floorMaterial;

            GameObject front = GameObject.CreatePrimitive(PrimitiveType.Quad);
            front.name = "SpriteSurfaceEvidenceFront";
            front.transform.position = new Vector3(-1.35f, 1.2f, 0f);
            front.transform.localScale = new Vector3(2.2f, 2.2f, 1f);
            front.GetComponent<MeshRenderer>().sharedMaterial = surfaceMaterial;

            GameObject angled = GameObject.CreatePrimitive(PrimitiveType.Quad);
            angled.name = "SpriteSurfaceEvidenceAngled";
            angled.transform.position = new Vector3(1.25f, 1.15f, 0.1f);
            angled.transform.localScale = new Vector3(1.8f, 2.1f, 1f);
            angled.transform.rotation = Quaternion.Euler(0f, -24f, 0f);
            angled.GetComponent<MeshRenderer>().sharedMaterial = backMaterial;

            GameObject lightObject = new GameObject("SpriteSurfaceEvidenceLight");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.4f;
            light.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(52f, -32f, 0f);

            GameObject cameraObject = new GameObject("SpriteSurfaceEvidenceCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.07f, 0.1f, 1f);
            camera.fieldOfView = 42f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            cameraObject.transform.position = new Vector3(0f, 2.55f, -8.2f);
            cameraObject.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 0.95f, 0f) - cameraObject.transform.position, Vector3.up);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.16f, 0.18f, 0.22f, 1f);
            RenderSettings.fog = false;
            EditorSceneManager.SaveScene(scene, ScenePath);
            Selection.activeGameObject = front;
            Debug.Log("SE001 SpriteSurface evidence scene created at " + ScenePath);
        }
    }
}
