#pragma warning disable CS8600, CS8604
using D4egon.RimRimModelEditor.Logic;
using D4egon.RimRimModelEditor.Model;
using UnityEngine;
using Verse;

namespace D4egon.RimRimModelEditor.UI.Panels
{
    public class PreviewPanel
    {
        // Used to tick the animation controller exactly once per Unity frame.
        private int _lastTickFrame = -1;

        public void Draw(Rect rect, EditorState state)
        {
            // ── Animation tick (once per frame, unscaled so pause doesn't freeze it) ──
            int frameCount = UnityEngine.Time.frameCount;
            if (frameCount != _lastTickFrame)
            {
                StudioAnimController.Tick(UnityEngine.Time.unscaledDeltaTime);
                _lastTickFrame = frameCount;
            }

            Widgets.DrawMenuSection(rect);
            GUI.BeginClip(rect);

            HandleInput(rect, state);
            DrawCheckerboard(new Rect(0, 0, rect.width, rect.height));
            DrawCustomBackground(new Rect(0, 0, rect.width, rect.height), state);

            // cx/cy: centre of the draw area, offset by pan, relative to the clip origin.
            var drawArea = rect.ContractedBy(8f);
            float cx = drawArea.center.x - rect.x + state.PreviewPan.x;
            float cy = drawArea.center.y - rect.y + state.PreviewPan.y;

            // Clip rect passed to overlays (in screen space, matching what GUI.BeginClip set up).
            var clipRect = new Rect(0, 0, rect.width, rect.height);

            // --- THE STUDIO LOOP ---
            foreach (var sceneObj in state.ActiveScene)
            {
                // ── Per-object animation overrides ────────────────────────────
                // Walk bob: XY offset added to every layer's draw position.
                Vector2 animOffset = StudioAnimController.GetRootPositionOffset(sceneObj);

                // Walk direction: overrides state.CurrentRotation for this object's textures.
                Rot4 renderDir = StudioAnimController.TryGetDirectionOverride(sceneObj, out Rot4 dirOverride)
                    ? dirOverride
                    : state.CurrentRotation;

                for (int i = 0; i < sceneObj.Layers.Count; i++)
                {
                    var layer = sceneObj.Layers[i];
                    if (!layer.Visible) continue;

                    // ── Muzzle flash gating ───────────────────────────────────
                    if (!StudioAnimController.ShouldShowLayer(sceneObj, layer)) continue;

                    Texture2D tex = ResolveDirectionalTexture(layer, renderDir);
                    if (tex == null) tex = layer.CachedTexture;
                    if (tex == null)
                    {
                        // If a path was set but didn't load, show the magenta/black missing-texture
                        // checker so the layer is visible and the user knows the path is broken.
                        // If no path at all, skip the layer silently.
                        if (!string.IsNullOrEmpty(layer.TexturePath))
                            tex = PlaceholderTextureGen.GetMissing();
                        else
                            continue;
                    }

                    // Atlas / frame slicing — horizontal strip assumed.
                    int horizontalFrames = (tex.width > tex.height) ? (tex.width / tex.height) : 1;
                    float frameWidthPixels = (float)tex.width / horizontalFrames;

                    // Scale: Zoom × Root × Layer
                    float w = frameWidthPixels * sceneObj.RootScale.x * layer.Scale.x * state.PreviewZoom;
                    float h = tex.height        * sceneObj.RootScale.y * layer.Scale.y * state.PreviewZoom;

                    // Position: Root + Layer offset + animation bob, centred on cx/cy.
                    var texRect = new Rect(
                        cx + (sceneObj.RootPosition.x + animOffset.x + layer.Position.x) * state.PreviewZoom - w * 0.5f,
                        cy + (sceneObj.RootPosition.y + animOffset.y + layer.Position.y) * state.PreviewZoom - h * 0.5f,
                        w, h);

                    // ── Frame selection ───────────────────────────────────────
                    // Flicker objects: frame driven by StudioAnimController's global clock.
                    // Other objects:   frame driven by TimelinePanel's CurrentFrame scrubber.
                    int frame;
                    if ((sceneObj.AnimCaps & AnimCapability.Flicker) != 0 && horizontalFrames > 1)
                        frame = StudioAnimController.GetFlickerFrame(sceneObj, horizontalFrames);
                    else
                        frame = horizontalFrames > 1 ? state.CurrentFrame % horizontalFrames : 0;

                    Rect uvRect = new Rect(0, 0, 1, 1);
                    if (horizontalFrames > 1)
                    {
                        float uvWidth = 1f / horizontalFrames;
                        uvRect = new Rect(frame * uvWidth, 0, uvWidth, 1);
                    }

                    // West flip — mirror the east texture when no explicit west variant exists.
                    if (renderDir == Rot4.West && !HasExplicitWestTexture(layer))
                    {
                        uvRect.x += uvRect.width;
                        uvRect.width *= -1f;
                    }

                    // ── Opacity: normal × muzzle-flash fade multiplier ────────
                    float opacityMult = StudioAnimController.GetLayerOpacityMultiplier(sceneObj, layer);
                    float finalOpacity = layer.Opacity * opacityMult;

                    var oldColor = GUI.color;
                    GUI.color = new Color(layer.Color.r, layer.Color.g, layer.Color.b, finalOpacity);

                    float totalRotation = sceneObj.RootRotation + layer.Rotation;
                    GUIUtility.RotateAroundPivot(totalRotation, texRect.center);
                    GUI.DrawTextureWithTexCoords(texRect, tex, uvRect);
                    GUIUtility.RotateAroundPivot(-totalRotation, texRect.center);

                    GUI.color = oldColor;
                }
            }

            // --- OVERLAYS (drawn after scene objects so they sit on top) ---
            if (state.ShowCollision)
                CollisionOverlay.Draw(clipRect, state, cx, cy);

            if (state.ShowPlacement)
                PlacementSpawner.DrawOverlay(clipRect, state, cx, cy);

            // Empty state label
            if (state.ActiveScene.Count == 0)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(new Rect(0, 0, rect.width, rect.height),
                    "Studio is empty. Add a Def or Pawn to begin.");
                Text.Anchor = TextAnchor.UpperLeft;
            }

