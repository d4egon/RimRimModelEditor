using D4egon.RimRimModelEditor.Logic;
using D4egon.RimRimModelEditor.Model;
using UnityEngine;
using Verse;
using RimWorld;

namespace D4egon.RimRimModelEditor.UI.Dialogs
{
    /// <summary>Packages mod files into a community-share ZIP.</summary>
    public class ShareDialog : Window
    {
        private readonly EditorState _state;
        private string _packageName = "MyRimRimMod";
        private string _outputDir = "";

        public override Vector2 InitialSize => new Vector2(460f, 220f);

        public ShareDialog(EditorState state)
        {
            _state = state;
            _outputDir = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop),
                "RimRimExports");
            doCloseX = true;
            absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.Label("Share Mod — Create ZIP");
            listing.GapLine();

            listing.Label("Package name:");
            _packageName = listing.TextEntry(_packageName);

            listing.Label("Output directory:");
            _outputDir = listing.TextEntry(_outputDir);

            listing.Gap();

            if (Widgets.ButtonText(new Rect(inRect.x, listing.CurHeight, 140f, 30f), "Create ZIP"))
            {
                string outPath = ZipPackager.Package(_state, _packageName, _outputDir);
                if (outPath != null)
                    Messages.Message($"Package created: {outPath}", MessageTypeDefOf.PositiveEvent, false);
                else
                    Messages.Message("Packaging failed — check logs.", MessageTypeDefOf.NegativeEvent, false);
                Close();
            }

            if (Widgets.ButtonText(new Rect(inRect.x + 150f, listing.CurHeight, 80f, 30f), "Cancel"))
                Close();

            listing.End();
        }
    }
}
