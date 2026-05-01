using D4egon.RimRimModelEditor.Model;
using UnityEngine;
using Verse;

namespace D4egon.RimRimModelEditor.Logic
{
    public static class CollisionOverlay
    {
        private static readonly Color _fillColor  = new Color(1f, 0.2f, 0.2f, 0.25f);
        private static readonly Color _borderColor = new Color(1f, 0.2f, 0.2f, 0.8f);

        public static void Draw(Rect clipRect, EditorState state, float cx, float cy)
        {
            // 1. Calculate base size
            float w = state.CollisionSize.x * state.PreviewZoom * 64f;
            float h = state.CollisionSize.y * state.PreviewZoom * 64f;

            var box = new Rect(cx - w * 0.5f, cy - h * 0.5f, w, h);

            // 2. Surgical Clip: Only draw if the box is actually inside the visible preview area
            if (!box.Overlaps(clipRect)) return;

            // Optional: Use GUI.BeginGroup to ensure pixels don't bleed out of the preview panel
            GUI.BeginGroup(clipRect);
            
            // Adjust box coordinates to be relative to the group
            var localBox = new Rect(box.x - clipRect.x, box.y - clipRect.y, box.width, box.height);

            Widgets.DrawBoxSolid(localBox, _fillColor);
            Widgets.DrawBox(localBox, 2);

            // 3. Label rendering with a small shadow for readability
            Text.Anchor = TextAnchor.LowerCenter;
            Text.Font = GameFont.Tiny;
            
            var labelRect = new Rect(localBox.x, localBox.yMax + 2f, localBox.width, 18f);
            
            // Draw text shadow manually for better contrast against dark textures
            GUI.color = Color.black;
            Widgets.Label(new Rect(labelRect.x + 1f, labelRect.y + 1f, labelRect.width, labelRect.height),
                          $"{state.CollisionSize.x:F2}×{state.CollisionSize.y:F2}");
            
            GUI.color = _borderColor;
            Widgets.Label(labelRect, $"{state.CollisionSize.x:F2}×{state.CollisionSize.y:F2}");
            
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            GUI.EndGroup();
        }
    }
}