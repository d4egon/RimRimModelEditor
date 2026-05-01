#pragma warning disable CS8600, CS8602, CS8604
using System;
using System.Collections.Generic;
using System.Linq;
using D4egon.RimRimModelEditor.Logic;
using D4egon.RimRimModelEditor.Model;
using D4egon.RimRimModelEditor.UI.Dialogs;
using UnityEngine;
using Verse;
using RimWorld;

namespace D4egon.RimRimModelEditor.UI.Panels
{
    /// <summary>
    /// Custom input window to bypass IRenameable constraints.
    /// </summary>
    public class Dialog_InputPath : Window
    {
        private string _curPath;
        private readonly Action<string> _onConfirm;
        private readonly string _title;

        public override Vector2 InitialSize => new Vector2(400f, 120f);

        public Dialog_InputPath(Action<string> onConfirm, string defaultPath = "", string title = "Enter Texture Path:")
        {
            this._curPath = defaultPath;
            this._onConfirm = onConfirm;
            this._title = title;
            this.doCloseX = true;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            this.closeOnClickedOutside = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(0, 0, inRect.width, 24f), _title);
            
            _curPath = Widgets.TextField(new Rect(0, 30f, inRect.width, 30f), _curPath);

            if (Widgets.ButtonText(new Rect(0, inRect.height - 35f, inRect.width * 0.5f - 5f, 35f), "Confirm"))
            {
                _onConfirm?.Invoke(_curPath);
                Close();
            }

