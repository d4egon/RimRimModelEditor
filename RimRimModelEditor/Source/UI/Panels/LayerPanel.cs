using D4egon.RimRimModelEditor.Logic;
using D4egon.RimRimModelEditor.Model;
using UnityEngine;
using Verse;
using System.Collections.Generic;
using System.Linq;

namespace D4egon.RimRimModelEditor.UI.Panels
{
    public class LayerPanel
    {
        private Vector2 _scroll = Vector2.zero;

        public void Draw(Rect rect, EditorState state, System.Action<string> pushUndo)
        {
            if (state == null || state.SelectedObject == null)
            {
                Widgets.Label(rect.ContractedBy(8f), "No object selected. Add a Def or Pawn to the scene first.");
                return;
            }

            var inner = rect.ContractedBy(4f);
            float currentY = inner.y;

            // 1. Top Controls (Add / Delete / Move)
            float topBtnH = 24f;
            Rect topBarRect = new Rect(inner.x, currentY, inner.width, topBtnH);
            DrawTopBar(topBarRect, state, pushUndo);
            currentY += topBtnH + 6f;

            // 2. Pawn layer quick-pickers — shown for PawnKindDef objects.
            // Which rows appear depends on the active layer's role so that
            // "Hair Colour" can't accidentally tint the body and vice versa.
            if (state.ActiveLayer != null &&
                state.SelectedObject?.DefType == RimWorldDefType.PawnKindDef)
            {
                const float pickerRowH   = 24f;
                const float pickerRowGap = 4f;

                string ln = (state.ActiveLayer.Name ?? "").ToLowerInvariant();
                bool isHairLayer       = ln.Contains("hair");
                bool isBeardLayer      = ln.Contains("beard");
                bool isApparelLayer    = ln.Contains("apparel") || ln.Contains("duster")
                                      || ln.Contains("coat")   || ln.Contains("shirt")
                                      || ln.Contains("pants")  || ln.Contains("hat");
                bool isHeadAttachment  = ln.Contains("attachment") || ln.Contains("headattach");

                // Row 1 — Skin Tone | Body Type / Apparel Variant
                // Hidden only when on a pure hair layer (skin tone doesn't apply to hair).
                if (!isHairLayer)
                {
                    DrawBodySelectors(new Rect(inner.x, currentY, inner.width, pickerRowH),
                        state.ActiveLayer, pushUndo, isApparelLayer);
                    currentY += pickerRowH + pickerRowGap;
                }

                // Row 2 — Head Type | Tint colour
                DrawHeadSelectors(new Rect(inner.x, currentY, inner.width, pickerRowH),
                    state.ActiveLayer, pushUndo, isHairLayer || isBeardLayer);
                currentY += pickerRowH + pickerRowGap;

                // Row 3 — Beard | Head Attachment  (quick-add buttons)
                DrawAccessorySelectors(new Rect(inner.x, currentY, inner.width, pickerRowH),
                    state, pushUndo);
                currentY += pickerRowH + pickerRowGap;
            }

            // 3. Layer List Header
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(inner.x, currentY, inner.width, 18f), "LAYERS (TOP TO BOTTOM)");
            currentY += 18f;
            Text.Font = GameFont.Small;

            // 4. Scrollable List
            float rowH = 28f;
            Rect listRect = new Rect(inner.x, currentY, inner.width, inner.height - (currentY - inner.y));
            Rect viewRect = new Rect(0, 0, listRect.width - 16f, state.Layers.Count * rowH);

