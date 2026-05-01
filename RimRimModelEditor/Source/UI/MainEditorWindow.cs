#pragma warning disable CS8600, CS8604, CS8618
using System;
using System.Collections.Generic;
using System.Linq;
using D4egon.RimRimModelEditor.Logic;
using D4egon.RimRimModelEditor.Model;
using D4egon.RimRimModelEditor.UI.Dialogs;
using D4egon.RimRimModelEditor.UI.Panels;
using UnityEngine;
using Verse;
using RimWorld;

namespace D4egon.RimRimModelEditor.UI
{
    public class MainEditorWindow : Window
    {
        // ── Multitab State ──────────────────────────────────────────────────
        public static List<EditorState> OpenStates = new List<EditorState>();
        public static int ActiveTabIndex = 0;

        public static EditorState CurrentState => (OpenStates.Count > 0 && ActiveTabIndex < OpenStates.Count)
            ? OpenStates[ActiveTabIndex]
            : null;

        // Undo stack — depth driven by Settings so the slider actually does something.
        private static UndoRedoStack<EditorState> UndoStack =
            new UndoRedoStack<EditorState>(RimRimMod.Settings?.UndoDepth ?? 50);

        // Named version history for the History tab.
        private readonly List<VersionEntry> _versionHistory = new List<VersionEntry>();
        private const int MaxVersionHistory = 100;

        // ── Layout Constants ─────────────────────────────────────────────────
        private const float Margin = 4f;
        private const float SidePanelWidth = 300f;
        private const float ToolbarHeight = 35f;
        private const float TimelineHeight = 40f;
        private const float AnimBarHeight  = 38f;

        // ── Panels ──────────────────────────────────────────────────────────
        private readonly DefListPanel    _defList    = new DefListPanel();
        private readonly PreviewPanel    _preview    = new PreviewPanel();
        private readonly InspectorPanel  _inspector  = new InspectorPanel();
        private readonly TimelinePanel   _timeline   = new TimelinePanel();
        private readonly ToolbarPanel    _toolbar    = new ToolbarPanel();
        private readonly LayerPanel      _layerPanel = new LayerPanel();
        private readonly SceneListPanel  _sceneList  = new SceneListPanel();
        private readonly AnimationPanel    _animPanel   = new AnimationPanel();
        private readonly AnimationBarPanel _animBar     = new AnimationBarPanel();
        private readonly LogicPanel      _logicPanel = new LogicPanel();
        private readonly PresetPanel     _presetPanel = new PresetPanel();
        private readonly HistoryPanel    _historyPanel = new HistoryPanel();

        private enum EditorTab { Layers, Scene, Animation, Logic, Presets, History, XML }
        private EditorTab _currentSubTab = EditorTab.Layers;

        // Stored every frame so TakeSnapshot() can pass it to ScreenshotCapture.
        private Rect _centerRect;

        public override Vector2 InitialSize
            => new Vector2(Verse.UI.screenWidth * 0.95f, Verse.UI.screenHeight * 0.9f);

        public MainEditorWindow()
        {
            this.forcePause = true;
            this.doCloseX = true;
            this.absorbInputAroundWindow = true;
            this.closeOnClickedOutside = false;
            this.resizeable = true;
            this.draggable = false;
            this.preventCameraMotion = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (CurrentState == null && OpenStates.Count == 0)
                OpenStates.Add(new EditorState());

            // Keep undo stack depth in sync with the settings slider.
            UndoStack.SetDepth(RimRimMod.Settings.UndoDepth);

            // Drag handle (top-left corner).
            GUI.DragWindow(new Rect(0, 0, 40f, 40f));

            // Custom background.
            if (CurrentState?.BackgroundTexture != null)
            {
                GUI.color = new Color(1f, 1f, 1f, CurrentState.BackgroundOpacity);
                GUI.DrawTexture(inRect, CurrentState.BackgroundTexture, ScaleMode.ScaleAndCrop);
                GUI.color = Color.white;
            }

            // ── Toolbar ─────────────────────────────────────────────────────
            Rect toolbarRect = new Rect(inRect.x, inRect.y, inRect.width, ToolbarHeight);
            DrawToolbarWithSelector(toolbarRect);

            float mainY = toolbarRect.yMax + Margin;
            float mainH = inRect.height - ToolbarHeight - Margin;

            // ── Left panel (Def browser) ─────────────────────────────────────
            Rect leftRect = new Rect(inRect.x, mainY, SidePanelWidth, mainH);
            _defList.Draw(leftRect, CurrentState, PushUndo);

            // ── Right panel (Inspector + sub-tabs) ───────────────────────────
            Rect rightRect = new Rect(inRect.xMax - SidePanelWidth, mainY, SidePanelWidth, mainH);
            DrawRightPanel(rightRect);

            // ── Centre (Preview + Timeline + AnimBar) ────────────────────────
            float centerW = inRect.width - (SidePanelWidth * 2) - (Margin * 2);
            _centerRect = new Rect(
                leftRect.xMax + Margin, mainY,
                centerW, mainH - TimelineHeight - AnimBarHeight - Margin * 2);

            if (CurrentState != null)
            {
                _preview.Draw(_centerRect, CurrentState);

                Rect timelineRect = new Rect(_centerRect.x, _centerRect.yMax + Margin, centerW, TimelineHeight);
                _timeline.Draw(timelineRect, CurrentState);

                Rect animBarRect = new Rect(_centerRect.x, timelineRect.yMax + Margin, centerW, AnimBarHeight);
                _animBar.Draw(animBarRect, CurrentState);
            }

            HandleShortcuts();

            // Must be the LAST call — reads pixels from the already-drawn framebuffer.
            ScreenshotCapture.TryExecutePending();
        }