            GUI.EndClip();
        }

        private Texture2D ResolveDirectionalTexture(LayerData layer, Rot4 rot)
        {
            if (!layer.IsDirectional)
                return layer.CachedTexture ?? ContentFinder<Texture2D>.Get(layer.TexturePath, false);

            // Direction suffix. West falls back to east (caller mirrors the UV).
            string suffix = rot == Rot4.North ? "_north" :
                            rot == Rot4.East  ? "_east"  :
                            rot == Rot4.West  ? "_east"  : "_south";

            bool hasVariant = !string.IsNullOrEmpty(layer.BodyTypeVariant);

            // ── Apparel / body-type-variant path ─────────────────────────────
            // Pattern: texPath_BodyType_direction   e.g. Duster_Male_south
            if (hasVariant)
            {
                string variantBase = layer.TexturePath + "_" + layer.BodyTypeVariant;

                // Explicit west variant
                if (rot == Rot4.West)
                {
                    var wv = ContentFinder<Texture2D>.Get(variantBase + "_west", false);
                    if (wv != null) return wv;
                }

                var vt = ContentFinder<Texture2D>.Get(variantBase + suffix, false);
                if (vt != null) return vt;

                // Variant exists but no directional — try bare variant path
                var vtBare = ContentFinder<Texture2D>.Get(variantBase, false);
                if (vtBare != null) return vtBare;
            }

            // ── Standard directional path ─────────────────────────────────────
            if (rot == Rot4.West)
            {
                var westTex = ContentFinder<Texture2D>.Get(layer.TexturePath + "_west", false);
                if (westTex != null) return westTex;
            }

            var dirTex = ContentFinder<Texture2D>.Get(layer.TexturePath + suffix, false);
            return dirTex ?? layer.CachedTexture ?? ContentFinder<Texture2D>.Get(layer.TexturePath, false);
        }

        private bool HasExplicitWestTexture(LayerData layer)
        {
            if (!string.IsNullOrEmpty(layer.BodyTypeVariant))
            {
                if (ContentFinder<Texture2D>.Get(
                        layer.TexturePath + "_" + layer.BodyTypeVariant + "_west", false) != null)
                    return true;
            }
            return ContentFinder<Texture2D>.Get(layer.TexturePath + "_west", false) != null;
        }

        private void HandleInput(Rect rect, EditorState state)
        {
            if (!rect.Contains(Event.current.mousePosition)) return;

            if (Event.current.type == EventType.ScrollWheel)
            {
                float delta = -Event.current.delta.y * 0.05f;
                state.PreviewZoom = Mathf.Clamp(state.PreviewZoom + delta, 0.1f, 10f);
                Event.current.Use();
            }

            if (Event.current.type == EventType.MouseDrag && Event.current.button == 2)
            {
                state.PreviewPan += Event.current.delta;
                Event.current.Use();
            }
        }

        private void DrawCheckerboard(Rect rect)
        {
            const int cellSize = 16;
            Widgets.DrawBoxSolid(rect, new Color(0.15f, 0.15f, 0.15f));
            for (int r = 0; r < rect.height / cellSize; r++)
            for (int c = 0; c < rect.width / cellSize; c++)
                if ((r + c) % 2 == 0)
                    Widgets.DrawBoxSolid(
                        new Rect(c * cellSize, r * cellSize, cellSize, cellSize),
                        new Color(0.18f, 0.18f, 0.18f));
        }

        /// <summary>
        /// Draws the user's custom background image (if set) tiled or stretched
        /// to fill the entire preview area, respecting BackgroundOpacity.
        /// Call this after DrawCheckerboard so it layers on top.
        /// </summary>
        private void DrawCustomBackground(Rect rect, Model.EditorState state)
        {
            if (state.BackgroundTexture == null) return;

            var oldColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, state.BackgroundOpacity);
            // Stretch to fill: a single draw covers the whole rect.
            GUI.DrawTexture(rect, state.BackgroundTexture, ScaleMode.ScaleAndCrop);
            GUI.color = oldColor;
        }
    }
}
