#pragma warning disable CS8600, CS8604
using D4egon.RimRimModelEditor.Model;
using D4egon.RimRimModelEditor.Logic;
using D4egon.RimRimModelEditor.UI.Dialogs;
using UnityEngine;
using Verse;

namespace D4egon.RimRimModelEditor.UI.Panels
{
    public class InspectorPanel
    {
        private Vector2 _rightPanelScroll = Vector2.zero;
        private bool _linkScale = true; // The "Chain" state

        // Buffers for TextFields to prevent the "jumping" numbers bug
        private string _bufX, _bufY, _bufSingle;

        public void Draw(Rect rect, EditorState state, System.Action<string> pushUndo)
        {
            Widgets.DrawMenuSection(rect);
            
            Rect viewRect = new Rect(0, 0, rect.width - 20f, 1200f);
            Widgets.BeginScrollView(rect, ref _rightPanelScroll, viewRect);
            
            var inner = viewRect.ContractedBy(10f);
            var listing = new Listing_Standard();
            listing.Begin(inner);

            // --- 1. OBJECT TRANSFORM ---
            // Writes directly to SceneObject.RootScale/RootPosition/RootRotation —
            // the same fields PreviewPanel reads. "Global Settings" was previously
            // writing to orphaned EditorState fields that the preview never consumed.
            var activeObj = state.SelectedObject;
            if (activeObj != null)
            {
                DrawSectionHeader(listing, $"OBJECT: {activeObj.DefName}", 0.5f);
                DrawVector2(listing, "Draw Size",   ref activeObj.RootScale,    pushUndo, 0.01f, 10f, 1f, true);
                DrawVector2(listing, "Draw Offset", ref activeObj.RootPosition, pushUndo, -10f, 10f, 0f);
                DrawGradientSlider(listing, "Root Rotation", ref activeObj.RootRotation, -180f, 180f, 0f, pushUndo);

                listing.GapLine();

                DrawSectionHeader(listing, $"DEF TYPE: {activeObj.DefType}", 0.3f);

                if (activeObj.DefType == RimWorldDefType.ThingDef)
                {
                    DrawGradientSlider(listing, "Muzzle Flash Scale", ref activeObj.MuzzleFlashScale, 0f, 15f, 1f, pushUndo);
                    DrawGradientSlider(listing, "Equipped Angle",     ref activeObj.EquippedAngleOffset, -360f, 360f, 0f, pushUndo);
                }

                if (activeObj.DefType == RimWorldDefType.FleckDef)
                {
                    DrawGradientSlider(listing, "Growth Rate", ref activeObj.GrowthRate, -5f, 5f, 0f, pushUndo);
                }

                listing.GapLine();
            }
            else
            {
                DrawSectionHeader(listing, "NO OBJECT SELECTED", 0.5f);
                Widgets.Label(listing.GetRect(22f), "Add or select an object from the Scene tab.");
                listing.GapLine();
            }

            // --- 3. LAYER CONTROLS ---
            var layer = state.ActiveLayer;
            if (layer != null)
            {
                DrawSectionHeader(listing, $"LAYER: {(layer.Name ?? "unnamed").ToUpper()}", 0.8f);

                DrawVector2(listing, "Position", ref layer.Position, pushUndo, -500f, 500f, 0f);
                DrawVector2(listing, "Scale", ref layer.Scale, pushUndo, 0.1f, 10f, 1f, true); // Added link support
                
                DrawGradientSlider(listing, "Rotation", ref layer.Rotation, -180f, 180f, 0f, pushUndo);
                DrawGradientSlider(listing, "Opacity", ref layer.Opacity, 0f, 1f, 1f, pushUndo);

                layer.Color.r = DrawGradientSlider(listing, "Red Channel", ref layer.Color.r, 0f, 1f, 1f, null);
                layer.Color.g = DrawGradientSlider(listing, "Green Channel", ref layer.Color.g, 0f, 1f, 1f, null);
                layer.Color.b = DrawGradientSlider(listing, "Blue Channel", ref layer.Color.b, 0f, 1f, 1f, null);

                listing.Gap(10f);

                Rect visRect = listing.GetRect(22f);
                TooltipHandler.TipRegion(visRect, "Toggle this layer's visibility in the preview.\nHidden layers are still exported.");
                bool visVal = layer.Visible;
                Widgets.CheckboxLabeled(visRect, "Visible", ref visVal);
                layer.Visible = visVal;

                Rect loadRect = listing.GetRect(30f);
                TooltipHandler.TipRegion(loadRect,
                    "Load a texture from a file on disk (PNG, JPG).\nThe image is embedded directly — no RimWorld content path required.");
                if (Widgets.ButtonText(loadRect, "Load Custom Texture..."))
                {
                    Find.WindowStack.Add(new Dialog_FilePicker(path => {
                        LayerOperations.LoadExternalTexture(layer, path);
                    }));
                }
            }

            listing.End();
            Widgets.EndScrollView();
        }

