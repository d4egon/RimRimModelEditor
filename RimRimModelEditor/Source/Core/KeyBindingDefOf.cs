#pragma warning disable CS8600, CS8604, CS8618
using RimWorld;
using Verse;

namespace D4egon.RimRimModelEditor
{
    /// <summary>
    /// Static accessors for all 17 RimRimModelEditor key bindings.
    /// Auto-populated by DefOf injection at startup.
    /// </summary>
    [DefOf]
    public static class KeyBindingDefOf
    {
        // Undo / Redo
        public static KeyBindingDef RimRimModelEditor_Undo;
        public static KeyBindingDef RimRimModelEditor_Redo;

        // File operations
        public static KeyBindingDef RimRimModelEditor_Save;
        public static KeyBindingDef RimRimModelEditor_Export;
        public static KeyBindingDef RimRimModelEditor_BatchExport;
        public static KeyBindingDef RimRimModelEditor_Share;

        // View
        public static KeyBindingDef RimRimModelEditor_Screenshot;
        public static KeyBindingDef RimRimModelEditor_HotReload;
        public static KeyBindingDef RimRimModelEditor_ZoomIn;
        public static KeyBindingDef RimRimModelEditor_ZoomOut;
        public static KeyBindingDef RimRimModelEditor_ResetView;

        // Overlays
        public static KeyBindingDef RimRimModelEditor_ToggleCollision;
        public static KeyBindingDef RimRimModelEditor_TogglePlacement;

        // Animation
        public static KeyBindingDef RimRimModelEditor_NextFrame;
        public static KeyBindingDef RimRimModelEditor_PrevFrame;

        // Layers
        public static KeyBindingDef RimRimModelEditor_AddLayer;
        public static KeyBindingDef RimRimModelEditor_DeleteLayer;

        static KeyBindingDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(KeyBindingDefOf));
    }
}