        // ── Toolbar ─────────────────────────────────────────────────────────

        private void DrawToolbarWithSelector(Rect rect)
        {
            Rect selectorRect = new Rect(rect.x, rect.y, 220f, rect.height).ContractedBy(2f);

            // Button label: active scene's display name (custom name or first def or "Empty Scene")
            string btnLabel = CurrentState != null
                ? $"▼  {CurrentState.DisplayName}"
                : "▼  No Scene";

            TooltipHandler.TipRegion(selectorRect,
                "Switch between open studio scenes.\nRight-click options: Rename, Close.");

            if (Widgets.ButtonText(selectorRect, btnLabel))
            {
                var options = new List<FloatMenuOption>();

                for (int i = 0; i < OpenStates.Count; i++)
                {
                    int idx = i;
                    var scene = OpenStates[i];
                    bool isCurrent = (i == ActiveTabIndex);

                    // ── Scene header — click to switch ───────────────────
                    string sceneLabel = isCurrent
                        ? $"● {scene.DisplayName}"      // filled dot = active
                        : $"  {scene.DisplayName}";
                    options.Add(new FloatMenuOption(sceneLabel, () => ActiveTabIndex = idx));

                    // ── Rename ───────────────────────────────────────────
                    // Replace the FloatMenuOption instantiations for Rename and Close within the loop:

                    options.Add(new FloatMenuOption("    ✎  Rename \"" + scene.DisplayName + "\"...", () =>
                    {
                        Find.WindowStack.Add(new Dialog_InputPath(
                            newName =>
                            {
                                if (!string.IsNullOrWhiteSpace(newName))
                                    OpenStates[idx].SceneName = newName.Trim();
                            },
                            defaultPath: scene.SceneName,
                            title: "Scene name:"));
                    }));

                    options.Add(new FloatMenuOption("    ✕  Close \"" + scene.DisplayName + "\"", () =>
                    {
                        CloseScene(idx);
                    }));

                    // Separator between scenes (but not after the last one)
                    if (i < OpenStates.Count - 1)
                        options.Add(new FloatMenuOption("──────────────────────", null));
                }

                options.Add(new FloatMenuOption("+ New Scene", () =>
                {
                    OpenStates.Add(new EditorState());
                    ActiveTabIndex = OpenStates.Count - 1;
                }));

                Find.WindowStack.Add(new FloatMenu(options));
            }

            Rect toolbarArea = new Rect(selectorRect.xMax + 10f, rect.y,
                rect.width - selectorRect.width - 10f, rect.height);
            if (CurrentState != null)
                _toolbar.Draw(toolbarArea, CurrentState, Undo, Redo, OpenExportDialog, OpenShareDialog,
                    TakeScreenshot, DoHotReload, PushUndo, TakeSnapshot);
        }

        /// <summary>
        /// Removes the scene at <paramref name="idx"/> from the open list.
        /// Always ensures at least one scene remains; adjusts ActiveTabIndex to stay valid.
        /// </summary>
        private void CloseScene(int idx)
        {
            OpenStates.RemoveAt(idx);

            // Always keep at least one scene open.
            if (OpenStates.Count == 0)
            {
                OpenStates.Add(new EditorState());
                ActiveTabIndex = 0;
                return;
            }

            // Keep ActiveTabIndex in bounds — prefer the scene that was to the left.
            ActiveTabIndex = Mathf.Clamp(
                idx > 0 ? idx - 1 : 0,
                0, OpenStates.Count - 1);
        }

        // ── Right panel ──────────────────────────────────────────────────────

