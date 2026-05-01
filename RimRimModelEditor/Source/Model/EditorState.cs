#pragma warning disable CS8600, CS8604, CS8618
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace D4egon.RimRimModelEditor.Model
{
    // NOTE: RimWorldDefType, AnimationPreset, and SceneObject class live in SceneObject.cs.
    // EditorState.cs only defines EditorState.

    public class EditorState
    {
        // ── The Studio Scene ────────────────────────────────────────────────
        public List<SceneObject> ActiveScene = new List<SceneObject>();
        public int SelectedObjectIndex = -1;

        /// <summary>User-visible name for this studio session. Empty = auto-generate from scene content.</summary>
        public string SceneName = "";

        /// <summary>
        /// Display label for the scene selector.
        /// Uses SceneName when set; otherwise falls back to the first object's DefName; then "Empty Scene".
        /// </summary>
        public string DisplayName =>
            !string.IsNullOrEmpty(SceneName)
                ? SceneName
                : (ActiveScene.Count > 0
                    ? (ActiveScene[0].DefName ?? "Untitled")
                    : "Empty Scene");

        // --- BACKGROUND SETTINGS ---
        public Texture2D BackgroundTexture; 
        public float BackgroundOpacity = 1f;

        // ── Global Viewport ─────────────────────────────────────────────────
        public float PreviewZoom = 1f;
        public Vector2 PreviewPan = Vector2.zero;
        public Rot4 CurrentRotation = Rot4.South;

        // ── Global Transform Overrides ──────────────────────────────────────
        public Vector2 DrawSize = Vector2.one;
        public Vector2 DrawOffset = Vector2.zero;
        public float GlobalRotation = 0f;

        // ── Animation Clock ─────────────────────────────────────────────────
        // Single unified system: CurrentFrame is the source of truth.
        // AnimFPS controls playback speed. AnimPlaying gates auto-advance.
        public int CurrentFrame = 0;
        public float AnimFPS = 8f;
        public bool AnimPlaying = false;
        public Def ActiveAnimation = null;  // Set to an AnimDef when available
        public List<AnimationPreset> AvailablePresets = new List<AnimationPreset>();

        // ── Overlay Controls ────────────────────────────────────────────────
        public bool ShowCollision = false;
        public Vector2 CollisionSize = Vector2.one;
        public bool ShowPlacement = false;

        // ── Weapon helpers ──────────────────────────────────────────────────
        public float EquippedAngle = 0f;

        // ── Selected object shortcut (Property) ─────────────────────────────
        public SceneObject SelectedObject =>
            (ActiveScene.Count > 0 && SelectedObjectIndex >= 0 && SelectedObjectIndex < ActiveScene.Count)
                ? ActiveScene[SelectedObjectIndex] : null;

        // ── Backward-compat convenience wrappers (delegate to SelectedObject) ──
        public string SelectedDefName
        {
            get => SelectedObject?.DefName ?? "";
            set { if (SelectedObject != null) SelectedObject.DefName = value; }
        }

        public RimWorldDefType CurrentDefType
        {
            get => SelectedObject?.DefType ?? RimWorldDefType.ThingDef;
            set { if (SelectedObject != null) SelectedObject.DefType = value; }
        }

        // Returns the layer list of the active scene object
        public List<LayerData> Layers => SelectedObject?.Layers;

        public int ActiveLayerIndex
        {
            get => SelectedObject?.ActiveLayerIndex ?? 0;
            set { if (SelectedObject != null) SelectedObject.ActiveLayerIndex = value; }
        }

        public LayerData ActiveLayer => SelectedObject?.ActiveLayer;

        // ── Scene helpers ────────────────────────────────────────────────────
        public void AddObjectToScene(SceneObject obj)
        {
            ActiveScene.Add(obj);
            SelectedObjectIndex = ActiveScene.Count - 1;
        }

        public void ClearScene()
        {
            ActiveScene.Clear();
            SelectedObjectIndex = -1;
        }

        // ── Deep clone ───────────────────────────────────────────────────────
        public EditorState Clone()
        {
            var s = new EditorState
            {
                SelectedObjectIndex = SelectedObjectIndex,
                SceneName          = SceneName,
                BackgroundTexture  = BackgroundTexture,
                BackgroundOpacity  = BackgroundOpacity,
                PreviewZoom        = PreviewZoom,
                PreviewPan         = PreviewPan,
                CurrentRotation    = CurrentRotation,
                DrawSize           = DrawSize,
                DrawOffset         = DrawOffset,
                GlobalRotation     = GlobalRotation,
                CurrentFrame       = CurrentFrame,
                AnimFPS            = AnimFPS,
                AnimPlaying        = AnimPlaying,
                ActiveAnimation    = ActiveAnimation,
                AvailablePresets   = new List<AnimationPreset>(AvailablePresets),
                ShowCollision      = ShowCollision,
                CollisionSize      = CollisionSize,
                ShowPlacement      = ShowPlacement,
                EquippedAngle      = EquippedAngle
            };

            foreach (var obj in ActiveScene)
            {
                s.ActiveScene.Add(obj.Clone());
            }
                
            return s;
        }
    }
}