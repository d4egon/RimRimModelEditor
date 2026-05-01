using System;
using System.IO;
using System.Text;
using D4egon.RimRimModelEditor.Logic;
using D4egon.RimRimModelEditor.Model;
using RimWorld;
using UnityEngine;
using Verse;

namespace D4egon.RimRimModelEditor.UI.Dialogs
{
    /// <summary>
    /// Export dialog shown after the user clicks "Snapshot".
    /// Offers three export modes:
    ///   • Entire Scene  — the captured preview snapshot → portrait painting mod
    ///   • Current Layer — the active layer's resolved texture → portrait painting mod
    ///   • Scene as XML  — every layer written to a fully-annotated Defs XML file
    /// </summary>
    public class Dialog_PortraitExport : Window
    {
        // ── Export modes ──────────────────────────────────────────────────────
        private enum ExportMode { EntireScene, CurrentLayer, SceneXml }

        private ExportMode _mode = ExportMode.EntireScene;

        // ── Data ──────────────────────────────────────────────────────────────
        private readonly Texture2D  _snapshotTex;   // full-scene capture — we own this
        private readonly EditorState _state;

        private string _defName      = "Portrait_MyColonist";
        private string _outputFolder = "";

        // We only destroy the texture on close when we created it (full-scene snapshot).
        // ContentFinder textures are owned by the engine — never destroy those.
        private bool _ownsSnapshot;

        // ── Layout constants ──────────────────────────────────────────────────
        private const float PreviewSize = 160f;
        private const float LabelH      = 22f;
        private const float FieldH      = 28f;
        private const float ModeRowH    = 28f;
        private const float BtnH        = 34f;
        private const float Pad         = 6f;

        public override Vector2 InitialSize => new Vector2(520f, 460f);

        // ── Constructor ───────────────────────────────────────────────────────
        /// <param name="snapshotTex">Full-scene texture captured by ScreenshotCapture (may be null).</param>
        /// <param name="state">Current editor state, used for Current-Layer and XML modes.</param>
        public Dialog_PortraitExport(Texture2D snapshotTex, EditorState state)
        {
            _snapshotTex  = snapshotTex;
            _ownsSnapshot = (snapshotTex != null);
            _state        = state;

            doCloseX               = true;
            forcePause             = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside  = false;

            _outputFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "RimWorldPortraits");
        }

        // ── Cleanup ───────────────────────────────────────────────────────────
        public override void PostClose()
        {
            base.PostClose();
            if (_ownsSnapshot && _snapshotTex != null)
                UnityEngine.Object.Destroy(_snapshotTex);
        }

        // ── Draw ──────────────────────────────────────────────────────────────
        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;
            float x = inRect.x;
            float y = inRect.y;