        private void DrawRightPanel(Rect rect)
        {
            if (CurrentState == null) return;

            // Inspector takes the top 45%.
            Rect inspectorRect = new Rect(rect.x, rect.y, rect.width, rect.height * 0.45f);
            _inspector.Draw(inspectorRect, CurrentState, PushUndo);

            Rect subTabRect = new Rect(rect.x, inspectorRect.yMax + Margin,
                rect.width, rect.height - inspectorRect.height - Margin);

            var subTabs = new List<TabRecord>
            {
                new TabRecord("Layers",   () => _currentSubTab = EditorTab.Layers,    _currentSubTab == EditorTab.Layers),
                new TabRecord("Scene",    () => _currentSubTab = EditorTab.Scene,     _currentSubTab == EditorTab.Scene),
                new TabRecord("Anim",     () => _currentSubTab = EditorTab.Animation, _currentSubTab == EditorTab.Animation),
                new TabRecord("Logic",    () => _currentSubTab = EditorTab.Logic,     _currentSubTab == EditorTab.Logic),
                new TabRecord("Presets",  () => _currentSubTab = EditorTab.Presets,   _currentSubTab == EditorTab.Presets),
                new TabRecord("History",  () => _currentSubTab = EditorTab.History,   _currentSubTab == EditorTab.History),
                new TabRecord("XML",      () => _currentSubTab = EditorTab.XML,       _currentSubTab == EditorTab.XML),
            };

            TabDrawer.DrawTabs(subTabRect, subTabs);
            Rect contentRect = subTabRect.ContractedBy(2f);
            contentRect.yMin += 32f;

            switch (_currentSubTab)
            {
                case EditorTab.Layers:
                    _layerPanel.Draw(contentRect, CurrentState, PushUndo);
                    break;
                case EditorTab.Scene:
                    _sceneList.Draw(contentRect, CurrentState);
                    break;
                case EditorTab.Animation:
                    _animPanel.Draw(contentRect, CurrentState);
                    break;
                case EditorTab.Logic:
                    _logicPanel.Draw(contentRect, CurrentState, PushUndo);
                    break;
                case EditorTab.Presets:
                    _presetPanel.Draw(contentRect, CurrentState, PushUndo);
                    break;
                case EditorTab.History:
                    _historyPanel.Draw(contentRect, CurrentState, _versionHistory, RestoreVersion);
                    break;
                case EditorTab.XML:
                    XmlPreviewPanel.Draw(contentRect, CurrentState);
                    break;
            }
        }

        // ── Undo / Redo / History ────────────────────────────────────────────

        private void PushUndo(string label)
        {
            if (CurrentState == null) return;
            var snapshot = CurrentState.Clone();
            UndoStack.Push(snapshot);

            // Also record a named entry in the History tab.
            _versionHistory.Add(new VersionEntry(label, CurrentState));
            if (_versionHistory.Count > MaxVersionHistory)
                _versionHistory.RemoveAt(0);
        }

        private void Undo()
        {
            if (CurrentState == null) return;
            var s = UndoStack.Undo(CurrentState);
            if (s != null) OpenStates[ActiveTabIndex] = s;
        }

        private void Redo()
        {
            if (CurrentState == null) return;
            var s = UndoStack.Redo(CurrentState);
            if (s != null) OpenStates[ActiveTabIndex] = s;
        }

        private void RestoreVersion(VersionEntry entry)
        {
            if (entry?.Snapshot == null) return;
            PushUndo("Before restore");
            OpenStates[ActiveTabIndex] = entry.Snapshot.Clone();
        }

        // ── Keyboard shortcuts ───────────────────────────────────────────────

