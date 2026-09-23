#if UNITY_EDITOR
using System.Globalization;
using UnityEngine.UIElements;

namespace SE001.Editor.Level
{
    public sealed partial class LayoutBakeWindow
    {
        private Label statusLabel;
        private Foldout advancedFoldout;
        private Label advancedText;

        private void BuildStatusBar(VisualElement root)
        {
            VisualElement bar = new VisualElement();
            bar.AddToClassList("lb-status");
            statusLabel = new Label("Ready.");
            statusLabel.AddToClassList("lb-status-label");
            bar.Add(statusLabel);
            root.Add(bar);
        }

        private void BuildAdvancedSection(VisualElement parent)
        {
            advancedFoldout = new Foldout { text = "Advanced (technical details)", value = false };
            advancedFoldout.AddToClassList("lb-advanced");
            advancedText = new Label();
            advancedText.AddToClassList("lb-advanced-text");
            advancedFoldout.Add(advancedText);
            parent.Add(advancedFoldout);
        }

        private void RefreshAdvancedView()
        {
            if (advancedText == null) return;
            string text = "Sand grid cell: " + library.RuntimeCellSize.ToString("0.####", CultureInfo.InvariantCulture) +
                " · max cells: " + library.RuntimeMaxCells.ToString("N0", CultureInfo.InvariantCulture) +
                " · importer v" + LayoutBaker.ImporterVersion +
                "\nLayouts folder: " + LayoutBaker.LayoutResourceFolder +
                "\nPrefabs folder: " + LayoutBaker.LayoutPrefabFolder;
            LayoutBakeEntry selected = library.Find(selectedLayoutId);
            if (selected != null)
            {
                LayoutBakeFacts facts = selected.Facts;
                text += "\n\nSelected: " + facts.LayoutId +
                    "\nSVG path: " + (string.IsNullOrEmpty(facts.SourceSvgPath) ? "(none)" : facts.SourceSvgPath) +
                    "\nLayout hash: " + Short(facts.DefinitionHash) +
                    "\nMask hash: " + Short(facts.MaskHash) +
                    "\nSVG hash now: " + Short(facts.SourceHash) +
                    "\nMask cell size: " + facts.MaskCellSize.ToString("0.####", CultureInfo.InvariantCulture) +
                    " · importer v" + facts.MaskImporterVersion;
            }

            advancedText.text = text;
        }

        private static string Short(string hash)
        {
            if (string.IsNullOrEmpty(hash)) return "(none)";
            return hash.Length > 12 ? hash.Substring(0, 12) + "…" : hash;
        }

        // severity: info | success | warning | error
        private void SetStatus(string severity, string message)
        {
            if (statusLabel == null) return;
            statusLabel.text = message;
            statusLabel.EnableInClassList("status-error", severity == "error");
            statusLabel.EnableInClassList("status-warning", severity == "warning");
            statusLabel.EnableInClassList("status-success", severity == "success");
        }

        private static void ShowMessage(VisualElement host, string severity, string what, string where, string how)
        {
            host.Clear();
            VisualElement box = new VisualElement();
            box.AddToClassList("lb-message");
            box.AddToClassList("msg-" + severity);
            Label title = new Label(what);
            title.AddToClassList("lb-message-title");
            box.Add(title);
            if (!string.IsNullOrEmpty(where))
            {
                Label body = new Label(where);
                body.AddToClassList("lb-message-body");
                box.Add(body);
            }

            if (!string.IsNullOrEmpty(how))
            {
                Label fix = new Label(how);
                fix.AddToClassList("lb-message-body");
                box.Add(fix);
            }

            host.Add(box);
        }

        private static void ClearMessage(VisualElement host)
        {
            if (host != null) host.Clear();
        }
    }
}
#endif
