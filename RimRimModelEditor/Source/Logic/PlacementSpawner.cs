using D4egon.RimRimModelEditor.Model;
using UnityEngine;
using Verse;

namespace D4egon.RimRimModelEditor.Logic
{
    /// <summary>
    /// Draws a placement grid overlay in the preview panel showing where the
    /// thing would sit on a RimWorld map tile grid.
    /// </summary>
    public static class PlacementSpawner
    {
        private static readonly Color _gridColor = new Color(0.4f, 0.8f, 1f, 0.35f);
        private static readonly Color _footprintColor = new Color(0.4f, 0.8f, 1f, 0.18f);

        public static void DrawOverlay(Rect clipRect, EditorState state, float cx, float cy)
        {
            float cellPx = 64f * state.PreviewZoom;

            // 1. Draw a 5×5 background grid for context
            for (int row = -2; row <= 2; row++)
            {
                for (int col = -2; col <= 2; col++)
                {
                    var cell = new Rect(
                        cx + col * cellPx - cellPx * 0.5f,
                        cy + row * cellPx - cellPx * 0.5f,
                        cellPx, cellPx);

                    Widgets.DrawBoxSolid(cell, new Color(0.4f, 0.8f, 1f, 0.05f));
                    Widgets.DrawBox(cell, 1);
                }
            }

            // --- SURGICAL FIX: DYNAMIC FOOTPRINT ---
            // 2. Resolve the actual size of the Def to draw the correct footprint
            var def = DefDatabase<ThingDef>.GetNamedSilentFail(state.SelectedDefName);
            IntVec2 size = def?.size ?? new IntVec2(1, 1);

            float fw = size.x * cellPx;
            float fh = size.z * cellPx; // RimWorld uses Z for vertical map depth
            
            var footprint = new Rect(cx - fw * 0.5f, cy - fh * 0.5f, fw, fh);
            
            // Draw a slightly stronger highlight for the actual tiles the object occupies
            Widgets.DrawBoxSolid(footprint, _footprintColor);
            
            // Draw a thicker border for the footprint
            GUI.color = _gridColor;
            Widgets.DrawBox(footprint, 2);
            GUI.color = Color.white;
        }
    }
}