            Widgets.BeginScrollView(listRect, ref _scroll, viewRect);
            for (int i = state.Layers.Count - 1; i >= 0; i--) // Render top-down
            {
                int index = i;
                var layer = state.Layers[index];
                // Calculate position relative to scroll view (inverted index for visual top-down)
                Rect rowRect = new Rect(0, (state.Layers.Count - 1 - i) * rowH, viewRect.width, rowH);

                if (index == state.ActiveLayerIndex) Widgets.DrawHighlight(rowRect);

                // Visibility checkbox
                Rect checkRect = new Rect(rowRect.x + 2f, rowRect.y + 4f, 20f, 20f);
                TooltipHandler.TipRegion(checkRect,
                    layer.Visible ? "Hide this layer in the preview." : "Show this layer in the preview.");
                bool vis = layer.Visible;
                Widgets.Checkbox(new Vector2(checkRect.x, checkRect.y), ref vis);
                layer.Visible = vis;

                // Name & Texture Path
                string texName = string.IsNullOrEmpty(layer.TexturePath)
                    ? "none"
                    : System.IO.Path.GetFileName(layer.TexturePath);
                string label = $"{layer.Name ?? "unnamed"} ({texName})";
                Rect labelRect = new Rect(rowRect.x + 28f, rowRect.y, rowRect.width - 30f, rowRect.height);
                Widgets.Label(labelRect, label);

                // Row tooltip — show full texture path + priority info on hover
                TooltipHandler.TipRegion(labelRect,
                    $"Layer: {layer.Name}\n"
                  + $"Texture: {(string.IsNullOrEmpty(layer.TexturePath) ? "(none)" : layer.TexturePath)}\n"
                  + $"Priority: {layer.Priority}   Opacity: {layer.Opacity:P0}\n"
                  + $"Position: ({layer.Position.x:F2}, {layer.Position.y:F2})\n"
                  + $"Scale: ({layer.Scale.x:F2}, {layer.Scale.y:F2})\n"
                  + "Click to select.");

                // Gradients
                float colorTarget = layer.Visible ? 1.0f : 0.0f; // Green if on, Red if off
                GradientStyle.Label(labelRect, layer.Name ?? "unnamed", colorTarget);

                // Selection Logic
                if (Widgets.ButtonInvisible(rowRect))
                {
                    state.ActiveLayerIndex = index;
                }
            }
            Widgets.EndScrollView();
        }

        private void DrawTopBar(Rect rect, EditorState state, System.Action<string> pushUndo)
        {
            float btnW = rect.width / 4f - 2f;

            var addRect = new Rect(rect.x, rect.y, btnW, rect.height);
            TooltipHandler.TipRegion(addRect, "Add a blank layer to the selected object.\nShortcut: Ctrl+L");
            if (Widgets.ButtonText(addRect, "+ Add"))
            {
                pushUndo("Add layer");
                state.Layers.Add(new LayerData { Name = $"Layer {state.Layers.Count + 1}" });
                state.ActiveLayerIndex = state.Layers.Count - 1;
            }

            if (state.ActiveLayer != null)
            {
                var delRect = new Rect(rect.x + btnW + 2f, rect.y, btnW, rect.height);
                TooltipHandler.TipRegion(delRect, "Delete the selected layer.\nThis cannot be undone beyond the undo stack.\nShortcut: Ctrl+Delete");
                if (Widgets.ButtonText(delRect, "Delete"))
                {
                    pushUndo("Delete layer");
                    state.Layers.RemoveAt(state.ActiveLayerIndex);
                    state.ActiveLayerIndex = Mathf.Clamp(state.ActiveLayerIndex, 0, state.Layers.Count - 1);
                }

                var upRect = new Rect(rect.x + (btnW * 2) + 4f, rect.y, btnW, rect.height);
                TooltipHandler.TipRegion(upRect, "Move the selected layer up in draw order\n(renders in front of layers below it).");
                if (state.ActiveLayerIndex < state.Layers.Count - 1 && Widgets.ButtonText(upRect, "Up"))
                {
                    pushUndo("Layer Up");
                    SwapLayers(state, state.ActiveLayerIndex, state.ActiveLayerIndex + 1);
                    state.ActiveLayerIndex++;
                }

                var downRect = new Rect(rect.x + (btnW * 3) + 6f, rect.y, btnW, rect.height);
                TooltipHandler.TipRegion(downRect, "Move the selected layer down in draw order\n(renders behind layers above it).");
                if (state.ActiveLayerIndex > 0 && Widgets.ButtonText(downRect, "Down"))
                {
                    pushUndo("Layer Down");
                    SwapLayers(state, state.ActiveLayerIndex, state.ActiveLayerIndex - 1);
                    state.ActiveLayerIndex--;
                }
            }
        }

