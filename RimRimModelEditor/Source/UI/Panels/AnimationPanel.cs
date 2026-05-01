#pragma warning disable CS8600, CS8604
using D4egon.RimRimModelEditor.Logic;
using D4egon.RimRimModelEditor.Model;
using UnityEngine;
using Verse;

namespace D4egon.RimRimModelEditor.UI.Panels
{
    public class AnimationPanel
    {
        public void Draw(Rect rect, EditorState state)
        {
            var inner = rect.ContractedBy(8f);

            // Frame count from the atlas or named frame list — unified with TimelinePanel.
            var frames = AnimationFrameDiscovery.GetFrames(state.SelectedDefName);
            int totalFrames = frames?.Count ?? AnimationFrameDiscovery.GetFrameCount(state);

            var listing = new Listing_Standard();
            listing.Begin(inner);

            // --- HEADER ---
            listing.Label($"Animation: {(state.ActiveAnimation?.defName ?? "Atlas / Manual")}");
            listing.Label($"Frames detected: {(totalFrames > 0 ? totalFrames.ToString() : "none")}");

            // Progress bar — driven by the unified CurrentFrame
            float progress = totalFrames > 0 ? (float)state.CurrentFrame / Mathf.Max(1, totalFrames - 1) : 0f;
            Widgets.FillableBar(listing.GetRect(20f), progress);

            listing.Gap(8f);

            // --- PLAYBACK CONTROLS ---
            Rect btnRow = listing.GetRect(28f);
            float btnW = btnRow.width / 3f - 2f;

            if (Widgets.ButtonText(new Rect(btnRow.x, btnRow.y, btnW, btnRow.height),
                state.AnimPlaying ? "Pause" : "Play"))
            {
                state.AnimPlaying = !state.AnimPlaying;
            }

            if (Widgets.ButtonText(new Rect(btnRow.x + btnW + 2f, btnRow.y, btnW, btnRow.height), "Stop"))
            {
                state.AnimPlaying = false;
                state.CurrentFrame = 0;
            }

            // Step buttons
            if (Widgets.ButtonText(new Rect(btnRow.x + (btnW + 2f) * 2f, btnRow.y, btnW, btnRow.height), "Step →"))
            {
                state.AnimPlaying = false;
                if (totalFrames > 0)
                    state.CurrentFrame = (state.CurrentFrame + 1) % totalFrames;
            }

            listing.Gap(6f);

            // --- FPS SLIDER ---
            listing.Label($"FPS: {state.AnimFPS:F1}");
            state.AnimFPS = listing.Slider(state.AnimFPS, 1f, 60f);

            // --- SCRUBBER ---
            if (totalFrames > 1)
            {
                listing.Label($"Frame: {state.CurrentFrame + 1} / {totalFrames}");
                state.CurrentFrame = Mathf.RoundToInt(
                    listing.Slider(state.CurrentFrame, 0f, totalFrames - 1f));
            }

            listing.End();

            if (totalFrames == 0)
            {
                Widgets.Label(new Rect(inner.x, inner.yMax - 36f, inner.width, 36f),
                    "No atlas frames or AnimationDef found for this def.");
            }
        }
    }
}
