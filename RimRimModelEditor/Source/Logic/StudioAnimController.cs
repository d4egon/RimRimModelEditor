#pragma warning disable CS8600, CS8604
using System.Collections.Generic;
using D4egon.RimRimModelEditor.Model;
using UnityEngine;
using Verse;

namespace D4egon.RimRimModelEditor.Logic
{
    // ── Clip enum ──────────────────────────────────────────────────────────────
    public enum StudioAnimType
    {
        Idle,
        WalkNorth,
        WalkSouth,
        WalkEast,
        WalkWest,
        Fire,
        PowerOn,
        PowerOff,
    }

    /// <summary>
    /// Manages live (non-serialised) per-object animation state for the studio.
    ///
    /// Usage:
    ///   • Call Tick(dt) once per frame (PreviewPanel.Draw does this).
    ///   • Call SetClip(defName, clip) when the user presses an animation button.
    ///   • Query GetRootPositionOffset / TryGetDirectionOverride / etc. in PreviewPanel
    ///     to modify each object's draw parameters before rendering.
    /// </summary>
    public static class StudioAnimController
    {
        // ── Global wall-clock time (unscaled, wraps at 10 000 s) ─────────────
        private static float _globalTime;
        public static float GlobalTime => _globalTime;

        // ── Per-object animation state ────────────────────────────────────────
        private sealed class ObjState
        {
            public StudioAnimType Clip       = StudioAnimType.Idle;
            public float          Phase      = 0f;   // accumulates while clip is active
            public float          BurstTimer = 0f;   // time within current burst shot
            public int            BurstFired = 0;    // shots fired so far in this burst
        }

        private static readonly Dictionary<string, ObjState> _states
            = new Dictionary<string, ObjState>();

        // ── Tuning ────────────────────────────────────────────────────────────
        private const float WalkCycleSpeed   = 4.0f;  // rad/s for sin
        private const float WalkBobAmplitude = 0.06f; // world units, Y (up/down)
        private const float WalkSwayAmplitude= 0.03f; // world units, X (side-to-side)

        // Graphic_Flicker: 15 game ticks per frame change @ 60 ticks/s = 0.25 s
        private const float FlickerFrameSec  = 0.25f;

        private const float FireShotInterval = 0.45f; // seconds per shot cycle
        private const float MuzzleFlashSec   = 0.12f; // seconds the flash is visible

        // ── Tick ── call once per frame ───────────────────────────────────────
        public static void Tick(float dt)
        {
            _globalTime += dt;
            if (_globalTime > 10000f) _globalTime -= 10000f;

            foreach (var s in _states.Values)
            {
                if (s.Clip == StudioAnimType.Idle)
                {
                    s.Phase = 0f;
                    continue;
                }

                s.Phase += dt;
                if (s.Phase > 1000f) s.Phase -= 1000f;  // prevent float precision loss

                if (s.Clip == StudioAnimType.Fire)
                {
                    s.BurstTimer += dt;
                    if (s.BurstTimer >= FireShotInterval)
                    {
                        s.BurstTimer -= FireShotInterval;
                        s.BurstFired++;
                    }
                }
            }
        }

        // ── Clip control ──────────────────────────────────────────────────────

        public static void SetClip(string defName, StudioAnimType clip)
        {
            if (string.IsNullOrEmpty(defName)) return;
            if (!_states.TryGetValue(defName, out var s))
            {
                s = new ObjState();
                _states[defName] = s;
            }
            s.Clip       = clip;
            s.Phase      = 0f;
            s.BurstTimer = 0f;
            s.BurstFired = 0;
        }

        public static StudioAnimType GetClip(string defName)
        {
            if (string.IsNullOrEmpty(defName)) return StudioAnimType.Idle;
            return _states.TryGetValue(defName, out var s) ? s.Clip : StudioAnimType.Idle;
        }

        public static void StopAll()
        {
            foreach (var s in _states.Values)
            {
                s.Clip       = StudioAnimType.Idle;
                s.Phase      = 0f;
                s.BurstTimer = 0f;
                s.BurstFired = 0;
            }
        }

        // ── Pawn walk bob: root position offset ───────────────────────────────

