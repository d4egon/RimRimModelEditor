using UnityEngine;
using Verse;

namespace D4egon.RimRimModelEditor
{
    public class RimRimSettings : ModSettings
    {
        public int UndoDepth = 50;
        public bool AutoHotReload = false;
        public bool ShowTutorialOnOpen = true;
        public bool ShowCollisionByDefault = false;
        public bool ShowPlacementByDefault = false;
        public string DefaultExportPath = "";
        public float PreviewZoomDefault = 1f;
        public int screenWidth = 1280;
        public int screenHeight = 720;
        public bool skipCloseConfirmation = false;

        public override void ExposeData()
        {
            base.ExposeData(); // Call base first
            Scribe_Values.Look(ref UndoDepth, "undoDepth", 50);
            Scribe_Values.Look(ref AutoHotReload, "autoHotReload", false);
            Scribe_Values.Look(ref ShowTutorialOnOpen, "showTutorialOnOpen", true);
            Scribe_Values.Look(ref ShowCollisionByDefault, "showCollisionByDefault", false);
            Scribe_Values.Look(ref ShowPlacementByDefault, "showPlacementByDefault", false);
            Scribe_Values.Look(ref DefaultExportPath, "defaultExportPath", "");
            Scribe_Values.Look(ref PreviewZoomDefault, "previewZoomDefault", 1f);
            Scribe_Values.Look(ref screenWidth, "screenWidth", 1280);
            Scribe_Values.Look(ref screenHeight, "screenHeight", 720);
            Scribe_Values.Look(ref skipCloseConfirmation, "skipCloseConfirmation", false);
        }
        
        public void DoWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.Label($"Undo depth: {UndoDepth} (History steps)");
            UndoDepth = (int)listing.Slider(UndoDepth, 10, 200);

            listing.GapLine(); // Visual separator for clarity

            listing.CheckboxLabeled("Auto hot-reload on file change", ref AutoHotReload, "Updates the preview as soon as you save your PNG in an external editor.");
            listing.CheckboxLabeled("Show tutorial on first open", ref ShowTutorialOnOpen);
            listing.CheckboxLabeled("Show collision overlay by default", ref ShowCollisionByDefault);
            listing.CheckboxLabeled("Show placement overlay by default", ref ShowPlacementByDefault);
            
            // Add this inside your DoWindowContents listing block:
            listing.GapLine();
            listing.Label($"Base Resolution Width: {screenWidth}");
            screenWidth = (int)listing.Slider(screenWidth, 800, 3840);

            listing.Label($"Base Resolution Height: {screenHeight}");
            screenHeight = (int)listing.Slider(screenHeight, 600, 2160);

            listing.Label("Default export path (Absolute path):");
            // Added a small fixed height to the text entry to prevent layout jumping
            DefaultExportPath = listing.TextEntry(DefaultExportPath, 1); 
            
            if (listing.ButtonText("Reset to defaults"))
            {
                Reset();
            }

            listing.End();
        }

        private void Reset()
        {
            UndoDepth = 50;
            AutoHotReload = false;
            ShowTutorialOnOpen = true;
            ShowCollisionByDefault = false;
            ShowPlacementByDefault = false;
            DefaultExportPath = "";
            PreviewZoomDefault = 1f;
        }
        
    }
    
}