            if (Widgets.ButtonText(new Rect(inRect.width * 0.5f + 5f, inRect.height - 35f, inRect.width * 0.5f - 5f, 35f), "Cancel"))
            {
                Close();
            }
        }
    }

    /// <summary>Tiny float-menu-sized window for tuning background opacity.</summary>
    public class Dialog_BackgroundOpacity : Window
    {
        private readonly EditorState _state;
        public override Vector2 InitialSize => new Vector2(300f, 100f);

        public Dialog_BackgroundOpacity(EditorState state)
        {
            _state = state;
            doCloseX = true;
            forcePause = false;
            absorbInputAroundWindow = false;
            closeOnClickedOutside = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(0, 0, inRect.width, 24f),
                $"Background Opacity: {_state.BackgroundOpacity:P0}");
            _state.BackgroundOpacity =
                Widgets.HorizontalSlider(new Rect(0, 30f, inRect.width, 20f),
                    _state.BackgroundOpacity, 0f, 1f);
        }
    }

    public class ToolbarPanel
    {
        public void Draw(Rect rect, EditorState state, Action undoAction, Action redoAction, Action openExport, Action openShare, Action screenshot, Action hotReload, Action<string> pushUndo, Action snapshotAction = null)
        {
            if (state == null) return;

            float x = rect.x + 5f; 
            float y = rect.y + 2f;
            float h = rect.height - 4f;
            float gap = 2f;

            // --- CATEGORY SELECTOR ---
            Rect catRect = new Rect(x, y, 100f, h);
            TooltipHandler.TipRegion(catRect,
                "Switch the active Def type.\nChanges what XML the studio generates and which\nquick-pickers appear in the Layer panel.");
            if (Widgets.ButtonText(catRect, state.CurrentDefType.ToString()))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (RimWorldDefType type in Enum.GetValues(typeof(RimWorldDefType)))
                {
                    options.Add(new FloatMenuOption(type.ToString(), () => {
                        pushUndo("Change Def Type");
                        state.CurrentDefType = type;
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
            x += 100f + 10f;

            Btn(ref x, y, 60f, h, gap, "Undo",  undoAction,
                "Undo the last action.\nShortcut: Ctrl+Z");
            Btn(ref x, y, 60f, h, gap, "Redo",  redoAction,
                "Redo the last undone action.\nShortcut: Ctrl+Y");

            x += 10f;

            Btn(ref x, y, 90f, h, gap, "Save Preset", () => PresetIO.SaveDialog(state),
                "Save the current scene to a .rimrim preset file\nso you can reload it later.");
            Btn(ref x, y, 85f, h, gap, "Export XML",  openExport,
                "Export the scene as RimWorld Def XML.\nDrops a ready-to-use mod file into the chosen folder.\nShortcut: Ctrl+E");
            
            // --- BACKGROUND BUTTON ---
            TooltipHandler.TipRegion(new Rect(x, y, 95f, h),
                "Set or clear the preview background.\nChoose a solid colour preset, load a RimWorld\ntexture path, or import an image from disk.");
            Btn(ref x, y, 95f, h, gap, "Background", () => {
                List<FloatMenuOption> bgOptions = new List<FloatMenuOption>();

                // ── Solid colour presets ────────────────────────────────────
                bgOptions.Add(new FloatMenuOption("── Solid colours ──", null));

                AddSolidPreset(bgOptions, state, "Black",          new Color(0f,    0f,    0f   ));
                AddSolidPreset(bgOptions, state, "Dark grey",      new Color(0.15f, 0.15f, 0.15f));
                AddSolidPreset(bgOptions, state, "Mid grey",       new Color(0.4f,  0.4f,  0.4f ));
                AddSolidPreset(bgOptions, state, "White",          new Color(1f,    1f,    1f   ));
                AddSolidPreset(bgOptions, state, "Sky blue",       new Color(0.47f, 0.65f, 0.85f));
                AddSolidPreset(bgOptions, state, "Warm parchment", new Color(0.87f, 0.78f, 0.60f));
                AddSolidPreset(bgOptions, state, "Forest green",   new Color(0.18f, 0.35f, 0.18f));
                AddSolidPreset(bgOptions, state, "Deep space",     new Color(0.05f, 0.05f, 0.15f));

                bgOptions.Add(new FloatMenuOption("── Custom ──", null));

                // Load a texture from a RimWorld content path
                bgOptions.Add(new FloatMenuOption("RimWorld texture path…", () => {
                    Find.WindowStack.Add(new Dialog_InputPath((string newPath) => {
                        var tex = ContentFinder<Texture2D>.Get(newPath, false);
                        if (tex != null)
                            state.BackgroundTexture = tex;
                        else
                            Messages.Message($"Texture not found: {newPath}", MessageTypeDefOf.RejectInput, false);
                    }, "UI/Overlays/WorldTransitionTex"));
                }));

                // Load an image from an absolute file path on disk
                bgOptions.Add(new FloatMenuOption("Load from file…", () => {
                    Find.WindowStack.Add(new Dialog_FilePicker(path => {
                        LayerOperations.LoadExternalTexture(state, path);
                    }));
                }));

                // Opacity slider
                bgOptions.Add(new FloatMenuOption($"Opacity: {state.BackgroundOpacity:P0}", () => {
                    Find.WindowStack.Add(new Dialog_BackgroundOpacity(state));
                }));

                bgOptions.Add(new FloatMenuOption("Clear background", () => state.BackgroundTexture = null));
                Find.WindowStack.Add(new FloatMenu(bgOptions));
            });
    
            x += 10f; 

            Btn(ref x, y, 85f, h, gap, "Screenshot", screenshot,
                "Save a full-window screenshot to\nDesktop/RimRimScreenshots/.\nShortcut: Ctrl+F12");
            Btn(ref x, y, 80f, h, gap, "Snapshot", snapshotAction,
                "Capture the preview panel and open\nthe export dialog to save it as a\nportrait painting mod or XML.");
            Btn(ref x, y, 85f, h, gap, "Hot-Reload", hotReload,
                "Force-reload the selected Def's textures\nwithout restarting the game.\nShortcut: Ctrl+R");
            Btn(ref x, y, 60f, h, gap, "Share", openShare,
                "Share the current scene as a\nmod-compatible package.\nShortcut: Ctrl+Shift+S");
        }

        private void Btn(ref float x, float y, float w, float h, float gap,
            string label, Action action, string tip = null)
        {
            var r = new Rect(x, y, w, h);
            if (tip != null) TooltipHandler.TipRegion(r, tip);
            if (Widgets.ButtonText(r, label)) action?.Invoke();
            x += w + gap;
        }

        /// <summary>Creates a 1×1 pixel Texture2D filled with a solid colour.</summary>
        private static Texture2D MakeSolidTexture(Color color)
        {
            var tex = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

        /// <summary>Adds a solid-colour preset entry to a background FloatMenu list.</summary>
        private static void AddSolidPreset(List<FloatMenuOption> list, EditorState state,
            string label, Color color)
        {
            list.Add(new FloatMenuOption(label, () =>
            {
                state.BackgroundTexture = MakeSolidTexture(color);
                state.BackgroundOpacity = 1f;
            }));
        }
    }
}