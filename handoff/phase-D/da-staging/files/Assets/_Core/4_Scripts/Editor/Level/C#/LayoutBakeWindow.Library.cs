#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace SE001.Editor.Level
{
    public sealed partial class LayoutBakeWindow
    {
        private VisualElement librarySection;
        private ToolbarSearchField searchField;
        private VisualElement layoutRows;
        private Button rebakeAllButton;
        private VisualElement rebakeAllConfirm;
        private Label rebakeAllConfirmText;
        private Button rebakeAllConfirmButton;

        private void BuildLibrarySection(VisualElement parent)
        {
            librarySection = new VisualElement();
            librarySection.AddToClassList("lb-section");
            parent.Add(librarySection);

            VisualElement header = new VisualElement();
            header.AddToClassList("lb-list-header");
            Label title = new Label("Existing Layouts");
            title.AddToClassList("lb-section-title");
            title.AddToClassList("lb-grow");
            searchField = new ToolbarSearchField { tooltip = "Filter layouts by name or SVG file." };
            searchField.AddToClassList("lb-search");
            searchField.SetValueWithoutNotify(searchText);
            searchField.RegisterValueChangedCallback(OnSearchChanged);
            Button refresh = new Button(OnRefreshClicked) { text = "Refresh", tooltip = "Re-check every layout against its SVG file." };
            refresh.AddToClassList("btn-secondary");
            header.Add(title);
            header.Add(searchField);
            header.Add(refresh);
            librarySection.Add(header);

            layoutRows = new VisualElement();
            librarySection.Add(layoutRows);

            rebakeAllButton = new Button(ShowRebakeAllConfirm) { text = "Rebake All…" };
            rebakeAllButton.AddToClassList("btn-bulk");
            librarySection.Add(rebakeAllButton);

            rebakeAllConfirm = new VisualElement();
            rebakeAllConfirm.AddToClassList("lb-confirm");
            rebakeAllConfirmText = new Label();
            rebakeAllConfirmText.AddToClassList("lb-message-body");
            VisualElement confirmButtons = new VisualElement();
            confirmButtons.AddToClassList("lb-action-row");
            Button cancel = new Button(HideRebakeAllConfirm) { text = "Cancel" };
            cancel.AddToClassList("btn-secondary");
            rebakeAllConfirmButton = new Button(RunRebakeAll);
            rebakeAllConfirmButton.AddToClassList("btn-warning");
            confirmButtons.Add(cancel);
            confirmButtons.Add(rebakeAllConfirmButton);
            rebakeAllConfirm.Add(rebakeAllConfirmText);
            rebakeAllConfirm.Add(confirmButtons);
            rebakeAllConfirm.style.display = DisplayStyle.None;
            librarySection.Add(rebakeAllConfirm);
        }

        private void OnSearchChanged(ChangeEvent<string> change)
        {
            searchText = change.newValue ?? string.Empty;
            RefreshLibraryView();
        }

        private void OnRefreshClicked()
        {
            RefreshAll();
            SetStatus("info", "Checked " + library.Entries.Count + " layout(s).");
        }

        private void RefreshLibraryView()
        {
            if (layoutRows == null) return;
            layoutRows.Clear();
            IReadOnlyList<LayoutBakeEntry> entries = library.Entries;
            if (entries.Count == 0)
            {
                Label empty = new Label("No baked layouts yet. Choose an SVG above and press Bake Layout — it will appear here.");
                empty.AddToClassList("empty-state");
                layoutRows.Add(empty);
            }

            int shown = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (!MatchesSearch(entries[i])) continue;
                layoutRows.Add(BuildRow(entries[i]));
                shown++;
            }

            if (entries.Count > 0 && shown == 0)
            {
                Label noMatch = new Label("No layout matches \"" + searchText + "\".");
                noMatch.AddToClassList("empty-state");
                layoutRows.Add(noMatch);
            }

            int rebakeable = CountRebakeable();
            string blocker = BlockingEditorState();
            rebakeAllButton.SetEnabled(rebakeable > 0 && blocker == null);
            rebakeAllButton.tooltip = blocker ?? (rebakeable > 0
                ? "Rebake every layout that has its SVG file (" + rebakeable + ")."
                : "No layout has an SVG file to rebake from.");
        }

        private bool MatchesSearch(LayoutBakeEntry entry)
        {
            if (string.IsNullOrWhiteSpace(searchText)) return true;
            string needle = searchText.Trim();
            return entry.LayoutId.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0 ||
                entry.SourceFileName.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private VisualElement BuildRow(LayoutBakeEntry entry)
        {
            LayoutBakeStatus status = entry.Status;
            VisualElement row = new VisualElement();
            row.AddToClassList("lb-layout-row");
            row.EnableInClassList("lb-layout-row--selected", string.Equals(entry.LayoutId, selectedLayoutId, StringComparison.Ordinal));
            row.RegisterCallback<ClickEvent>(evt =>
            {
                // Row buttons handle their own action; only clicks on the row body select it.
                if (evt.target is Button) return;
                SelectEntry(entry);
            });

            Label badge = new Label(BadgeIcon(status.State) + " " + status.Headline);
            badge.AddToClassList("lb-badge");
            badge.AddToClassList(BadgeClass(status.State));
            badge.tooltip = status.Reason;
            row.Add(badge);

            VisualElement text = new VisualElement();
            text.AddToClassList("lb-layout-text");
            Label name = new Label(entry.LayoutId);
            name.AddToClassList("lb-layout-name");
            Label detail = new Label(DetailLine(entry));
            detail.AddToClassList("lb-layout-detail");
            text.Add(name);
            text.Add(detail);
            row.Add(text);

            VisualElement actions = new VisualElement();
            actions.AddToClassList("lb-row-actions");
            Button rebake = new Button(() => RebakeEntry(entry.LayoutId)) { text = "Rebake" };
            rebake.AddToClassList("btn-secondary");
            string blocker = BlockingEditorState();
            rebake.SetEnabled(status.CanRebake && blocker == null);
            rebake.tooltip = blocker ?? (status.CanRebake ? "Rebake from " + entry.SourceFileName + "." : status.Recovery);
            Button ping = new Button(() => PingEntry(entry)) { text = "Show", tooltip = "Select this layout asset in the Project window." };
            ping.AddToClassList("btn-secondary");
            actions.Add(rebake);
            actions.Add(ping);
            row.Add(actions);
            return row;
        }

        private static string DetailLine(LayoutBakeEntry entry)
        {
            LayoutBakeStatus status = entry.Status;
            string source = "SVG: " + entry.SourceFileName;
            if (status.State == LayoutBakeState.Ready) return source;
            string recovery = string.IsNullOrEmpty(status.Recovery) ? string.Empty : " " + status.Recovery;
            return source + " · " + status.Reason + recovery;
        }

        private static string BadgeIcon(LayoutBakeState state)
        {
            switch (state)
            {
                case LayoutBakeState.Ready: return "✔";
                case LayoutBakeState.NeedsRebake: return "↻";
                case LayoutBakeState.SourceMissing: return "?";
                default: return "✖";
            }
        }

        private static string BadgeClass(LayoutBakeState state)
        {
            switch (state)
            {
                case LayoutBakeState.Ready: return "badge-success";
                case LayoutBakeState.NeedsRebake: return "badge-warning";
                case LayoutBakeState.SourceMissing: return "badge-warning";
                default: return "badge-blocking";
            }
        }

        private void SelectEntry(LayoutBakeEntry entry)
        {
            selectedLayoutId = entry.LayoutId;
            if (!string.IsNullOrWhiteSpace(entry.Facts.SourceSvgPath))
            {
                layoutIdEditedByUser = false;
                layoutId = entry.LayoutId;
                layoutIdField.SetValueWithoutNotify(layoutId);
                SetSvgPath(entry.Facts.SourceSvgPath, false);
            }

            SetStatus("info", entry.LayoutId + ": " + entry.Status.Headline + ". " + entry.Status.Reason);
            RefreshLibraryView();
            RefreshAdvancedView();
        }

        private static void PingEntry(LayoutBakeEntry entry)
        {
            if (entry.Definition == null) return;
            Selection.activeObject = entry.Definition;
            EditorGUIUtility.PingObject(entry.Definition);
        }

        private void RebakeEntry(string id)
        {
            LayoutBakeEntry entry = library.Find(id);
            if (entry == null) return;
            selectedLayoutId = id;
            string error;
            if (RunBake(entry.Facts.SourceSvgPath, id, out error)) SetStatus("success", "Rebaked '" + id + "' — Ready.");
            else SetStatus("error", "Rebake failed for '" + id + "': " + error + " Fix the SVG, then press Rebake again.");
            RefreshAll();
        }

        private int CountRebakeable()
        {
            int count = 0;
            for (int i = 0; i < library.Entries.Count; i++)
                if (library.Entries[i].Status.CanRebake)
                    count++;
            return count;
        }

        private void ShowRebakeAllConfirm()
        {
            int rebakeable = CountRebakeable();
            int skipped = library.Entries.Count - rebakeable;
            string skippedNote = skipped > 0 ? " " + skipped + " layout(s) without a usable SVG are skipped." : string.Empty;
            rebakeAllConfirmText.text = "Rebake " + rebakeable + " layout(s) from their SVG files? " +
                "Their baked prefab and mask are replaced; levels keep pointing at the same layouts." + skippedNote;
            rebakeAllConfirmButton.text = "Rebake " + rebakeable + " Layout(s)";
            rebakeAllConfirm.style.display = DisplayStyle.Flex;
            rebakeAllButton.style.display = DisplayStyle.None;
        }

        private void HideRebakeAllConfirm()
        {
            rebakeAllConfirm.style.display = DisplayStyle.None;
            rebakeAllButton.style.display = DisplayStyle.Flex;
        }

        private void RunRebakeAll()
        {
            HideRebakeAllConfirm();
            List<LayoutBakeEntry> targets = new List<LayoutBakeEntry>();
            for (int i = 0; i < library.Entries.Count; i++)
                if (library.Entries[i].Status.CanRebake)
                    targets.Add(library.Entries[i]);
            int skipped = library.Entries.Count - targets.Count;
            int succeeded = 0;
            List<string> failed = new List<string>();
            try
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    LayoutBakeEntry entry = targets[i];
                    EditorUtility.DisplayProgressBar("Rebake All", "Rebaking " + entry.LayoutId, (float)i / targets.Count);
                    string error;
                    if (RunBake(entry.Facts.SourceSvgPath, entry.LayoutId, out error)) succeeded++;
                    else failed.Add(entry.LayoutId);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            string summary = "Rebake All: " + succeeded + " rebaked · " + failed.Count + " failed · " + skipped + " skipped (no usable SVG).";
            if (failed.Count > 0) SetStatus("error", summary + " Failed: " + string.Join(", ", failed) + " — see their rows.");
            else SetStatus("success", summary);
            RefreshAll();
        }
    }
}
#endif
