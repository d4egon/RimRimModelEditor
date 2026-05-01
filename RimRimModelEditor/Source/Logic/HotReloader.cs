using System;
using Verse;
using RimWorld;

namespace D4egon.RimRimModelEditor.Logic
{
    /// <summary>
    /// Hot-reloads a ThingDef by forcing RimWorld to clear its cached graphic
    /// and re-resolve references. This is the extent of what's possible without
    /// a full game restart — it won't pick up XML field changes, but it will
    /// force textures to reload from disk on next render.
    /// </summary>
    public static class HotReloader
    {
        public static void Reload(string defName)
        {
            if (string.IsNullOrEmpty(defName))
            {
                Messages.Message("Hot-reload: no def selected.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            try
            {
                ReloadDef(defName);
                // Also clear the animation frame cache so new textures are picked up.
                AnimationFrameDiscovery.ClearCacheFor(defName);
                Messages.Message($"Hot-reload complete: {defName}", MessageTypeDefOf.PositiveEvent, false);
            }
            catch (Exception ex)
            {
                Log.Error($"[RimRimModelEditor] Hot-reload failed for '{defName}': {ex}");
                Messages.Message($"Hot-reload failed: {ex.Message}", MessageTypeDefOf.NegativeEvent, false);
            }
        }

        private static void ReloadDef(string defName)
        {
            var def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (def == null)
            {
                Log.Warning($"[RimRim] Hot-reload: '{defName}' not found in ThingDef database.");
                return;
            }

            // Force graphic rebuild on next access.
            def.graphic = null;

            if (def.graphicData != null)
            {
                try { def.graphicData.ExplicitlyInitCachedGraphic(); }
                catch { /* Not all graphicData types support this — silently skip. */ }
            }

            // Re-resolve cross-references (stuffCategories, comps, etc.).
            def.ResolveReferences();
        }
    }
}
