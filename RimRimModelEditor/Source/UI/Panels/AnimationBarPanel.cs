#pragma warning disable CS8600, CS8604
using D4egon.RimRimModelEditor.Logic;
using D4egon.RimRimModelEditor.Model;
using UnityEngine;
using Verse;

namespace D4egon.RimRimModelEditor.UI.Panels
{
    /// <summary>
    /// Context-sensitive animation control bar drawn below the timeline.
    ///
    /// Button groups shown depend on what the active scene contains:
    ///   Walk N/S/E/W — any pawn object (AnimCapability.Walk)
    ///   Fire          — any weapon with projectile verb (AnimCapability.Fire)
    ///   Power ON/OFF  — any building with power comp (AnimCapability.Power)
    ///   Flicker ON/OFF— any Graphic_Flicker object without a power comp (AnimCapability.Flicker)
    ///   Stop All      — always shown when any clip is running
    ///
    /// Walk, Fire, and Power operate independently — all three can run at once
    /// (pawn walks while weapon fires while building flickers).
    /// </summary>
    public class AnimationBarPanel
    {
        private const float BtnH  = 28f;
        private const float BtnW  = 76f;   // default button width
        private const float WalkW = 70f;   // walk buttons are slightly narrower
        private const float Gap   = 4f;
        private const float SepGap = 12f;  // gap between capability groups

        // ── Active-button colour ──────────────────────────────────────────────
        private static readonly Color ActiveBg  = new Color(0.18f, 0.50f, 0.22f);
        private static readonly Color StopColor = new Color(0.85f, 0.35f, 0.35f);

        public void Draw(Rect rect, EditorState state)
        {
            if (state == null || state.ActiveScene.Count == 0) return;

            // Gather combined capabilities for the whole scene.
            AnimCapability caps = AnimCapability.None;
            foreach (var obj in state.ActiveScene)
                caps |= obj.AnimCaps;

            // Nothing to animate — show a subtle "no animations" hint and exit.
            if (caps == AnimCapability.None)
            {
                GUI.color = new Color(0.5f, 0.5f, 0.5f, 0.6f);
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(rect.ContractedBy(6f),
                    "No animations detected.  Load a Pawn, Weapon, or Flicker building to enable playback controls.");
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
                return;
            }

            Widgets.DrawMenuSection(rect);
            var inner = rect.ContractedBy(4f);

            float x = inner.x + 4f;
            float y = inner.y + (inner.height - BtnH) * 0.5f;

            // ── Pawn walk buttons ────────────────────────────────────────────
            if ((caps & AnimCapability.Walk) != 0)
            {
                // Small group label
                DrawGroupLabel(ref x, y, "Walk:");

                DrawWalkBtn(ref x, y, state, StudioAnimType.WalkNorth, "▲ N",
                    "Walk North — pawn bobs upward and switches to the north texture variant.");
                DrawWalkBtn(ref x, y, state, StudioAnimType.WalkSouth, "▼ S",
                    "Walk South — pawn bobs and switches to the south texture variant.");
                DrawWalkBtn(ref x, y, state, StudioAnimType.WalkEast, "► E",
                    "Walk East — pawn bobs and switches to the east texture variant.");
                DrawWalkBtn(ref x, y, state, StudioAnimType.WalkWest, "◄ W",
                    "Walk West — pawn bobs and switches to the west (mirrored east) texture variant.");

                x += SepGap;
            }

            // ── Weapon fire button ───────────────────────────────────────────
            if ((caps & AnimCapability.Fire) != 0)
            {
                DrawGroupLabel(ref x, y, "Weapon:");

                DrawAnimBtn(ref x, y, state, StudioAnimType.Fire, AnimCapability.Fire,
                    "🔥 Fire", BtnW,
                    "Preview weapon firing — muzzle flash layer pulses at burst rate.\n"
                  + "Burst timing matches the weapon's ticksBetweenBurstShots.");

                x += SepGap;
            }

            // ── Building power buttons ───────────────────────────────────────
            if ((caps & AnimCapability.Power) != 0)
            {
                DrawGroupLabel(ref x, y, "Building:");

                DrawPowerBtn(ref x, y, state, StudioAnimType.PowerOn,  AnimCapability.Power,
                    "⚡ ON",  BtnW, "Powered on — Graphic_Flicker cycles at 15 ticks/frame.");
                DrawPowerBtn(ref x, y, state, StudioAnimType.PowerOff, AnimCapability.Power,
                    "⚡ OFF", BtnW, "Powered off — Graphic_Flicker freezes on frame 0.");

                x += SepGap;
            }
            // Flicker-only objects (no power comp: e.g. fire, candle)
            else if ((caps & AnimCapability.Flicker) != 0)
            {
                DrawGroupLabel(ref x, y, "Flicker:");

                DrawPowerBtn(ref x, y, state, StudioAnimType.PowerOn,  AnimCapability.Flicker,
                    "▶ ON",  BtnW, "Play Graphic_Flicker frame cycling (0.25 s per frame, matches in-game rate).");
                DrawPowerBtn(ref x, y, state, StudioAnimType.PowerOff, AnimCapability.Flicker,
                    "■ OFF", BtnW, "Freeze Graphic_Flicker on frame 0.");

                x += SepGap;
            }

            // ── Stop All ─────────────────────────────────────────────────────
            bool anyPlaying = false;
            foreach (var obj in state.ActiveScene)
                if (StudioAnimController.GetClip(obj.DefName) != StudioAnimType.Idle)
                { anyPlaying = true; break; }

            Rect stopRect = new Rect(x, y, BtnW, BtnH);
            TooltipHandler.TipRegion(stopRect, "Stop all animations and return every object to Idle.");
            if (anyPlaying) GUI.color = StopColor;
            if (Widgets.ButtonText(stopRect, "■ Stop All"))
                StudioAnimController.StopAll();
            GUI.color = Color.white;
        }