        /// <summary>
        /// Returns a world-unit XY offset to add to the scene object's root position
        /// while a walk clip is playing (sine-wave bob + gentle sway).
        /// Returns Vector2.zero when not walking.
        /// </summary>
        public static Vector2 GetRootPositionOffset(SceneObject obj)
        {
            if (obj == null) return Vector2.zero;
            if (!_states.TryGetValue(obj.DefName, out var s)) return Vector2.zero;

            switch (s.Clip)
            {
                case StudioAnimType.WalkNorth:
                case StudioAnimType.WalkSouth:
                case StudioAnimType.WalkEast:
                case StudioAnimType.WalkWest:
                    float y = Mathf.Sin(s.Phase * WalkCycleSpeed)        * WalkBobAmplitude;
                    float x = Mathf.Sin(s.Phase * WalkCycleSpeed * 0.5f) * WalkSwayAmplitude;
                    return new Vector2(x, y);

                default:
                    return Vector2.zero;
            }
        }

        // ── Walk direction override ───────────────────────────────────────────

        /// <summary>
        /// When a directional walk clip is active, returns the matching Rot4 so the
        /// preview uses the correct N/S/E/W texture variant.
        /// Returns false (leaves <paramref name="dir"/> at South) when not walking.
        /// </summary>
        public static bool TryGetDirectionOverride(SceneObject obj, out Rot4 dir)
        {
            dir = Rot4.South;
            if (obj == null) return false;
            if (!_states.TryGetValue(obj.DefName, out var s)) return false;

            switch (s.Clip)
            {
                case StudioAnimType.WalkNorth: dir = Rot4.North; return true;
                case StudioAnimType.WalkSouth: dir = Rot4.South; return true;
                case StudioAnimType.WalkEast:  dir = Rot4.East;  return true;
                case StudioAnimType.WalkWest:  dir = Rot4.West;  return true;
                default: return false;
            }
        }

        // ── Graphic_Flicker frame ─────────────────────────────────────────────

        /// <summary>
        /// Returns the frame index to use for a Graphic_Flicker object.
        /// Matches in-game timing: 15 ticks @ 60fps = 0.25 s per frame.
        /// Returns 0 when PowerOff clip is active (frozen) or frameCount ≤ 1.
        /// </summary>
        public static int GetFlickerFrame(SceneObject obj, int frameCount)
        {
            if (frameCount <= 1) return 0;
            if (obj == null)    return 0;

            if (_states.TryGetValue(obj.DefName, out var s) && s.Clip == StudioAnimType.PowerOff)
                return 0;

            // Divide global time by frame duration, cycle through frameCount
            return (int)(_globalTime / FlickerFrameSec) % frameCount;
        }

        // ── Muzzle flash layer gating ─────────────────────────────────────────

        /// <summary>
        /// Returns false only for muzzle-flash layers when the fire animation is NOT
        /// in its brief flash window.  All other layers always return true.
        /// </summary>
        public static bool ShouldShowLayer(SceneObject obj, LayerData layer)
        {
            if (layer == null) return true;
            if (!IsMuzzleLayer(layer)) return true;

            if (obj == null) return false;
            if (!_states.TryGetValue(obj.DefName, out var s)) return false;
            if (s.Clip != StudioAnimType.Fire) return false;

            return s.BurstTimer < MuzzleFlashSec;
        }

        /// <summary>
        /// Returns 0→1 fade-in multiplier for muzzle-flash layers.
        /// Non-flash layers always return 1.  Flash layers return 0 when fire is idle.
        /// </summary>
        public static float GetLayerOpacityMultiplier(SceneObject obj, LayerData layer)
        {
            if (layer == null) return 1f;
            if (!IsMuzzleLayer(layer)) return 1f;

            if (obj == null) return 0f;
            if (!_states.TryGetValue(obj.DefName, out var s)) return 0f;
            if (s.Clip != StudioAnimType.Fire) return 0f;

            // Fade out over the flash window: 1 at start → 0 at MuzzleFlashSec
            float t = s.BurstTimer / MuzzleFlashSec;
            return Mathf.Clamp01(1f - t);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static bool IsMuzzleLayer(LayerData layer)
            => layer.Name != null
               && (layer.Name.IndexOf("Muzzle", System.StringComparison.OrdinalIgnoreCase) >= 0
                || layer.Name.IndexOf("Flash",  System.StringComparison.OrdinalIgnoreCase) >= 0);
    }
}
