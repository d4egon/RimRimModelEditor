using UnityEngine;
using Verse;
using RimWorld;
using D4egon.RimRimModelEditor.Model;
using D4egon.RimRimModelEditor.Logic;

namespace D4egon.RimRimModelEditor.UI.Panels
{
    public static class XmlPreviewPanel
    {
        private static Vector2 scrollPos = Vector2.zero;

        public static void Draw(Rect rect, EditorState state)
        {
            Widgets.DrawMenuSection(rect);
            Rect inner = rect.ContractedBy(10f);

            // 1. HEADER
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inner.x, inner.y, inner.width, 30f), "Live XML Preview");
            Text.Font = GameFont.Small;

            // 2. GENERATE PREVIEW STRING
            // We use the existing Exporter but redirect it to a string buffer instead of a file
            string xmlContent = XmlExporter.GeneratePreviewString(state);

            // 3. SCROLLABLE TEXT AREA
            Rect viewRect = new Rect(0, 0, inner.width - 20f, Text.CalcHeight(xmlContent, inner.width - 20f) + 50f);
            Widgets.BeginScrollView(new Rect(inner.x, inner.y + 35f, inner.width, inner.height - 40f), ref scrollPos, viewRect);
            
            // Draw the XML text with a monospaced-style look (if possible in Verse)
            GUI.color = new Color(0.7f, 0.85f, 1f); // Light blue "Code" tint
            Widgets.Label(viewRect, xmlContent);
            GUI.color = Color.white;

            Widgets.EndScrollView();

            // 4. COPY TO CLIPBOARD BUTTON
            if (Widgets.ButtonText(new Rect(rect.xMax - 110f, rect.y + 5f, 100f, 25f), "Copy XML"))
            {
                GUIUtility.systemCopyBuffer = xmlContent;
                Messages.Message("XML copied to clipboard!", MessageTypeDefOf.PositiveEvent, false);
            }
        }
    }
}