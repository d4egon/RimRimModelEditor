using D4egon.RimRimModelEditor.Logic;
using D4egon.RimRimModelEditor.Model;
using UnityEngine;
using Verse;

namespace D4egon.RimRimModelEditor.UI.Panels
{
    /// <summary>
    /// Animation timeline strip — scrubber, play/pause, frame thumbnails.
    /// </summary>
    public class TimelinePanel
    {
        private double _lastTick;

        public void Draw(Rect rect, EditorState state)
        {
            Widgets.DrawMenuSection(rect);
            var inner = rect.ContractedBy(4f);

            // FIX: Define a consistent height for elements within the timeline
            float elementHeight = 30f; 
            // Center the elements vertically in the panel
            float yOffset = inner.y + (inner.height - elementHeight) / 2f;

            var frames = AnimationFrameDiscovery.GetFrames(state.SelectedDefName);
            int frameCount = frames?.Count ?? 0;

            float btnW = 60f;
            float scrubberX = inner.x + btnW * 3 + 8f;
            float scrubberW = inner.width - btnW * 3 - 8f;

            // SURGICAL FIX: Use elementHeight and yOffset instead of inner.y and inner.height
            if (Widgets.ButtonText(new Rect(inner.x, yOffset, btnW, elementHeight), state.AnimPlaying ? "Pause" : "Play"))
                state.AnimPlaying = !state.AnimPlaying;

            if (Widgets.ButtonText(new Rect(inner.x + btnW + 2f, yOffset, btnW, elementHeight), "Stop"))
            { 
                state.AnimPlaying = false; 
                state.CurrentFrame = 0; 
            }

            // Center the label text vertically as well
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(inner.x + btnW * 2 + 4f, yOffset, btnW, elementHeight),
                $"{state.CurrentFrame + 1}/{(frameCount > 0 ? frameCount : 1)}");
            Text.Anchor = TextAnchor.UpperLeft; // Reset anchor

            // Scrubber
            
            if (frameCount > 1)
            {
                float current = state.CurrentFrame;
                // Widgets.HorizontalSlider(Rect, currentVal, min, max, middleAlignment, label, leftLabel, rightLabel, roundTo)
                state.CurrentFrame = Mathf.RoundToInt(Widgets.HorizontalSlider(
                    new Rect(scrubberX, yOffset, scrubberW, elementHeight), 
                    current, 0f, frameCount - 1, true
                ));
            }

            // Auto-advance frames
            if (state.AnimPlaying && frameCount > 0)
            {
                double now = UnityEngine.Time.realtimeSinceStartupAsDouble;
                double interval = 1.0 / (state.AnimFPS > 0 ? state.AnimFPS : 8);
                if (now - _lastTick >= interval)
                {
                    state.CurrentFrame = (state.CurrentFrame + 1) % frameCount;
                    _lastTick = now;
                }
            }
        }
    }
}
