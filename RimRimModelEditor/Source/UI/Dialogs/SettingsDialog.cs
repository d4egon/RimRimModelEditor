using UnityEngine;
using Verse;

namespace D4egon.RimRimModelEditor.UI.Dialogs
{
    /// <summary>In-editor settings dialog — delegates entirely to RimRimSettings.DoWindowContents.</summary>
    public class SettingsDialog : Window
    {
        public override Vector2 InitialSize => new Vector2(480f, 420f);

        public SettingsDialog()
        {
            doCloseX = true;
            absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            // Title
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 30f), "RimRim Model Editor — Settings");
            Text.Font = GameFont.Small;

            // Reserve space at the bottom for the Close button.
            const float btnH = 34f;
            Rect contentRect = new Rect(inRect.x, inRect.y + 36f, inRect.width, inRect.height - 36f - btnH - 4f);

            // Delegate the full settings UI to the shared DoWindowContents —
            // no second Listing_Standard here to avoid double-layout overlap.
            RimRimMod.Settings.DoWindowContents(contentRect);

            // Close + save button.
            if (Widgets.ButtonText(new Rect(inRect.xMax - 90f, inRect.yMax - btnH, 88f, btnH - 2f), "Close"))
            {
                RimRimMod.Instance.WriteSettings();
                Close();
            }
        }
    }
}