            // ── Title ──────────────────────────────────────────────────────
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(x, y, inRect.width, LabelH + 6f), "Export Studio Scene");
            Text.Font = GameFont.Small;
            y += LabelH + 6f + Pad;

            // ── Mode radio buttons ─────────────────────────────────────────
            Widgets.Label(new Rect(x, y, 80f, LabelH), "Export as:");
            float modeX = x + 82f;

            DrawModeButton(ref modeX, y, ExportMode.EntireScene,  "Entire Scene");
            DrawModeButton(ref modeX, y, ExportMode.CurrentLayer, "Current Layer");
            DrawModeButton(ref modeX, y, ExportMode.SceneXml,     "Scene as XML");
            y += ModeRowH + Pad;

            // ── Mode description ───────────────────────────────────────────
            GUI.color = new Color(0.72f, 0.82f, 0.72f);
            Widgets.Label(new Rect(x, y, inRect.width, LabelH), ModeDescription());
            GUI.color = Color.white;
            y += LabelH + Pad;

            // ── Preview + fields side-by-side ──────────────────────────────
            Texture2D previewTex  = GetPreviewTexture();
            float rightX = x + PreviewSize + Pad * 2f;
            float rightW = inRect.width - PreviewSize - Pad * 2f;

            // Thumbnail
            Rect thumbRect = new Rect(x, y, PreviewSize, PreviewSize);
            Widgets.DrawBoxSolid(thumbRect, new Color(0.08f, 0.08f, 0.08f));
            if (previewTex != null)
                GUI.DrawTexture(thumbRect, previewTex, ScaleMode.ScaleToFit);
            else
            {
                GUI.color = new Color(0.5f, 0.5f, 0.5f);
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(thumbRect, _mode == ExportMode.SceneXml
                    ? "XML\nno preview" : "No texture\nfound");
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }

            // Fields
            float fieldY = y;

            Widgets.Label(new Rect(rightX, fieldY, rightW, LabelH),
                _mode == ExportMode.SceneXml ? "File name (no spaces):" : "Def name (no spaces):");
            fieldY += LabelH;
            _defName = Widgets.TextField(new Rect(rightX, fieldY, rightW, FieldH), _defName);
            fieldY += FieldH + Pad;

            Widgets.Label(new Rect(rightX, fieldY, rightW, LabelH),
                _mode == ExportMode.SceneXml ? "Save folder:" : "Output mod folder:");
            fieldY += LabelH;
            _outputFolder = Widgets.TextField(new Rect(rightX, fieldY, rightW, FieldH), _outputFolder);
            fieldY += FieldH + Pad;

            // Hint
            GUI.color = new Color(0.65f, 0.65f, 0.65f);
            Widgets.Label(new Rect(rightX, fieldY, rightW, LabelH * 2f), HintText());
            GUI.color = Color.white;

            y += PreviewSize + Pad;

            // ── Validation bar ────────────────────────────────────────────
            string err = Validate();
            if (err != null)
            {
                GUI.color = new Color(1f, 0.38f, 0.38f);
                Widgets.Label(new Rect(x, y, inRect.width, LabelH), "⚠ " + err);
                GUI.color = Color.white;
            }
            y += LabelH + Pad;

            // ── Buttons ───────────────────────────────────────────────────
            float btnY  = inRect.yMax - BtnH;
            float halfW = (inRect.width - Pad) * 0.5f;

            GUI.enabled = (err == null);
            if (Widgets.ButtonText(new Rect(x, btnY, halfW, BtnH), "Export"))
                TryExport();
            GUI.enabled = true;

            if (Widgets.ButtonText(new Rect(x + halfW + Pad, btnY, halfW, BtnH), "Cancel"))
                Close();
        }

        // ── Mode helpers ──────────────────────────────────────────────────────

        private void DrawModeButton(ref float x, float y, ExportMode mode, string label)
        {
            bool active = (_mode == mode);
            // Highlight active mode
            if (active)
            {
                GUI.color = new Color(0.3f, 0.7f, 0.4f);
                Widgets.DrawBoxSolid(new Rect(x, y, label.Length * 7f + 20f, ModeRowH - 4f),
                    new Color(0.25f, 0.45f, 0.25f));
                GUI.color = Color.white;
            }
            if (Widgets.ButtonText(new Rect(x, y, label.Length * 7f + 20f, ModeRowH - 4f), label))
                _mode = mode;
            x += label.Length * 7f + 24f;
        }

        private string ModeDescription()
        {
            switch (_mode)
            {
                case ExportMode.EntireScene:
                    return "Saves the full preview snapshot as a wall-mounted portrait painting mod.";
                case ExportMode.CurrentLayer:
                    return "Saves the active layer's texture as a wall-mounted portrait painting mod.";
                case ExportMode.SceneXml:
                    return "Exports all layers of every scene object as annotated ThingDef XML blocks.";
                default:
                    return "";
            }
        }

        private string HintText()
        {
            switch (_mode)
            {
                case ExportMode.EntireScene:
                case ExportMode.CurrentLayer:
                    return "Point to a single portrait mod folder (e.g. Mods/MyPortraits).\nAll portraits are added to that one mod — no duplicates, auto-numbered if needed.";
                case ExportMode.SceneXml:
                    return "Drop the saved .xml into your mod's Defs/ folder and adjust as needed.";
                default:
                    return "";
            }
        }

        // ── Texture resolution ────────────────────────────────────────────────

        private Texture2D GetPreviewTexture()
        {
            switch (_mode)
            {
                case ExportMode.EntireScene:
                    return _snapshotTex;

                case ExportMode.CurrentLayer:
                    return ResolveLayerTexture(_state?.ActiveLayer);

                case ExportMode.SceneXml:
                    return null;   // no image for XML mode

                default:
                    return null;
            }
        }

        /// <summary>
        /// Tries to find a usable Texture2D for a layer without touching ContentFinder
        /// on a layer that has a CachedTexture already.
        /// </summary>
        private static Texture2D ResolveLayerTexture(LayerData layer)
        {
            if (layer == null) return null;

            // Already cached (e.g. loaded from disk)
            if (layer.CachedTexture != null) return layer.CachedTexture;

            if (string.IsNullOrEmpty(layer.TexturePath)) return null;

            // Try directional south variant first (most common preview facing)
            if (layer.IsDirectional)
            {
                var south = ContentFinder<Texture2D>.Get(layer.TexturePath + "_south", false);
                if (south != null) return south;

                var east = ContentFinder<Texture2D>.Get(layer.TexturePath + "_east", false);
                if (east != null) return east;
            }

            return ContentFinder<Texture2D>.Get(layer.TexturePath, false);
        }

        // ── Validation ────────────────────────────────────────────────────────

        private string Validate()
        {
            if (string.IsNullOrWhiteSpace(_defName))
                return "Name cannot be empty.";

            foreach (char c in _defName)
                if (char.IsWhiteSpace(c) || c == '<' || c == '>' || c == '&' || c == '"')
                    return "Name contains invalid characters (no spaces or < > & \").";

            if (string.IsNullOrWhiteSpace(_outputFolder))
                return "Output folder cannot be empty.";

            switch (_mode)
            {
                case ExportMode.EntireScene:
                    if (_snapshotTex == null)
                        return "No scene snapshot available — close and click Snapshot again.";
                    break;

                case ExportMode.CurrentLayer:
                    if (_state?.ActiveLayer == null)
                        return "No active layer selected.";
                    if (ResolveLayerTexture(_state.ActiveLayer) == null)
                        return "Active layer has no texture to export.";
                    break;

                case ExportMode.SceneXml:
                    if (_state == null || _state.ActiveScene.Count == 0)
                        return "Scene is empty — nothing to export.";
                    break;
            }

            return null;
        }

        // ── Export ────────────────────────────────────────────────────────────

        private void TryExport()
        {
            try
            {
                switch (_mode)
                {
                    case ExportMode.EntireScene:
                        ExportPortrait(_snapshotTex);
                        break;

                    case ExportMode.CurrentLayer:
                        ExportPortrait(ResolveLayerTexture(_state.ActiveLayer));
                        break;

                    case ExportMode.SceneXml:
                        ExportSceneXml();
                        break;
                }
                Close();
            }
            catch (Exception ex)
            {
                Log.Error("[RimRim] Export failed: " + ex);
                Messages.Message("Export failed — check log for details.",
                    MessageTypeDefOf.RejectInput, false);
            }
        }

        /// <summary>
        /// Adds a portrait to the shared mod folder.
        /// The output folder IS the mod — no per-portrait subfolder is created.
        /// </summary>
        private void ExportPortrait(Texture2D tex)
        {
            if (tex == null) throw new InvalidOperationException("Texture is null.");
            PortraitExporter.Export(tex, _defName.Trim(), _outputFolder.Trim());
        }

        /// <summary>Writes a Defs XML file containing every layer of every scene object.</summary>
        private void ExportSceneXml()
        {
            string folder = _outputFolder.Trim();
            Directory.CreateDirectory(folder);

            string xml  = XmlExporter.GenerateAllLayersXml(_state);
            string file = Path.Combine(folder, _defName.Trim() + ".xml");
            File.WriteAllText(file, xml, Encoding.UTF8);

            Messages.Message($"XML exported to {file}", MessageTypeDefOf.TaskCompletion, false);
        }
    }
}