        private void HandleShortcuts()
        {
            if (CurrentState == null) return;

            // Undo / Redo — also handled by the standard Ctrl+Z/Y for muscle memory.
            if (KeyBindingDefOf.RimRimModelEditor_Undo.KeyDownEvent ||
                (Event.current.type == EventType.KeyDown && Event.current.control && Event.current.keyCode == KeyCode.Z))
            { Undo(); Event.current.Use(); return; }

            if (KeyBindingDefOf.RimRimModelEditor_Redo.KeyDownEvent ||
                (Event.current.type == EventType.KeyDown && Event.current.control && Event.current.keyCode == KeyCode.Y))
            { Redo(); Event.current.Use(); return; }

            // File operations
            if (KeyBindingDefOf.RimRimModelEditor_Save.KeyDownEvent)
            { PresetIO.SaveDialog(CurrentState); Event.current.Use(); return; }

            if (KeyBindingDefOf.RimRimModelEditor_Export.KeyDownEvent)
            { OpenExportDialog(); Event.current.Use(); return; }

            if (KeyBindingDefOf.RimRimModelEditor_BatchExport.KeyDownEvent)
            { BatchExporter.Run(CurrentState); Event.current.Use(); return; }

            if (KeyBindingDefOf.RimRimModelEditor_Share.KeyDownEvent)
            { OpenShareDialog(); Event.current.Use(); return; }

            // View
            if (KeyBindingDefOf.RimRimModelEditor_Screenshot.KeyDownEvent)
            { TakeScreenshot(); Event.current.Use(); return; }

            if (KeyBindingDefOf.RimRimModelEditor_HotReload.KeyDownEvent)
            { DoHotReload(); Event.current.Use(); return; }

            if (KeyBindingDefOf.RimRimModelEditor_ZoomIn.KeyDownEvent)
            { CurrentState.PreviewZoom = Mathf.Clamp(CurrentState.PreviewZoom + 0.1f, 0.1f, 10f); Event.current.Use(); return; }

            if (KeyBindingDefOf.RimRimModelEditor_ZoomOut.KeyDownEvent)
            { CurrentState.PreviewZoom = Mathf.Clamp(CurrentState.PreviewZoom - 0.1f, 0.1f, 10f); Event.current.Use(); return; }

            if (KeyBindingDefOf.RimRimModelEditor_ResetView.KeyDownEvent)
            { CurrentState.PreviewZoom = 1f; CurrentState.PreviewPan = Vector2.zero; Event.current.Use(); return; }

            // Overlays
            if (KeyBindingDefOf.RimRimModelEditor_ToggleCollision.KeyDownEvent)
            { CurrentState.ShowCollision = !CurrentState.ShowCollision; Event.current.Use(); return; }

            if (KeyBindingDefOf.RimRimModelEditor_TogglePlacement.KeyDownEvent)
            { CurrentState.ShowPlacement = !CurrentState.ShowPlacement; Event.current.Use(); return; }

            // Animation frame stepping
            if (KeyBindingDefOf.RimRimModelEditor_NextFrame.KeyDownEvent)
            {
                var frames = AnimationFrameDiscovery.GetFrames(CurrentState.SelectedDefName);
                int count = frames?.Count ?? 1;
                CurrentState.CurrentFrame = (CurrentState.CurrentFrame + 1) % count;
                Event.current.Use(); return;
            }

            if (KeyBindingDefOf.RimRimModelEditor_PrevFrame.KeyDownEvent)
            {
                var frames = AnimationFrameDiscovery.GetFrames(CurrentState.SelectedDefName);
                int count = frames?.Count ?? 1;
                CurrentState.CurrentFrame = (CurrentState.CurrentFrame - 1 + count) % count;
                Event.current.Use(); return;
            }

            // Layers
            if (KeyBindingDefOf.RimRimModelEditor_AddLayer.KeyDownEvent)
            {
                if (CurrentState.SelectedObject != null)
                {
                    PushUndo("Add layer (keybind)");
                    CurrentState.Layers.Add(new LayerData { Name = $"Layer {CurrentState.Layers.Count + 1}" });
                    CurrentState.ActiveLayerIndex = CurrentState.Layers.Count - 1;
                }
                Event.current.Use(); return;
            }

            if (KeyBindingDefOf.RimRimModelEditor_DeleteLayer.KeyDownEvent)
            {
                if (CurrentState.ActiveLayer != null)
                {
                    PushUndo("Delete layer (keybind)");
                    CurrentState.Layers.RemoveAt(CurrentState.ActiveLayerIndex);
                    CurrentState.ActiveLayerIndex = Mathf.Clamp(
                        CurrentState.ActiveLayerIndex, 0, CurrentState.Layers.Count - 1);
                }
                Event.current.Use(); return;
            }
        }

        // ── Actions ──────────────────────────────────────────────────────────

        private void OpenExportDialog() => Find.WindowStack.Add(new ExportDialog(CurrentState));
        private void OpenShareDialog()  => Find.WindowStack.Add(new ShareDialog(CurrentState));
        private void TakeScreenshot()   => ScreenshotCapture.Capture($"Studio_{CurrentState?.SelectedDefName ?? "scene"}_{DateTime.Now:HHmmss}");

        /// <summary>
        /// Queues a preview-panel snapshot. When the next Repaint fires the pixels are
        /// sampled and Dialog_PortraitExport is opened so the user can name and export
        /// the painting mod.
        /// </summary>
        private void TakeSnapshot()
        {
            // windowRect is in screen coordinates; _centerRect is in window-content-local space.
            // ScreenshotCapture.ExecuteSnapshot() adds them together and flips Y itself.
            var stateForDialog = CurrentState;
            ScreenshotCapture.RequestSnapshot(this.windowRect, _centerRect,
                tex => Find.WindowStack.Add(new Dialog_PortraitExport(tex, stateForDialog)));
        }

        private void DoHotReload()
        {
            if (CurrentState?.SelectedDefName != null)
                HotReloader.Reload(CurrentState.SelectedDefName);
            else
                Messages.Message("Hot-reload: no def selected.", MessageTypeDefOf.RejectInput, false);
        }
    }
}
