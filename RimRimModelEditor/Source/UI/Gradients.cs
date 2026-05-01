using UnityEngine;
using Verse;

namespace D4egon.RimRimModelEditor.UI
{
    public static class GradientStyle
    {
        // 0.0 = Red, 0.5 = Yellow, 1.0 = Green
        public static Color Get(float percent) 
            => Color.HSVToRGB(Mathf.Clamp01(percent) * 0.33f, 0.75f, 0.9f);

        // Helper for text labels
        public static void Label(Rect rect, string text, float percent)
        {
            GUI.color = Get(percent);
            Widgets.Label(rect, text);
            GUI.color = Color.white;
        }
    }
}