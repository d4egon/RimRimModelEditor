using D4egon.RimRimModelEditor.Model;
using UnityEngine;
using Verse;
using System;
using System.Collections.Generic;
using System.Linq;

namespace D4egon.RimRimModelEditor.UI.Panels
{
    public class LogicPanel
    {
        public void Draw(Rect rect, EditorState state, Action<string> pushUndo)
        {
            var obj = state?.SelectedObject;
            if (obj == null)
            {
                Widgets.Label(rect.ContractedBy(8f), "No object selected.");
                return;
            }

            var inner = rect.ContractedBy(4f);
            var listing = new Listing_Standard();
            listing.Begin(inner);

            if (obj.DefType == RimWorldDefType.ThingDef)
                DrawThingStats(listing, obj, pushUndo);
            else if (obj.DefType == RimWorldDefType.FleckDef)
                DrawFleckStats(listing, obj, pushUndo);

            listing.End();
        }

        private void DrawThingStats(Listing_Standard listing, SceneObject obj, Action<string> pushUndo)
        {
            listing.Label($"Weapon Range: {obj.WeaponRange:F2}");
            float r = listing.Slider(obj.WeaponRange, 1f, 50f);
            if (!Mathf.Approximately(r, obj.WeaponRange)) { pushUndo("Range"); obj.WeaponRange = r; }

            listing.Label($"Warmup Time: {obj.WarmupTime:F2}s");
            float w = listing.Slider(obj.WarmupTime, 0f, 5f);
            if (!Mathf.Approximately(w, obj.WarmupTime)) { pushUndo("Warmup"); obj.WarmupTime = w; }

            listing.Label("Comps (Active): " + (obj.SelectedComps.Any() ? string.Join(", ", obj.SelectedComps) : "None"));
            if (listing.ButtonText("Manage Comps..."))
            {
                // Trigger Comp selection dialog — future feature
            }
        }

        private void DrawFleckStats(Listing_Standard listing, SceneObject obj, Action<string> pushUndo)
        {
            listing.Label($"Fade In: {obj.FadeInTime:F2}s");
            float fi = listing.Slider(obj.FadeInTime, 0f, 2f);
            if (!Mathf.Approximately(fi, obj.FadeInTime)) { pushUndo("Fade In"); obj.FadeInTime = fi; }

            listing.Label($"Solid Time: {obj.SolidTime:F2}s");
            float st = listing.Slider(obj.SolidTime, 0f, 5f);
            if (!Mathf.Approximately(st, obj.SolidTime)) { pushUndo("Solid Time"); obj.SolidTime = st; }

            listing.Label($"Fade Out: {obj.FadeOutTime:F2}s");
            float fo = listing.Slider(obj.FadeOutTime, 0f, 2f);
            if (!Mathf.Approximately(fo, obj.FadeOutTime)) { pushUndo("Fade Out"); obj.FadeOutTime = fo; }
        }
    }
}
