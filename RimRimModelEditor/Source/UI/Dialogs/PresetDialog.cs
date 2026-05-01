using D4egon.RimRimModelEditor.Logic;
using D4egon.RimRimModelEditor.Model;
using UnityEngine;
using Verse;
using RimWorld;

namespace D4egon.RimRimModelEditor.UI.Dialogs
{
    /// <summary>Save-preset dialog — prompts for a name then serialises to XML.</summary>
    public class PresetDialog : Window
    {
        private readonly EditorState _state;
        private string _name = "";

        public override Vector2 InitialSize => new Vector2(360f, 160f);

        public PresetDialog(EditorState state)
        {
            _state = state;
            doCloseX = true;
            absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.Label("Save Preset");
            listing.GapLine();
            listing.Label("Preset name:");
            _name = listing.TextEntry(_name);
            listing.Gap();

            if (Widgets.ButtonText(new Rect(inRect.x, listing.CurHeight, 100f, 30f), "Save"))
            {
                if (!string.IsNullOrWhiteSpace(_name))
                {
                    PresetIO.Save(_name, _state);
                    Messages.Message($"Preset saved: {_name}", MessageTypeDefOf.PositiveEvent, false);
                    Close();
                }
            }

            if (Widgets.ButtonText(new Rect(inRect.x + 110f, listing.CurHeight, 80f, 30f), "Cancel"))
                Close();

            listing.End();
        }
    }
}
