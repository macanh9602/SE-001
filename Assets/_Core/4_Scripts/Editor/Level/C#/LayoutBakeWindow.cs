#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SE001.Editor.Level
{
    /// <summary>
    /// Layout Bake — GD tool that turns an SVG into a baked layout (prefab + mask) used by levels.
    /// Partials: lifecycle/state (this file), SVG source + bake, existing layouts, status bar.
    /// The bake pipeline itself lives in <see cref="LayoutBaker"/> and is not changed by the UI.
    /// </summary>
    public sealed partial class LayoutBakeWindow : EditorWindow
    {
        private const string StyleSheetPath = "Assets/_Core/4_Scripts/Editor/Level/USS/LayoutBakeWindow.uss";

        // ViewState — persisted by Unity across domain reload (EditorWindow serialization). Never baked.
        [SerializeField] private string svgPath = string.Empty;
        [SerializeField] private string layoutId = string.Empty;
        [SerializeField] private string searchText = string.Empty;
        [SerializeField] private string selectedLayoutId = string.Empty;
        [SerializeField] private bool layoutIdEditedByUser;

        // DerivedState — rebuilt at refresh boundaries only.
        private readonly LayoutBakeLibrary library = new LayoutBakeLibrary();
        private SvgParseResult currentParse;

        [MenuItem("SE001/Phase D/Layout Bake")]
        public static void Open()
        {
            LayoutBakeWindow window = GetWindow<LayoutBakeWindow>();
            window.titleContent = new GUIContent("Layout Bake");
            window.minSize = new Vector2(640f, 420f);
            window.Show();
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.Clear();
            StyleSheet sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(StyleSheetPath);
            if (sheet != null) root.styleSheets.Add(sheet);
            root.AddToClassList("lb-root");

            BuildHeader(root);
            ScrollView scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("lb-scroll");
            root.Add(scroll);
            BuildSourceSection(scroll.contentContainer);
            BuildLibrarySection(scroll.contentContainer);
            BuildAdvancedSection(scroll.contentContainer);
            BuildStatusBar(root);

            RefreshAll();
        }

        private void OnFocus()
        {
            // Lifecycle-safe refresh boundary: picks up SVG edits made in another app. Unchanged SVGs hit the parse cache.
            if (sourceSection != null) RefreshAll();
        }

        private static void BuildHeader(VisualElement root)
        {
            VisualElement header = new VisualElement();
            header.AddToClassList("lb-header");
            Label title = new Label("Layout Bake");
            title.AddToClassList("lb-title");
            Label subtitle = new Label("Create or update baked layouts from SVG. Levels pick a baked layout in the Level Editor.");
            subtitle.AddToClassList("lb-subtitle");
            header.Add(title);
            header.Add(subtitle);
            root.Add(header);
        }

        private void RefreshAll()
        {
            library.Refresh();
            RefreshSourceParse();
            RefreshSourceView();
            RefreshLibraryView();
            RefreshAdvancedView();
        }

        private string BlockingEditorState()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return "Exit Play Mode to bake layouts.";
            if (EditorApplication.isCompiling) return "Wait until Unity finishes compiling.";
            if (!string.IsNullOrEmpty(library.ProfileError)) return library.ProfileError;
            return null;
        }

        private static string ToAbsolute(string path)
        {
            return LayoutBaker.ResolveProjectPath(path);
        }

        private static string DisplayName(string path)
        {
            return string.IsNullOrWhiteSpace(path) ? string.Empty : Path.GetFileName(path.Replace('\\', '/'));
        }
    }
}
#endif
