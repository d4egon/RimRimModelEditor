using D4egon.RimRimModelEditor.Logic;
using D4egon.RimRimModelEditor.Model;
using UnityEngine;
using Verse;
using RimWorld;

namespace D4egon.RimRimModelEditor.UI.Dialogs
{
    /// <summary>Export single def to XML (and optionally copy textures).</summary>
    public class ExportDialog : Window
    {
        private readonly EditorState _state;
        private string _outputPath;
        private bool _includeTextures = true;
        private bool _overwrite = false;

        public override Vector2 InitialSize => new Vector2(480f, 260f);

        public ExportDialog(EditorState state)
        {
            _state = state;
            _outputPath = RimRimMod.Settings.DefaultExportPath;
            doCloseX = true;
            absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.Label("Export XML");
            listing.GapLine();

            listing.Label("Output path:");
            _outputPath = listing.TextEntry(_outputPath);

            listing.CheckboxLabeled("Include textures", ref _includeTextures);
            listing.CheckboxLabeled("Overwrite existing files", ref _overwrite);

            listing.Gap();

            if (Widgets.ButtonText(new Rect(inRect.x, listing.CurHeight, 120f, 30f), "Export"))
            {
                var errors = XmlExporter.Export(_state, _outputPath, _includeTextures, _overwrite);
                if (errors.Count == 0)
                    Messages.Message($"Export complete: {_outputPath}", MessageTypeDefOf.PositiveEvent, false);
                else
                    Messages.Message($"Export finished with {errors.Count} warnings.", MessageTypeDefOf.CautionInput, false);
                Close();
            }

            if (Widgets.ButtonText(new Rect(inRect.x + 130f, listing.CurHeight, 80f, 30f), "Cancel"))
                Close();

            listing.End();
        }
    }
}