        private void DrawSectionHeader(Listing_Standard listing, string text, float huePercent)
        {
            Rect r = listing.GetRect(24f);
            GradientStyle.Label(r, $"─── {text} ───", huePercent);
        }

        private float DrawGradientSlider(Listing_Standard listing, string label, ref float val, float min, float max, float neutral, System.Action<string> pushUndo)
        {
            float dist = Mathf.Abs(val - neutral);
            float maxDist = Mathf.Max(Mathf.Abs(max - neutral), Mathf.Abs(min - neutral));
            float colorPercent = 1f - (dist / maxDist);

            Rect r = listing.GetRect(22f);

            TooltipHandler.TipRegion(r,
                $"{label}\nRange: {min} – {max}   Current: {val:F3}\nDouble-click to reset to {neutral}.");

            // Logic: Double-click label to reset to neutral
            if (Event.current.type == EventType.MouseDown && Event.current.clickCount == 2 && r.Contains(Event.current.mousePosition))
            {
                val = neutral;
                pushUndo?.Invoke(label + " Reset");
            }

            GradientStyle.Label(r, label, colorPercent);
            
            // Logic: TextField for exact values next to the slider
            Rect inputRect = new Rect(r.xMax - 50f, r.y, 50f, 18f);
            string buffer = val.ToString("F2");
            Widgets.TextFieldNumeric(inputRect, ref val, ref buffer, min, max);

            float newVal = listing.Slider(val, min, max);
            if (!Mathf.Approximately(newVal, val))
            {
                pushUndo?.Invoke(label);
                val = newVal;
            }
            return val;
        }

        private void DrawVector2(Listing_Standard listing, string label, ref Vector2 v, System.Action<string> pushUndo, float min, float max, float neutral, bool canLink = false)
        {
            Rect r = listing.GetRect(22f);
            TooltipHandler.TipRegion(r,
                $"{label}   Current: ({v.x:F3}, {v.y:F3})\nDouble-click to reset both axes to {neutral}.");
            Widgets.Label(r, label);

            // Double click header to reset both X and Y
            if (Event.current.type == EventType.MouseDown && Event.current.clickCount == 2 && r.Contains(Event.current.mousePosition))
            {
                v = new Vector2(neutral, neutral);
                pushUndo?.Invoke(label + " Reset");
            }

            // Draw the Link/Chain button if supported (Scale)
            if (canLink)
            {
                Rect chainRect = new Rect(r.x + 80f, r.y, 20f, 20f);
                TooltipHandler.TipRegion(chainRect,
                    _linkScale
                        ? "Axes are linked — changing X also changes Y.\nClick to unlink."
                        : "Axes are independent.\nClick to link X and Y together.");
                GUI.color = _linkScale ? Color.white : new Color(1, 1, 1, 0.3f);
                if (Widgets.ButtonImage(chainRect, Widgets.CheckboxOnTex)) // Using checkbox as a "link" icon
                {
                    _linkScale = !_linkScale;
                }
                GUI.color = Color.white;
            }

            float oldX = v.x;
            float oldY = v.y;

            float nx = DrawGradientSlider(listing, "  X", ref v.x, min, max, neutral, null);
            float ny = DrawGradientSlider(listing, "  Y", ref v.y, min, max, neutral, null);

            // Aspect Ratio Linkage
            if (canLink && _linkScale)
            {
                if (!Mathf.Approximately(nx, oldX)) ny = nx;
                else if (!Mathf.Approximately(ny, oldY)) nx = ny;
            }
            
            v = new Vector2(nx, ny);
        }
    }
}