        // Row 1: Skin Tone | Body Type (naked body) -OR- Apparel Variant (for apparel layers)
        // isApparelLayer: when true the right button sets BodyTypeVariant instead of TexturePath.
        private void DrawBodySelectors(Rect rect, LayerData layer, System.Action<string> pushUndo, bool isApparelLayer = false)
        {
            float half = rect.width / 2f - 2f;
            float rowH = rect.height;

            // Left: Skin Tone — tints this layer's colour.
            TooltipHandler.TipRegion(new Rect(rect.x, rect.y, half, rowH),
                "Apply a skin-tone colour tint to this layer.\nDoes not affect hair or apparel layers.");
            if (Widgets.ButtonText(new Rect(rect.x, rect.y, half, rowH), "Skin Tone"))
            {
                Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>
                {
                    new FloatMenuOption("Light",         () => { pushUndo("Skin: Light");  layer.Color = new Color(1.00f, 0.90f, 0.80f); }),
                    new FloatMenuOption("Medium",        () => { pushUndo("Skin: Medium"); layer.Color = new Color(0.78f, 0.60f, 0.44f); }),
                    new FloatMenuOption("Brown",         () => { pushUndo("Skin: Brown");  layer.Color = new Color(0.60f, 0.40f, 0.28f); }),
                    new FloatMenuOption("Dark",          () => { pushUndo("Skin: Dark");   layer.Color = new Color(0.30f, 0.20f, 0.14f); }),
                    new FloatMenuOption("Reset (White)", () => { pushUndo("Skin: Reset");  layer.Color = Color.white; }),
                }));
            }

            // Right: Body Type (naked body) OR Apparel Variant (duster, coat, etc.)
            // Apparel mode sets BodyTypeVariant (e.g. "Male") so the renderer chains
            //   texPath_Male_south automatically.
            // Body mode sets the full TexturePath to the chosen Naked_X path.
            string rightBtnLabel = isApparelLayer ? "Apparel Variant" : "Body Type";
            TooltipHandler.TipRegion(new Rect(rect.x + half + 4f, rect.y, half, rowH),
                isApparelLayer
                    ? "Choose which body-type variant to use for this apparel layer.\nSets the suffix token (e.g. Male → Duster_Male_south)."
                    : "Switch the naked body texture for this layer.\nPaths are discovered automatically from the current RimWorld install.");
            if (Widgets.ButtonText(new Rect(rect.x + half + 4f, rect.y, half, rowH), rightBtnLabel))
            {
                var opts = new List<FloatMenuOption>();

                if (isApparelLayer)
                {
                    // Apparel body-type variants — these are the suffix tokens.
                    string[] variants = { "Thin", "Fat", "Hulk", "Male", "Female" };
                    foreach (var v in variants)
                    {
                        string vv = v;
                        string current = layer.BodyTypeVariant == vv ? " ✓" : "";
                        opts.Add(new FloatMenuOption(vv + current, () =>
                        {
                            pushUndo($"Variant: {vv}");
                            layer.BodyTypeVariant = vv;
                            layer.IsDirectional   = true;
                            layer.CachedTexture   = null;
                        }));
                    }
                    opts.Add(new FloatMenuOption("None (single texture)", () =>
                    {
                        pushUndo("Variant: None");
                        layer.BodyTypeVariant = "";
                        layer.CachedTexture   = null;
                    }));
                }
                else
                {
                    var paths = PawnTextureScanner.AvailableBodies;
                    if (paths.Count == 0)
                        opts.Add(new FloatMenuOption("(no bodies found)", null));
                    else
                        foreach (var fullPath in paths)
                        {
                            string p = fullPath;
                            string lbl = System.IO.Path.GetFileName(p);
                            opts.Add(new FloatMenuOption(lbl, () =>
                            {
                                pushUndo($"Body: {lbl}");
                                layer.TexturePath   = p;
                                layer.IsDirectional = true;
                                layer.CachedTexture = null;
                            }));
                        }
                }

                Find.WindowStack.Add(new FloatMenu(opts));
            }
        }

        // Called separately for the second row — head and hair pickers.
        // isHairLayer: when true the Hair Colour button label reflects that.
        private void DrawHeadSelectors(Rect rect, LayerData layer, System.Action<string> pushUndo, bool isHairLayer = false)
        {
            float half = rect.width / 2f - 2f;
            float rowH = rect.height;

            // Head Type — discovered paths + a manual entry fallback.
            TooltipHandler.TipRegion(new Rect(rect.x, rect.y, half, rowH),
                "Switch the head texture for this layer.\nPaths are scanned from the live RimWorld install.\n1.6 stores heads in Heads/Male/ and Heads/Female/.\nChoose 'Custom path…' to enter any path manually.");
            if (Widgets.ButtonText(new Rect(rect.x, rect.y, half, rowH), "Head Type"))
            {
                var opts = new List<FloatMenuOption>();
                var paths = PawnTextureScanner.AvailableHeads;

                foreach (var fullPath in paths)
                {
                    string p = fullPath;
                    string lbl = System.IO.Path.GetFileName(p);
                    opts.Add(new FloatMenuOption(lbl, () =>
                    {
                        pushUndo($"Head: {lbl}");
                        layer.TexturePath   = p;
                        layer.IsDirectional = true;
                        layer.CachedTexture = null;
                    }));
                }

                // Always show manual entry — essential when auto-scan found nothing.
                opts.Add(new FloatMenuOption(paths.Count == 0
                        ? "⚠ No heads found — enter path manually…"
                        : "Custom path…",
                    () => Find.WindowStack.Add(new Dialog_InputPath(path =>
                    {
                        if (string.IsNullOrWhiteSpace(path)) return;
                        pushUndo("Head: Custom");
                        layer.TexturePath   = path.Trim();
                        layer.IsDirectional = true;
                        layer.CachedTexture = null;
                        PawnTextureScanner.RegisterCustomHeadPath(layer.TexturePath);
                    }, "Things/Pawn/Humanlike/Heads/Male/Male_Average_Normal"))));

                Find.WindowStack.Add(new FloatMenu(opts));
            }

            // Hair/tint colour — button label reflects the active layer so the user
            // knows exactly what they're colouring.
            string hairBtnLabel = isHairLayer ? "Hair Colour" : $"Tint: {layer.Name}";
            TooltipHandler.TipRegion(new Rect(rect.x + half + 4f, rect.y, half, rowH),
                isHairLayer
                    ? "Set the hair colour tint for this layer.\nOnly affects the current layer — skin tone is separate."
                    : $"Apply a colour tint to the '{layer.Name}' layer.\nWhite = no tint. Use the Inspector RGB sliders for precise control.");
            if (Widgets.ButtonText(new Rect(rect.x + half + 4f, rect.y, half, rowH), hairBtnLabel))
            {
                Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>
                {
                    new FloatMenuOption("Black",   () => { pushUndo("Tint: Black");  layer.Color = new Color(0.10f, 0.08f, 0.06f); }),
                    new FloatMenuOption("Brown",   () => { pushUndo("Tint: Brown");  layer.Color = new Color(0.45f, 0.28f, 0.12f); }),
                    new FloatMenuOption("Blonde",  () => { pushUndo("Tint: Blonde"); layer.Color = new Color(0.95f, 0.85f, 0.50f); }),
                    new FloatMenuOption("Red",     () => { pushUndo("Tint: Red");    layer.Color = new Color(0.70f, 0.22f, 0.10f); }),
                    new FloatMenuOption("Grey",    () => { pushUndo("Tint: Grey");   layer.Color = new Color(0.70f, 0.70f, 0.70f); }),
                    new FloatMenuOption("White",   () => { pushUndo("Tint: White");  layer.Color = Color.white; }),
                    new FloatMenuOption("Reset",   () => { pushUndo("Tint: Reset");  layer.Color = Color.white; }),
                }));
            }
        }

        // Row 3 — Beard | Head Attachment
        // These are quick-ADD buttons: they append a new layer to the scene object
        // rather than modifying the current layer, so the user doesn't have to
        // manually create a layer and type a path themselves.
        private void DrawAccessorySelectors(Rect rect, EditorState state, System.Action<string> pushUndo)
        {
            float half = rect.width / 2f - 2f;
            float rowH = rect.height;

            // ── Beard ──────────────────────────────────────────────────────────
            TooltipHandler.TipRegion(new Rect(rect.x, rect.y, half, rowH),
                "Add a new Beard layer to this pawn.\nAutomatically positioned at the head and\nassigned draw priority 3 (below hair).");
            if (Widgets.ButtonText(new Rect(rect.x, rect.y, half, rowH), "+ Beard"))
            {
                var opts = new List<FloatMenuOption>();
                var paths = PawnTextureScanner.AvailableBeards;

                foreach (var fullPath in paths)
                {
                    string p = fullPath;
                    string lbl = System.IO.Path.GetFileName(p);
                    opts.Add(new FloatMenuOption(lbl, () =>
                    {
                        pushUndo("Add Beard layer");
                        var layer = new Model.LayerData
                        {
                            Name          = "Beard",
                            TexturePath   = p,
                            IsDirectional = true,
                            Position      = new Vector2(0f, Logic.PawnCompositor.HEAD_Y_OFFSET),
                            Scale         = Vector2.one,
                            Opacity       = 1f,
                            Color         = Color.white,
                            Priority      = 3,   // LAYER_BEARD
                            Visible       = true
                        };
                        state.SelectedObject.Layers.Add(layer);
                        state.ActiveLayerIndex = state.SelectedObject.Layers.Count - 1;
                    }));
                }

                if (paths.Count == 0)
                    opts.Add(new FloatMenuOption("No beards found — enter path manually…", () =>
                        Find.WindowStack.Add(new Dialog_InputPath(path =>
                        {
                            if (string.IsNullOrWhiteSpace(path)) return;
                            pushUndo("Add Beard layer");
                            var layer = new Model.LayerData
                            {
                                Name          = "Beard",
                                TexturePath   = path.Trim(),
                                IsDirectional = true,
                                Position      = new Vector2(0f, Logic.PawnCompositor.HEAD_Y_OFFSET),
                                Scale         = Vector2.one,
                                Opacity       = 1f,
                                Priority      = 3,
                                Visible       = true
                            };
                            state.SelectedObject.Layers.Add(layer);
                            state.ActiveLayerIndex = state.SelectedObject.Layers.Count - 1;
                        }, "Things/Pawn/Humanlike/Beards/"))));

                Find.WindowStack.Add(new FloatMenu(opts));
            }

            // ── Head Attachment ────────────────────────────────────────────────
            TooltipHandler.TipRegion(new Rect(rect.x + half + 4f, rect.y, half, rowH),
                "Add a Head Attachment layer (eye patch, horns, scars, etc.).\nPositioned at the head with draw priority 5\n(above hair, below hats).");
            if (Widgets.ButtonText(new Rect(rect.x + half + 4f, rect.y, half, rowH), "+ HeadAttach"))
            {
                var opts = new List<FloatMenuOption>();
                var paths = PawnTextureScanner.AvailableHeadAttachments;

                foreach (var fullPath in paths)
                {
                    string p = fullPath;
                    string lbl = System.IO.Path.GetFileName(p);
                    opts.Add(new FloatMenuOption(lbl, () =>
                    {
                        pushUndo("Add HeadAttachment layer");
                        var layer = new Model.LayerData
                        {
                            Name          = "HeadAttachment",
                            TexturePath   = p,
                            IsDirectional = true,
                            Position      = new Vector2(0f, Logic.PawnCompositor.HEAD_Y_OFFSET),
                            Scale         = Vector2.one,
                            Opacity       = 1f,
                            Priority      = 5,   // LAYER_HEAD_ATTACHMENT
                            Visible       = true
                        };
                        state.SelectedObject.Layers.Add(layer);
                        state.ActiveLayerIndex = state.SelectedObject.Layers.Count - 1;
                    }));
                }

                opts.Add(new FloatMenuOption("Custom path…", () =>
                    Find.WindowStack.Add(new Dialog_InputPath(path =>
                    {
                        if (string.IsNullOrWhiteSpace(path)) return;
                        pushUndo("Add HeadAttachment layer");
                        var layer = new Model.LayerData
                        {
                            Name          = "HeadAttachment",
                            TexturePath   = path.Trim(),
                            IsDirectional = true,
                            Position      = new Vector2(0f, Logic.PawnCompositor.HEAD_Y_OFFSET),
                            Scale         = Vector2.one,
                            Opacity       = 1f,
                            Priority      = 5,
                            Visible       = true
                        };
                        state.SelectedObject.Layers.Add(layer);
                        state.ActiveLayerIndex = state.SelectedObject.Layers.Count - 1;
                    }, "Things/Pawn/Humanlike/HeadAttachments/"))));

                Find.WindowStack.Add(new FloatMenu(opts));
            }
        }

        private void SwapLayers(EditorState state, int idxA, int idxB)
        {
            var temp = state.Layers[idxA];
            state.Layers[idxA] = state.Layers[idxB];
            state.Layers[idxB] = temp;
        }
    }
}