        // ── Walk-direction toggle: click once to enable, click again to idle ─

        private void DrawWalkBtn(ref float x, float y, EditorState state,
            StudioAnimType clip, string label, string tip)
        {
            bool active = IsAnyObjectOnClip(state, clip, AnimCapability.Walk);
            DrawToggleBtn(ref x, y, WalkW, BtnH, label, tip, active, () =>
            {
                var next = active ? StudioAnimType.Idle : clip;
                SetClipForCap(state, AnimCapability.Walk, next);
            });
        }

        // ── Generic animated clip button (Fire) ──────────────────────────────

        private void DrawAnimBtn(ref float x, float y, EditorState state,
            StudioAnimType clip, AnimCapability cap,
            string label, float width, string tip)
        {
            bool active = IsAnyObjectOnClip(state, clip, cap);
            DrawToggleBtn(ref x, y, width, BtnH, label, tip, active, () =>
            {
                var next = active ? StudioAnimType.Idle : clip;
                SetClipForCap(state, cap, next);
            });
        }

        // ── Power / Flicker exclusive-select button (ON xor OFF) ─────────────

        private void DrawPowerBtn(ref float x, float y, EditorState state,
            StudioAnimType clip, AnimCapability cap,
            string label, float width, string tip)
        {
            bool active = IsAnyObjectOnClip(state, clip, cap);
            DrawToggleBtn(ref x, y, width, BtnH, label, tip, active, () =>
            {
                // Power buttons are exclusive: clicking ON while ON → Idle; clicking OFF sets PowerOff.
                var next = active ? StudioAnimType.Idle : clip;
                SetClipForCap(state, cap, next);
            });
        }

        // ── Primitive: highlighted toggle button ──────────────────────────────

        private void DrawToggleBtn(ref float x, float y,
            float width, float height,
            string label, string tip,
            bool active, System.Action onClick)
        {
            var r = new Rect(x, y, width, height);
            TooltipHandler.TipRegion(r, tip);

            if (active)
                Widgets.DrawBoxSolid(r, ActiveBg);

            if (Widgets.ButtonText(r, label))
                onClick?.Invoke();

            x += width + Gap;
        }

        // ── Group label (small, grey, non-interactive) ─────────────────────────

        private void DrawGroupLabel(ref float x, float y, string text)
        {
            GUI.color = new Color(0.7f, 0.7f, 0.7f);
            float w = Text.CalcSize(text).x + 4f;
            Widgets.Label(new Rect(x, y + (BtnH - 18f) * 0.5f, w, 18f), text);
            GUI.color = Color.white;
            x += w + Gap;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        /// <summary>True if any scene object with <paramref name="cap"/> is on <paramref name="clip"/>.</summary>
        private bool IsAnyObjectOnClip(EditorState state, StudioAnimType clip, AnimCapability cap)
        {
            foreach (var obj in state.ActiveScene)
                if ((obj.AnimCaps & cap) != 0 && StudioAnimController.GetClip(obj.DefName) == clip)
                    return true;
            return false;
        }

        /// <summary>Sets <paramref name="clip"/> on all scene objects that have <paramref name="cap"/>.</summary>
        private void SetClipForCap(EditorState state, AnimCapability cap, StudioAnimType clip)
        {
            foreach (var obj in state.ActiveScene)
                if ((obj.AnimCaps & cap) != 0)
                    StudioAnimController.SetClip(obj.DefName, clip);
        }
    }
}
